using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Feedback;
using Cutrium.Unity.Simulation;
using UnityEngine;

namespace Cutrium.Presentation.Feedback
{
    [DisallowMultipleComponent]
    public sealed class FeedbackAudioPresenter : MonoBehaviour
    {
        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _startClip;
        [SerializeField] private AudioClip _growClip;
        [SerializeField] private AudioClip _lockClip;
        [SerializeField] private AudioClip _breakClip;
        [SerializeField] private AudioClip _fillClip;
        [SerializeField] private AudioClip _largeCaptureClip;
        [SerializeField] private AudioClip _nearMissClip;
        [SerializeField] private AudioClip _completeClip;
        [SerializeField] private AudioClip _uiClip;

        private bool _subscribed;
        private bool _effectsEnabled = true;
        private BarrierId _growPlayedFor;

        public FirstPlayableController Controller => _controller;

        public AudioSource AudioSource => _audioSource;

        public bool EffectsEnabled => _effectsEnabled;

        public int HandledEventCount { get; private set; }

        public void Configure(
            FirstPlayableController controller,
            AudioSource audioSource)
        {
            Unsubscribe();
            _controller = controller;
            _audioSource = audioSource;
            if (isActiveAndEnabled && Application.isPlaying)
            {
                Subscribe();
            }
        }

        public void PlayUi()
        {
            Play(_uiClip);
        }

        public void SetEffectsEnabled(bool enabled)
        {
            _effectsEnabled = enabled;
            if (!enabled && _audioSource != null)
            {
                _audioSource.Stop();
            }
        }

        private void OnEnable()
        {
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
            HandledEventCount++;
            switch (feedbackEvent.Kind)
            {
                case FeedbackEventKind.SessionReset:
                    _growPlayedFor = default;
                    break;
                case FeedbackEventKind.BarrierStarted:
                    _growPlayedFor = default;
                    Play(_startClip);
                    break;
                case FeedbackEventKind.BarrierGrowing:
                    if (_growPlayedFor != feedbackEvent.BarrierId)
                    {
                        _growPlayedFor = feedbackEvent.BarrierId;
                        Play(_growClip);
                    }
                    break;
                case FeedbackEventKind.BarrierLocked:
                    Play(_lockClip);
                    break;
                case FeedbackEventKind.BarrierBroken:
                    Play(_breakClip);
                    break;
                case FeedbackEventKind.RegionCaptured:
                    Play(_fillClip);
                    break;
                case FeedbackEventKind.LargeCapture:
                    Play(_largeCaptureClip);
                    break;
                case FeedbackEventKind.NearMiss:
                    Play(_nearMissClip);
                    break;
                case FeedbackEventKind.LevelCompleted:
                    Play(_completeClip);
                    break;
                case FeedbackEventKind.Ui:
                    Play(_uiClip);
                    break;
            }
        }

        private void Play(AudioClip clip)
        {
            if (_effectsEnabled && _audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}
