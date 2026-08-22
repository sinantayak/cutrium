using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Frontend
{
    [DisallowMultipleComponent]
    public sealed class FrontEndRoundedRectangleGraphic : MaskableGraphic
    {
        [SerializeField, Min(0f)] private float _cornerRadius = 30f;
        [SerializeField, Range(1, 12)] private int _cornerSegments = 6;
        [SerializeField] private bool _roundBottomCorners = true;

        public void ConfigureForSetup(
            Color fillColor,
            float cornerRadius,
            bool roundBottomCorners)
        {
            color = fillColor;
            _cornerRadius = Mathf.Max(0f, cornerRadius);
            _roundBottomCorners = roundBottomCorners;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float topRadius = Mathf.Min(
                _cornerRadius,
                Mathf.Min(rect.width, rect.height) * 0.5f);
            float bottomRadius = _roundBottomCorners ? topRadius : 0f;
            var perimeter = new List<Vector2>(4 * (_cornerSegments + 1));
            AddArc(
                perimeter,
                new Vector2(rect.xMax - bottomRadius,
                    rect.yMin + bottomRadius),
                bottomRadius,
                -90f,
                0f);
            AddArc(
                perimeter,
                new Vector2(rect.xMax - topRadius, rect.yMax - topRadius),
                topRadius,
                0f,
                90f);
            AddArc(
                perimeter,
                new Vector2(rect.xMin + topRadius, rect.yMax - topRadius),
                topRadius,
                90f,
                180f);
            AddArc(
                perimeter,
                new Vector2(rect.xMin + bottomRadius,
                    rect.yMin + bottomRadius),
                bottomRadius,
                180f,
                270f);

            Color32 vertexColor = color;
            Vector2 center = rect.center;
            vertexHelper.AddVert(
                center,
                vertexColor,
                new Vector2(0.5f, 0.5f));
            foreach (Vector2 point in perimeter)
            {
                Vector2 uv = new Vector2(
                    Mathf.InverseLerp(rect.xMin, rect.xMax, point.x),
                    Mathf.InverseLerp(rect.yMin, rect.yMax, point.y));
                vertexHelper.AddVert(point, vertexColor, uv);
            }

            for (int index = 0; index < perimeter.Count; index++)
            {
                int current = index + 1;
                int next = (index + 1) % perimeter.Count + 1;
                vertexHelper.AddTriangle(0, next, current);
            }
        }

        private void AddArc(
            ICollection<Vector2> points,
            Vector2 center,
            float radius,
            float startDegrees,
            float endDegrees)
        {
            if (radius <= 0.001f)
            {
                float radians = startDegrees * Mathf.Deg2Rad;
                points.Add(center + new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians)) * radius);
                return;
            }

            for (int segment = 0; segment <= _cornerSegments; segment++)
            {
                float degrees = Mathf.Lerp(
                    startDegrees,
                    endDegrees,
                    segment / (float)_cornerSegments);
                float radians = degrees * Mathf.Deg2Rad;
                points.Add(center + new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians)) * radius);
            }
        }
    }
}
