using System;
using System.Collections.Generic;
using System.Linq;
using Cutrium.Gameplay.Economy;
using Cutrium.Gameplay.Feedback;
using Cutrium.Gameplay.Session;
using Cutrium.Unity.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Feedback
{
    [DisallowMultipleComponent]
    public sealed class FeedbackPresenter : MonoBehaviour
    {
        /// One line of the clean-board completion summary: an itemized
        /// stat (or the header) with its own CanvasGroup so
        /// ShowCompletionSummary can reveal rows one at a time instead of
        /// fading the whole block in at once.
        [Serializable]
        public sealed class CompletionSummaryRow
        {
            [SerializeField] private TMP_Text _text;
            [SerializeField] private CanvasGroup _group;
            [SerializeField] private Image _icon;
            [SerializeField] private TMP_Text _amountText;

            /// Header-row constructor: a single centered label, no coin
            /// icon/amount.
            public CompletionSummaryRow(TMP_Text text, CanvasGroup group)
                : this(text, group, null, null)
            {
            }

            /// Bonus-row constructor: a left-aligned label plus a trailing
            /// coin icon and "+N" amount, e.g. "PERFECT CUT x2   [coin] +40".
            public CompletionSummaryRow(
                TMP_Text text,
                CanvasGroup group,
                Image icon,
                TMP_Text amountText)
            {
                _text = text;
                _group = group;
                _icon = icon;
                _amountText = amountText;
            }

            public TMP_Text Text => _text;
            public CanvasGroup Group => _group;
            public Image Icon => _icon;
            public TMP_Text AmountText => _amountText;
            public bool HasAmount => _icon != null && _amountText != null;
        }

        [SerializeField]
        private FirstPlayableController _controller;

        [SerializeField]
        private FeedbackTuningDefinition _tuning;

        [SerializeField]
        private Text _cueLabel;

        [SerializeField]
        private CanvasGroup _cueCanvasGroup;

        [SerializeField]
        private Graphic _boardFrameGraphic;

        [SerializeField]
        private Image _completionSummaryBackground;

        [SerializeField]
        private CanvasGroup _summaryListGroup;

        [SerializeField]
        private CompletionSummaryRow[] _summaryRows;

        [SerializeField]
        private Image[] _completionStars;

        [SerializeField]
        private Sprite _filledStarSprite;

        [SerializeField]
        private Sprite _emptyStarSprite;

        private readonly Queue<string> _cueQueue = new Queue<string>();
        private float _cueRemaining;
        private float _activeCueDuration;
        private Color _baseFrameColor;
        private float _emphasis;
        private bool _subscribed;
        private bool _completionSummaryVisible;
        private float _summaryStartTime;
        private float _summaryDuration;

        // Header + fixed base-reward row + up to 4 optional bonus rows.
        public const int CompletionSummaryRowCount = 6;
        // Public: LevelCoinRewardPresenter reuses these to time its own
        // reward-total count-up so each increment lands in sync with the
        // bonus row it corresponds to, without duplicating the stagger
        // schedule in a second place.
        public const float CompletionSummaryRowStaggerSeconds = 0.35f;
        public const float CompletionSummaryRowFadeSeconds = 0.28f;
        private const float CompletionSummaryFadeOutSeconds = 0.4f;
        private const float StarRevealStartSeconds = 0.05f;
        private const float StarRevealStaggerSeconds = 0.13f;
        private const float StarRevealSeconds = 0.3f;

        public FirstPlayableController Controller => _controller;

        public FeedbackTuningDefinition Tuning => _tuning;

        public Text CueLabel => _cueLabel;

        public CanvasGroup CueCanvasGroup => _cueCanvasGroup;

        public Image CompletionSummaryBackground =>
            _completionSummaryBackground;

        public CanvasGroup SummaryListGroup => _summaryListGroup;

        public IReadOnlyList<CompletionSummaryRow> SummaryRows => _summaryRows;

        public IReadOnlyList<Image> CompletionStars => _completionStars;

        public Sprite FilledStarSprite => _filledStarSprite;

        public Sprite EmptyStarSprite => _emptyStarSprite;

        public int ReceivedEventCount { get; private set; }

        public FeedbackEventKind LastEventKind { get; private set; }

        public int PendingCueCount => _cueQueue.Count;

        public float GrowthIntensity { get; private set; }

        public bool CompletionSummaryVisible =>
            _completionSummaryVisible;

        public void ShowCompletionSummary(
            float duration,
            int baseAmount,
            IReadOnlyList<PerformanceCoinRewardLine> bonusLines,
            int starRating = 0)
        {
            if (float.IsNaN(duration)
                || float.IsInfinity(duration)
                || duration < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            if (_controller == null || _controller.Session == null)
            {
                return;
            }

            if (_summaryRows == null
                || _summaryRows.Length != CompletionSummaryRowCount
                || _summaryListGroup == null
                || _completionSummaryBackground == null)
            {
                return;
            }

            // The completion summary shares its on-screen band with the
            // ephemeral single-line cues (LOCKED, BIG CUT, ...). Clear any
            // cue still fading out so it never overlaps the stats card.
            _cueQueue.Clear();
            _cueRemaining = 0f;
            HideCue();

            SetRowText(
                0,
                $"LEVEL {_controller.CurrentLevelNumber}\nCOMPLETE");
            _summaryRows[0].Group.gameObject.SetActive(true);
            ConfigureStars(starRating);

            // Row 1 is the fixed base completion reward (every completion
            // earns it, unlike the conditional bonus slots below).
            bool hasBaseReward = baseAmount > 0;
            if (hasBaseReward)
            {
                SetRowText(1, "LEVEL COMPLETE");
                SetRowAmount(1, baseAmount);
            }

            _summaryRows[1].Group.gameObject.SetActive(hasBaseReward);

            // Rows 2..(count-1) are a fixed number of optional bonus slots,
            // one per performance-bonus line actually earned this run (see
            // PerformanceCoinRewardCalculator) -- packed contiguously in
            // the order they were computed, any unused trailing slot
            // deactivated so the layout group collapses its gap instead of
            // leaving a blank row.
            int bonusSlotCount = _summaryRows.Length - 2;
            int lineCount = bonusLines?.Count ?? 0;
            for (int slot = 0; slot < bonusSlotCount; slot++)
            {
                int rowIndex = slot + 2;
                bool hasLine = slot < lineCount;
                if (hasLine)
                {
                    PerformanceCoinRewardLine line = bonusLines[slot];
                    SetRowText(rowIndex, FormatBonusLabel(line));
                    SetRowAmount(rowIndex, line.CoinAmount);
                }

                _summaryRows[rowIndex].Group.gameObject.SetActive(hasLine);
            }

            for (int index = 0; index < _summaryRows.Length; index++)
            {
                _summaryRows[index].Group.alpha = 0f;
                _summaryRows[index].Group.transform.localScale = Vector3.zero;
            }

            _completionSummaryBackground.gameObject.SetActive(true);
            _summaryListGroup.gameObject.SetActive(true);
            _summaryListGroup.alpha = 1f;
            _summaryStartTime = Time.unscaledTime;
            _summaryDuration = duration;
            _completionSummaryVisible = true;
        }

        public void DismissCompletionSummary()
        {
            if (!_completionSummaryVisible)
            {
                return;
            }

            HideCompletionSummary();
        }

        public void SetBaseFrameColor(Color color)
        {
            _baseFrameColor = color;
            if (_boardFrameGraphic != null && _emphasis <= 0f)
            {
                _boardFrameGraphic.color = color;
            }
        }

        public void Configure(
            FirstPlayableController controller,
            FeedbackTuningDefinition tuning,
            Text cueLabel,
            CanvasGroup cueCanvasGroup,
            Graphic boardFrameGraphic)
        {
            Unsubscribe();
            _controller = controller;
            _tuning = tuning;
            _cueLabel = cueLabel;
            _cueCanvasGroup = cueCanvasGroup;
            _boardFrameGraphic = boardFrameGraphic;
            _baseFrameColor = _boardFrameGraphic != null
                ? _boardFrameGraphic.color
                : Color.white;
            HideCue();
            if (isActiveAndEnabled && Application.isPlaying)
            {
                Subscribe();
            }
        }

        public void ConfigureCompletionSummaryForSetup(
            Image completionSummaryBackground,
            CanvasGroup summaryListGroup,
            CompletionSummaryRow[] summaryRows,
            Image[] completionStars = null,
            Sprite filledStarSprite = null,
            Sprite emptyStarSprite = null)
        {
            _completionSummaryBackground = completionSummaryBackground
                ?? throw new ArgumentNullException(
                    nameof(completionSummaryBackground));
            _summaryListGroup = summaryListGroup
                ?? throw new ArgumentNullException(nameof(summaryListGroup));
            if (summaryRows == null
                || summaryRows.Length != CompletionSummaryRowCount)
            {
                throw new ArgumentException(
                    "The completion summary needs exactly " +
                    $"{CompletionSummaryRowCount} rows.",
                    nameof(summaryRows));
            }

            for (int index = 0; index < summaryRows.Length; index++)
            {
                CompletionSummaryRow row = summaryRows[index];
                if (row?.Text == null || row.Group == null)
                {
                    throw new ArgumentException(
                        "Every completion summary row needs both a Text " +
                        "and a CanvasGroup.",
                        nameof(summaryRows));
                }

                // Row 0 is the plain "LEVEL N COMPLETE" header; every other
                // slot is an itemized bonus row and needs its own coin icon
                // and amount label.
                if (index > 0 && !row.HasAmount)
                {
                    throw new ArgumentException(
                        "Every completion summary bonus row needs an Icon " +
                        "and an AmountText.",
                        nameof(summaryRows));
                }
            }

            _summaryRows = summaryRows;
            bool hasAnyStarConfiguration = completionStars != null
                || filledStarSprite != null
                || emptyStarSprite != null;
            if (hasAnyStarConfiguration
                && (completionStars == null
                    || completionStars.Length
                        != LevelStarRatingCalculator.MaximumStars
                    || completionStars.Any(star => star == null)
                    || filledStarSprite == null
                    || emptyStarSprite == null))
            {
                throw new ArgumentException(
                    "The completion summary needs exactly three star " +
                    "Images plus filled and empty star sprites.",
                    nameof(completionStars));
            }

            _completionStars = completionStars;
            _filledStarSprite = filledStarSprite;
            _emptyStarSprite = emptyStarSprite;
            ConfigureCompletionSummaryBackground();
            _completionSummaryBackground.gameObject.SetActive(false);
            _summaryListGroup.gameObject.SetActive(false);
            for (int index = 0; index < _summaryRows.Length; index++)
            {
                _summaryRows[index].Group.alpha = 0f;
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            GrowthIntensity = Mathf.MoveTowards(
                GrowthIntensity,
                0f,
                delta * 5f);
            _emphasis = Mathf.MoveTowards(_emphasis, 0f, delta * 6f);
            if (_boardFrameGraphic != null)
            {
                _boardFrameGraphic.color = Color.Lerp(
                    _baseFrameColor,
                    Color.white,
                    _emphasis);
            }

            if (_completionSummaryVisible)
            {
                UpdateCompletionSummaryReveal();
            }

            if (_cueRemaining > 0f)
            {
                _cueRemaining = Mathf.Max(0f, _cueRemaining - delta);
                if (_cueCanvasGroup != null)
                {
                    float duration = _activeCueDuration;
                    _cueCanvasGroup.alpha = duration <= 0f
                        ? 0f
                        : Mathf.Clamp01(
                            _cueRemaining / (duration * 0.25f));
                }
            }

            if (_cueRemaining <= 0f)
            {
                if (_cueQueue.Count > 0)
                {
                    ShowCue(_cueQueue.Dequeue(), CueDuration);
                }
                else
                {
                    HideCue();
                }
            }
        }

        private void UpdateCompletionSummaryReveal()
        {
            float elapsed = Time.unscaledTime - _summaryStartTime;
            UpdateStarReveal(elapsed);
            for (int index = 0; index < _summaryRows.Length; index++)
            {
                float start = index * CompletionSummaryRowStaggerSeconds;
                float t = Mathf.Clamp01(
                    (elapsed - start) / CompletionSummaryRowFadeSeconds);
                CanvasGroup group = _summaryRows[index].Group;
                group.alpha = Mathf.SmoothStep(0f, 1f, t);
                group.transform.localScale = Vector3.one * EaseOutBack(t);
            }

            float fadeOutStart = Mathf.Max(
                0f,
                _summaryDuration - CompletionSummaryFadeOutSeconds);
            _summaryListGroup.alpha = elapsed >= fadeOutStart
                ? 1f - Mathf.Clamp01(
                    (elapsed - fadeOutStart)
                        / Mathf.Max(0.01f, CompletionSummaryFadeOutSeconds))
                : 1f;

            if (elapsed >= _summaryDuration)
            {
                HideCompletionSummary();
            }
        }

        private void HideCompletionSummary()
        {
            _completionSummaryVisible = false;
            if (_summaryListGroup != null)
            {
                _summaryListGroup.gameObject.SetActive(false);
            }

            if (_completionSummaryBackground != null)
            {
                _completionSummaryBackground.gameObject.SetActive(false);
            }
        }

        // Standard "ease out back" curve: overshoots past 1 then settles,
        // giving each row a small pop instead of a flat linear fade.
        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            float shifted = t - 1f;
            return 1f
                + (overshoot + 1f) * shifted * shifted * shifted
                + overshoot * shifted * shifted;
        }

        private void SetRowText(int index, string text)
        {
            TMP_Text label = _summaryRows[index].Text;
            if (label != null)
            {
                label.text = text;
            }
        }

        private void SetRowAmount(int index, int amount)
        {
            TMP_Text amountLabel = _summaryRows[index].AmountText;
            if (amountLabel != null)
            {
                amountLabel.text = $"+{amount:N0}";
            }

            if (_summaryRows[index].Icon != null)
            {
                _summaryRows[index].Icon.gameObject.SetActive(true);
            }
        }

        private static string FormatBonusLabel(PerformanceCoinRewardLine line)
        {
            string label;
            switch (line.Kind)
            {
                case PerformanceCoinRewardKind.NearMiss:
                    label = "NEAR MISS";
                    break;
                case PerformanceCoinRewardKind.PerfectCut:
                    label = "PERFECT CUT";
                    break;
                case PerformanceCoinRewardKind.NoLifeLost:
                    label = "NO LIFE LOST";
                    break;
                case PerformanceCoinRewardKind.NoPowerUpUsed:
                    label = "NO POWER-UP USED";
                    break;
                default:
                    label = string.Empty;
                    break;
            }

            string countSuffix = line.OccurrenceCount > 1
                ? $" x{line.OccurrenceCount}"
                : string.Empty;
            return $"{label}{countSuffix}";
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
            ReceivedEventCount++;
            LastEventKind = feedbackEvent.Kind;
            switch (feedbackEvent.Kind)
            {
                case FeedbackEventKind.SessionReset:
                    _cueQueue.Clear();
                    _cueRemaining = 0f;
                    GrowthIntensity = 0f;
                    HideCue();
                    break;
                case FeedbackEventKind.BarrierGrowing:
                    GrowthIntensity = 1f;
                    break;
                case FeedbackEventKind.BarrierLocked:
                    EnqueueCue("LOCKED");
                    _emphasis = 0.55f;
                    break;
                case FeedbackEventKind.BarrierBroken:
                    EnqueueCue("TRY AGAIN");
                    break;
                case FeedbackEventKind.LargeCapture:
                    EnqueueCue("BIG CUT");
                    _emphasis = 1f;
                    break;
                case FeedbackEventKind.NearMiss:
                    EnqueueCue("CLOSE!");
                    break;
                case FeedbackEventKind.ComboChanged:
                    if (feedbackEvent.ComboCount >= 2)
                    {
                        EnqueueCue($"COMBO x{feedbackEvent.ComboCount}");
                    }
                    break;
                case FeedbackEventKind.LevelCompleted:
                    // The logical completion event precedes the final sand
                    // reveal. LandmarkRevealPresenter explicitly releases a
                    // richer summary after the clean board is visible.
                    _emphasis = 1f;
                    break;
            }
        }

        private void EnqueueCue(string text)
        {
            if (_cueRemaining <= 0f && _cueQueue.Count == 0)
            {
                ShowCue(text, CueDuration);
            }
            else
            {
                _cueQueue.Enqueue(text);
            }
        }

        private void ShowCue(string text, float duration)
        {
            if (_cueLabel != null)
            {
                _cueLabel.text = text;
            }

            if (_cueCanvasGroup != null)
            {
                _cueCanvasGroup.alpha = 1f;
                _cueCanvasGroup.interactable = false;
                _cueCanvasGroup.blocksRaycasts = false;
            }

            _activeCueDuration = duration;
            _cueRemaining = duration;
        }

        private void HideCue()
        {
            if (_cueCanvasGroup != null)
            {
                _cueCanvasGroup.alpha = 0f;
                _cueCanvasGroup.interactable = false;
                _cueCanvasGroup.blocksRaycasts = false;
            }
        }

        private void ConfigureCompletionSummaryBackground()
        {
            if (_completionSummaryBackground == null)
            {
                return;
            }

            _completionSummaryBackground.raycastTarget = false;
        }

        private void ConfigureStars(int starRating)
        {
            if (_completionStars == null
                || _completionStars.Length
                    != LevelStarRatingCalculator.MaximumStars
                || _filledStarSprite == null
                || _emptyStarSprite == null)
            {
                return;
            }

            int clamped = Mathf.Clamp(
                starRating,
                0,
                LevelStarRatingCalculator.MaximumStars);
            for (int index = 0; index < _completionStars.Length; index++)
            {
                Image star = _completionStars[index];
                star.sprite = index < clamped
                    ? _filledStarSprite
                    : _emptyStarSprite;
                star.color = Color.white;
                star.rectTransform.localScale = Vector3.zero;
                star.gameObject.SetActive(true);
            }
        }

        private void UpdateStarReveal(float elapsed)
        {
            if (_completionStars == null)
            {
                return;
            }

            for (int index = 0; index < _completionStars.Length; index++)
            {
                Image star = _completionStars[index];
                if (star == null || !star.gameObject.activeSelf)
                {
                    continue;
                }

                float start = StarRevealStartSeconds
                    + index * StarRevealStaggerSeconds;
                float t = Mathf.Clamp01(
                    (elapsed - start) / StarRevealSeconds);
                star.rectTransform.localScale =
                    Vector3.one * EaseOutBack(t);
            }
        }

        private float CueDuration => _tuning != null
            ? _tuning.LabelSeconds
            : 0.65f;
    }
}
