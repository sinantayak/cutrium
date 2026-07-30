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
            ValidateRect(viewportRect, nameof(viewportRect));

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
                viewportRect.center.y - (height * 0.5f),
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
