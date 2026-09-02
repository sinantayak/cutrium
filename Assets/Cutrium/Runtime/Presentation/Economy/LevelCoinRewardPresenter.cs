using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Session;
using Cutrium.Presentation.Feedback;
using Cutrium.Unity.Services;
using Cutrium.Unity.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Economy
{
    /// Presents one authoritative level-completion reward. Wallet credit and
    /// persistence happen at presentation start; only the HUD readout waits
    /// for the cosmetic Coin flight to arrive.
    [DisallowMultipleComponent]
    public sealed class LevelCoinRewardPresenter : MonoBehaviour
    {
        private const float MinimumDuration = 0.01f;

        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private CloudServicesBootstrap _cloudServices;
        [SerializeField] private FeedbackAudioPresenter _feedbackAudio;
        [SerializeField] private CoinBalanceHudPresenter _balanceHud;
        [SerializeField] private CanvasGroup _rewardCanvasGroup;
        [SerializeField] private Image _rewardIcon;
        [SerializeField] private TMP_Text _rewardText;
        [SerializeField] private RectTransform _flightRoot;
        [SerializeField] private Image _flightCoinTemplate;
        [SerializeField] [Min(1)] private int _flightCoinCount = 7;
        [SerializeField] [Min(0f)] private float _revealDelaySeconds = 1.8f;
        // Also doubles as the 0-to-amount count-up duration for the reward
        // text (see Update()) -- long enough to read as a real count, not a
        // flicker.
        [SerializeField] [Min(0f)] private float _rewardFadeSeconds = 0.7f;
        [SerializeField] [Min(0f)] private float _flightDelaySeconds = 0.9f;
        [SerializeField] [Min(0.01f)] private float _flightSeconds = 0.72f;
        [SerializeField] [Min(0f)] private float _flightStaggerSeconds = 0.1f;
        [SerializeField] [Min(0f)] private float _postArrivalHoldSeconds = 0.4f;
        [SerializeField] [Min(0f)] private float _settleSeconds = 0.18f;

        private readonly List<Image> _flightCoins = new List<Image>();
        private readonly List<bool> _flightCoinArrivalSoundPlayed =
            new List<bool>();
        private float _presentationStartTime;
        private bool _balanceReleased;
        private Vector3 _targetBaseScale = Vector3.one;
        private bool _targetScaleCaptured;

        public FirstPlayableController Controller => _controller;
        public CloudServicesBootstrap CloudServices => _cloudServices;
        public FeedbackAudioPresenter FeedbackAudio => _feedbackAudio;
        public CoinBalanceHudPresenter BalanceHud => _balanceHud;
        public CanvasGroup RewardCanvasGroup => _rewardCanvasGroup;
        public Image RewardIcon => _rewardIcon;
        public TMP_Text RewardText => _rewardText;
        public RectTransform FlightRoot => _flightRoot;
        public Image FlightCoinTemplate => _flightCoinTemplate;
        public int FlightCoinCount => _flightCoinCount;
        public bool IsPresenting { get; private set; }
        public bool IsPresentationComplete { get; private set; } = true;
        public int LastAwardedAmount { get; private set; }
        public LevelCoinRewardClaimStatus LastClaimStatus { get; private set; }

        public void ConfigureForSetup(
            FirstPlayableController controller,
            CloudServicesBootstrap cloudServices,
            FeedbackAudioPresenter feedbackAudio,
            CoinBalanceHudPresenter balanceHud,
            CanvasGroup rewardCanvasGroup,
            Image rewardIcon,
            TMP_Text rewardText,
            RectTransform flightRoot,
            Image flightCoinTemplate,
            int flightCoinCount = 7,
            float revealDelaySeconds = 1.8f)
        {
            if (flightCoinCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(flightCoinCount));
            }

            if (revealDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(revealDelaySeconds));
            }

            _revealDelaySeconds = revealDelaySeconds;
            _controller = controller
                ?? throw new ArgumentNullException(nameof(controller));
            _cloudServices = cloudServices
                ?? throw new ArgumentNullException(nameof(cloudServices));
            _feedbackAudio = feedbackAudio
                ?? throw new ArgumentNullException(nameof(feedbackAudio));
            _balanceHud = balanceHud
                ?? throw new ArgumentNullException(nameof(balanceHud));
            _rewardCanvasGroup = rewardCanvasGroup
                ?? throw new ArgumentNullException(nameof(rewardCanvasGroup));
            _rewardIcon = rewardIcon
                ?? throw new ArgumentNullException(nameof(rewardIcon));
            _rewardText = rewardText
                ?? throw new ArgumentNullException(nameof(rewardText));
            _flightRoot = flightRoot
                ?? throw new ArgumentNullException(nameof(flightRoot));
            _flightCoinTemplate = flightCoinTemplate
                ?? throw new ArgumentNullException(nameof(flightCoinTemplate));
            _flightCoinCount = flightCoinCount;
            HidePresentationVisuals();
        }

        public void BeginCompletionPresentation()
        {
            if (IsPresenting)
            {
                return;
            }

            CancelPresentation();
            if (_controller == null
                || _controller.Session == null
                || _controller.Session.LevelStatus
                    != CaptureLevelStatus.Completed
                || _cloudServices == null
                || _balanceHud == null)
            {
                return;
            }

            int amount = _controller.CurrentLevelConfiguration
                .CompletionCoinReward;
            if (amount <= 0)
            {
                LastClaimStatus = LevelCoinRewardClaimStatus.InvalidReward;
                return;
            }

            int previousBalance = _cloudServices.Coins.Balance;
            _balanceHud.HoldDisplayedBalance(previousBalance);
            LevelCoinRewardClaimResult claim = _cloudServices.LevelRewards
                .Claim(
                    _controller.CurrentLevelRunId,
                    _controller.CurrentLevelId,
                    amount);
            LastClaimStatus = claim.Status;
            if (!claim.Awarded)
            {
                _balanceHud.ReleaseDisplayedBalance();
                return;
            }

            LastAwardedAmount = amount;
            if (_rewardText != null)
            {
                _rewardText.text = $"+{amount:N0} COINS";
            }

            EnsureFlightCoinPool();
            _rewardCanvasGroup.alpha = 0f;
            _rewardCanvasGroup.interactable = false;
            _rewardCanvasGroup.blocksRaycasts = false;
            _rewardCanvasGroup.gameObject.SetActive(true);
            _flightCoinTemplate.gameObject.SetActive(false);
            _presentationStartTime = Time.unscaledTime;
            _balanceReleased = false;
            for (int index = 0; index < _flightCoinArrivalSoundPlayed.Count;
                index++)
            {
                _flightCoinArrivalSoundPlayed[index] = false;
            }

            IsPresenting = true;
            IsPresentationComplete = false;

            RectTransform target = _balanceHud.FlightTarget;
            if (target != null)
            {
                _targetBaseScale = target.localScale;
                _targetScaleCaptured = true;
            }

            if (!HasUsableFlightVisuals())
            {
                _feedbackAudio?.PlayCoinEarn();
                CompletePresentation();
            }
        }

        public void CancelPresentation()
        {
            IsPresenting = false;
            IsPresentationComplete = true;
            ReleaseBalanceAndTargetScale();
            HidePresentationVisuals();
        }

        private void Update()
        {
            if (!IsPresenting)
            {
                return;
            }

            float rawElapsed = Mathf.Max(
                0f,
                Time.unscaledTime - _presentationStartTime);
            if (rawElapsed < _revealDelaySeconds)
            {
                // Held invisible until the clean-board stats above have had
                // time to read -- the reward is the last line in that same
                // summary, not a competing simultaneous popup.
                _rewardCanvasGroup.alpha = 0f;
                return;
            }

            float elapsed = rawElapsed - _revealDelaySeconds;
            float fadeDuration = Mathf.Max(MinimumDuration, _rewardFadeSeconds);
            float fadeIn = Mathf.Clamp01(elapsed / fadeDuration);
            _rewardCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, fadeIn);
            _rewardCanvasGroup.transform.localScale =
                Vector3.one * EaseOutBack(fadeIn);
            if (_rewardText != null)
            {
                // Ease-out cubic: monotonic (never overshoots past the
                // final amount, unlike the icon's bouncy pop) so the
                // counted number always lands cleanly on the true total.
                float countEased = 1f - Mathf.Pow(1f - fadeIn, 3f);
                int displayedAmount = Mathf.RoundToInt(
                    Mathf.Lerp(0, LastAwardedAmount, countEased));
                _rewardText.text = $"+{displayedAmount:N0} COINS";
            }

            float lastArrival = _flightDelaySeconds
                + Mathf.Max(0, _flightCoinCount - 1)
                    * _flightStaggerSeconds
                + _flightSeconds;
            // Hold the arrived reward fully visible for a beat before
            // fading it out -- the level-complete landmark screen only
            // opens once this whole presentation finishes, so this pause is
            // what gives the player a moment to see the balance actually
            // land before the scene moves on.
            float holdEnd = lastArrival
                + Mathf.Max(0f, _postArrivalHoldSeconds);
            if (elapsed >= holdEnd)
            {
                float fadeOut = Mathf.InverseLerp(
                    holdEnd + Mathf.Max(MinimumDuration, _settleSeconds),
                    holdEnd,
                    elapsed);
                _rewardCanvasGroup.alpha *= fadeOut;
            }
            for (int index = 0; index < _flightCoins.Count; index++)
            {
                UpdateFlightCoin(index, elapsed);
            }

            if (!_balanceReleased && elapsed >= lastArrival)
            {
                _balanceReleased = true;
                _balanceHud.ReleaseDisplayedBalance();
            }

            AnimateTargetPulse(elapsed, lastArrival);
            float settle = Mathf.Max(0f, _settleSeconds);
            if (elapsed >= holdEnd + settle)
            {
                CompletePresentation();
            }
        }

        private void EnsureFlightCoinPool()
        {
            if (_flightCoinTemplate == null || _flightRoot == null)
            {
                return;
            }

            while (_flightCoins.Count < _flightCoinCount)
            {
                Image coin = Instantiate(_flightCoinTemplate, _flightRoot);
                coin.name = "RewardFlightCoin" + (_flightCoins.Count + 1);
                coin.raycastTarget = false;
                coin.gameObject.SetActive(false);
                _flightCoins.Add(coin);
                _flightCoinArrivalSoundPlayed.Add(false);
            }

            for (int index = 0; index < _flightCoins.Count; index++)
            {
                _flightCoins[index].gameObject.SetActive(false);
            }
        }

        private void UpdateFlightCoin(int index, float elapsed)
        {
            Image coin = _flightCoins[index];
            float startTime = _flightDelaySeconds
                + index * _flightStaggerSeconds;
            float arrivalTime = startTime
                + Mathf.Max(MinimumDuration, _flightSeconds);
            // One "cha-ching" per coin as it lands in the HUD, staggered
            // just like the flight itself -- reads as coins being counted
            // into the balance rather than one cue for the whole batch.
            if (!_flightCoinArrivalSoundPlayed[index] && elapsed >= arrivalTime)
            {
                _flightCoinArrivalSoundPlayed[index] = true;
                _feedbackAudio?.PlayCoinEarn();
            }

            float normalized = Mathf.InverseLerp(
                startTime,
                arrivalTime,
                elapsed);
            bool active = elapsed >= startTime && normalized < 1f;
            coin.gameObject.SetActive(active);
            if (!active)
            {
                return;
            }

            float eased = 1f - Mathf.Pow(1f - normalized, 3f);
            float phase = index * 2.399963f;
            Vector3 start = _rewardIcon.rectTransform.position
                + new Vector3(
                    Mathf.Cos(phase) * 34f,
                    Mathf.Sin(phase) * 18f,
                    0f);
            Vector3 end = _balanceHud.FlightTarget.position;
            Vector3 control = (start + end) * 0.5f
                + Vector3.up * (90f + index * 7f);
            float inverse = 1f - eased;
            coin.rectTransform.position = inverse * inverse * start
                + 2f * inverse * eased * control
                + eased * eased * end;
            float scale = Mathf.Sin(normalized * Mathf.PI);
            coin.rectTransform.localScale = Vector3.one
                * Mathf.Lerp(0.55f, 1.05f, scale);
            coin.rectTransform.localRotation = Quaternion.Euler(
                0f,
                0f,
                (index % 2 == 0 ? 1f : -1f) * normalized * 220f);
        }

        private void AnimateTargetPulse(float elapsed, float arrivalTime)
        {
            RectTransform target = _balanceHud.FlightTarget;
            if (target == null)
            {
                return;
            }

            float pulseDuration = Mathf.Max(MinimumDuration, _settleSeconds);
            float pulse = Mathf.InverseLerp(
                arrivalTime,
                arrivalTime + pulseDuration,
                elapsed);
            float scale = pulse > 0f && pulse < 1f
                ? 1f + Mathf.Sin(pulse * Mathf.PI) * 0.18f
                : 1f;
            target.localScale = _targetBaseScale * scale;
        }

        // Standard "ease out back" curve: overshoots past 1 then settles,
        // giving the reward row a small pop instead of a flat linear fade.
        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            float shifted = t - 1f;
            return 1f
                + (overshoot + 1f) * shifted * shifted * shifted
                + overshoot * shifted * shifted;
        }

        private bool HasUsableFlightVisuals() =>
            _rewardCanvasGroup != null
            && _rewardIcon != null
            && _flightRoot != null
            && _flightCoinTemplate != null
            && _balanceHud != null
            && _balanceHud.FlightTarget != null
            && _flightCoins.Count >= _flightCoinCount;

        private void CompletePresentation()
        {
            IsPresenting = false;
            IsPresentationComplete = true;
            ReleaseBalanceAndTargetScale();
            HidePresentationVisuals();
        }

        private void ReleaseBalanceAndTargetScale()
        {
            _balanceHud?.ReleaseDisplayedBalance();
            RectTransform target = _balanceHud != null
                ? _balanceHud.FlightTarget
                : null;
            if (target != null && _targetScaleCaptured)
            {
                target.localScale = _targetBaseScale;
            }

            _targetScaleCaptured = false;
        }

        private void HidePresentationVisuals()
        {
            if (_rewardCanvasGroup != null)
            {
                _rewardCanvasGroup.alpha = 0f;
                _rewardCanvasGroup.interactable = false;
                _rewardCanvasGroup.blocksRaycasts = false;
                _rewardCanvasGroup.transform.localScale = Vector3.one;
            }

            if (_flightCoinTemplate != null)
            {
                _flightCoinTemplate.gameObject.SetActive(false);
            }

            for (int index = 0; index < _flightCoins.Count; index++)
            {
                if (_flightCoins[index] != null)
                {
                    _flightCoins[index].gameObject.SetActive(false);
                }
            }
        }
    }
}
