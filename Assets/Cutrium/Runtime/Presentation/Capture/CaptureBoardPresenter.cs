using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Presentation.Theme;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Capture
{
    [DisallowMultipleComponent]
    public sealed class CaptureBoardPresenter : MonoBehaviour
    {
        private static readonly Color CapturedColor =
            new Color(0.85f, 0.72f, 0.42f, 0.16f);
        private static readonly Color CompletedBarrierColor =
            new Color(0.95f, 0.82f, 0.5f, 0.55f);

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

        [SerializeField]
        [Min(0f)]
        private float _captureRevealDuration = 0.18f;

        private readonly List<RectTransform> _capturedViews =
            new List<RectTransform>();
        private readonly List<RectTransform> _barrierViews =
            new List<RectTransform>();
        private readonly List<CanvasGroup> _capturedGroups =
            new List<CanvasGroup>();
        private readonly List<float> _capturedRevealStarts =
            new List<float>();
        private CaptureVisualStyle _themeStyle;
        private bool _hasThemeStyle;

        public FirstPlayableController Controller => _controller;

        public RectTransform BoardFrame => _boardFrame;

        public RectTransform CapturedRegionRoot => _capturedRegionRoot;

        public RectTransform CompletedBarrierRoot => _completedBarrierRoot;

        public int VisibleCapturedRegionCount { get; private set; }

        public int VisibleCompletedBarrierCount { get; private set; }

        public float CaptureRevealDuration => _captureRevealDuration;

        public CaptureVisualStyle ThemeStyle => _themeStyle;

        public bool HasThemeStyle => _hasThemeStyle;

        public bool AllVisibleCapturesRevealed
        {
            get
            {
                for (int index = 0;
                    index < VisibleCapturedRegionCount;
                    index++)
                {
                    if (_capturedGroups[index].alpha < 1f)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

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

        public void ConfigureFeedbackRevealForSetup(float duration)
        {
            if (!IsFinite(duration) || duration < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            _captureRevealDuration = duration;
        }

        public void ApplyTheme(CaptureVisualStyle style)
        {
            _themeStyle = style;
            _hasThemeStyle = true;
            for (int index = 0; index < _capturedViews.Count; index++)
            {
                ApplyCapturedStyle(
                    _capturedViews[index].GetComponent<Image>());
            }

            for (int index = 0; index < _barrierViews.Count; index++)
            {
                ApplyCompletedBarrierStyle(
                    _barrierViews[index].GetComponent<Image>());
            }
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
            EnsureCapturedGroups();
            for (int index = 0; index < _capturedViews.Count; index++)
            {
                bool visible = index < rooms.Count;
                bool wasVisible = _capturedViews[index].gameObject.activeSelf;
                _capturedViews[index].gameObject.SetActive(visible);
                if (visible)
                {
                    ApplyCapturedStyle(
                        _capturedViews[index].GetComponent<Image>());
                    if (!wasVisible)
                    {
                        _capturedRevealStarts[index] = Time.unscaledTime;
                        _capturedGroups[index].alpha = 0f;
                    }

                    RenderRectangle(_capturedViews[index], rooms[index].Bounds);
                    float elapsed =
                        Time.unscaledTime - _capturedRevealStarts[index];
                    _capturedGroups[index].alpha =
                        _captureRevealDuration <= 0f
                            ? 1f
                            : Mathf.Clamp01(
                                elapsed / _captureRevealDuration);
                }
            }

            VisibleCapturedRegionCount = rooms.Count;
        }

        private void EnsureCapturedGroups()
        {
            while (_capturedGroups.Count < _capturedViews.Count)
            {
                RectTransform view = _capturedViews[_capturedGroups.Count];
                CanvasGroup group = view.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = view.gameObject.AddComponent<CanvasGroup>();
                }

                group.interactable = false;
                group.blocksRaycasts = false;
                group.alpha = 0f;
                _capturedGroups.Add(group);
                _capturedRevealStarts.Add(Time.unscaledTime);
            }
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
                    ApplyCompletedBarrierStyle(
                        _barrierViews[index].GetComponent<Image>());
                    BarrierState barrier = barriers[index];
                    RenderSegment(
                        _barrierViews[index],
                        barrier.NegativeEndpoint,
                        barrier.PositiveEndpoint);
                }
            }

            VisibleCompletedBarrierCount = barriers.Count;
        }

        private void ApplyCapturedStyle(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = _hasThemeStyle ? _themeStyle.Sprite : null;
            image.material = _hasThemeStyle ? _themeStyle.Material : null;
            image.color = _hasThemeStyle
                ? _themeStyle.Color
                : CapturedColor;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        private void ApplyCompletedBarrierStyle(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = _hasThemeStyle
                ? _themeStyle.CompletedBarrierSprite
                : null;
            image.material = null;
            image.color = _hasThemeStyle
                ? _themeStyle.CompletedBarrierColor
                : CompletedBarrierColor;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
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
