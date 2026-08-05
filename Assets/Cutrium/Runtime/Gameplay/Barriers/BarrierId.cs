using System;

namespace Cutrium.Gameplay.Barriers
{
    public readonly struct BarrierId : IEquatable<BarrierId>
    {
        public BarrierId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, "Barrier IDs must be positive.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(BarrierId other) => Value == other.Value;

        public override bool Equals(object obj) =>
            obj is BarrierId other && Equals(other);

        public override int GetHashCode() => Value;

        public static bool operator ==(BarrierId left, BarrierId right) =>
            left.Equals(right);

        public static bool operator !=(BarrierId left, BarrierId right) =>
            !left.Equals(right);
    }
}
