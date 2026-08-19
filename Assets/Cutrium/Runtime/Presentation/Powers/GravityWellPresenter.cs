using Cutrium.Gameplay.Geometry;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Powers
{
    [DisallowMultipleComponent]
    public sealed class GravityWellPresenter : MonoBehaviour
    {
        [SerializeField]
        private FirstPlayableController _controller;

        [SerializeField]
        private RectTransform _boardFrame;

        [SerializeField]
        private RectTransform _cueRoot;

        [SerializeField]
        private Image _cueImage;

        [SerializeField]
        private RectTransform _iconRoot;

        [SerializeField]
        private GravityWellRangeGraphic _rangeGraphic;

        [SerializeField]
        private float _rotationDegreesPerSecond = 72f;

        [SerializeField]
        private Color _rangeBaseColor = new Color(1f, 0.68f, 0.18f, 0.9f);

        public FirstPlayableController Controller => _controller;
        public RectTransform BoardFrame => _boardFrame;
        public RectTransform CueRoot => _cueRoot;
        public Image CueImage => _cueImage;
        public RectTransform IconRoot => _iconRoot;
        public GravityWellRangeGraphic RangeGraphic => _rangeGraphic;

        public void ConfigureForSetup(
            FirstPlayableController controller,
            RectTransform boardFrame,
            RectTransform cueRoot,
            RectTransform iconRoot,
            Image cueImage,
            GravityWellRangeGraphic rangeGraphic)
        {
            _controller = controller;
            _boardFrame = boardFrame;
            _cueRoot = cueRoot;
            _iconRoot = iconRoot;
            _cueImage = cueImage;
            _rangeGraphic = rangeGraphic;
            _rangeBaseColor = _rangeGraphic != null
                ? _rangeGraphic.color
                : Color.white;
            RefreshNow();
        }

        public void RefreshNow()
        {
            if (_cueRoot == null)
            {
                return;
            }

            bool visible = _controller != null
                && _controller.GravityWellActive
                && _controller.GravityWellPosition.HasValue
                && _boardFrame != null;
            if (_cueImage != null)
            {
                _cueImage.enabled = visible;
            }

            if (_rangeGraphic != null)
            {
                _rangeGraphic.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            _cueRoot.SetAsLastSibling();
            LogicalPoint point = _controller.GravityWellPosition.Value;
            LogicalRect bounds = _controller.BoardBounds;
            Rect rect = _boardFrame.rect;
            float normalizedX = (point.X - bounds.MinX) / bounds.Width;
            float normalizedY = (point.Y - bounds.MinY) / bounds.Height;
            _cueRoot.anchoredPosition = new Vector2(
                rect.xMin + normalizedX * rect.width,
                rect.yMin + normalizedY * rect.height);

            float diameterX = rect.width
                * (_controller.GravityWellRadius * 2f / bounds.Width);
            float diameterY = rect.height
                * (_controller.GravityWellRadius * 2f / bounds.Height);
            float diameter = Mathf.Min(diameterX, diameterY);
            _cueRoot.sizeDelta = new Vector2(diameter, diameter);
        }

        private void LateUpdate()
        {
            RefreshNow();
            if (_cueRoot == null
                || _cueImage == null
                || !_cueImage.enabled)
            {
                return;
            }

            if (_iconRoot != null)
            {
                _iconRoot.Rotate(
                    0f,
                    0f,
                    -_rotationDegreesPerSecond * Time.unscaledDeltaTime);
            }

            _cueRoot.localScale = Vector3.one;
            if (_rangeGraphic != null)
            {
                Color pulsed = _rangeBaseColor;
                pulsed.a *= 0.86f
                    + Mathf.Sin(Time.unscaledTime * 4f) * 0.14f;
                _rangeGraphic.color = pulsed;
            }
        }
    }
}
