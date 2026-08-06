using Cutrium.Gameplay.Session;
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

        private bool _retryButtonSubscribed;
        private bool _nextButtonSubscribed;

        public FirstPlayableController Controller => _controller;

        public Text PercentageText => _percentageText;

        public Text LevelText => _levelText;

        public Text PurposeText => _purposeText;

        public Text TargetText => _targetText;

        public GameObject CompleteOverlay => _completeOverlay;

        public CanvasGroup CompletionCanvasGroup => _completionCanvasGroup;

        public Button RetryButton => _retryButton;

        public Button NextButton => _nextButton;

        public Text NextButtonLabel => _nextButtonLabel;

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
            SetCompletionVisible(false);
            if (isActiveAndEnabled && Application.isPlaying)
            {
                SubscribeButtons();
            }
        }

        public void RefreshNow()
        {
            if (_controller == null || _controller.Session == null)
            {
                return;
            }

            float captured = _controller.Session.CapturedFraction;
            float target = _controller.Session.TargetCapturedFraction;
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
                    $"Captured {RoundedPercent(captured)}%";
            }

            if (_targetText != null)
            {
                _targetText.text =
                    $"Target {RoundedPercent(target)}%";
            }

            bool completed = _controller.Session.LevelStatus
                == CaptureLevelStatus.Completed;
            SetCompletionVisible(completed);

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
            if (Application.isPlaying)
            {
                SubscribeButtons();
            }
        }

        private void OnDisable()
        {
            UnsubscribeButtons();
        }

        private void LateUpdate()
        {
            RefreshNow();
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
            _controller.RetryLevel();
            RefreshNow();
        }

        private void OnNextClicked()
        {
            _controller.AdvanceLevelOrRestartSequence();
            RefreshNow();
        }

        private void SetCompletionVisible(bool visible)
        {
            if (_completeOverlay != null && !_completeOverlay.activeSelf)
            {
                _completeOverlay.SetActive(true);
            }

            if (_completionCanvasGroup == null)
            {
                return;
            }

            _completionCanvasGroup.alpha = visible ? 1f : 0f;
            _completionCanvasGroup.interactable = visible;
            _completionCanvasGroup.blocksRaycasts = visible;
        }

        private static int RoundedPercent(float fraction) =>
            Mathf.FloorToInt(fraction * 100f + 0.5f);
    }
}
