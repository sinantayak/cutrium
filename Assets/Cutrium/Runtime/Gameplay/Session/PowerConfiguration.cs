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
            : this(
                freezePulseCharges,
                freezePulseDurationSeconds,
                freezePulseSpeedMultiplier,
                instantBarrierCharges,
                instantBarrierGrowthSpeed,
                0,
                4f,
                2.75f,
                100f)
        {
        }

        public PowerConfiguration(
            int freezePulseCharges,
            float freezePulseDurationSeconds,
            float freezePulseSpeedMultiplier,
            int instantBarrierCharges,
            float instantBarrierGrowthSpeed,
            int gravityWellCharges,
            float gravityWellDurationSeconds,
            float gravityWellRadius,
            float gravityWellTurnDegreesPerSecond)
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

            if (gravityWellCharges < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gravityWellCharges),
                    gravityWellCharges,
                    "Gravity Well charges cannot be negative.");
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


            if (gravityWellCharges > 0)
            {
                ValidatePositive(
                    gravityWellDurationSeconds,
                    nameof(gravityWellDurationSeconds),
                    "Gravity Well duration");
                ValidatePositive(
                    gravityWellRadius,
                    nameof(gravityWellRadius),
                    "Gravity Well radius");
                ValidatePositive(
                    gravityWellTurnDegreesPerSecond,
                    nameof(gravityWellTurnDegreesPerSecond),
                    "Gravity Well turn rate");
            }

            FreezePulseCharges = freezePulseCharges;
            FreezePulseDurationSeconds = freezePulseDurationSeconds;
            FreezePulseSpeedMultiplier = freezePulseSpeedMultiplier;
            InstantBarrierCharges = instantBarrierCharges;
            InstantBarrierGrowthSpeed = instantBarrierGrowthSpeed;
            GravityWellCharges = gravityWellCharges;
            GravityWellDurationSeconds = gravityWellDurationSeconds;
            GravityWellRadius = gravityWellRadius;
            GravityWellTurnDegreesPerSecond =
                gravityWellTurnDegreesPerSecond;
        }

        public int FreezePulseCharges { get; }

        public float FreezePulseDurationSeconds { get; }

        public float FreezePulseSpeedMultiplier { get; }

        public int InstantBarrierCharges { get; }

        public float InstantBarrierGrowthSpeed { get; }

        public int GravityWellCharges { get; }

        public float GravityWellDurationSeconds { get; }

        public float GravityWellRadius { get; }

        public float GravityWellTurnDegreesPerSecond { get; }

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
                other.InstantBarrierGrowthSpeed)
            && GravityWellCharges == other.GravityWellCharges
            && GravityWellDurationSeconds.Equals(
                other.GravityWellDurationSeconds)
            && GravityWellRadius.Equals(other.GravityWellRadius)
            && GravityWellTurnDegreesPerSecond.Equals(
                other.GravityWellTurnDegreesPerSecond);

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
                hashCode = (hashCode * 397) ^ GravityWellCharges;
                hashCode = (hashCode * 397)
                    ^ GravityWellDurationSeconds.GetHashCode();
                hashCode = (hashCode * 397)
                    ^ GravityWellRadius.GetHashCode();
                hashCode = (hashCode * 397)
                    ^ GravityWellTurnDegreesPerSecond.GetHashCode();
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

        private static void ValidatePositive(
            float value,
            string parameterName,
            string displayName)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    $"{displayName} must be finite and positive.");
            }
        }
    }
}
