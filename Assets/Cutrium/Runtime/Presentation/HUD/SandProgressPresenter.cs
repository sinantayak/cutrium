using System;
using Cutrium.Gameplay.Session;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.HUD
{
    /// <summary>
    /// Presents captured area as completion toward the current level target.
    /// Logical progress remains immediate in the gameplay session; this class
    /// owns only a delayed, sand-arrival-gated display value.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SandProgressPresenter : MonoBehaviour
    {
        // Fixed, not proportional to the bar's own (now flexible) width or
        // height: Background/Fill render as Image.Type.Sliced (see
        // EnsureUiSpriteImportSettings' sliced9Slice option), whose border
        // -- the visible plaque frame thickness -- stays a fixed pixel
        // size regardless of how far the flat middle stretches, so the
        // fill's inset from that frame should too.
        private const float FillInsetHorizontal = 12f;
        private const float FillInsetVertical = 10f;
        // Matches SkillRow's fixed icon cell size
        // (LandmarkRevealPresentationSetup.SkillCellSize) so the bar and
        // the skill icons -- BottomHudRow's other 50% half -- line up at
        // the same height. The percentage text overlays the bar itself
        // (last sibling, rendered on top) instead of reserving a second
        // row underneath it.
        private const float TargetVisualHeight = 100f;
        private const float ParentSidePadding = 18f;
        private const float ParentVerticalPadding = 8f;
        private const float ComparisonEpsilon = 0.00001f;

        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private RectTransform _progressBarRect;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private RectTransform _fillMaskRect;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Text _progressText;
        [SerializeField] private RectTransform _fillStartTarget;
        [SerializeField] [Min(0f)]
        private float _animationSeconds = 0.48f;
        [SerializeField] [Min(0f)]
        private float _arrivalFallbackSeconds = 0.85f;

        private ThreatMotionSession _observedSession;
        private bool _initialized;
        private bool _waitingForArrival;
        private float _waitingElapsed;
        private float _latestLogicalCapturedFraction;
        private float _displayedCapturedFraction;
        private float _animationStart;
        private float _animationTarget;
        private float _animationElapsed;

        public FirstPlayableController Controller => _controller;
        public RectTransform ProgressBarRect => _progressBarRect;
        public Image BackgroundImage => _backgroundImage;
        public RectTransform FillMaskRect => _fillMaskRect;
        public Image FillImage => _fillImage;
        public Text ProgressText => _progressText;
        public RectTransform FillStartTarget => _fillStartTarget;
        public float DisplayedCapturedFraction =>
            _displayedCapturedFraction;
        public float LatestLogicalCapturedFraction =>
            _latestLogicalCapturedFraction;
        public float AnimationTargetCapturedFraction => _animationTarget;
        public float AnimationSeconds => _animationSeconds;
        public float ArrivalFallbackSeconds => _arrivalFallbackSeconds;
        public bool WaitingForSandArrival => _waitingForArrival;
        public bool IsSettledAtLatestLogicalValue
        {
            get
            {
                if (_controller == null || _controller.Session == null)
                {
                    return true;
                }

                float logical = _controller.Session.CapturedFraction;
                return !_waitingForArrival
                    && Mathf.Abs(
                        _latestLogicalCapturedFraction - logical)
                        <= ComparisonEpsilon
                    && Mathf.Abs(_animationTarget - logical)
                        <= ComparisonEpsilon
                    && Mathf.Abs(_displayedCapturedFraction - logical)
                        <= ComparisonEpsilon;
            }
        }

        public float CurrentFillRatio
        {
            get
            {
                float target = CurrentTargetFraction;
                if (target <= ComparisonEpsilon)
                {
                    return _displayedCapturedFraction > ComparisonEpsilon
                        ? 1f
                        : 0f;
                }

                return Mathf.Clamp01(
                    _displayedCapturedFraction / target);
            }
        }

        private float CurrentTargetFraction =>
            _controller != null && _controller.Session != null
                ? _controller.Session.TargetCapturedFraction
                : 0f;

        public void Configure(
            FirstPlayableController controller,
            RectTransform progressBarRect,
            Image backgroundImage,
            RectTransform fillMaskRect,
            Image fillImage,
            Text progressText,
            RectTransform fillStartTarget)
        {
            _controller = controller;
            _progressBarRect = progressBarRect;
            _backgroundImage = backgroundImage;
            _fillMaskRect = fillMaskRect;
            _fillImage = fillImage;
            _progressText = progressText;
            _fillStartTarget = fillStartTarget;
            ResetForCurrentSession();
            RefreshLayoutNow();
        }

        public void ConfigureAnimationForSetup(
            float animationSeconds,
            float arrivalFallbackSeconds)
        {
            ValidateNonNegative(animationSeconds, nameof(animationSeconds));
            ValidateNonNegative(
                arrivalFallbackSeconds,
                nameof(arrivalFallbackSeconds));

            _animationSeconds = animationSeconds;
            _arrivalFallbackSeconds = arrivalFallbackSeconds;
            RefreshLayoutNow();
        }

        /// <summary>
        /// Releases a pending logical value when the leading grains from
        /// that capture reach the bar. Values are clamped to the current
        /// authoritative session and can never move the display backward.
        /// </summary>
        public void NotifySandArrived(float capturedFractionAtRelease)
        {
            if (!IsFinite(capturedFractionAtRelease)
                || _controller == null
                || _controller.Session == null)
            {
                return;
            }

            EnsureCurrentSession();
            float authoritative = _controller.Session.CapturedFraction;
            ObserveLogicalIncrease(authoritative);
            float releasedTarget = Mathf.Clamp(
                capturedFractionAtRelease,
                _displayedCapturedFraction,
                authoritative);
            RetargetAnimation(releasedTarget);

            if (_latestLogicalCapturedFraction
                <= releasedTarget + ComparisonEpsilon)
            {
                _waitingForArrival = false;
                _waitingElapsed = 0f;
            }
        }

        public void AdvancePresentation(float elapsedTime)
        {
            ValidateNonNegative(elapsedTime, nameof(elapsedTime));
            if (_controller == null || _controller.Session == null)
            {
                return;
            }

            EnsureCurrentSession();
            float logical = _controller.Session.CapturedFraction;
            if (logical + ComparisonEpsilon
                < _latestLogicalCapturedFraction)
            {
                ResetForCurrentSession();
                return;
            }

            ObserveLogicalIncrease(logical);
            if (_waitingForArrival)
            {
                _waitingElapsed += elapsedTime;
                if (_waitingElapsed + ComparisonEpsilon
                    >= _arrivalFallbackSeconds)
                {
                    RetargetAnimation(_latestLogicalCapturedFraction);
                    _waitingForArrival = false;
                    _waitingElapsed = 0f;
                }
            }

            AdvanceAnimation(elapsedTime);
            ApplyVisuals();
            RefreshLayoutNow();
        }

        /// <summary>
        /// Used by setup and explicit reset flows to settle to the current
        /// logical value without carrying timing state across sessions.
        /// </summary>
        public void RefreshNow()
        {
            ResetForCurrentSession();
            RefreshLayoutNow();
        }

        public void RefreshLayoutNow()
        {
            if (_progressBarRect == null
                || !(_progressBarRect.parent is RectTransform parent))
            {
                return;
            }

            // Flush left (mirrors SkillRow's flush-right skills on the
            // other BottomHudRow half) instead of centered with unused
            // margin on both sides. Background/Fill render as
            // Image.Type.Sliced (see EnsureUiSpriteImportSettings'
            // sliced9Slice option), so width no longer needs to stay
            // aspect-locked to fill the slot -- only a single left inset
            // is needed, not a symmetric pair.
            float barWidth = Mathf.Max(
                0f,
                parent.rect.width - ParentSidePadding);
            float availableVisualFootprintHeight = Mathf.Max(
                0f,
                parent.rect.height - (ParentVerticalPadding * 2f));
            float visualHeight = Mathf.Min(
                TargetVisualHeight,
                availableVisualFootprintHeight);
            if (barWidth <= 0f || visualHeight <= 0f)
            {
                return;
            }

            _progressBarRect.anchorMin = new Vector2(0f, 0.5f);
            _progressBarRect.anchorMax = new Vector2(0f, 0.5f);
            _progressBarRect.pivot = new Vector2(0f, 0.5f);
            _progressBarRect.anchoredPosition = new Vector2(
                ParentSidePadding,
                0f);
            _progressBarRect.sizeDelta = new Vector2(barWidth, visualHeight);

            StretchFull(_backgroundImage?.rectTransform);

            if (_fillMaskRect != null)
            {
                float horizontalInset = Mathf.Min(
                    FillInsetHorizontal,
                    barWidth * 0.5f);
                float verticalInset = Mathf.Min(
                    FillInsetVertical,
                    visualHeight * 0.5f);
                float innerWidth = Mathf.Max(
                    0f,
                    barWidth - (horizontalInset * 2f));
                float innerHeight = Mathf.Max(
                    0f,
                    visualHeight - (verticalInset * 2f));
                _fillMaskRect.anchorMin = new Vector2(0f, 1f);
                _fillMaskRect.anchorMax = new Vector2(0f, 1f);
                _fillMaskRect.pivot = new Vector2(0f, 1f);
                _fillMaskRect.anchoredPosition = new Vector2(
                    horizontalInset,
                    -verticalInset);
                _fillMaskRect.sizeDelta = new Vector2(
                    innerWidth * CurrentFillRatio,
                    innerHeight);

                if (_fillImage != null)
                {
                    RectTransform fillRect = _fillImage.rectTransform;
                    fillRect.anchorMin = new Vector2(0f, 0.5f);
                    fillRect.anchorMax = new Vector2(0f, 0.5f);
                    fillRect.pivot = new Vector2(0f, 0.5f);
                    fillRect.anchoredPosition = Vector2.zero;
                    fillRect.sizeDelta = new Vector2(innerWidth, innerHeight);
                }

                if (_fillStartTarget != null)
                {
                    _fillStartTarget.anchorMin = new Vector2(0f, 0.5f);
                    _fillStartTarget.anchorMax = new Vector2(0f, 0.5f);
                    _fillStartTarget.pivot = new Vector2(0.5f, 0.5f);
                    _fillStartTarget.anchoredPosition = Vector2.zero;
                    _fillStartTarget.sizeDelta = Vector2.zero;
                }
            }

            // Overlays the bar itself (the text component is already the
            // last sibling, see LandmarkRevealPresentationSetup) instead of
            // reserving a separate row underneath it, so the bar can use
            // BottomHudRow's full shared height.
            StretchFull(_progressText?.rectTransform);
        }

        private void Awake()
        {
            ResetForCurrentSession();
        }

        private void LateUpdate()
        {
            AdvancePresentation(Time.unscaledDeltaTime);
        }

        private void EnsureCurrentSession()
        {
            ThreatMotionSession session = _controller.Session;
            if (!_initialized || !ReferenceEquals(session, _observedSession))
            {
                ResetForCurrentSession();
            }
        }

        private void ResetForCurrentSession()
        {
            if (_controller == null || _controller.Session == null)
            {
                _initialized = false;
                _observedSession = null;
                _waitingForArrival = false;
                _waitingElapsed = 0f;
                _latestLogicalCapturedFraction = 0f;
                _displayedCapturedFraction = 0f;
                _animationStart = 0f;
                _animationTarget = 0f;
                _animationElapsed = 0f;
                ApplyVisuals();
                return;
            }

            _initialized = true;
            _observedSession = _controller.Session;
            float logical = _observedSession.CapturedFraction;
            _latestLogicalCapturedFraction = logical;
            _displayedCapturedFraction = logical;
            _animationStart = logical;
            _animationTarget = logical;
            _animationElapsed = 0f;
            _waitingForArrival = false;
            _waitingElapsed = 0f;
            ApplyVisuals();
        }

        private void ObserveLogicalIncrease(float logical)
        {
            if (logical <= _latestLogicalCapturedFraction + ComparisonEpsilon)
            {
                return;
            }

            bool wasAlreadyWaiting = _waitingForArrival;
            _latestLogicalCapturedFraction = logical;
            _waitingForArrival = true;
            if (!wasAlreadyWaiting)
            {
                _waitingElapsed = 0f;
            }
        }

        private void RetargetAnimation(float target)
        {
            float monotonicTarget = Mathf.Max(
                _displayedCapturedFraction,
                target);
            if (monotonicTarget <= _animationTarget + ComparisonEpsilon)
            {
                return;
            }

            _animationStart = _displayedCapturedFraction;
            _animationTarget = monotonicTarget;
            _animationElapsed = 0f;
        }

        private void AdvanceAnimation(float elapsedTime)
        {
            if (_displayedCapturedFraction
                >= _animationTarget - ComparisonEpsilon)
            {
                _displayedCapturedFraction = _animationTarget;
                return;
            }

            _animationElapsed += elapsedTime;
            float progress = _animationSeconds <= 0f
                ? 1f
                : Mathf.Clamp01(_animationElapsed / _animationSeconds);
            float eased = progress * progress * (3f - (2f * progress));
            _displayedCapturedFraction = Mathf.Lerp(
                _animationStart,
                _animationTarget,
                eased);
            if (progress >= 1f)
            {
                _displayedCapturedFraction = _animationTarget;
            }
        }

        private void ApplyVisuals()
        {
            if (_progressText != null)
            {
                _progressText.text =
                    $"{RoundedPercent(_displayedCapturedFraction)}% / " +
                    $"{RoundedPercent(CurrentTargetFraction)}%";
            }
        }

        private static void StretchFull(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static int RoundedPercent(float fraction) =>
            Mathf.FloorToInt(fraction * 100f + 0.5f);

        private static void ValidateNonNegative(float value, string name)
        {
            if (!IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
