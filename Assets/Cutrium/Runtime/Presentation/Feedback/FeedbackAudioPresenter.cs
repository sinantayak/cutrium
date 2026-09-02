using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Feedback;
using Cutrium.Unity.Simulation;
using UnityEngine;

namespace Cutrium.Presentation.Feedback
{
    public struct FeedbackAudioClipSet
    {
        public AudioClip StartClip { get; set; }
        public AudioClip GrowClip { get; set; }
        public AudioClip LockClip { get; set; }
        public AudioClip BreakClip { get; set; }
        public AudioClip FillClip { get; set; }
        public AudioClip SandFillClip { get; set; }
        public AudioClip LargeCaptureClip { get; set; }
        public AudioClip NearMissClip { get; set; }
        public AudioClip ComboClip { get; set; }
        public AudioClip CompleteClip { get; set; }
        public AudioClip UiClip { get; set; }
        public AudioClip PowerFreezeActivateClip { get; set; }
        public AudioClip PowerInstantBarrierArmClip { get; set; }
        public AudioClip PowerInstantBarrierConsumeClip { get; set; }
        public AudioClip PowerGravityWellActivateClip { get; set; }
        public AudioClip PowerUnavailableClip { get; set; }
        public AudioClip HunterReactClip { get; set; }
        public AudioClip OutOfCutsClip { get; set; }
        public AudioClip OutOfLivesClip { get; set; }
        public AudioClip CoinEarnClip { get; set; }
        public AudioClip CoinSpendClip { get; set; }
    }

    [DisallowMultipleComponent]
    public sealed class FeedbackAudioPresenter : MonoBehaviour
    {
        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioSource _barrierLoopSource;
        [SerializeField] private AudioSource _gravityWellLoopSource;
        [SerializeField] private AudioClip _startClip;
        [SerializeField] private AudioClip _growClip;
        [SerializeField] private AudioClip _lockClip;
        [SerializeField] private AudioClip _breakClip;
        [SerializeField] private AudioClip _fillClip;
        [SerializeField] private AudioClip _sandFillClip;
        [SerializeField] private AudioClip _largeCaptureClip;
        [SerializeField] private AudioClip _nearMissClip;
        [SerializeField] private AudioClip _comboClip;
        [SerializeField] private AudioClip _completeClip;
        [SerializeField] private AudioClip _uiClip;
        [SerializeField] private AudioClip _powerFreezeActivateClip;
        [SerializeField] private AudioClip _powerInstantBarrierArmClip;
        [SerializeField] private AudioClip _powerInstantBarrierConsumeClip;
        [SerializeField] private AudioClip _powerGravityWellActivateClip;
        [SerializeField] private AudioClip _powerUnavailableClip;
        [SerializeField] private AudioClip _hunterReactClip;
        [SerializeField] private AudioClip _outOfCutsClip;
        [SerializeField] private AudioClip _outOfLivesClip;
        [SerializeField] private AudioClip _coinEarnClip;
        [SerializeField] private AudioClip _coinSpendClip;

        [SerializeField] private float _comboPitchStep = 0.15f;
        [SerializeField] private float _comboPitchMax = 2.5f;

        private bool _subscribed;
        private bool _effectsEnabled = true;
        private BarrierId _growPlayedFor;
        private BarrierId _instantConsumedFor;
        private bool _gravityWellWasActive;

        public FirstPlayableController Controller => _controller;

        public AudioSource AudioSource => _audioSource;

        public AudioSource BarrierLoopSource => _barrierLoopSource;

        public AudioSource GravityWellLoopSource => _gravityWellLoopSource;

        public bool EffectsEnabled => _effectsEnabled;

        public int HandledEventCount { get; private set; }

        public AudioClip StartClip => _startClip;
        public AudioClip GrowClip => _growClip;
        public AudioClip LockClip => _lockClip;
        public AudioClip BreakClip => _breakClip;
        public AudioClip FillClip => _fillClip;
        public AudioClip SandFillClip => _sandFillClip;
        public AudioClip LargeCaptureClip => _largeCaptureClip;
        public AudioClip NearMissClip => _nearMissClip;
        public AudioClip ComboClip => _comboClip;
        public AudioClip CompleteClip => _completeClip;
        public AudioClip UiClip => _uiClip;
        public AudioClip PowerFreezeActivateClip => _powerFreezeActivateClip;
        public AudioClip PowerInstantBarrierArmClip =>
            _powerInstantBarrierArmClip;
        public AudioClip PowerInstantBarrierConsumeClip =>
            _powerInstantBarrierConsumeClip;
        public AudioClip PowerGravityWellActivateClip =>
            _powerGravityWellActivateClip;
        public AudioClip PowerUnavailableClip => _powerUnavailableClip;
        public AudioClip HunterReactClip => _hunterReactClip;
        public AudioClip OutOfCutsClip => _outOfCutsClip;
        public AudioClip OutOfLivesClip => _outOfLivesClip;
        public AudioClip CoinEarnClip => _coinEarnClip;
        public AudioClip CoinSpendClip => _coinSpendClip;

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

        public void ConfigureLoopSources(
            AudioSource barrierLoopSource,
            AudioSource gravityWellLoopSource)
        {
            _barrierLoopSource = barrierLoopSource;
            _gravityWellLoopSource = gravityWellLoopSource;
        }

        public void ConfigureClips(FeedbackAudioClipSet clips)
        {
            _startClip = clips.StartClip;
            _growClip = clips.GrowClip;
            _lockClip = clips.LockClip;
            _breakClip = clips.BreakClip;
            _fillClip = clips.FillClip;
            _sandFillClip = clips.SandFillClip;
            _largeCaptureClip = clips.LargeCaptureClip;
            _nearMissClip = clips.NearMissClip;
            _comboClip = clips.ComboClip;
            _completeClip = clips.CompleteClip;
            _uiClip = clips.UiClip;
            _powerFreezeActivateClip = clips.PowerFreezeActivateClip;
            _powerInstantBarrierArmClip = clips.PowerInstantBarrierArmClip;
            _powerInstantBarrierConsumeClip =
                clips.PowerInstantBarrierConsumeClip;
            _powerGravityWellActivateClip =
                clips.PowerGravityWellActivateClip;
            _powerUnavailableClip = clips.PowerUnavailableClip;
            _hunterReactClip = clips.HunterReactClip;
            _outOfCutsClip = clips.OutOfCutsClip;
            _outOfLivesClip = clips.OutOfLivesClip;
            _coinEarnClip = clips.CoinEarnClip;
            _coinSpendClip = clips.CoinSpendClip;
        }

        public void PlayUi()
        {
            Play(_uiClip);
        }

        /// Called only by a user-visible Coin reward flow. The wallet does
        /// not call this method, preventing silent/background mutations from
        /// producing misleading feedback.
        public void PlayCoinEarn()
        {
            Play(_coinEarnClip);
        }

        /// Called only after a user-visible Coin spend succeeds. Failed or
        /// cancelled transactions intentionally stay silent.
        public void PlayCoinSpend()
        {
            Play(_coinSpendClip);
        }

        public void SetEffectsEnabled(bool enabled)
        {
            _effectsEnabled = enabled;
            if (!enabled)
            {
                _audioSource?.Stop();
                StopLoop(_barrierLoopSource);
                StopLoop(_gravityWellLoopSource);
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

        private void Update()
        {
            // Gravity Well has no discrete "ended" feedback event -- its
            // duration simply counts down -- so the sustained loop clip
            // needs to be stopped by polling for the falling edge instead.
            bool gravityWellActive = _controller != null
                && _controller.GravityWellActive;
            if (_gravityWellWasActive && !gravityWellActive)
            {
                StopLoop(_gravityWellLoopSource);
            }

            _gravityWellWasActive = gravityWellActive;
        }

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
                    _instantConsumedFor = default;
                    StopLoop(_barrierLoopSource);
                    StopLoop(_gravityWellLoopSource);
                    _gravityWellWasActive = false;
                    break;
                case FeedbackEventKind.BarrierStarted:
                    _growPlayedFor = default;
                    StopLoop(_barrierLoopSource);
                    // Instant Barrier already has its own consumed-power
                    // cue (played just before this event, same BarrierId);
                    // skip the default start clip so the two don't layer.
                    if (feedbackEvent.BarrierId != _instantConsumedFor)
                    {
                        Play(_startClip);
                    }
                    break;
                case FeedbackEventKind.BarrierGrowing:
                    if (_growPlayedFor != feedbackEvent.BarrierId)
                    {
                        _growPlayedFor = feedbackEvent.BarrierId;
                        PlayLoop(_barrierLoopSource, _growClip);
                    }
                    break;
                case FeedbackEventKind.BarrierLocked:
                    StopLoop(_barrierLoopSource);
                    Play(_lockClip);
                    break;
                case FeedbackEventKind.BarrierBroken:
                    StopLoop(_barrierLoopSource);
                    Play(_breakClip);
                    break;
                case FeedbackEventKind.RegionCaptured:
                    // Fixed capture confirmation; the escalating feel of a
                    // streak comes from the layered, pitch-rising combo cue
                    // below, not from re-pitching this clip.
                    Play(_sandFillClip != null ? _sandFillClip : _fillClip);
                    break;
                case FeedbackEventKind.LargeCapture:
                    Play(_largeCaptureClip);
                    break;
                case FeedbackEventKind.NearMiss:
                    Play(_nearMissClip);
                    break;
                case FeedbackEventKind.ComboChanged:
                    // A barrier failure/idle-timeout also resets the combo
                    // to zero via this same event kind; that has no cue of
                    // its own (BarrierBroken's own clip already covers a
                    // failed cut), so only a rising combo gets the chime.
                    if (feedbackEvent.ComboCount > 0)
                    {
                        PlayComboRise(feedbackEvent.ComboCount);
                    }
                    break;
                case FeedbackEventKind.LevelCompleted:
                    Play(_completeClip);
                    break;
                case FeedbackEventKind.Ui:
                    Play(_uiClip);
                    break;
                case FeedbackEventKind.PowerFreezePulseActivated:
                    Play(_powerFreezeActivateClip);
                    break;
                case FeedbackEventKind.PowerInstantBarrierArmed:
                    Play(_powerInstantBarrierArmClip);
                    break;
                case FeedbackEventKind.PowerInstantBarrierConsumed:
                    _instantConsumedFor = feedbackEvent.BarrierId;
                    Play(_powerInstantBarrierConsumeClip);
                    break;
                case FeedbackEventKind.PowerGravityWellActivated:
                    _gravityWellWasActive = true;
                    PlayLoop(
                        _gravityWellLoopSource,
                        _powerGravityWellActivateClip);
                    break;
                case FeedbackEventKind.PowerUnavailable:
                    Play(_powerUnavailableClip);
                    break;
                case FeedbackEventKind.HunterReacted:
                    Play(_hunterReactClip);
                    break;
                case FeedbackEventKind.CutLimitExhausted:
                    Play(_outOfCutsClip);
                    break;
                case FeedbackEventKind.BurnLimitExhausted:
                    Play(_outOfLivesClip);
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

        private void PlayLoop(AudioSource source, AudioClip clip)
        {
            if (!_effectsEnabled || source == null || clip == null)
            {
                return;
            }

            source.clip = clip;
            source.loop = true;
            source.mute = false;
            source.Play();
        }

        private void StopLoop(AudioSource source)
        {
            if (source != null && source.isPlaying)
            {
                source.Stop();
            }
        }

        private void PlayComboRise(int comboCount)
        {
            if (!_effectsEnabled
                || _audioSource == null
                || _comboClip == null)
            {
                return;
            }

            float pitch = Mathf.Min(
                1f + Mathf.Max(0, comboCount - 1) * _comboPitchStep,
                _comboPitchMax);
            float originalPitch = _audioSource.pitch;
            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(_comboClip);
            _audioSource.pitch = originalPitch;
        }
    }
}
