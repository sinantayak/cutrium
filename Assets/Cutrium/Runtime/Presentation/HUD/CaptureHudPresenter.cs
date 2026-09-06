using Cutrium.Gameplay.Session;
using Cutrium.Presentation.Frontend;
using Cutrium.Presentation.Landmark;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class CaptureHudPresenter : MonoBehaviour
    {
        [SerializeField]
        private FirstPlayableController _controller;

        [SerializeField]
        private Text _percentageText;

        [SerializeField]
        private Text _levelText;

        [SerializeField]
        private Text _purposeText;

        [SerializeField]
        private Text _targetText;

        [SerializeField]
        private Image _progressBarFillImage;

        [SerializeField]
        private RectTransform _progressBarTrackRect;

        [SerializeField]
        private RectTransform _targetTickRect;

        [SerializeField]
        private Text _targetTickLabel;

        [SerializeField]
        private GameObject _completeOverlay;

        [SerializeField]
        private CanvasGroup _completionCanvasGroup;

        [SerializeField]
        private Text _completeText;

        [SerializeField]
        private Button _retryButton;

        [SerializeField]
        private Button _nextButton;

        [SerializeField]
        private Text _nextButtonLabel;

        [SerializeField]
        private LandmarkRevealPresenter _completionRevealGate;

        [SerializeField]
        private ScreenTransitionPresenter _screenTransition;

        [SerializeField]
        [Min(0f)]
        private float _percentageAnimationSeconds = 0.22f;

        [SerializeField]
        [Min(0f)]
        private float _completionOverlayFadeSeconds = 0.45f;

        private bool _retryButtonSubscribed;
        private bool _nextButtonSubscribed;
        private bool _percentageInitialized;
        private float _displayedCapturedFraction;
        private float _percentageAnimationStart;
        private float _percentageAnimationTarget;
        private float _percentageAnimationElapsed;
        private float _completionOverlayAlpha;

        public FirstPlayableController Controller => _controller;

        public Text PercentageText => _percentageText;

        public Text LevelText => _levelText;

        public Text PurposeText => _purposeText;

        public Text TargetText => _targetText;

        public Image ProgressBarFillImage => _progressBarFillImage;

        public RectTransform ProgressBarTrackRect => _progressBarTrackRect;

        public RectTransform TargetTickRect => _targetTickRect;

        public Text TargetTickLabel => _targetTickLabel;

        public GameObject CompleteOverlay => _completeOverlay;

        public CanvasGroup CompletionCanvasGroup => _completionCanvasGroup;

        public Button RetryButton => _retryButton;

        public Button NextButton => _nextButton;

        public Text NextButtonLabel => _nextButtonLabel;

        public LandmarkRevealPresenter CompletionRevealGate =>
            _completionRevealGate;

        public ScreenTransitionPresenter ScreenTransition =>
            _screenTransition;

        public float DisplayedCapturedFraction =>
            _displayedCapturedFraction;

        public float PercentageAnimationSeconds =>
            _percentageAnimationSeconds;

        public float CompletionOverlayFadeSeconds =>
            _completionOverlayFadeSeconds;

        public void Configure(
            FirstPlayableController controller,
            Text percentageText,
            Text targetText,
            GameObject completeOverlay,
            CanvasGroup completionCanvasGroup,
            Text completeText,
            Button retryButton)
        {
            Configure(
                controller,
                null,
                null,
                percentageText,
                targetText,
                completeOverlay,
                completionCanvasGroup,
                completeText,
                retryButton,
                null,
                null);
        }

        public void Configure(
            FirstPlayableController controller,
            Text levelText,
            Text purposeText,
            Text percentageText,
            Text targetText,
            GameObject completeOverlay,
            CanvasGroup completionCanvasGroup,
            Text completeText,
            Button retryButton,
            Button nextButton,
            Text nextButtonLabel)
        {
            UnsubscribeButtons();
            _controller = controller;
            _levelText = levelText;
            _purposeText = purposeText;
            _percentageText = percentageText;
            _targetText = targetText;
            _completeOverlay = completeOverlay;
            _completionCanvasGroup = completionCanvasGroup;
            _completeText = completeText;
            _retryButton = retryButton;
            _nextButton = nextButton;
            _nextButtonLabel = nextButtonLabel;
            _percentageInitialized = false;
            _completionOverlayAlpha = 0f;
            SetCompletionVisible(false, true, 0f);
            if (isActiveAndEnabled && Application.isPlaying)
            {
                SubscribeButtons();
            }
        }

        public void RefreshNow()
        {
            RefreshPresentation(true, 0f);
        }

        public void AdvancePercentageAnimation(float elapsedTime)
        {
            if (float.IsNaN(elapsedTime)
                || float.IsInfinity(elapsedTime)
                || elapsedTime < 0f)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(elapsedTime));
            }

            RefreshPresentation(false, elapsedTime);
        }

        public void ConfigureProgressBar(
            Image progressBarFillImage,
            RectTransform progressBarTrackRect,
            RectTransform targetTickRect,
            Text targetTickLabel)
        {
            _progressBarFillImage = progressBarFillImage;
            _progressBarTrackRect = progressBarTrackRect;
            _targetTickRect = targetTickRect;
            _targetTickLabel = targetTickLabel;
            RefreshNow();
        }

        public void ConfigureFeedbackAnimationForSetup(float duration)
        {
            if (float.IsNaN(duration)
                || float.IsInfinity(duration)
                || duration < 0f)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(duration));
            }

            _percentageAnimationSeconds = duration;
        }

        public void ConfigureCompletionRevealGateForSetup(
            LandmarkRevealPresenter completionRevealGate)
        {
            _completionRevealGate = completionRevealGate;
            RefreshNow();
        }

        public void ConfigureCompletionOverlayFadeForSetup(float duration)
        {
            if (float.IsNaN(duration)
                || float.IsInfinity(duration)
                || duration < 0f)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(duration));
            }

            _completionOverlayFadeSeconds = duration;
        }

        public void ConfigureScreenTransitionForSetup(
            ScreenTransitionPresenter screenTransition)
        {
            _screenTransition = screenTransition
                ?? throw new System.ArgumentNullException(
                    nameof(screenTransition));
        }

        private void RefreshPresentation(
            bool settleImmediately,
            float elapsedTime)
        {
            if (_controller == null || _controller.Session == null)
            {
                return;
            }

            ResolveLegacyCompletionRevealGate();

            float captured = _controller.Session.CapturedFraction;
            float target = _controller.Session.TargetCapturedFraction;
            UpdateDisplayedPercentage(
                captured,
                settleImmediately,
                elapsedTime);
            if (_levelText != null)
            {
                _levelText.text = $"LEVEL {_controller.CurrentLevelNumber}";
            }

            if (_purposeText != null)
            {
                _purposeText.text =
                    _controller.CurrentLevelConfiguration.PurposeLine;
            }

            if (_percentageText != null)
            {
                _percentageText.text =
                    $"Captured {RoundedPercent(_displayedCapturedFraction)}%";
            }

            // This slot used to show the target; the target is now marked
            // directly on the bar (see UpdateTargetTick) so this reads the
            // current captured fraction instead, in sync with the fill --
            // the sole percentage readout, shown after the bar's right edge.
            if (_targetText != null)
            {
                _targetText.text = $"{RoundedPercent(_displayedCapturedFraction)}%";
            }

            if (_progressBarFillImage != null)
            {
                _progressBarFillImage.fillAmount =
                    Mathf.Clamp01(_displayedCapturedFraction);
            }

            UpdateTargetTick(target);

            bool completed = _controller.Session.LevelStatus
                == CaptureLevelStatus.Completed;
            bool completionPresentationReady =
                _completionRevealGate == null
                || _completionRevealGate.CompletionPresentationReady;
            SetCompletionVisible(
                completed && completionPresentationReady,
                settleImmediately,
                elapsedTime);

            if (_completeText != null)
            {
                CoreFunLevelMetrics metrics = _controller.Metrics.Current;
                _completeText.text = completed
                    ? $"LEVEL {_controller.CurrentLevelNumber} COMPLETE\n" +
                      $"Captured {RoundedPercent(captured)}%  " +
                      $"Time {metrics.ElapsedSeconds:0.0}s\n" +
                      $"Attempts {metrics.BarrierAttempts}  " +
                      $"Breaks {metrics.FailedBarriers}"
                    : $"LEVEL {_controller.CurrentLevelNumber}";
            }

            if (_nextButtonLabel != null)
            {
                _nextButtonLabel.text = _controller.HasNextLevel
                    ? "NEXT"
                    : "RESTART SEQUENCE";
            }
        }

        private void OnEnable()
        {
            ResolveLegacyCompletionRevealGate();
            if (Application.isPlaying)
            {
                SubscribeButtons();
            }
        }

        private void ResolveLegacyCompletionRevealGate()
        {
            if (_completionRevealGate != null || _controller == null)
            {
                return;
            }

            // Compatibility only for scenes serialized before the focused
            // completion-popup setup existed. The normal dependency path is
            // the explicit ConfigureCompletionRevealGateForSetup reference.
            _completionRevealGate = _controller.transform.root
                .GetComponentInChildren<LandmarkRevealPresenter>(true);
        }

        private void OnDisable()
        {
            UnsubscribeButtons();
        }

        private void LateUpdate()
        {
            RefreshPresentation(false, Time.unscaledDeltaTime);
        }

        private void UpdateDisplayedPercentage(
            float logicalCapturedFraction,
            bool settleImmediately,
            float elapsedTime)
        {
            if (!_percentageInitialized
                || settleImmediately
                || logicalCapturedFraction < _displayedCapturedFraction)
            {
                _percentageInitialized = true;
                _displayedCapturedFraction = logicalCapturedFraction;
                _percentageAnimationStart = logicalCapturedFraction;
                _percentageAnimationTarget = logicalCapturedFraction;
                _percentageAnimationElapsed = 0f;
                return;
            }

            if (!Mathf.Approximately(
                    logicalCapturedFraction,
                    _percentageAnimationTarget))
            {
                _percentageAnimationStart = _displayedCapturedFraction;
                _percentageAnimationTarget = logicalCapturedFraction;
                _percentageAnimationElapsed = 0f;
            }

            if (Mathf.Approximately(
                    _displayedCapturedFraction,
                    _percentageAnimationTarget))
            {
                _displayedCapturedFraction = _percentageAnimationTarget;
                return;
            }

            _percentageAnimationElapsed += elapsedTime;
            float progress = _percentageAnimationSeconds <= 0f
                ? 1f
                : Mathf.Clamp01(
                    _percentageAnimationElapsed
                    / _percentageAnimationSeconds);
            _displayedCapturedFraction = Mathf.Lerp(
                _percentageAnimationStart,
                _percentageAnimationTarget,
                progress);
            if (progress >= 1f)
            {
                _displayedCapturedFraction = _percentageAnimationTarget;
            }
        }

        private void UpdateTargetTick(float target)
        {
            if (_targetTickRect == null || _progressBarTrackRect == null)
            {
                return;
            }

            float trackWidth = _progressBarTrackRect.rect.width;
            float x = Mathf.Clamp01(target) * trackWidth;
            _targetTickRect.anchoredPosition =
                new Vector2(x, _targetTickRect.anchoredPosition.y);

            if (_targetTickLabel != null)
            {
                RectTransform labelRect = _targetTickLabel.rectTransform;
                labelRect.anchoredPosition =
                    new Vector2(x, labelRect.anchoredPosition.y);
            }
        }

        private void SubscribeButtons()
        {
            if (!_retryButtonSubscribed && _retryButton != null)
            {
                _retryButton.onClick.AddListener(OnRetryClicked);
                _retryButtonSubscribed = true;
            }

            if (!_nextButtonSubscribed && _nextButton != null)
            {
                _nextButton.onClick.AddListener(OnNextClicked);
                _nextButtonSubscribed = true;
            }
        }

        private void UnsubscribeButtons()
        {
            if (_retryButtonSubscribed && _retryButton != null)
            {
                _retryButton.onClick.RemoveListener(OnRetryClicked);
            }

            if (_nextButtonSubscribed && _nextButton != null)
            {
                _nextButton.onClick.RemoveListener(OnNextClicked);
            }

            _retryButtonSubscribed = false;
            _nextButtonSubscribed = false;
        }

        private void OnRetryClicked()
        {
            _controller.NotifyUiFeedback();
            RunScreenTransition(() =>
            {
                _controller.RetryLevel();
                RefreshNow();
            });
        }

        private void OnNextClicked()
        {
            _controller.NotifyUiFeedback();
            RunScreenTransition(() =>
            {
                _controller.AdvanceLevelOrRestartSequence();
                RefreshNow();
            });
        }

        private void RunScreenTransition(System.Action midpointAction)
        {
            if (_screenTransition != null)
            {
                _screenTransition.TryTransition(midpointAction);
                return;
            }

            midpointAction();
        }

        private void SetCompletionVisible(
            bool visible,
            bool settleImmediately,
            float elapsedTime)
        {
            if (_completeOverlay != null && !_completeOverlay.activeSelf)
            {
                _completeOverlay.SetActive(true);
            }

            if (_completionCanvasGroup == null)
            {
                return;
            }

            float target = visible ? 1f : 0f;
            if (!visible
                || settleImmediately
                || _completionOverlayFadeSeconds <= 0f)
            {
                _completionOverlayAlpha = target;
            }
            else
            {
                _completionOverlayAlpha = Mathf.MoveTowards(
                    _completionOverlayAlpha,
                    target,
                    elapsedTime / _completionOverlayFadeSeconds);
            }

            _completionCanvasGroup.alpha = _completionOverlayAlpha;
            bool interactionReady = visible
                && _completionOverlayAlpha >= 0.999f;
            _completionCanvasGroup.interactable = interactionReady;
            // Block gameplay/HUD input from the first fade frame, but only
            // allow popup controls once the overlay is fully readable.
            _completionCanvasGroup.blocksRaycasts = visible;
        }

        private static int RoundedPercent(float fraction) =>
            Mathf.FloorToInt(fraction * 100f + 0.5f);
    }
}
