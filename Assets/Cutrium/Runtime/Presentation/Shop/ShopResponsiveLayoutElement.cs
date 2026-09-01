using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cutrium.Presentation.Shop
{
    /// <summary>
    /// Reports a width-derived preferred height to a parent layout group.
    /// A single-column card preserves its authored aspect ratio; a multi-column
    /// row preserves each item after subtracting spacing and visual padding.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Cutrium/Shop/Responsive Layout Element")]
    public sealed class ShopResponsiveLayoutElement : UIBehaviour, ILayoutElement
    {
        private const float MinimumAspectRatio = 0.01f;

        [SerializeField, Min(MinimumAspectRatio)]
        private float _itemAspectRatio = 1f;

        [SerializeField, Min(1)] private int _columnCount = 1;
        [SerializeField, Min(0f)] private float _columnSpacing;
        [SerializeField, Min(0f)] private float _horizontalPadding;
        [SerializeField, Min(0f)] private float _verticalPadding;

        private RectTransform _rectTransform;
        private float _lastObservedWidth = float.NaN;

        public float ItemAspectRatio => _itemAspectRatio;
        public int ColumnCount => _columnCount;
        public float ColumnSpacing => _columnSpacing;
        public float HorizontalPadding => _horizontalPadding;
        public float VerticalPadding => _verticalPadding;

        public float minWidth => -1f;
        public float preferredWidth => -1f;
        public float flexibleWidth => -1f;
        public float minHeight => -1f;
        public float preferredHeight => CalculatePreferredHeight(CurrentWidth);
        public float flexibleHeight => -1f;
        public int layoutPriority => 2;

        private RectTransform RectTransform =>
            _rectTransform != null
                ? _rectTransform
                : _rectTransform = (RectTransform)transform;

        private float CurrentWidth => Mathf.Max(0f, RectTransform.rect.width);

        public void ConfigureForSetup(
            float itemAspectRatio,
            int columnCount = 1,
            float columnSpacing = 0f,
            float horizontalPadding = 0f,
            float verticalPadding = 0f)
        {
            _itemAspectRatio = Mathf.Max(
                MinimumAspectRatio,
                itemAspectRatio);
            _columnCount = Mathf.Max(1, columnCount);
            _columnSpacing = Mathf.Max(0f, columnSpacing);
            _horizontalPadding = Mathf.Max(0f, horizontalPadding);
            _verticalPadding = Mathf.Max(0f, verticalPadding);
            SetLayoutDirty();
        }

        public float CalculatePreferredHeight(float availableWidth)
        {
            if (availableWidth <= 0f)
            {
                return 0f;
            }

            float totalSpacing = _columnSpacing * (_columnCount - 1);
            float usableWidth = Mathf.Max(
                0f,
                availableWidth - _horizontalPadding);
            float itemWidth = Mathf.Max(
                0f,
                (usableWidth - totalSpacing) / _columnCount);
            return itemWidth / Mathf.Max(
                MinimumAspectRatio,
                _itemAspectRatio)
                + _verticalPadding;
        }

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
            _lastObservedWidth = CurrentWidth;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _lastObservedWidth = float.NaN;
            SetLayoutDirty();
        }

        protected override void OnDisable()
        {
            SetLayoutDirty();
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            float width = CurrentWidth;
            if (!float.IsNaN(_lastObservedWidth)
                && Mathf.Abs(width - _lastObservedWidth) < 0.01f)
            {
                return;
            }

            _lastObservedWidth = width;
            SetLayoutDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _itemAspectRatio = Mathf.Max(
                MinimumAspectRatio,
                _itemAspectRatio);
            _columnCount = Mathf.Max(1, _columnCount);
            _columnSpacing = Mathf.Max(0f, _columnSpacing);
            _horizontalPadding = Mathf.Max(0f, _horizontalPadding);
            _verticalPadding = Mathf.Max(0f, _verticalPadding);
            SetLayoutDirty();
        }
#endif

        private void SetLayoutDirty()
        {
            if (!IsActive())
            {
                return;
            }

            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }
    }
}
