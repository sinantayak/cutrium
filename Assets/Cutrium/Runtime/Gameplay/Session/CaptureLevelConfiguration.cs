using System;

namespace Cutrium.Gameplay.Session
{
    public readonly struct CaptureLevelConfiguration
    {
        public CaptureLevelConfiguration(
            float targetCapturedFraction,
            int maximumAcceptedCuts = 0)
        {
            if (float.IsNaN(targetCapturedFraction)
                || float.IsInfinity(targetCapturedFraction)
                || targetCapturedFraction <= 0f
                || targetCapturedFraction > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetCapturedFraction),
                    targetCapturedFraction,
                    "Capture target must be greater than zero and at most one.");
            }

            if (maximumAcceptedCuts < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumAcceptedCuts),
                    maximumAcceptedCuts,
                    "A cut limit cannot be negative; zero means unlimited.");
            }

            TargetCapturedFraction = targetCapturedFraction;
            MaximumAcceptedCuts = maximumAcceptedCuts;
        }

        public float TargetCapturedFraction { get; }

        public int MaximumAcceptedCuts { get; }

        public bool HasCutLimit => MaximumAcceptedCuts > 0;
    }
}
