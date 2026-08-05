using System;

namespace Cutrium.Gameplay.Session
{
    public readonly struct CaptureLevelConfiguration
    {
        public CaptureLevelConfiguration(float targetCapturedFraction)
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

            TargetCapturedFraction = targetCapturedFraction;
        }

        public float TargetCapturedFraction { get; }
    }
}
