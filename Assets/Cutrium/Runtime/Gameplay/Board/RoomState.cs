using System;
using Cutrium.Gameplay.Geometry;

namespace Cutrium.Gameplay.Board
{
    public readonly struct RoomState : IEquatable<RoomState>
    {
        public RoomState(RoomId id, LogicalRect bounds)
        {
            if (bounds.Width <= 0f || bounds.Height <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bounds),
                    bounds,
                    "Room dimensions must be positive.");
            }

            Id = id;
            Bounds = bounds;
        }

        public RoomId Id { get; }

        public LogicalRect Bounds { get; }

        public bool Equals(RoomState other) =>
            Id == other.Id && Bounds == other.Bounds;

        public override bool Equals(object obj) =>
            obj is RoomState other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id.GetHashCode() * 397) ^ Bounds.GetHashCode();
            }
        }

        public static bool operator ==(RoomState left, RoomState right) =>
            left.Equals(right);

        public static bool operator !=(RoomState left, RoomState right) =>
            !left.Equals(right);
    }
}
