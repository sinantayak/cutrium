using System;

namespace Cutrium.Gameplay.Session
{
    public readonly struct PowerConfiguration : IEquatable<PowerConfiguration>
    {
        public PowerConfiguration(
            int freezePulseCharges,
            float freezePulseDurationSeconds,
            float freezePulseSpeedMultiplier,
            int instantBarrierCharges,
            float instantBarrierGrowthSpeed)
        {
            if (freezePulseCharges < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(freezePulseCharges),
                    freezePulseCharges,
                    "Freeze Pulse charges cannot be negative.");
            }

            if (instantBarrierCharges < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(instantBarrierCharges),
                    instantBarrierCharges,
                    "Instant Barrier charges cannot be negative.");
            }

            if (freezePulseCharges > 0)
            {
                if (!IsFinite(freezePulseDurationSeconds)
                    || freezePulseDurationSeconds <= 0f)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(freezePulseDurationSeconds),
                        freezePulseDurationSeconds,
                        "Freeze Pulse duration must be finite and positive.");
                }

                if (!IsFinite(freezePulseSpeedMultiplier)
                    || freezePulseSpeedMultiplier <= 0f
                    || freezePulseSpeedMultiplier >= 1f)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(freezePulseSpeedMultiplier),
                        freezePulseSpeedMultiplier,
                        "Freeze Pulse must heavily slow threats: the " +
                        "multiplier must be in the range (0, 1).");
                }
            }

            if (instantBarrierCharges > 0
                && (!IsFinite(instantBarrierGrowthSpeed)
                    || instantBarrierGrowthSpeed <= 0f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(instantBarrierGrowthSpeed),
                    instantBarrierGrowthSpeed,
                    "Instant Barrier growth speed must be finite and positive.");
            }

            FreezePulseCharges = freezePulseCharges;
            FreezePulseDurationSeconds = freezePulseDurationSeconds;
            FreezePulseSpeedMultiplier = freezePulseSpeedMultiplier;
            InstantBarrierCharges = instantBarrierCharges;
            InstantBarrierGrowthSpeed = instantBarrierGrowthSpeed;
        }

        public int FreezePulseCharges { get; }

        public float FreezePulseDurationSeconds { get; }

        public float FreezePulseSpeedMultiplier { get; }

        public int InstantBarrierCharges { get; }

        public float InstantBarrierGrowthSpeed { get; }

        public static PowerConfiguration None { get; } =
            new PowerConfiguration(0, 1f, 0.05f, 0, 1f);

        public bool Equals(PowerConfiguration other) =>
            FreezePulseCharges == other.FreezePulseCharges
            && FreezePulseDurationSeconds.Equals(
                other.FreezePulseDurationSeconds)
            && FreezePulseSpeedMultiplier.Equals(
                other.FreezePulseSpeedMultiplier)
            && InstantBarrierCharges == other.InstantBarrierCharges
            && InstantBarrierGrowthSpeed.Equals(
                other.InstantBarrierGrowthSpeed);

        public override bool Equals(object obj) =>
            obj is PowerConfiguration other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = FreezePulseCharges;
                hashCode = (hashCode * 397)
                    ^ FreezePulseDurationSeconds.GetHashCode();
                hashCode = (hashCode * 397)
                    ^ FreezePulseSpeedMultiplier.GetHashCode();
                hashCode = (hashCode * 397) ^ InstantBarrierCharges;
                hashCode = (hashCode * 397)
                    ^ InstantBarrierGrowthSpeed.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(
            PowerConfiguration left,
            PowerConfiguration right) => left.Equals(right);

        public static bool operator !=(
            PowerConfiguration left,
            PowerConfiguration right) => !left.Equals(right);

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
