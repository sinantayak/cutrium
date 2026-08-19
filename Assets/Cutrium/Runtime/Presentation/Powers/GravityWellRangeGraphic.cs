using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Powers
{
    [DisallowMultipleComponent]
    public sealed class GravityWellRangeGraphic : MaskableGraphic
    {
        [SerializeField, Min(1f)]
        private float _ringThickness = 6f;

        [SerializeField, Range(24, 160)]
        private int _segmentCount = 96;

        [SerializeField, Range(0f, 0.25f)]
        private float _fillAlphaMultiplier = 0.09f;

        public float RingThickness => _ringThickness;

        public int SegmentCount => _segmentCount;

        public void ConfigureForSetup(
            Color rangeColor,
            float ringThickness,
            int segmentCount)
        {
            color = rangeColor;
            _ringThickness = Mathf.Max(1f, ringThickness);
            _segmentCount = Mathf.Clamp(segmentCount, 24, 160);
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            float outerRadius = Mathf.Max(
                0f,
                Mathf.Min(rect.width, rect.height) * 0.5f);
            if (outerRadius <= 0f)
            {
                return;
            }

            float innerRadius = Mathf.Max(
                0f,
                outerRadius - Mathf.Min(_ringThickness, outerRadius));
            Vector2 center = rect.center;
            Color fillColor = color;
            fillColor.a *= _fillAlphaMultiplier;

            AddVertex(vertexHelper, center, fillColor);
            for (int index = 0; index <= _segmentCount; index++)
            {
                float radians = Mathf.PI * 2f * index / _segmentCount;
                Vector2 direction = new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians));
                AddVertex(
                    vertexHelper,
                    center + direction * innerRadius,
                    fillColor);
            }

            for (int index = 0; index < _segmentCount; index++)
            {
                vertexHelper.AddTriangle(0, index + 1, index + 2);
            }

            int ringStart = vertexHelper.currentVertCount;
            for (int index = 0; index <= _segmentCount; index++)
            {
                float radians = Mathf.PI * 2f * index / _segmentCount;
                Vector2 direction = new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians));
                AddVertex(
                    vertexHelper,
                    center + direction * innerRadius,
                    color);
                AddVertex(
                    vertexHelper,
                    center + direction * outerRadius,
                    color);
            }

            for (int index = 0; index < _segmentCount; index++)
            {
                int current = ringStart + index * 2;
                int next = current + 2;
                vertexHelper.AddTriangle(current, next, current + 1);
                vertexHelper.AddTriangle(current + 1, next, next + 1);
            }
        }

        private static void AddVertex(
            VertexHelper vertexHelper,
            Vector2 position,
            Color vertexColor)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = vertexColor;
            vertexHelper.AddVert(vertex);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _ringThickness = Mathf.Max(1f, _ringThickness);
            _segmentCount = Mathf.Clamp(_segmentCount, 24, 160);
            _fillAlphaMultiplier = Mathf.Clamp(
                _fillAlphaMultiplier,
                0f,
                0.25f);
            SetVerticesDirty();
        }
#endif
    }
}
