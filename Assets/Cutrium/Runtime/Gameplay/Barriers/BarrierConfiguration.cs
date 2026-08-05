using System;

namespace Cutrium.Gameplay.Barriers
{
    public readonly struct BarrierConfiguration
    {
        public BarrierConfiguration(
            float growthSpeed,
            float collisionHalfWidth,
            float minimumEdgeMargin,
            int maximumSolverIterations)
        {
            ValidatePositiveFinite(growthSpeed, nameof(growthSpeed));
            ValidatePositiveFinite(
                collisionHalfWidth,
                nameof(collisionHalfWidth));
            ValidateNonNegativeFinite(
                minimumEdgeMargin,
                nameof(minimumEdgeMargin));
            if (maximumSolverIterations <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumSolverIterations),
                    maximumSolverIterations,
                    "The solver iteration limit must be positive.");
            }

            GrowthSpeed = growthSpeed;
            CollisionHalfWidth = collisionHalfWidth;
            MinimumEdgeMargin = minimumEdgeMargin;
            MaximumSolverIterations = maximumSolverIterations;
        }

        public float GrowthSpeed { get; }

        public float CollisionHalfWidth { get; }

        public float MinimumEdgeMargin { get; }

        public int MaximumSolverIterations { get; }

        private static void ValidatePositiveFinite(float value, string name)
        {
            ValidateNonNegativeFinite(value, name);
            if (value == 0f)
            {
                throw new ArgumentOutOfRangeException(
                    name, value, "The value must be positive.");
            }
        }

        private static void ValidateNonNegativeFinite(
            float value,
            string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    name, value, "The value must be finite and non-negative.");
            }
        }
    }
}
