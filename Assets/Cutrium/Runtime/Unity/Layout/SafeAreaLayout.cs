using System;
using UnityEngine;

namespace Cutrium.Unity.Layout
{
    public readonly struct SafeAreaAnchors : IEquatable<SafeAreaAnchors>
    {
        public SafeAreaAnchors(Vector2 minimum, Vector2 maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public Vector2 Minimum { get; }

        public Vector2 Maximum { get; }

        public bool Equals(SafeAreaAnchors other)
        {
            return Minimum == other.Minimum && Maximum == other.Maximum;
        }

        public override bool Equals(object obj)
        {
            return obj is SafeAreaAnchors other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Minimum.GetHashCode() * 397) ^ Maximum.GetHashCode();
            }
        }
    }

    public static class SafeAreaLayout
    {
        public static SafeAreaAnchors CalculateAnchors(Rect safeArea, Vector2 screenSize)
        {
            if (!IsFinite(screenSize.x)
                || !IsFinite(screenSize.y)
                || screenSize.x <= 0f
                || screenSize.y <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(screenSize),
                    screenSize,
                    "Screen dimensions must be finite and positive.");
            }

            Rect clamped = ClampToScreen(safeArea, screenSize);
            return new SafeAreaAnchors(
                new Vector2(clamped.xMin / screenSize.x, clamped.yMin / screenSize.y),
                new Vector2(clamped.xMax / screenSize.x, clamped.yMax / screenSize.y));
        }

        private static Rect ClampToScreen(Rect safeArea, Vector2 screenSize)
        {
            float xMin = Mathf.Clamp(safeArea.xMin, 0f, screenSize.x);
            float yMin = Mathf.Clamp(safeArea.yMin, 0f, screenSize.y);
            float xMax = Mathf.Clamp(safeArea.xMax, xMin, screenSize.x);
            float yMax = Mathf.Clamp(safeArea.yMax, yMin, screenSize.y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
