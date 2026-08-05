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
        private Text _targetText;

        [SerializeField]
        private GameObject _completeOverlay;

        [SerializeField]
        private CanvasGroup _completionCanvasGroup;

        [SerializeField]
        private Text _completeText;

        [SerializeField]
        private Button _retryButton;

        private bool _buttonSubscribed;

        public FirstPlayableController Controller => _controller;

        public Text PercentageText => _percentageText;

        public Text TargetText => _targetText;

        public GameObject CompleteOverlay => _completeOverlay;

        public CanvasGroup CompletionCanvasGroup => _completionCanvasGroup;

        public Button RetryButton => _retryButton;

        public void Configure(
            FirstPlayableController controller,
            Text percentageText,
            Text targetText,
            GameObject completeOverlay,
            CanvasGroup completionCanvasGroup,
            Text completeText,
            Button retryButton)
        {
            UnsubscribeButton();
            _controller = controller;
            _percentageText = percentageText;
            _targetText = targetText;
            _completeOverlay = completeOverlay;
            _completionCanvasGroup = completionCanvasGroup;
            _completeText = completeText;
            _retryButton = retryButton;
            SetCompletionVisible(false);
            if (isActiveAndEnabled && Application.isPlaying)
            {
                SubscribeButton();
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
            if (_percentageText != null)
            {
                _percentageText.text =
                    $"Captured {Mathf.RoundToInt(captured * 100f)}%";
            }

            if (_targetText != null)
            {
                _targetText.text =
                    $"Target {Mathf.RoundToInt(target * 100f)}%";
            }

            bool completed = _controller.Session.LevelStatus
                == CaptureLevelStatus.Completed;
            SetCompletionVisible(completed);

            if (_completeText != null)
            {
                _completeText.text = "LEVEL COMPLETE";
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                SubscribeButton();
            }
        }

        private void OnDisable()
        {
            UnsubscribeButton();
        }

        private void LateUpdate()
        {
            RefreshNow();
        }

        private void SubscribeButton()
        {
            if (_buttonSubscribed || _retryButton == null)
            {
                return;
            }

            _retryButton.onClick.AddListener(OnRetryClicked);
            _buttonSubscribed = true;
        }

        private void UnsubscribeButton()
        {
            if (!_buttonSubscribed)
            {
                return;
            }

            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveListener(OnRetryClicked);
            }

            _buttonSubscribed = false;
        }

        private void OnRetryClicked()
        {
            _controller.RetryLevel();
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
    }
}
