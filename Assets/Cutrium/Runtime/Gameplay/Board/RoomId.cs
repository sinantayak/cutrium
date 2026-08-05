using System;

namespace Cutrium.Gameplay.Board
{
    public readonly struct RoomId : IEquatable<RoomId>
    {
        public RoomId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Room IDs must be positive.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(RoomId other) => Value == other.Value;

        public override bool Equals(object obj) =>
            obj is RoomId other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => $"Room {Value}";

        public static bool operator ==(RoomId left, RoomId right) =>
            left.Equals(right);

        public static bool operator !=(RoomId left, RoomId right) =>
            !left.Equals(right);
    }
}
