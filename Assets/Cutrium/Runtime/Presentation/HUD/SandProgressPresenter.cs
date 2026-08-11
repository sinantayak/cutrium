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
        private const float AuthoredWidth = 838f;
        private const float AuthoredHeight = 55f;
        private const float AuthoredHorizontalInset = 11f;
        private const float AuthoredVerticalInset = 10f;
        private const float TextHeight = 26f;
        private const float TextGap = 4f;
        private const float MinimumBarWidth = 280f;
        private const float ParentSidePadding = 18f;
        private const float ComparisonEpsilon = 0.00001f;

        private readonly Vector3[] _boardWorldCorners = new Vector3[4];

        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private RectTransform _boardFrame;
        [SerializeField] private RectTransform _progressBarRect;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private RectTransform _fillMaskRect;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _frameImage;
        [SerializeField] private Text _progressText;
        [SerializeField] private RectTransform _fillStartTarget;
        [SerializeField] [Range(0.5f, 1f)]
        private float _boardWidthFraction = 0.86f;
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
        public RectTransform BoardFrame => _boardFrame;
        public RectTransform ProgressBarRect => _progressBarRect;
        public Image BackgroundImage => _backgroundImage;
        public RectTransform FillMaskRect => _fillMaskRect;
        public Image FillImage => _fillImage;
        public Image FrameImage => _frameImage;
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
            RectTransform boardFrame,
            RectTransform progressBarRect,
            Image backgroundImage,
            RectTransform fillMaskRect,
            Image fillImage,
            Image frameImage,
            Text progressText,
            RectTransform fillStartTarget)
        {
            _controller = controller;
            _boardFrame = boardFrame;
            _progressBarRect = progressBarRect;
            _backgroundImage = backgroundImage;
            _fillMaskRect = fillMaskRect;
            _fillImage = fillImage;
            _frameImage = frameImage;
            _progressText = progressText;
            _fillStartTarget = fillStartTarget;
            ResetForCurrentSession();
            RefreshLayoutNow();
        }

        public void ConfigureAnimationForSetup(
            float animationSeconds,
            float arrivalFallbackSeconds,
            float boardWidthFraction)
        {
            ValidateNonNegative(animationSeconds, nameof(animationSeconds));
            ValidateNonNegative(
                arrivalFallbackSeconds,
                nameof(arrivalFallbackSeconds));
            if (!IsFinite(boardWidthFraction)
                || boardWidthFraction < 0.5f
                || boardWidthFraction > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(boardWidthFraction));
            }

            _animationSeconds = animationSeconds;
            _arrivalFallbackSeconds = arrivalFallbackSeconds;
            _boardWidthFraction = boardWidthFraction;
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
            if (_boardFrame == null
                || _progressBarRect == null
                || !(_progressBarRect.parent is RectTransform parent))
            {
                return;
            }

            _boardFrame.GetWorldCorners(_boardWorldCorners);
            Vector3 leftLocal = parent.InverseTransformPoint(
                _boardWorldCorners[0]);
            Vector3 rightLocal = parent.InverseTransformPoint(
                _boardWorldCorners[3]);
            float boardWidth = Mathf.Abs(rightLocal.x - leftLocal.x);
            float availableWidth = Mathf.Max(
                0f,
                parent.rect.width - (ParentSidePadding * 2f));
            float preferredWidth = boardWidth * _boardWidthFraction;
            float barWidth = Mathf.Min(
                availableWidth,
                Mathf.Max(MinimumBarWidth, preferredWidth));
            if (barWidth <= 0f)
            {
                return;
            }

            float visualHeight = barWidth * AuthoredHeight / AuthoredWidth;
            float rootHeight = visualHeight + TextGap + TextHeight;
            _progressBarRect.sizeDelta = new Vector2(barWidth, rootHeight);

            ConfigureTopVisualRect(_backgroundImage?.rectTransform, visualHeight);
            ConfigureTopVisualRect(_frameImage?.rectTransform, visualHeight);

            if (_fillMaskRect != null)
            {
                float horizontalInset =
                    barWidth * AuthoredHorizontalInset / AuthoredWidth;
                float verticalInset =
                    visualHeight * AuthoredVerticalInset / AuthoredHeight;
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
            }

            if (_progressText != null)
            {
                RectTransform textRect = _progressText.rectTransform;
                textRect.anchorMin = new Vector2(0f, 0f);
                textRect.anchorMax = new Vector2(1f, 0f);
                textRect.pivot = new Vector2(0.5f, 0f);
                textRect.anchoredPosition = Vector2.zero;
                textRect.sizeDelta = new Vector2(0f, TextHeight);
            }
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

        private static void ConfigureTopVisualRect(
            RectTransform rect,
            float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
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
