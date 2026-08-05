using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Capture
{
    [DisallowMultipleComponent]
    public sealed class CaptureBoardPresenter : MonoBehaviour
    {
        private static readonly Color CapturedColor =
            new Color(0.2f, 0.72f, 0.68f, 0.7f);
        private static readonly Color CompletedBarrierColor =
            new Color(0.96f, 0.89f, 0.48f, 1f);

        [SerializeField]
        private FirstPlayableController _controller;

        [SerializeField]
        private RectTransform _boardFrame;

        [SerializeField]
        private RectTransform _capturedRegionRoot;

        [SerializeField]
        private RectTransform _completedBarrierRoot;

        [SerializeField]
        private float _completedBarrierLogicalThickness = 0.22f;

        private readonly List<RectTransform> _capturedViews =
            new List<RectTransform>();
        private readonly List<RectTransform> _barrierViews =
            new List<RectTransform>();

        public FirstPlayableController Controller => _controller;

        public RectTransform BoardFrame => _boardFrame;

        public RectTransform CapturedRegionRoot => _capturedRegionRoot;

        public RectTransform CompletedBarrierRoot => _completedBarrierRoot;

        public int VisibleCapturedRegionCount { get; private set; }

        public int VisibleCompletedBarrierCount { get; private set; }

        public void Configure(
            FirstPlayableController controller,
            RectTransform boardFrame,
            RectTransform capturedRegionRoot,
            RectTransform completedBarrierRoot,
            float completedBarrierLogicalThickness)
        {
            if (!IsFinite(completedBarrierLogicalThickness)
                || completedBarrierLogicalThickness <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedBarrierLogicalThickness));
            }

            _controller = controller;
            _boardFrame = boardFrame;
            _capturedRegionRoot = capturedRegionRoot;
            _completedBarrierRoot = completedBarrierRoot;
            _completedBarrierLogicalThickness =
                completedBarrierLogicalThickness;
        }

        public void RefreshNow()
        {
            if (_controller == null
                || _controller.Session == null
                || _boardFrame == null
                || _capturedRegionRoot == null
                || _completedBarrierRoot == null)
            {
                return;
            }

            RenderCapturedRooms(_controller.Session.Board.CapturedRooms);
            RenderCompletedBarriers(
                _controller.Session.Board.CompletedBarriers);
        }

        private void LateUpdate()
        {
            RefreshNow();
        }

        private void RenderCapturedRooms(IReadOnlyList<RoomState> rooms)
        {
            EnsureViews(_capturedViews, _capturedRegionRoot, rooms.Count,
                "CapturedRegion", CapturedColor);
            for (int index = 0; index < _capturedViews.Count; index++)
            {
                bool visible = index < rooms.Count;
                _capturedViews[index].gameObject.SetActive(visible);
                if (visible)
                {
                    RenderRectangle(_capturedViews[index], rooms[index].Bounds);
                }
            }

            VisibleCapturedRegionCount = rooms.Count;
        }

        private void RenderCompletedBarriers(
            IReadOnlyList<BarrierState> barriers)
        {
            EnsureViews(_barrierViews, _completedBarrierRoot, barriers.Count,
                "CompletedBarrier", CompletedBarrierColor);
            for (int index = 0; index < _barrierViews.Count; index++)
            {
                bool visible = index < barriers.Count;
                _barrierViews[index].gameObject.SetActive(visible);
                if (visible)
                {
                    BarrierState barrier = barriers[index];
                    RenderSegment(
                        _barrierViews[index],
                        barrier.NegativeEndpoint,
                        barrier.PositiveEndpoint);
                }
            }

            VisibleCompletedBarrierCount = barriers.Count;
        }

        private static void EnsureViews(
            ICollection<RectTransform> views,
            Transform parent,
            int count,
            string prefix,
            Color color)
        {
            while (views.Count < count)
            {
                int index = views.Count;
                var gameObject = new GameObject(
                    prefix + (index + 1),
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                var rect = (RectTransform)gameObject.transform;
                rect.SetParent(parent, false);
                Image image = gameObject.GetComponent<Image>();
                image.color = color;
                image.raycastTarget = false;
                views.Add(rect);
            }
        }

        private void RenderRectangle(RectTransform visual, LogicalRect bounds)
        {
            Vector2 min = LogicalToAnchored(bounds.Min);
            Vector2 max = LogicalToAnchored(bounds.Max);
            visual.anchorMin = new Vector2(0.5f, 0.5f);
            visual.anchorMax = new Vector2(0.5f, 0.5f);
            visual.pivot = new Vector2(0.5f, 0.5f);
            visual.anchoredPosition = (min + max) * 0.5f;
            visual.localRotation = Quaternion.identity;
            visual.sizeDelta = new Vector2(max.x - min.x, max.y - min.y);
        }

        private void RenderSegment(
            RectTransform visual,
            LogicalPoint start,
            LogicalPoint end)
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
                _completedBarrierLogicalThickness * GetLogicalScale());
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
