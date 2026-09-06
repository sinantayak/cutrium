using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Economy;
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
        [SerializeField] private PerformanceCoinRewardTuning
            _performanceTuning;
        [SerializeField] private CanvasGroup _rewardCanvasGroup;
        [SerializeField] private Image _rewardIcon;
        [SerializeField] private TMP_Text _rewardText;
        [SerializeField] private RectTransform _flightRoot;
        [SerializeField] private Image _flightCoinTemplate;
        [SerializeField] [Min(1)] private int _flightCoinCount = 7;
        // How long each bonus's own increment takes to count up once its
        // row appears (see BuildRevealSteps/Update()) -- the *first* step's
        // reveal time (base amount, timed to the header settling) and every
        // later step's (timed to each bonus row's own reveal) come from
        // FeedbackPresenter's row-stagger schedule, not this field.
        [SerializeField] [Min(0f)] private float _stepCountSeconds = 0.32f;
        [SerializeField] [Min(0f)] private float _rewardFadeSeconds = 0.7f;
        [SerializeField] [Min(0f)] private float _flightDelaySeconds = 0.9f;
        [SerializeField] [Min(0.01f)] private float _flightSeconds = 0.72f;
        [SerializeField] [Min(0f)] private float _flightStaggerSeconds = 0.1f;
        [SerializeField] [Min(0f)] private float _postArrivalHoldSeconds = 0.4f;
        [SerializeField] [Min(0f)] private float _settleSeconds = 0.18f;

        private readonly List<Image> _flightCoins = new List<Image>();
        private readonly List<bool> _flightCoinLaunchSoundPlayed =
            new List<bool>();
        private float _presentationStartTime;
        private bool _balanceReleased;
        private Vector3 _targetBaseScale = Vector3.one;
        private bool _targetScaleCaptured;
        // Reveal schedule for the running total: index 0 is the base
        // amount (always earned by completing the level); index k>0 is the
        // cumulative total through bonus line k-1. Times are absolute,
        // relative to _presentationStartTime -- see BuildRevealSteps.
        private int[] _cumulativeStepAmounts = Array.Empty<int>();
        private float[] _stepRevealTimes = Array.Empty<float>();
        private int _lastRevealedStepIndex;
        private float _lastStepPulseTime = float.NegativeInfinity;
        private float _countCompleteTime;
        private int _awardedBalance;

        public FirstPlayableController Controller => _controller;
        public CloudServicesBootstrap CloudServices => _cloudServices;
        public FeedbackAudioPresenter FeedbackAudio => _feedbackAudio;
        public CoinBalanceHudPresenter BalanceHud => _balanceHud;
        public PerformanceCoinRewardTuning PerformanceTuning =>
            _performanceTuning;
        public CanvasGroup RewardCanvasGroup => _rewardCanvasGroup;
        public Image RewardIcon => _rewardIcon;
        public TMP_Text RewardText => _rewardText;
        public RectTransform FlightRoot => _flightRoot;
        public Image FlightCoinTemplate => _flightCoinTemplate;
        public int FlightCoinCount => _flightCoinCount;
        public bool IsPresenting { get; private set; }
        public bool IsPresentationComplete { get; private set; } = true;
        public int LastAwardedAmount { get; private set; }
        public int LastBaseAmount { get; private set; }
        public LevelCoinRewardClaimStatus LastClaimStatus { get; private set; }
        public PerformanceCoinRewardBreakdown LastBreakdown { get; private set; }
            = PerformanceCoinRewardBreakdown.Empty;

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
            PerformanceCoinRewardTuning performanceTuning = null)
        {
            if (flightCoinCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(flightCoinCount));
            }

            _performanceTuning = performanceTuning;
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
                || _cloudServices == null)
            {
                return;
            }

            int maximumBaseAmount = _controller.CurrentLevelConfiguration
                .CompletionCoinReward;
            LevelStarCoinRewardConfiguration starRewardConfiguration =
                _performanceTuning != null
                    ? _performanceTuning.ToStarRewardConfiguration()
                    : LevelStarCoinRewardConfiguration.Default;
            int baseAmount = LevelStarCoinRewardCalculator.Calculate(
                maximumBaseAmount,
                _controller.LastCompletedStarRating,
                starRewardConfiguration);
            PerformanceCoinRewardBreakdown breakdown =
                CalculatePerformanceBreakdown();
            int amount = baseAmount + breakdown.TotalCoinAmount;
            if (amount <= 0)
            {
                LastClaimStatus = LevelCoinRewardClaimStatus.InvalidReward;
                return;
            }

            int previousBalance = _cloudServices.Coins.Balance;
            _balanceHud?.HoldDisplayedBalance(previousBalance);
            LevelCoinRewardClaimResult claim = _cloudServices.LevelRewards
                .Claim(
                    _controller.CurrentLevelRunId,
                    _controller.CurrentLevelId,
                    amount);
            LastClaimStatus = claim.Status;
            if (!claim.Awarded)
            {
                _balanceHud?.ReleaseDisplayedBalance();
                return;
            }

            LastAwardedAmount = amount;
            _awardedBalance = claim.Balance;
            LastBaseAmount = baseAmount;
            LastBreakdown = breakdown;
            BuildRevealSteps(baseAmount, breakdown);
            if (_rewardText != null)
            {
                SetTotalText(amount);
            }

            if (_rewardCanvasGroup == null)
            {
                _feedbackAudio?.PlayCoinEarn();
                CompletePresentation();
                return;
            }

            EnsureFlightCoinPool();
            _rewardCanvasGroup.alpha = 0f;
            _rewardCanvasGroup.interactable = false;
            _rewardCanvasGroup.blocksRaycasts = false;
            _rewardCanvasGroup.gameObject.SetActive(true);
            _flightCoinTemplate.gameObject.SetActive(false);
            _presentationStartTime = Time.unscaledTime;
            _balanceReleased = false;
            for (int index = 0; index < _flightCoinLaunchSoundPlayed.Count;
                index++)
            {
                _flightCoinLaunchSoundPlayed[index] = false;
            }

            IsPresenting = true;
            IsPresentationComplete = false;

            RectTransform target = _balanceHud != null
                ? _balanceHud.FlightTarget
                : null;
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
            // Cleared unconditionally (not only reset on success) so a
            // completion that doesn't end up crediting anything this call
            // -- an early return below, a rejected/duplicate claim -- never
            // leaves FeedbackPresenter displaying a stale breakdown from
            // whatever the last successful claim happened to be.
            LastBaseAmount = 0;
            LastBreakdown = PerformanceCoinRewardBreakdown.Empty;
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
            if (_stepRevealTimes.Length == 0
                || rawElapsed < _stepRevealTimes[0])
            {
                // Held invisible until the clean-board stats above have had
                // time to read -- the reward is the last line in that same
                // summary, not a competing simultaneous popup.
                _rewardCanvasGroup.alpha = 0f;
                return;
            }

            float popElapsed = rawElapsed - _stepRevealTimes[0];
            float fadeDuration = Mathf.Max(MinimumDuration, _rewardFadeSeconds);
            float fadeIn = Mathf.Clamp01(popElapsed / fadeDuration);
            float pulseScale = UpdateAccumulatingRewardText(rawElapsed);
            _rewardCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, fadeIn);
            _rewardCanvasGroup.transform.localScale =
                Vector3.one * EaseOutBack(fadeIn) * pulseScale;

            if (rawElapsed < _countCompleteTime)
            {
                // Still counting bonuses up into the running total -- the
                // fly-to-HUD sequence below only starts once every earned
                // line has landed in that total.
                return;
            }

            float elapsed = rawElapsed - _countCompleteTime;
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
            for (int index = 0; index < _flightCoins.Count; index++)
            {
                UpdateFlightCoin(index, elapsed);
            }

            if (!_balanceReleased && elapsed >= lastArrival)
            {
                _balanceReleased = true;
                _balanceHud.ReleaseDisplayedBalanceAnimated(
                    _awardedBalance);
            }

            AnimateTargetPulse(elapsed, lastArrival);
            float settle = Mathf.Max(0f, _settleSeconds);
            if (elapsed >= holdEnd + settle
                && (_balanceHud == null
                    || !_balanceHud.IsAnimatingDisplayedBalance))
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
                _flightCoinLaunchSoundPlayed.Add(false);
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
            // One "cha-ching" per coin the instant it launches (not when it
            // lands) -- staggered just like the flight itself, so the
            // counting sound stays in sync with each coin visibly taking
            // off instead of trailing behind its whole flight.
            if (!_flightCoinLaunchSoundPlayed[index] && elapsed >= startTime)
            {
                _flightCoinLaunchSoundPlayed[index] = true;
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

        // Reads this run's already-tracked performance signals straight off
        // the authoritative Metrics snapshot -- see
        // PerformanceCoinRewardCalculator for why each signal is real
        // rather than fabricated for display.
        private PerformanceCoinRewardBreakdown CalculatePerformanceBreakdown()
        {
            CoreFunLevelMetrics metrics = _controller.Metrics.Current;
            PowerConfiguration power =
                _controller.CurrentLevelConfiguration.Power;
            bool powerUpEligible = power.FreezePulseCharges > 0
                || power.InstantBarrierCharges > 0
                || power.GravityWellCharges > 0;
            PerformanceCoinRewardConfiguration configuration =
                _performanceTuning != null
                    ? _performanceTuning.ToRuntimeConfiguration()
                    : PerformanceCoinRewardConfiguration.Default;
            return PerformanceCoinRewardCalculator.Calculate(
                metrics.NearMissCount,
                metrics.PerfectCutCount,
                metrics.FailedBarriers == 0,
                powerUpEligible,
                metrics.AnyPowerUpUsed,
                configuration);
        }

        // Builds the running-total reveal schedule: step 0 is the base
        // amount, timed to when FeedbackPresenter's fixed "LEVEL COMPLETE"
        // row (row 1) starts popping in; each later step k is the
        // cumulative total through bonus line k-1, timed to when that
        // line's own bonus row (row k+1) starts popping in -- so the
        // number climbing at the bottom, and its "cha-ching" tick, land
        // right as the matching row above it begins appearing, not after
        // it has already finished fading in.
        private void BuildRevealSteps(
            int baseAmount,
            PerformanceCoinRewardBreakdown breakdown)
        {
            IReadOnlyList<PerformanceCoinRewardLine> lines = breakdown.Lines;
            _cumulativeStepAmounts = new int[lines.Count + 1];
            _stepRevealTimes = new float[lines.Count + 1];
            _cumulativeStepAmounts[0] = baseAmount;
            _stepRevealTimes[0] = RowRevealStartTime(1);

            int running = baseAmount;
            for (int i = 0; i < lines.Count; i++)
            {
                running += lines[i].CoinAmount;
                _cumulativeStepAmounts[i + 1] = running;
                _stepRevealTimes[i + 1] = RowRevealStartTime(i + 2);
            }

            _countCompleteTime = _stepRevealTimes[_stepRevealTimes.Length - 1]
                + Mathf.Max(MinimumDuration, _stepCountSeconds);
            _lastRevealedStepIndex = -1;
            _lastStepPulseTime = float.NegativeInfinity;
        }

        private static float RowRevealStartTime(int rowIndex) =>
            rowIndex * FeedbackPresenter.CompletionSummaryRowStaggerSeconds;

        // Advances the reward text through BuildRevealSteps's schedule and
        // returns a short punchy scale multiplier (layered on top of the
        // icon's own pop-in scale) for the moment each new step lands.
        private float UpdateAccumulatingRewardText(float rawElapsed)
        {
            if (_rewardText == null || _stepRevealTimes.Length == 0)
            {
                return 1f;
            }

            int displayedAmount = 0;
            int targetStepIndex = -1;
            for (int step = 0; step < _stepRevealTimes.Length; step++)
            {
                if (rawElapsed < _stepRevealTimes[step])
                {
                    break;
                }

                targetStepIndex = step;
                float stepElapsed = rawElapsed - _stepRevealTimes[step];
                float stepT = Mathf.Clamp01(
                    stepElapsed / Mathf.Max(MinimumDuration, _stepCountSeconds));
                // Ease-out cubic: monotonic (never overshoots past this
                // step's own end value) so each increment lands cleanly.
                float eased = 1f - Mathf.Pow(1f - stepT, 3f);
                int stepStart = step == 0 ? 0 : _cumulativeStepAmounts[step - 1];
                int stepEnd = _cumulativeStepAmounts[step];
                displayedAmount = Mathf.RoundToInt(
                    Mathf.Lerp(stepStart, stepEnd, eased));
            }

            if (targetStepIndex < 0)
            {
                return 1f;
            }

            SetTotalText(displayedAmount);
            if (targetStepIndex > _lastRevealedStepIndex)
            {
                _lastRevealedStepIndex = targetStepIndex;
                _lastStepPulseTime = rawElapsed;
                _feedbackAudio?.PlayCoinEarn();
            }

            const float pulseDuration = 0.22f;
            float pulseElapsed = rawElapsed - _lastStepPulseTime;
            if (pulseElapsed < 0f || pulseElapsed >= pulseDuration)
            {
                return 1f;
            }

            float pulseT = pulseElapsed / pulseDuration;
            return 1f + Mathf.Sin(pulseT * Mathf.PI) * 0.15f;
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

        private void SetTotalText(int amount)
        {
            if (_rewardText == null)
            {
                return;
            }

            _rewardText.text =
                "<color=#551A07>TOTAL:</color>\n" +
                $"<color=#FFFFFF>{amount:N0} COINS</color>";
        }

        private void CompletePresentation()
        {
            IsPresenting = false;
            IsPresentationComplete = true;
            ReleaseBalanceAndTargetScale();
            // Keep the completed total visible until the summary owner
            // dismisses the whole result card. LandmarkRevealPresenter
            // calls CancelPresentation at that exact transition.
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
