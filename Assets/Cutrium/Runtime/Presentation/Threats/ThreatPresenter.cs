using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Threats;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Threats
{
    [DisallowMultipleComponent]
    public sealed class ThreatPresenter : MonoBehaviour
    {
        [SerializeField]
        private FirstPlayableController _controller;

        [SerializeField]
        private RectTransform _boardFrame;

        [SerializeField]
        private RectTransform _visual;

        [SerializeField]
        private Image _image;

        [SerializeField]
        private Sprite _optionalSprite;

        [SerializeField]
        private float _visualLogicalDiameter = 0.9f;

        private readonly Dictionary<ThreatId, ThreatView> _activeViews =
            new Dictionary<ThreatId, ThreatView>();
        private readonly List<ThreatId> _staleIds = new List<ThreatId>();
        private readonly List<ThreatView> _availableViews =
            new List<ThreatView>();
        private ThreatView _primaryView;

        public FirstPlayableController Controller => _controller;

        public RectTransform BoardFrame => _boardFrame;

        public RectTransform Visual => _visual;

        public Image Image => _image;

        public Sprite OptionalSprite => _optionalSprite;

        public float VisualLogicalDiameter => _visualLogicalDiameter;

        public ThreatId PresentedThreatId => _controller.Session.Threat.Id;

        public int ActiveViewCount => _activeViews.Count;

        public IReadOnlyCollection<ThreatId> PresentedThreatIds =>
            _activeViews.Keys;

        public void Configure(
            FirstPlayableController controller,
            RectTransform boardFrame,
            RectTransform visual,
            Image image,
            Sprite optionalSprite,
            float visualLogicalDiameter)
        {
            _controller = controller;
            _boardFrame = boardFrame;
            _visual = visual;
            _image = image;
            _optionalSprite = optionalSprite;
            _activeViews.Clear();
            _availableViews.Clear();
            _primaryView = null;
            SetVisualLogicalDiameter(visualLogicalDiameter);
            ApplyOptionalSprite(_image);
        }

        public void SetVisualLogicalDiameter(float visualLogicalDiameter)
        {
            if (!IsFinite(visualLogicalDiameter)
                || visualLogicalDiameter <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(visualLogicalDiameter),
                    visualLogicalDiameter,
                    "Threat visual diameter must be finite and positive.");
            }

            _visualLogicalDiameter = visualLogicalDiameter;
        }

        public bool TryGetVisual(ThreatId id, out RectTransform visual)
        {
            if (_activeViews.TryGetValue(id, out ThreatView view))
            {
                visual = view.RectTransform;
                return true;
            }

            visual = null;
            return false;
        }

        public void RefreshNow()
        {
            if (_controller == null
                || _controller.Session == null
                || _boardFrame == null
                || _visual == null
                || _image == null)
            {
                return;
            }

            SynchronizeViews();
            LogicalRect board = _controller.BoardBounds;
            Rect frameRect = _boardFrame.rect;
            float logicalScale = Math.Min(
                frameRect.width / board.Width,
                frameRect.height / board.Height);
            for (int index = 0;
                 index < _controller.Session.Threats.Count;
                 index++)
            {
                ThreatState threat = _controller.Session.Threats[index];
                ThreatView view = _activeViews[threat.Id];
                PositionView(view, threat, board, frameRect, logicalScale);
            }
        }

        private void LateUpdate()
        {
            RefreshNow();
        }

        private void SynchronizeViews()
        {
            EnsurePrimaryView();
            _staleIds.Clear();
            foreach (KeyValuePair<ThreatId, ThreatView> pair in _activeViews)
            {
                if (!SessionContains(pair.Key))
                {
                    _staleIds.Add(pair.Key);
                }
            }

            for (int index = 0; index < _staleIds.Count; index++)
            {
                ThreatId id = _staleIds[index];
                ThreatView view = _activeViews[id];
                _activeViews.Remove(id);
                view.RectTransform.gameObject.SetActive(false);
                if (view != _primaryView)
                {
                    _availableViews.Add(view);
                }
            }

            for (int index = 0;
                 index < _controller.Session.Threats.Count;
                 index++)
            {
                ThreatId id = _controller.Session.Threats[index].Id;
                if (_activeViews.ContainsKey(id))
                {
                    continue;
                }

                ThreatView view = id.Value == 1
                    ? _primaryView
                    : GetOrCreateAdditionalView();
                view.RectTransform.name = id.Value == 1
                    ? "ThreatVisual"
                    : $"ThreatVisual_{id.Value}";
                view.RectTransform.gameObject.SetActive(true);
                ApplyOptionalSprite(view.Image);
                _activeViews.Add(id, view);
            }
        }

        private void EnsurePrimaryView()
        {
            if (_primaryView == null)
            {
                _primaryView = new ThreatView(_visual, _image);
            }
        }

        private ThreatView GetOrCreateAdditionalView()
        {
            if (_availableViews.Count > 0)
            {
                int last = _availableViews.Count - 1;
                ThreatView reused = _availableViews[last];
                _availableViews.RemoveAt(last);
                return reused;
            }

            RectTransform clone = Instantiate(_visual, _visual.parent, false);
            Image image = clone.GetComponent<Image>();
            if (image == null)
            {
                throw new InvalidOperationException(
                    "A cloned threat visual requires an Image component.");
            }

            return new ThreatView(clone, image);
        }

        private bool SessionContains(ThreatId id)
        {
            for (int index = 0;
                 index < _controller.Session.Threats.Count;
                 index++)
            {
                if (_controller.Session.Threats[index].Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private void PositionView(
            ThreatView view,
            ThreatState threat,
            LogicalRect board,
            Rect frameRect,
            float logicalScale)
        {
            float normalizedX =
                (threat.Position.X - board.MinX) / board.Width;
            float normalizedY =
                (threat.Position.Y - board.MinY) / board.Height;
            RectTransform rect = view.RectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(
                (normalizedX - 0.5f) * frameRect.width,
                (normalizedY - 0.5f) * frameRect.height);
            float diameter = _visualLogicalDiameter * logicalScale;
            rect.sizeDelta = new Vector2(diameter, diameter);
            ApplyOptionalSprite(view.Image);
        }

        private void ApplyOptionalSprite(Image image)
        {
            if (image != null && _optionalSprite != null)
            {
                image.sprite = _optionalSprite;
            }
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        private sealed class ThreatView
        {
            public ThreatView(RectTransform rectTransform, Image image)
            {
                RectTransform = rectTransform;
                Image = image;
            }

            public RectTransform RectTransform { get; }

            public Image Image { get; }
        }
    }
}
