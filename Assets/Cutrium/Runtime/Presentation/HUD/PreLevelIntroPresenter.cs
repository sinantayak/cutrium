using System;
using Cutrium.Gameplay.Session;
using Cutrium.Presentation.Threats;
using Cutrium.Unity.Simulation;
using TMPro;
using UnityEngine;

namespace Cutrium.Presentation.HUD
{
    /// Plays a staged "LEVEL N -> TARGET X% -> CUT N -> intro copy" cinematic
    /// before each genuinely new level starts: only the threats stay hidden
    /// and the simulation held while big centered text appears in turn -- the
    /// board, sand, and landmark art underneath stay visible and ready. The
    /// target and Cut stages fly into their matching live HUD destinations.
    /// Mechanic introductions such as Hunter, Pulse, and Instant stay centered
    /// and fade away in place.
    /// Retries are intentionally excluded (see RetryCount handling below) so
    /// the player drops straight back into a visible, playable board.
    [DisallowMultipleComponent]
    public sealed class PreLevelIntroPresenter : MonoBehaviour
    {
        private enum Stage
        {
            Idle,
            ShowingLevel,
            ShowingTarget,
            FlyingTarget,
            ShowingCut,
            FlyingCut,
            ShowingInfo,
            Done
        }

        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private ThreatPresenter _threatPresenter;
        [SerializeField] private CanvasGroup _levelGroup;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private CanvasGroup _targetGroup;
        [SerializeField] private TMP_Text _targetText;
        [SerializeField] private CanvasGroup _infoGroup;
        [SerializeField] private TMP_Text _infoTitleText;
        [SerializeField] private TMP_Text _infoMessageText;
        [SerializeField] private RectTransform _flightRoot;
        [SerializeField] private RectTransform _progressFlightDestination;
        [SerializeField] private RectTransform _cutFlightDestination;

        [SerializeField] private float _stageFadeInSeconds = 0.2f;
        [SerializeField] private float _stageHoldSeconds = 0.85f;
        [SerializeField] private float _stageFadeOutSeconds = 0.25f;
        [SerializeField] private float _flightDurationSeconds = 0.55f;
        [SerializeField] private float _flightArcHeight = 46f;
        [SerializeField] private float _flightLandingScale = 0.35f;

        private ThreatMotionSession _observedSession;
        private int _observedRetryCount;
        private Stage _stage = Stage.Idle;
        private float _elapsed;
        private Vector2 _flightStart;

        public bool IsPlaying => _stage != Stage.Idle && _stage != Stage.Done;

        // Test-only escape hatch: most PlayMode tests exercise gameplay
        // systems that assume the board is immediately visible and
        // interactive, not this multi-second cinematic. Call once the
        // controller's session exists (e.g. right after scene load) to park
        // the sequence in its finished state without ever holding the
        // simulation or disabling barrier input.
        public void SkipForTesting()
        {
            if (_controller == null || _controller.Session == null)
            {
                return;
            }

            _observedSession = _controller.Session;
            _observedRetryCount = _controller.RetryCount;
            SkipSequence();
        }

        public void Configure(
            FirstPlayableController controller,
            ThreatPresenter threatPresenter,
            CanvasGroup levelGroup,
            TMP_Text levelText,
            CanvasGroup targetGroup,
            TMP_Text targetText,
            CanvasGroup infoGroup,
            TMP_Text infoTitleText,
            TMP_Text infoMessageText,
            RectTransform flightRoot,
            RectTransform progressFlightDestination,
            RectTransform cutFlightDestination)
        {
            ConfigureForSetup(
                controller,
                threatPresenter,
                levelGroup,
                levelText,
                targetGroup,
                targetText,
                infoGroup,
                infoTitleText,
                infoMessageText,
                flightRoot,
                progressFlightDestination,
                cutFlightDestination);
        }

        public void ConfigureForSetup(
            FirstPlayableController controller,
            ThreatPresenter threatPresenter,
            CanvasGroup levelGroup,
            TMP_Text levelText,
            CanvasGroup targetGroup,
            TMP_Text targetText,
            CanvasGroup infoGroup,
            TMP_Text infoTitleText,
            TMP_Text infoMessageText,
            RectTransform flightRoot,
            RectTransform progressFlightDestination,
            RectTransform cutFlightDestination)
        {
            _controller = controller;
            _threatPresenter = threatPresenter;
            _levelGroup = levelGroup;
            _levelText = levelText;
            _targetGroup = targetGroup;
            _targetText = targetText;
            _infoGroup = infoGroup;
            _infoTitleText = infoTitleText;
            _infoMessageText = infoMessageText;
            _flightRoot = flightRoot;
            _progressFlightDestination = progressFlightDestination;
            _cutFlightDestination = cutFlightDestination;
            _observedSession = null;
            _observedRetryCount = 0;
            _stage = Stage.Idle;
            SetGroup(_levelGroup, 0f);
            SetGroup(_targetGroup, 0f);
            SetGroup(_infoGroup, 0f);
            _threatPresenter?.SetVisible(true);
            RefreshNow(0f);
        }

        public void RefreshNow(float elapsedTime)
        {
            if (float.IsNaN(elapsedTime)
                || float.IsInfinity(elapsedTime)
                || elapsedTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedTime));
            }

            if (_controller == null || _controller.Session == null)
            {
                return;
            }

            bool sessionChanged = !ReferenceEquals(
                _observedSession,
                _controller.Session);
            if (sessionChanged)
            {
                bool isRetry = _controller.RetryCount != _observedRetryCount;
                _observedSession = _controller.Session;
                _observedRetryCount = _controller.RetryCount;
                _elapsed = 0f;
                if (isRetry)
                {
                    SkipSequence();
                    return;
                }

                StartSequence();
                return;
            }

            _elapsed += elapsedTime;
            Advance();
        }

        private void LateUpdate()
        {
            RefreshNow(Time.unscaledDeltaTime);
        }

        private void SkipSequence()
        {
            _controller.SetSimulationHold(false);
            _threatPresenter?.SetVisible(true);
            SetGroup(_levelGroup, 0f);
            SetGroup(_targetGroup, 0f);
            SetGroup(_infoGroup, 0f);
            _stage = Stage.Done;
        }

        private void StartSequence()
        {
            _controller.SetSimulationHold(true);
            _threatPresenter?.SetVisible(false);
            SetGroup(_targetGroup, 0f);
            SetGroup(_infoGroup, 0f);
            if (_levelText != null)
            {
                _levelText.text = $"LEVEL {_controller.CurrentLevelNumber}";
            }

            _stage = Stage.ShowingLevel;
        }

        private void Advance()
        {
            switch (_stage)
            {
                case Stage.ShowingLevel:
                    AdvanceFadeStage(
                        _levelGroup,
                        onComplete: () =>
                        {
                            if (_targetText != null)
                            {
                                int percent = Mathf.RoundToInt(
                                    _controller.TargetCapturedFraction
                                        * 100f);
                                _targetText.text = $"TARGET {percent}%";
                            }

                            _stage = Stage.ShowingTarget;
                        });
                    break;
                case Stage.ShowingTarget:
                    AdvanceHoldStage(
                        _targetGroup,
                        onComplete: () => BeginFlight(
                            _targetGroup,
                            Stage.FlyingTarget));
                    break;
                case Stage.FlyingTarget:
                    AdvanceFlightStage(
                        _targetGroup,
                        _progressFlightDestination,
                        onComplete: () =>
                        {
                            SetGroup(_targetGroup, 0f);
                            ResetFlightVisualState(_targetGroup);
                            if (ApplyCutCopy())
                            {
                                _stage = Stage.ShowingCut;
                            }
                            else if (ApplyInfoCopy())
                            {
                                _stage = Stage.ShowingInfo;
                            }
                            else
                            {
                                CompleteSequence();
                            }
                        });
                    break;
                case Stage.ShowingCut:
                    AdvanceHoldStage(
                        _infoGroup,
                        onComplete: () => BeginFlight(
                            _infoGroup,
                            Stage.FlyingCut));
                    break;
                case Stage.FlyingCut:
                    AdvanceFlightStage(
                        _infoGroup,
                        _cutFlightDestination,
                        onComplete: () =>
                        {
                            SetGroup(_infoGroup, 0f);
                            ResetFlightVisualState(_infoGroup);
                            if (ApplyInfoCopy())
                            {
                                _stage = Stage.ShowingInfo;
                            }
                            else
                            {
                                CompleteSequence();
                            }
                        });
                    break;
                case Stage.ShowingInfo:
                    AdvanceFadeStage(_infoGroup, CompleteSequence);
                    break;
            }
        }

        private void CompleteSequence()
        {
            _threatPresenter?.SetVisible(true);
            _controller.SetSimulationHold(false);
            _elapsed = 0f;
            _stage = Stage.Done;
        }

        private void AdvanceFadeStage(
            CanvasGroup group,
            Action onComplete)
        {
            float holdEnd = _stageFadeInSeconds + _stageHoldSeconds;
            float end = holdEnd + _stageFadeOutSeconds;
            float alpha;
            if (_elapsed < _stageFadeInSeconds)
            {
                alpha = _stageFadeInSeconds <= 0f
                    ? 1f
                    : Mathf.Clamp01(_elapsed / _stageFadeInSeconds);
            }
            else if (_elapsed < holdEnd)
            {
                alpha = 1f;
            }
            else if (_elapsed < end)
            {
                alpha = _stageFadeOutSeconds <= 0f
                    ? 0f
                    : 1f - Mathf.Clamp01(
                        (_elapsed - holdEnd) / _stageFadeOutSeconds);
            }
            else
            {
                alpha = 0f;
                SetGroup(group, alpha);
                _elapsed = 0f;
                onComplete();
                return;
            }

            SetGroup(group, alpha);
        }

        private void AdvanceHoldStage(
            CanvasGroup group,
            Action onComplete)
        {
            float holdEnd = _stageFadeInSeconds + _stageHoldSeconds;
            float alpha;
            if (_elapsed < _stageFadeInSeconds)
            {
                alpha = _stageFadeInSeconds <= 0f
                    ? 1f
                    : Mathf.Clamp01(_elapsed / _stageFadeInSeconds);
                SetGroup(group, alpha);
            }
            else if (_elapsed < holdEnd)
            {
                SetGroup(group, 1f);
            }
            else
            {
                SetGroup(group, 1f);
                _elapsed = 0f;
                onComplete();
            }
        }

        private void BeginFlight(CanvasGroup group, Stage nextStage)
        {
            if (group != null && group.transform is RectTransform rect)
            {
                _flightStart = rect.anchoredPosition;
            }
            else
            {
                _flightStart = Vector2.zero;
            }

            _elapsed = 0f;
            _stage = nextStage;
        }

        private void AdvanceFlightStage(
            CanvasGroup group,
            RectTransform destination,
            Action onComplete)
        {
            if (group == null
                || destination == null
                || _flightRoot == null
                || !(group.transform is RectTransform rect))
            {
                onComplete();
                return;
            }

            float progress = _flightDurationSeconds <= 0f
                ? 1f
                : Mathf.Clamp01(_elapsed / _flightDurationSeconds);

            Vector3 targetWorld = destination.TransformPoint(Vector3.zero);
            Vector2 liveEnd = _flightRoot.InverseTransformPoint(targetWorld);
            Vector2 linear = Vector2.Lerp(_flightStart, liveEnd, progress);
            float arc = Mathf.Sin(progress * Mathf.PI) * _flightArcHeight;
            rect.anchoredPosition = linear + new Vector2(0f, arc);
            rect.localScale = Vector3.one
                * Mathf.Lerp(1f, _flightLandingScale, progress);

            float fadeStart = 0.75f;
            float alpha = progress < fadeStart
                ? 1f
                : 1f - Mathf.Clamp01((progress - fadeStart) / (1f - fadeStart));
            group.alpha = alpha;

            if (progress >= 1f)
            {
                _elapsed = 0f;
                onComplete();
            }
        }

        private void ResetFlightVisualState(CanvasGroup group)
        {
            if (group != null && group.transform is RectTransform rect)
            {
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
            }
        }

        private bool ApplyInfoCopy()
        {
            if (_controller.CurrentLevelIndex < 0
                || _controller.CurrentLevelIndex
                    >= _controller.LevelDefinitions.Count)
            {
                return false;
            }

            CoreFunLevelDefinition definition =
                _controller.LevelDefinitions[_controller.CurrentLevelIndex];
            string title = definition.IntroTitle?.Trim() ?? string.Empty;
            string message = definition.IntroMessage?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(message))
            {
                return false;
            }

            // Earlier cut-only levels authored their cut count inside the
            // generic intro fields. The dedicated Cut stage now owns that
            // responsibility, so do not repeat the same card immediately
            // after it lands in the HUD.
            if (_controller.Session.HasCutLimit
                && IsCutLimitTitle(
                    title,
                    _controller.Session.MaximumAcceptedCuts))
            {
                return false;
            }

            if (_infoTitleText != null)
            {
                _infoTitleText.text = title;
            }

            if (_infoMessageText != null)
            {
                _infoMessageText.text = message;
            }

            return true;
        }

        private bool ApplyCutCopy()
        {
            if (_controller.Session == null
                || !_controller.Session.HasCutLimit)
            {
                return false;
            }

            int maximumCuts = _controller.Session.MaximumAcceptedCuts;
            if (_infoTitleText != null)
            {
                _infoTitleText.text = maximumCuts == 1
                    ? "1 CUT"
                    : $"{maximumCuts} CUTS";
            }

            if (_infoMessageText != null)
            {
                _infoMessageText.text = "MAKE THEM COUNT";
            }

            return true;
        }

        private static bool IsCutLimitTitle(string title, int maximumCuts)
        {
            string normalized = title.Trim().ToUpperInvariant();
            return normalized == $"{maximumCuts} CUT"
                || normalized == $"{maximumCuts} CUTS"
                || normalized == $"CUT {maximumCuts}"
                || normalized == $"CUTS {maximumCuts}";
        }

        private static void SetGroup(CanvasGroup group, float alpha)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = alpha;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
}
