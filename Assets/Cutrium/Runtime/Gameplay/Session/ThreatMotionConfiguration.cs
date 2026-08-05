using System;
using Cutrium.Gameplay.Geometry;

namespace Cutrium.Gameplay.Session
{
    public readonly struct ThreatMotionConfiguration
    {
        public ThreatMotionConfiguration(
            LogicalRect boardBounds,
            LogicalPoint initialPosition,
            LogicalVector initialDirection,
            float speed,
            float radius,
            int maximumImpactsPerTick)
        {
            if (boardBounds.Width <= 0f || boardBounds.Height <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(boardBounds),
                    boardBounds,
                    "Board dimensions must be positive.");
            }

            if (!IsFinite(speed) || speed <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(speed),
                    speed,
                    "Threat speed must be finite and positive.");
            }

            if (!IsFinite(radius) || radius <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radius),
                    radius,
                    "Threat radius must be finite and positive.");
            }

            if (initialDirection.LengthSquared <= 0f
                || !IsFinite(initialDirection.LengthSquared))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialDirection),
                    initialDirection,
                    "Initial direction must be finite and non-zero.");
            }

            if (maximumImpactsPerTick <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumImpactsPerTick),
                    maximumImpactsPerTick,
                    "The impact limit must be positive.");
            }

            BoardBounds = boardBounds;
            InitialPosition = initialPosition;
            InitialDirection = initialDirection / initialDirection.Length;
            Speed = speed;
            Radius = radius;
            MaximumImpactsPerTick = maximumImpactsPerTick;
        }

        public LogicalRect BoardBounds { get; }

        public LogicalPoint InitialPosition { get; }

        public LogicalVector InitialDirection { get; }

        public float Speed { get; }

        public float Radius { get; }

        public int MaximumImpactsPerTick { get; }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
