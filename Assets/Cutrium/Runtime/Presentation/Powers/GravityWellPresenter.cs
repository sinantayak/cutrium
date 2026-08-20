using Cutrium.Gameplay.Geometry;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.Serialization;
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
        [FormerlySerializedAs("_cueImage")]
        private Image _vortexImage;

        [SerializeField]
        [FormerlySerializedAs("_iconRoot")]
        private RectTransform _vortexRoot;

        [SerializeField]
        private float _rotationDegreesPerSecond = 72f;

        [SerializeField]
        private Color _vortexBaseColor = new Color(1f, 1f, 1f, 0.78f);

        public FirstPlayableController Controller => _controller;
        public RectTransform BoardFrame => _boardFrame;
        public RectTransform CueRoot => _cueRoot;
        public Image VortexImage => _vortexImage;
        public RectTransform VortexRoot => _vortexRoot;

        public void ConfigureForSetup(
            FirstPlayableController controller,
            RectTransform boardFrame,
            RectTransform cueRoot,
            RectTransform vortexRoot,
            Image vortexImage)
        {
            _controller = controller;
            _boardFrame = boardFrame;
            _cueRoot = cueRoot;
            _vortexRoot = vortexRoot;
            _vortexImage = vortexImage;
            _vortexBaseColor = _vortexImage != null
                ? _vortexImage.color
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
            if (_vortexImage != null)
            {
                _vortexImage.enabled = visible;
            }

            if (!visible)
            {
                if (_vortexRoot != null)
                {
                    _vortexRoot.localScale = Vector3.one;
                }

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
                || _vortexImage == null
                || !_vortexImage.enabled)
            {
                return;
            }

            if (_vortexRoot != null)
            {
                _vortexRoot.Rotate(
                    0f,
                    0f,
                    -_rotationDegreesPerSecond * Time.unscaledDeltaTime);
                float pulse = 0.96f
                    + Mathf.Sin(Time.unscaledTime * 4f) * 0.04f;
                _vortexRoot.localScale = new Vector3(pulse, pulse, 1f);
            }

            _cueRoot.localScale = Vector3.one;
            Color pulsed = _vortexBaseColor;
            pulsed.a *= 0.88f
                + Mathf.Sin(Time.unscaledTime * 4f) * 0.12f;
            _vortexImage.color = pulsed;
        }
    }
}
