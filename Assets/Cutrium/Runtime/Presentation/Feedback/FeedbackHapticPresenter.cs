using Cutrium.Gameplay.Feedback;
using Cutrium.Unity.Feedback;
using Cutrium.Unity.Simulation;
using UnityEngine;

namespace Cutrium.Presentation.Feedback
{
    [DisallowMultipleComponent]
    public sealed class FeedbackHapticPresenter : MonoBehaviour
    {
        [SerializeField]
        private FirstPlayableController _controller;

        private readonly IHapticFeedback _fallback =
            new NoOpHapticFeedback();
        private IHapticFeedback _service;
        private bool _subscribed;

        public FirstPlayableController Controller => _controller;

        public int HandledCueCount { get; private set; }

        public void Configure(FirstPlayableController controller)
        {
            Unsubscribe();
            _controller = controller;
            _service = _fallback;
            if (isActiveAndEnabled && Application.isPlaying)
            {
                Subscribe();
            }
        }

        public void SetService(IHapticFeedback service)
        {
            _service = service ?? _fallback;
        }

        public void PlayUi() => Play(HapticFeedbackCue.Ui);

        private void OnEnable()
        {
            _service ??= _fallback;
            if (Application.isPlaying)
            {
                Subscribe();
            }
        }

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || _controller == null)
            {
                return;
            }

            _controller.FeedbackEventRaised += OnFeedbackEvent;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && _controller != null)
            {
                _controller.FeedbackEventRaised -= OnFeedbackEvent;
            }

            _subscribed = false;
        }

        private void OnFeedbackEvent(FeedbackEvent feedbackEvent)
        {
            switch (feedbackEvent.Kind)
            {
                case FeedbackEventKind.BarrierLocked:
                    Play(HapticFeedbackCue.BarrierLock);
                    break;
                case FeedbackEventKind.BarrierBroken:
                    Play(HapticFeedbackCue.BarrierBreak);
                    break;
                case FeedbackEventKind.LargeCapture:
                    Play(HapticFeedbackCue.LargeCapture);
                    break;
                case FeedbackEventKind.NearMiss:
                    Play(HapticFeedbackCue.NearMiss);
                    break;
                case FeedbackEventKind.LevelCompleted:
                    Play(HapticFeedbackCue.LevelComplete);
                    break;
                case FeedbackEventKind.Ui:
                    Play(HapticFeedbackCue.Ui);
                    break;
            }
        }

        private void Play(HapticFeedbackCue cue)
        {
            (_service ?? _fallback).Play(cue);
            HandledCueCount++;
        }
    }
}
