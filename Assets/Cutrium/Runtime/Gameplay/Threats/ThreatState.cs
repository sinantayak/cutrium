using System;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;

namespace Cutrium.Gameplay.Threats
{
    public readonly struct ThreatState : IEquatable<ThreatState>
    {
        public ThreatState(
            ThreatId id,
            RoomId roomId,
            LogicalPoint position,
            LogicalVector velocity,
            float radius)
        {
            if (!IsFinite(radius) || radius <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radius),
                    radius,
                    "Threat radius must be finite and positive.");
            }

            if (velocity.LengthSquared <= 0f
                || !IsFinite(velocity.LengthSquared))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(velocity),
                    velocity,
                    "Threat velocity must be finite and non-zero.");
            }

            Id = id;
            RoomId = roomId;
            Position = position;
            Velocity = velocity;
            Radius = radius;
        }

        public ThreatId Id { get; }

        public RoomId RoomId { get; }

        public LogicalPoint Position { get; }

        public LogicalVector Velocity { get; }

        public float Radius { get; }

        public float Speed => Velocity.Length;

        public ThreatState WithMotion(
            LogicalPoint position,
            LogicalVector velocity)
        {
            return new ThreatState(Id, RoomId, position, velocity, Radius);
        }

        public ThreatState WithRoom(RoomId roomId)
        {
            return new ThreatState(Id, roomId, Position, Velocity, Radius);
        }

        public bool Equals(ThreatState other)
        {
            return Id == other.Id
                && RoomId == other.RoomId
                && Position == other.Position
                && Velocity == other.Velocity
                && Radius.Equals(other.Radius);
        }

        public override bool Equals(object obj) =>
            obj is ThreatState other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Id.GetHashCode();
                hashCode = (hashCode * 397) ^ RoomId.GetHashCode();
                hashCode = (hashCode * 397) ^ Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Velocity.GetHashCode();
                hashCode = (hashCode * 397) ^ Radius.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ThreatState left, ThreatState right) =>
            left.Equals(right);

        public static bool operator !=(ThreatState left, ThreatState right) =>
            !left.Equals(right);

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
