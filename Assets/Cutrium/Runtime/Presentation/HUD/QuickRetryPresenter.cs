using System;
using Cutrium.Presentation.Frontend;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class QuickRetryPresenter : MonoBehaviour
    {
        [SerializeField]
        private FirstPlayableController _controller;

        [SerializeField]
        private Button _retryButton;

        [SerializeField]
        private ScreenTransitionPresenter _screenTransition;

        private bool _retryButtonSubscribed;

        public FirstPlayableController Controller => _controller;

        public Button RetryButton => _retryButton;

        public ScreenTransitionPresenter ScreenTransition =>
            _screenTransition;

        public void Configure(
            FirstPlayableController controller,
            Button retryButton)
        {
            UnsubscribeButton();
            _controller = controller;
            _retryButton = retryButton;
            if (isActiveAndEnabled && Application.isPlaying)
            {
                SubscribeButton();
            }
        }

        public void ConfigureScreenTransitionForSetup(
            ScreenTransitionPresenter screenTransition)
        {
            _screenTransition = screenTransition
                ?? throw new ArgumentNullException(nameof(screenTransition));
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

        private void SubscribeButton()
        {
            if (!_retryButtonSubscribed && _retryButton != null)
            {
                _retryButton.onClick.AddListener(OnRetryClicked);
                _retryButtonSubscribed = true;
            }
        }

        private void UnsubscribeButton()
        {
            if (_retryButtonSubscribed && _retryButton != null)
            {
                _retryButton.onClick.RemoveListener(OnRetryClicked);
            }

            _retryButtonSubscribed = false;
        }

        private void OnRetryClicked()
        {
            _controller.NotifyUiFeedback();
            if (_screenTransition != null)
            {
                _screenTransition.TryTransition(_controller.RetryLevel);
                return;
            }

            _controller.RetryLevel();
        }
    }
}
