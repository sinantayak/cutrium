using System;

namespace Cutrium.Gameplay.Threats
{
    public readonly struct ThreatId : IEquatable<ThreatId>
    {
        public ThreatId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Threat IDs must be positive.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(ThreatId other) => Value == other.Value;

        public override bool Equals(object obj) =>
            obj is ThreatId other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => $"Threat {Value}";

        public static bool operator ==(ThreatId left, ThreatId right) =>
            left.Equals(right);

        public static bool operator !=(ThreatId left, ThreatId right) =>
            !left.Equals(right);
    }
}
