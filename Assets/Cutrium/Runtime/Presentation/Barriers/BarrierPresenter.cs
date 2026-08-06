using System;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
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
            new Color(0.3f, 0.85f, 1f, 0.45f);
        private static readonly Color VerticalPreview =
            new Color(0.8f, 0.5f, 1f, 0.45f);
        private static readonly Color GrowingColor =
            new Color(0.35f, 0.9f, 1f, 1f);
        private static readonly Color LockedColor =
            new Color(0.35f, 1f, 0.55f, 1f);
        private static readonly Color FailedColor =
            new Color(1f, 0.25f, 0.3f, 0.85f);

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
        private float _visualLogicalThickness = 0.22f;

        [SerializeField]
        private float _failureFeedbackSeconds = 0.16f;

        private int _observedFailureCount;
        private float _failureHideTime;
        private BarrierState? _failureSnapshot;

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
                    ? HorizontalPreview
                    : VerticalPreview;
        }

        private void RenderBarrier()
        {
            if (!_controller.Session.ActiveBarrier.HasValue)
            {
                _negativeHalf.gameObject.SetActive(false);
                _positiveHalf.gameObject.SetActive(false);
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
                ? LockedColor
                : GrowingColor;
            _negativeImage.color = stateColor;
            _positiveImage.color = stateColor;
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
            _failureImage.color = FailedColor;
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
