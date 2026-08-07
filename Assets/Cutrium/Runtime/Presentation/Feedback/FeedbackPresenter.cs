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

        private readonly Queue<string> _cueQueue = new Queue<string>();
        private float _cueRemaining;
        private Color _baseFrameColor;
        private float _emphasis;
        private bool _subscribed;

        public FirstPlayableController Controller => _controller;

        public FeedbackTuningDefinition Tuning => _tuning;

        public Text CueLabel => _cueLabel;

        public CanvasGroup CueCanvasGroup => _cueCanvasGroup;

        public int ReceivedEventCount { get; private set; }

        public FeedbackEventKind LastEventKind { get; private set; }

        public int PendingCueCount => _cueQueue.Count;

        public float GrowthIntensity { get; private set; }

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
                    float duration = CueDuration;
                    _cueCanvasGroup.alpha = duration <= 0f
                        ? 0f
                        : Mathf.Clamp01(_cueRemaining / (duration * 0.25f));
                }
            }

            if (_cueRemaining <= 0f)
            {
                if (_cueQueue.Count > 0)
                {
                    ShowCue(_cueQueue.Dequeue());
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
                    EnqueueCue("LEVEL CLEAR");
                    _emphasis = 1f;
                    break;
            }
        }

        private void EnqueueCue(string text)
        {
            if (_cueRemaining <= 0f && _cueQueue.Count == 0)
            {
                ShowCue(text);
            }
            else
            {
                _cueQueue.Enqueue(text);
            }
        }

        private void ShowCue(string text)
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

            _cueRemaining = CueDuration;
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

        private float CueDuration => _tuning != null
            ? _tuning.LabelSeconds
            : 0.65f;
    }
}
