using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Feedback;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Feedback
{
    [DisallowMultipleComponent]
    public sealed class FeedbackPresenter : MonoBehaviour
    {
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

        private readonly Queue<string> _cueQueue = new Queue<string>();
        private float _cueRemaining;
        private float _activeCueDuration;
        private Color _baseFrameColor;
        private float _emphasis;
        private bool _subscribed;
        private bool _completionSummaryVisible;
        private bool _cueTextStyleCaptured;
        private int _baseCueFontSize;
        private bool _baseCueResizeTextForBestFit;
        private int _baseCueResizeTextMinSize;
        private int _baseCueResizeTextMaxSize;
        private float _baseCueLineSpacing;
        private bool _baseCueSupportRichText;

        private const int CompletionSummaryFontSize = 54;
        private const int CompletionSummaryMinimumFontSize = 30;
        private const float CompletionSummaryLineSpacing = 0.88f;
        private const float CompletionSummaryFadeInFraction = 0.18f;
        private const float CompletionSummaryFadeOutFraction = 0.30f;
        private const string CompletionHeadingColor = "#F4C15D";
        private const float CompletionBackgroundHorizontalPadding = 28f;
        private const float CompletionBackgroundVerticalPadding = 18f;
        private static readonly Color CompletionBackgroundColor =
            new Color(0.09f, 0.045f, 0.02f, 0.74f);

        public FirstPlayableController Controller => _controller;

        public FeedbackTuningDefinition Tuning => _tuning;

        public Text CueLabel => _cueLabel;

        public CanvasGroup CueCanvasGroup => _cueCanvasGroup;

        public Image CompletionSummaryBackground =>
            _completionSummaryBackground;

        public int ReceivedEventCount { get; private set; }

        public FeedbackEventKind LastEventKind { get; private set; }

        public int PendingCueCount => _cueQueue.Count;

        public float GrowthIntensity { get; private set; }

        public bool CompletionSummaryVisible =>
            _completionSummaryVisible;

        public void ShowCompletionSummary(float duration)
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

            var metrics = _controller.Metrics.Current;
            int capturedPercent = Mathf.FloorToInt(
                _controller.Session.CapturedFraction * 100f + 0.5f);
            int cuts = metrics.BarrierAttempts;
            string cutLabel = cuts == 1 ? "CUT" : "CUTS";
            _cueQueue.Clear();
            ApplyCompletionSummaryTextStyle();
            ShowCompletionSummaryBackground();
            _completionSummaryVisible = true;
            ShowCue(
                $"<color={CompletionHeadingColor}>" +
                $"LEVEL {_controller.CurrentLevelNumber} COMPLETE" +
                "</color>\n" +
                $"CAPTURED {capturedPercent}%  •  {cuts} {cutLabel}\n" +
                $"TIME {metrics.ElapsedSeconds:0.0}s  •  " +
                $"BROKEN {metrics.FailedBarriers}",
                duration);
            if (_cueCanvasGroup != null)
            {
                _cueCanvasGroup.alpha = 0f;
            }
        }

        public void DismissCompletionSummary()
        {
            if (!_completionSummaryVisible)
            {
                return;
            }

            _cueQueue.Clear();
            _cueRemaining = 0f;
            HideCue();
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
            RestoreCueTextStyle();
            _controller = controller;
            _tuning = tuning;
            _cueLabel = cueLabel;
            _cueCanvasGroup = cueCanvasGroup;
            _boardFrameGraphic = boardFrameGraphic;
            _cueTextStyleCaptured = false;
            CaptureCueTextStyle();
            _baseFrameColor = _boardFrameGraphic != null
                ? _boardFrameGraphic.color
                : Color.white;
            HideCue();
            if (isActiveAndEnabled && Application.isPlaying)
            {
                Subscribe();
            }
        }

        public void ConfigureCompletionSummaryBackgroundForSetup(
            Image completionSummaryBackground)
        {
            _completionSummaryBackground = completionSummaryBackground
                ?? throw new ArgumentNullException(
                    nameof(completionSummaryBackground));
            ConfigureCompletionSummaryBackground();
            _completionSummaryBackground.gameObject.SetActive(false);
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

            if (_cueRemaining > 0f)
            {
                _cueRemaining = Mathf.Max(0f, _cueRemaining - delta);
                if (_cueCanvasGroup != null)
                {
                    float duration = _activeCueDuration;
                    if (_completionSummaryVisible && duration > 0f)
                    {
                        float elapsedFraction = Mathf.Clamp01(
                            1f - (_cueRemaining / duration));
                        float fadeIn = Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(
                                0f,
                                CompletionSummaryFadeInFraction,
                                elapsedFraction));
                        float fadeOut = Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(
                                0f,
                                CompletionSummaryFadeOutFraction,
                                1f - elapsedFraction));
                        _cueCanvasGroup.alpha = Mathf.Min(fadeIn, fadeOut);
                    }
                    else
                    {
                        _cueCanvasGroup.alpha = duration <= 0f
                            ? 0f
                            : Mathf.Clamp01(
                                _cueRemaining / (duration * 0.25f));
                    }
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
            if (_completionSummaryVisible)
            {
                RestoreCueTextStyle();
            }

            if (_completionSummaryBackground != null)
            {
                _completionSummaryBackground.gameObject.SetActive(false);
            }

            if (_cueCanvasGroup != null)
            {
                _cueCanvasGroup.alpha = 0f;
                _cueCanvasGroup.interactable = false;
                _cueCanvasGroup.blocksRaycasts = false;
            }

            _completionSummaryVisible = false;
        }

        private void CaptureCueTextStyle()
        {
            if (_cueLabel == null || _cueTextStyleCaptured)
            {
                return;
            }

            _baseCueFontSize = _cueLabel.fontSize;
            _baseCueResizeTextForBestFit = _cueLabel.resizeTextForBestFit;
            _baseCueResizeTextMinSize = _cueLabel.resizeTextMinSize;
            _baseCueResizeTextMaxSize = _cueLabel.resizeTextMaxSize;
            _baseCueLineSpacing = _cueLabel.lineSpacing;
            _baseCueSupportRichText = _cueLabel.supportRichText;
            _cueTextStyleCaptured = true;
        }

        private void ApplyCompletionSummaryTextStyle()
        {
            CaptureCueTextStyle();
            if (_cueLabel == null)
            {
                return;
            }

            _cueLabel.fontSize = CompletionSummaryFontSize;
            _cueLabel.resizeTextForBestFit = true;
            _cueLabel.resizeTextMinSize = CompletionSummaryMinimumFontSize;
            _cueLabel.resizeTextMaxSize = CompletionSummaryFontSize;
            _cueLabel.lineSpacing = CompletionSummaryLineSpacing;
            _cueLabel.supportRichText = true;
        }

        private void ShowCompletionSummaryBackground()
        {
            EnsureCompletionSummaryBackground();
            if (_completionSummaryBackground == null)
            {
                return;
            }

            ConfigureCompletionSummaryBackground();
            _completionSummaryBackground.gameObject.SetActive(true);
            _completionSummaryBackground.rectTransform.SetSiblingIndex(
                Mathf.Max(0, _cueLabel.transform.GetSiblingIndex() - 1));
        }

        private void EnsureCompletionSummaryBackground()
        {
            if (_completionSummaryBackground != null || _cueLabel == null)
            {
                return;
            }

            Transform parent = _cueLabel.transform.parent;
            Transform existing = parent.Find("CompletionSummaryBackground");
            if (existing != null)
            {
                _completionSummaryBackground = existing.GetComponent<Image>();
                if (_completionSummaryBackground == null)
                {
                    _completionSummaryBackground =
                        existing.gameObject.AddComponent<Image>();
                }
            }

            if (_completionSummaryBackground == null)
            {
                var backgroundObject = new GameObject(
                    "CompletionSummaryBackground",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                backgroundObject.transform.SetParent(parent, false);
                _completionSummaryBackground =
                    backgroundObject.GetComponent<Image>();
            }
        }

        private void ConfigureCompletionSummaryBackground()
        {
            if (_completionSummaryBackground == null || _cueLabel == null)
            {
                return;
            }

            RectTransform cueRect = _cueLabel.rectTransform;
            RectTransform backgroundRect =
                _completionSummaryBackground.rectTransform;
            backgroundRect.anchorMin = cueRect.anchorMin;
            backgroundRect.anchorMax = cueRect.anchorMax;
            backgroundRect.pivot = cueRect.pivot;
            backgroundRect.anchoredPosition = cueRect.anchoredPosition;
            backgroundRect.offsetMin = cueRect.offsetMin - new Vector2(
                CompletionBackgroundHorizontalPadding,
                CompletionBackgroundVerticalPadding);
            backgroundRect.offsetMax = cueRect.offsetMax + new Vector2(
                CompletionBackgroundHorizontalPadding,
                CompletionBackgroundVerticalPadding);
            backgroundRect.localScale = Vector3.one;
            backgroundRect.localRotation = Quaternion.identity;
            _completionSummaryBackground.sprite = null;
            _completionSummaryBackground.type = Image.Type.Simple;
            _completionSummaryBackground.color = CompletionBackgroundColor;
            _completionSummaryBackground.raycastTarget = false;
        }

        private void RestoreCueTextStyle()
        {
            if (_cueLabel == null || !_cueTextStyleCaptured)
            {
                return;
            }

            _cueLabel.fontSize = _baseCueFontSize;
            _cueLabel.resizeTextForBestFit = _baseCueResizeTextForBestFit;
            _cueLabel.resizeTextMinSize = _baseCueResizeTextMinSize;
            _cueLabel.resizeTextMaxSize = _baseCueResizeTextMaxSize;
            _cueLabel.lineSpacing = _baseCueLineSpacing;
            _cueLabel.supportRichText = _baseCueSupportRichText;
        }

        private float CueDuration => _tuning != null
            ? _tuning.LabelSeconds
            : 0.65f;
    }
}
