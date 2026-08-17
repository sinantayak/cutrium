using System;

namespace Cutrium.Gameplay.Session
{
    public readonly struct CaptureLevelConfiguration
    {
        public CaptureLevelConfiguration(
            float targetCapturedFraction,
            int maximumAcceptedCuts = 0,
            int maximumAcceptedBarrierBreaks = 0)
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

            if (maximumAcceptedBarrierBreaks < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumAcceptedBarrierBreaks),
                    maximumAcceptedBarrierBreaks,
                    "A burn limit cannot be negative; zero means unlimited.");
            }

            TargetCapturedFraction = targetCapturedFraction;
            MaximumAcceptedCuts = maximumAcceptedCuts;
            MaximumAcceptedBarrierBreaks = maximumAcceptedBarrierBreaks;
        }

        public float TargetCapturedFraction { get; }

        public int MaximumAcceptedCuts { get; }

        public int MaximumAcceptedBarrierBreaks { get; }

        public bool HasCutLimit => MaximumAcceptedCuts > 0;

        public bool HasBurnLimit => MaximumAcceptedBarrierBreaks > 0;
    }
}
