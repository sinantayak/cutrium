using System;
using UnityEngine;

namespace Cutrium.Unity.Layout
{
    public static class BoardViewportLayout
    {
        public const float LogicalWidth = 10f;
        public const float LogicalHeight = 16f;

        public static Vector2 LogicalSize => new Vector2(LogicalWidth, LogicalHeight);

        public static Rect CalculateAspectFitRect(Rect viewportRect)
        {
            return CalculateAspectFitRect(viewportRect, 0.5f);
        }

        public static Rect CalculateAspectFitRect(
            Rect viewportRect,
            float verticalAlignment)
        {
            ValidateRect(viewportRect, nameof(viewportRect));
            if (!IsFinite(verticalAlignment)
                || verticalAlignment < 0f
                || verticalAlignment > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(verticalAlignment),
                    verticalAlignment,
                    "Vertical alignment must be between zero (bottom) " +
                    "and one (top).");
            }

            float boardAspect = LogicalWidth / LogicalHeight;
            float viewportAspect = viewportRect.width / viewportRect.height;
            float width;
            float height;

            if (viewportAspect >= boardAspect)
            {
                height = viewportRect.height;
                width = height * boardAspect;
            }
            else
            {
                width = viewportRect.width;
                height = width / boardAspect;
            }

            return new Rect(
                viewportRect.center.x - (width * 0.5f),
                Mathf.Lerp(
                    viewportRect.yMin,
                    viewportRect.yMax - height,
                    verticalAlignment),
                width,
                height);
        }

        public static float CalculateOrthographicSize(Rect viewportRect)
        {
            ValidateRect(viewportRect, nameof(viewportRect));

            float viewportAspect = viewportRect.width / viewportRect.height;
            float verticalHalfSize = LogicalHeight * 0.5f;
            float horizontalHalfSize = (LogicalWidth * 0.5f) / viewportAspect;
            return Mathf.Max(verticalHalfSize, horizontalHalfSize);
        }

        private static void ValidateRect(Rect rect, string parameterName)
        {
            if (!IsFinite(rect.x)
                || !IsFinite(rect.y)
                || !IsFinite(rect.width)
                || !IsFinite(rect.height)
                || rect.width <= 0f
                || rect.height <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    rect,
                    "The viewport must have finite, positive dimensions.");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
