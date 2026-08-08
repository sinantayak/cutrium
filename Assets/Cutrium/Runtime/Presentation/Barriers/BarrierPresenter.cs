using System;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Feedback;
using Cutrium.Gameplay.Geometry;
using Cutrium.Presentation.Theme;
using Cutrium.Unity.Input;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Barriers
{
    [DisallowMultipleComponent]
    public sealed class BarrierPresenter : MonoBehaviour
    {
        private static readonly Color HorizontalPreview =
            new Color(0.55f, 0.82f, 0.92f, 0.32f);
        private static readonly Color VerticalPreview =
            new Color(0.78f, 0.68f, 0.9f, 0.32f);
        private static readonly Color GrowingColor =
            new Color(0.62f, 0.86f, 0.94f, 0.9f);
        private static readonly Color LockedColor =
            new Color(0.95f, 0.78f, 0.35f, 1f);
        private static readonly Color FailedColor =
            new Color(0.92f, 0.4f, 0.42f, 0.82f);

        [SerializeField]
        private FirstPlayableController _controller;

        [SerializeField]
        private BarrierGestureAdapter _gesture;

        [SerializeField]
        private RectTransform _boardFrame;

        [SerializeField]
        private RectTransform _preview;

        [SerializeField]
        private Image _previewImage;

        [SerializeField]
        private RectTransform _negativeHalf;

        [SerializeField]
        private Image _negativeImage;

        [SerializeField]
        private RectTransform _positiveHalf;

        [SerializeField]
        private Image _positiveImage;

        [SerializeField]
        private RectTransform _failureFeedback;

        [SerializeField]
        private Image _failureImage;

        [SerializeField]
        private float _visualLogicalThickness = 0.13f;

        [SerializeField]
        private float _failureFeedbackSeconds = 0.16f;

        private int _observedFailureCount;
        private float _failureHideTime;
        private BarrierState? _failureSnapshot;
        private bool _feedbackSubscribed;
        private BarrierVisualStyle _themeStyle;
        private bool _hasThemeStyle;
        private Image _negativeCap;
        private Image _positiveCap;
        private Image _originJoint;

        public FirstPlayableController Controller => _controller;
        public BarrierGestureAdapter Gesture => _gesture;
        public RectTransform BoardFrame => _boardFrame;
        public RectTransform Preview => _preview;
        public RectTransform NegativeHalf => _negativeHalf;
        public RectTransform PositiveHalf => _positiveHalf;
        public RectTransform FailureFeedback => _failureFeedback;
        public float VisualLogicalThickness => _visualLogicalThickness;
        public bool FailureFeedbackVisible =>
            _failureFeedback != null && _failureFeedback.gameObject.activeSelf;
        public FeedbackEventKind LastFeedbackEventKind { get; private set; }
        public int FeedbackEventCount { get; private set; }
        public BarrierVisualStyle ThemeStyle => _themeStyle;
        public bool HasThemeStyle => _hasThemeStyle;

        public void Configure(
            FirstPlayableController controller,
            BarrierGestureAdapter gesture,
            RectTransform boardFrame,
            RectTransform preview,
            Image previewImage,
            RectTransform negativeHalf,
            Image negativeImage,
            RectTransform positiveHalf,
            Image positiveImage,
            RectTransform failureFeedback,
            Image failureImage,
            float visualLogicalThickness,
            float failureFeedbackSeconds)
        {
            UnsubscribeFeedback();
            _controller = controller;
            _gesture = gesture;
            _boardFrame = boardFrame;
            _preview = preview;
            _previewImage = previewImage;
            _negativeHalf = negativeHalf;
            _negativeImage = negativeImage;
            _positiveHalf = positiveHalf;
            _positiveImage = positiveImage;
            _failureFeedback = failureFeedback;
            _failureImage = failureImage;
            SetVisualLogicalThickness(visualLogicalThickness);
            if (!IsFinite(failureFeedbackSeconds) || failureFeedbackSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(failureFeedbackSeconds));
            }

            _failureFeedbackSeconds = failureFeedbackSeconds;
            if (isActiveAndEnabled && Application.isPlaying)
            {
                SubscribeFeedback();
            }
        }

        public void ApplyTheme(BarrierVisualStyle style)
        {
            _themeStyle = style;
            _hasThemeStyle = true;
            _previewImage.sprite = style.PreviewSprite;
            _negativeImage.sprite = style.BodySprite;
            _positiveImage.sprite = style.BodySprite;
            _failureImage.sprite = style.CapSprite ?? style.BodySprite;
            _negativeCap = GetOrCreateCap(_negativeHalf, "NegativeCap");
            _positiveCap = GetOrCreateCap(_positiveHalf, "PositiveCap");
            _negativeCap.sprite = style.CapSprite;
            _positiveCap.sprite = style.CapSprite;
            _negativeCap.color = style.GrowingColor;
            _positiveCap.color = style.GrowingColor;
            _originJoint = GetOrCreateCap(_boardFrame, "BarrierOriginJoint");
            _originJoint.sprite = style.CapSprite;
            _originJoint.color = style.GrowingColor;
        }

        public void SetVisualLogicalThickness(float value)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _visualLogicalThickness = value;
        }

        public void RefreshNow()
        {
            if (_controller == null
                || _controller.Session == null
                || _boardFrame == null)
            {
                return;
            }

            ObserveFailure();
            RenderPreview();
            RenderBarrier();
            RenderFailure();
        }

        public void ClearFailureFeedbackNow()
        {
            _failureSnapshot = null;
            if (_failureFeedback != null)
            {
                _failureFeedback.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            RefreshNow();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                SubscribeFeedback();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFeedback();
        }

        private void SubscribeFeedback()
        {
            if (_feedbackSubscribed || _controller == null)
            {
                return;
            }

            _controller.FeedbackEventRaised += OnFeedbackEvent;
            _feedbackSubscribed = true;
        }

        private void UnsubscribeFeedback()
        {
            if (_feedbackSubscribed && _controller != null)
            {
                _controller.FeedbackEventRaised -= OnFeedbackEvent;
            }

            _feedbackSubscribed = false;
        }

        private void OnFeedbackEvent(FeedbackEvent feedbackEvent)
        {
            LastFeedbackEventKind = feedbackEvent.Kind;
            FeedbackEventCount++;
            if (feedbackEvent.Kind == FeedbackEventKind.SessionReset)
            {
                _observedFailureCount = 0;
                ClearFailureFeedbackNow();
                return;
            }

            if (feedbackEvent.Kind != FeedbackEventKind.BarrierBroken)
            {
                return;
            }

            _observedFailureCount = _controller.Session.FailedBarrierCount;
            _failureSnapshot = _controller.Session.LastBarrierSnapshot;
            _failureHideTime = Time.unscaledTime + _failureFeedbackSeconds;
        }

        private void RenderPreview()
        {
            bool gestureCanPreview = _gesture != null
                && _gesture.IsTracking
                && _gesture.SelectedOrientation != BarrierOrientation.None
                && _controller.Session.LevelStatus
                    == Cutrium.Gameplay.Session.CaptureLevelStatus.Playing
                && !_controller.Session.ActiveBarrier.HasValue;
            BarrierStartResult validation = default;
            RoomState room = default;
            if (gestureCanPreview)
            {
                validation = _controller.ValidateBarrierIntent(
                    new BarrierIntent(
                        _gesture.Origin,
                        _gesture.SelectedOrientation));
            }

            bool visible = gestureCanPreview
                && validation.Accepted
                && _controller.Session.Board.TryGetActiveRoom(
                    validation.Barrier.ParentRoomId,
                    out room);
            _preview.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            LogicalRect bounds = room.Bounds;
            LogicalPoint origin = _gesture.Origin;
            LogicalPoint start = _gesture.SelectedOrientation
                == BarrierOrientation.Horizontal
                    ? new LogicalPoint(bounds.MinX, origin.Y)
                    : new LogicalPoint(origin.X, bounds.MinY);
            LogicalPoint end = _gesture.SelectedOrientation
                == BarrierOrientation.Horizontal
                    ? new LogicalPoint(bounds.MaxX, origin.Y)
                    : new LogicalPoint(origin.X, bounds.MaxY);
            RenderSegment(
                _preview,
                start,
                end,
                _visualLogicalThickness * 0.55f);
            _previewImage.color = _gesture.SelectedOrientation
                == BarrierOrientation.Horizontal
                    ? _hasThemeStyle
                        ? _themeStyle.PreviewColor
                        : HorizontalPreview
                    : _hasThemeStyle
                        ? _themeStyle.PreviewColor
                        : VerticalPreview;
        }

        private void RenderBarrier()
        {
            if (!_controller.Session.ActiveBarrier.HasValue)
            {
                _negativeHalf.gameObject.SetActive(false);
                _positiveHalf.gameObject.SetActive(false);
                if (_originJoint != null)
                {
                    _originJoint.gameObject.SetActive(false);
                }

                return;
            }

            BarrierState barrier = _controller.Session.ActiveBarrier.Value;
            _negativeHalf.gameObject.SetActive(true);
            _positiveHalf.gameObject.SetActive(true);
            RenderSegment(
                _negativeHalf,
                barrier.NegativeEndpoint,
                barrier.Origin,
                _visualLogicalThickness);
            RenderSegment(
                _positiveHalf,
                barrier.Origin,
                barrier.PositiveEndpoint,
                _visualLogicalThickness);
            Color stateColor = barrier.Lifecycle == BarrierLifecycle.Locked
                ? _hasThemeStyle ? _themeStyle.LockedColor : LockedColor
                : _hasThemeStyle ? _themeStyle.GrowingColor : GrowingColor;
            _negativeImage.color = stateColor;
            _positiveImage.color = stateColor;
            if (_negativeCap != null && _positiveCap != null)
            {
                _negativeCap.color = stateColor;
                _positiveCap.color = stateColor;
                PositionCap(_negativeCap, false, _negativeHalf.sizeDelta.y);
                PositionCap(_positiveCap, true, _positiveHalf.sizeDelta.y);
            }

            // A dedicated round joint at the growth origin covers the seam
            // where the negative/positive halves meet, so the barrier reads
            // as one continuous elegant line rather than two blunt segments.
            if (_originJoint != null)
            {
                _originJoint.gameObject.SetActive(true);
                _originJoint.color = stateColor;
                RectTransform jointRect = (RectTransform)_originJoint.transform;
                jointRect.anchorMin = new Vector2(0.5f, 0.5f);
                jointRect.anchorMax = new Vector2(0.5f, 0.5f);
                jointRect.pivot = new Vector2(0.5f, 0.5f);
                jointRect.anchoredPosition = LogicalToAnchored(barrier.Origin);
                jointRect.localRotation = Quaternion.identity;
                float size = _visualLogicalThickness * GetLogicalScale();
                jointRect.sizeDelta = new Vector2(size, size);
            }
        }

        private void ObserveFailure()
        {
            int failureCount = _controller.Session.FailedBarrierCount;
            if (failureCount == _observedFailureCount)
            {
                return;
            }

            _observedFailureCount = failureCount;
            _failureSnapshot = _controller.Session.LastBarrierSnapshot;
            _failureHideTime = Time.unscaledTime + _failureFeedbackSeconds;
        }

        private void RenderFailure()
        {
            bool visible = _failureSnapshot.HasValue
                && Time.unscaledTime <= _failureHideTime;
            _failureFeedback.gameObject.SetActive(visible);
            if (!visible)
            {
                _failureSnapshot = null;
                return;
            }

            BarrierState failed = _failureSnapshot.Value;
            float scale = GetLogicalScale();
            Vector2 position = LogicalToAnchored(failed.Origin);
            _failureFeedback.anchorMin = new Vector2(0.5f, 0.5f);
            _failureFeedback.anchorMax = new Vector2(0.5f, 0.5f);
            _failureFeedback.pivot = new Vector2(0.5f, 0.5f);
            _failureFeedback.anchoredPosition = position;
            float size = _visualLogicalThickness * scale * 2.5f;
            _failureFeedback.sizeDelta = new Vector2(size, size);
            _failureImage.color = _hasThemeStyle
                ? _themeStyle.BreakColor
                : FailedColor;
        }

        private static Image GetOrCreateCap(
            RectTransform parent,
            string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<Image>()
                    ?? existing.gameObject.AddComponent<Image>();
            }

            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void PositionCap(
            Image image,
            bool positive,
            float thickness)
        {
            RectTransform rect = (RectTransform)image.transform;
            float anchor = positive ? 1f : 0f;
            rect.anchorMin = new Vector2(anchor, 0.5f);
            rect.anchorMax = new Vector2(anchor, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.sizeDelta = new Vector2(thickness, thickness);
        }

        private void RenderSegment(
            RectTransform visual,
            LogicalPoint start,
            LogicalPoint end,
            float thickness)
        {
            Vector2 startPosition = LogicalToAnchored(start);
            Vector2 endPosition = LogicalToAnchored(end);
            Vector2 delta = endPosition - startPosition;
            visual.anchorMin = new Vector2(0.5f, 0.5f);
            visual.anchorMax = new Vector2(0.5f, 0.5f);
            visual.pivot = new Vector2(0.5f, 0.5f);
            visual.anchoredPosition = (startPosition + endPosition) * 0.5f;
            visual.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            visual.sizeDelta = new Vector2(
                delta.magnitude,
                thickness * GetLogicalScale());
        }

        private Vector2 LogicalToAnchored(LogicalPoint point)
        {
            LogicalRect board = _controller.BoardBounds;
            Rect rect = _boardFrame.rect;
            return new Vector2(
                ((point.X - board.MinX) / board.Width - 0.5f) * rect.width,
                ((point.Y - board.MinY) / board.Height - 0.5f) * rect.height);
        }

        private float GetLogicalScale()
        {
            LogicalRect board = _controller.BoardBounds;
            Rect rect = _boardFrame.rect;
            return Math.Min(rect.width / board.Width, rect.height / board.Height);
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
