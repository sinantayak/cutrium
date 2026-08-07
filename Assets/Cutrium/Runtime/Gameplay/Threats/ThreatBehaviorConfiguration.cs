using System;

namespace Cutrium.Gameplay.Threats
{
    public enum ThreatBehaviorKind
    {
        Normal = 0,
        Hunter = 1,
        Pulse = 2
    }

    public readonly struct ThreatBehaviorConfiguration
        : IEquatable<ThreatBehaviorConfiguration>
    {
        private ThreatBehaviorConfiguration(
            ThreatBehaviorKind kind,
            float hunterSteerFactor,
            float pulseSlowSpeedMultiplier,
            float pulseFastSpeedMultiplier,
            float pulseSlowDurationSeconds,
            float pulseFastDurationSeconds)
        {
            Kind = kind;
            HunterSteerFactor = hunterSteerFactor;
            PulseSlowSpeedMultiplier = pulseSlowSpeedMultiplier;
            PulseFastSpeedMultiplier = pulseFastSpeedMultiplier;
            PulseSlowDurationSeconds = pulseSlowDurationSeconds;
            PulseFastDurationSeconds = pulseFastDurationSeconds;
        }

        public ThreatBehaviorKind Kind { get; }

        public float HunterSteerFactor { get; }

        public float PulseSlowSpeedMultiplier { get; }

        public float PulseFastSpeedMultiplier { get; }

        public float PulseSlowDurationSeconds { get; }

        public float PulseFastDurationSeconds { get; }

        public float PulseCycleSeconds =>
            PulseSlowDurationSeconds + PulseFastDurationSeconds;

        public static ThreatBehaviorConfiguration Normal { get; } =
            new ThreatBehaviorConfiguration(
                ThreatBehaviorKind.Normal,
                0f,
                1f,
                1f,
                0f,
                0f);

        public static ThreatBehaviorConfiguration CreateHunter(
            float steerFactor)
        {
            if (!IsFinite(steerFactor) || steerFactor <= 0f || steerFactor > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(steerFactor),
                    steerFactor,
                    "Hunter steer factor must be in the range (0, 1].");
            }

            return new ThreatBehaviorConfiguration(
                ThreatBehaviorKind.Hunter,
                steerFactor,
                1f,
                1f,
                0f,
                0f);
        }

        public static ThreatBehaviorConfiguration CreatePulse(
            float slowSpeedMultiplier,
            float fastSpeedMultiplier,
            float slowDurationSeconds,
            float fastDurationSeconds)
        {
            ValidateMultiplier(slowSpeedMultiplier, nameof(slowSpeedMultiplier));
            ValidateMultiplier(fastSpeedMultiplier, nameof(fastSpeedMultiplier));
            ValidateDuration(slowDurationSeconds, nameof(slowDurationSeconds));
            ValidateDuration(fastDurationSeconds, nameof(fastDurationSeconds));
            return new ThreatBehaviorConfiguration(
                ThreatBehaviorKind.Pulse,
                0f,
                slowSpeedMultiplier,
                fastSpeedMultiplier,
                slowDurationSeconds,
                fastDurationSeconds);
        }

        public bool Equals(ThreatBehaviorConfiguration other) =>
            Kind == other.Kind
            && HunterSteerFactor.Equals(other.HunterSteerFactor)
            && PulseSlowSpeedMultiplier.Equals(other.PulseSlowSpeedMultiplier)
            && PulseFastSpeedMultiplier.Equals(other.PulseFastSpeedMultiplier)
            && PulseSlowDurationSeconds.Equals(other.PulseSlowDurationSeconds)
            && PulseFastDurationSeconds.Equals(other.PulseFastDurationSeconds);

        public override bool Equals(object obj) =>
            obj is ThreatBehaviorConfiguration other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ HunterSteerFactor.GetHashCode();
                hashCode = (hashCode * 397)
                    ^ PulseSlowSpeedMultiplier.GetHashCode();
                hashCode = (hashCode * 397)
                    ^ PulseFastSpeedMultiplier.GetHashCode();
                hashCode = (hashCode * 397)
                    ^ PulseSlowDurationSeconds.GetHashCode();
                hashCode = (hashCode * 397)
                    ^ PulseFastDurationSeconds.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(
            ThreatBehaviorConfiguration left,
            ThreatBehaviorConfiguration right) => left.Equals(right);

        public static bool operator !=(
            ThreatBehaviorConfiguration left,
            ThreatBehaviorConfiguration right) => !left.Equals(right);

        private static void ValidateMultiplier(float value, string name)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    name,
                    value,
                    "Pulse speed multipliers must be finite and positive.");
            }
        }

        private static void ValidateDuration(float value, string name)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    name,
                    value,
                    "Pulse phase durations must be finite and positive.");
            }
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
