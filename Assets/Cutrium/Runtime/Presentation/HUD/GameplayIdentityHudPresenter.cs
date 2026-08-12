using System;
using Cutrium.Gameplay.Session;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class GameplayIdentityHudPresenter : MonoBehaviour
    {
        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private Text _cutCounterText;
        [SerializeField] private CanvasGroup _introCanvasGroup;
        [SerializeField] private Text _introTitleText;
        [SerializeField] private Text _introMessageText;
        [SerializeField] private CanvasGroup _failureCanvasGroup;
        [SerializeField] private Text _failureText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private float _introFadeInSeconds = 0.18f;
        [SerializeField] private float _introHoldSeconds = 1.45f;
        [SerializeField] private float _introFadeOutSeconds = 0.35f;

        private ThreatMotionSession _observedSession;
        private float _introElapsed;
        private bool _retrySubscribed;

        public Text CutCounterText => _cutCounterText;
        public CanvasGroup IntroCanvasGroup => _introCanvasGroup;
        public CanvasGroup FailureCanvasGroup => _failureCanvasGroup;

        public void Configure(
            FirstPlayableController controller,
            Text cutCounterText,
            CanvasGroup introCanvasGroup,
            Text introTitleText,
            Text introMessageText,
            CanvasGroup failureCanvasGroup,
            Text failureText,
            Button retryButton)
        {
            ConfigureForSetup(
                controller,
                cutCounterText,
                introCanvasGroup,
                introTitleText,
                introMessageText,
                failureCanvasGroup,
                failureText,
                retryButton);
        }

        public void ConfigureForSetup(
            FirstPlayableController controller,
            Text cutCounterText,
            CanvasGroup introCanvasGroup,
            Text introTitleText,
            Text introMessageText,
            CanvasGroup failureCanvasGroup,
            Text failureText,
            Button retryButton)
        {
            UnsubscribeRetry();
            _controller = controller;
            _cutCounterText = cutCounterText;
            _introCanvasGroup = introCanvasGroup;
            _introTitleText = introTitleText;
            _introMessageText = introMessageText;
            _failureCanvasGroup = failureCanvasGroup;
            _failureText = failureText;
            _retryButton = retryButton;
            _observedSession = null;
            if (_introCanvasGroup != null)
            {
                _introCanvasGroup.blocksRaycasts = false;
                _introCanvasGroup.interactable = false;
            }

            if (_failureCanvasGroup != null)
            {
                _failureCanvasGroup.alpha = 0f;
                _failureCanvasGroup.interactable = false;
                _failureCanvasGroup.blocksRaycasts = false;
            }
            if (isActiveAndEnabled && Application.isPlaying)
            {
                SubscribeRetry();
            }

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
                SetGroup(_introCanvasGroup, false, 0f, false);
                SetGroup(_failureCanvasGroup, false, 0f, true);
                return;
            }

            bool sessionChanged = !ReferenceEquals(
                _observedSession,
                _controller.Session);
            if (sessionChanged)
            {
                _observedSession = _controller.Session;
                _introElapsed = 0f;
                ApplyIntroCopy();
            }
            else
            {
                _introElapsed += elapsedTime;
            }

            bool limited = _controller.Session.HasCutLimit;
            if (_cutCounterText != null)
            {
                _cutCounterText.gameObject.SetActive(limited);
                if (limited)
                {
                    _cutCounterText.text =
                        $"CUTS {_controller.Session.CutsRemaining}/" +
                        _controller.Session.MaximumAcceptedCuts;
                }
            }

            bool failed = _controller.Session.LevelStatus
                == CaptureLevelStatus.OutOfCuts;
            SetGroup(_failureCanvasGroup, failed, failed ? 1f : 0f, true);
            if (_failureText != null && failed)
            {
                _failureText.text = "OUT OF CUTS\nTRY A BOLDER ROUTE";
            }

            UpdateIntroAlpha(failed);
        }

        private void LateUpdate()
        {
            RefreshNow(Time.unscaledDeltaTime);
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                SubscribeRetry();
            }
        }

        private void OnDisable()
        {
            UnsubscribeRetry();
        }

        private void ApplyIntroCopy()
        {
            if (_controller.CurrentLevelIndex < 0
                || _controller.CurrentLevelIndex
                    >= _controller.LevelDefinitions.Count)
            {
                return;
            }

            CoreFunLevelDefinition definition =
                _controller.LevelDefinitions[_controller.CurrentLevelIndex];
            if (_introTitleText != null)
            {
                _introTitleText.text = definition.IntroTitle;
            }

            if (_introMessageText != null)
            {
                _introMessageText.text = definition.IntroMessage;
            }
        }

        private void UpdateIntroAlpha(bool failed)
        {
            if (_introCanvasGroup == null)
            {
                return;
            }

            bool hasIntro = _introTitleText != null
                && !string.IsNullOrWhiteSpace(_introTitleText.text);
            if (!hasIntro
                || failed
                || _controller.Session.LevelStatus
                    == CaptureLevelStatus.Completed)
            {
                SetGroup(_introCanvasGroup, false, 0f, false);
                return;
            }

            float fadeInEnd = _introFadeInSeconds;
            float holdEnd = fadeInEnd + _introHoldSeconds;
            float end = holdEnd + _introFadeOutSeconds;
            float alpha;
            if (_introElapsed < fadeInEnd)
            {
                alpha = fadeInEnd <= 0f
                    ? 1f
                    : Mathf.Clamp01(_introElapsed / fadeInEnd);
            }
            else if (_introElapsed < holdEnd)
            {
                alpha = 1f;
            }
            else if (_introElapsed < end)
            {
                alpha = _introFadeOutSeconds <= 0f
                    ? 0f
                    : 1f - Mathf.Clamp01(
                        (_introElapsed - holdEnd) / _introFadeOutSeconds);
            }
            else
            {
                alpha = 0f;
            }

            SetGroup(_introCanvasGroup, alpha > 0f, alpha, false);
        }

        private void SubscribeRetry()
        {
            if (_retrySubscribed || _retryButton == null)
            {
                return;
            }

            _retryButton.onClick.AddListener(OnRetryClicked);
            _retrySubscribed = true;
        }

        private void UnsubscribeRetry()
        {
            if (_retrySubscribed && _retryButton != null)
            {
                _retryButton.onClick.RemoveListener(OnRetryClicked);
            }

            _retrySubscribed = false;
        }

        private void OnRetryClicked()
        {
            _controller.NotifyUiFeedback();
            _controller.RetryLevel();
            RefreshNow(0f);
        }

        private static void SetGroup(
            CanvasGroup group,
            bool visible,
            float alpha,
            bool blockWhenVisible)
        {
            if (group == null)
            {
                return;
            }

            if (!group.gameObject.activeSelf)
            {
                group.gameObject.SetActive(true);
            }

            group.alpha = alpha;
            group.interactable = visible && blockWhenVisible;
            group.blocksRaycasts = visible && blockWhenVisible;
        }
    }
}
