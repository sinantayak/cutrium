using System;
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

        public FirstPlayableController Controller => _controller;

        public RectTransform BoardFrame => _boardFrame;

        public RectTransform Visual => _visual;

        public Image Image => _image;

        public Sprite OptionalSprite => _optionalSprite;

        public float VisualLogicalDiameter => _visualLogicalDiameter;

        public ThreatId PresentedThreatId => _controller.Session.Threat.Id;

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
            SetVisualLogicalDiameter(visualLogicalDiameter);
            ApplyOptionalSprite();
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

        public void RefreshNow()
        {
            if (_controller == null
                || _controller.Session == null
                || _boardFrame == null
                || _visual == null)
            {
                return;
            }

            LogicalRect board = _controller.BoardBounds;
            ThreatState threat = _controller.Session.Threat;
            float normalizedX =
                (threat.Position.X - board.MinX) / board.Width;
            float normalizedY =
                (threat.Position.Y - board.MinY) / board.Height;
            Rect frameRect = _boardFrame.rect;
            float logicalScale = Math.Min(
                frameRect.width / board.Width,
                frameRect.height / board.Height);

            _visual.anchorMin = new Vector2(0.5f, 0.5f);
            _visual.anchorMax = new Vector2(0.5f, 0.5f);
            _visual.pivot = new Vector2(0.5f, 0.5f);
            _visual.anchoredPosition = new Vector2(
                (normalizedX - 0.5f) * frameRect.width,
                (normalizedY - 0.5f) * frameRect.height);
            float diameter = _visualLogicalDiameter * logicalScale;
            _visual.sizeDelta = new Vector2(diameter, diameter);
            ApplyOptionalSprite();
        }

        private void LateUpdate()
        {
            RefreshNow();
        }

        private void ApplyOptionalSprite()
        {
            if (_image != null && _optionalSprite != null)
            {
                _image.sprite = _optionalSprite;
            }
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
