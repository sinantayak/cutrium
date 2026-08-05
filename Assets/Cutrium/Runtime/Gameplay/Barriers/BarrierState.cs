using System;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;

namespace Cutrium.Gameplay.Barriers
{
    public enum BarrierLifecycle
    {
        Growing = 0,
        Failed = 1,
        Locked = 2
    }

    public readonly struct BarrierState : IEquatable<BarrierState>
    {
        public BarrierState(
            BarrierId id,
            RoomId parentRoomId,
            LogicalPoint origin,
            BarrierOrientation orientation,
            float negativeLength,
            float positiveLength,
            float negativeTargetLength,
            float positiveTargetLength,
            float growthSpeed,
            float collisionHalfWidth,
            BarrierLifecycle lifecycle)
        {
            if (orientation != BarrierOrientation.Horizontal
                && orientation != BarrierOrientation.Vertical)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orientation), orientation, "A barrier needs an axis.");
            }

            ValidateLength(negativeLength, nameof(negativeLength), true);
            ValidateLength(positiveLength, nameof(positiveLength), true);
            ValidateLength(
                negativeTargetLength,
                nameof(negativeTargetLength),
                false);
            ValidateLength(
                positiveTargetLength,
                nameof(positiveTargetLength),
                false);
            ValidateLength(growthSpeed, nameof(growthSpeed), false);
            ValidateLength(
                collisionHalfWidth,
                nameof(collisionHalfWidth),
                false);
            if (negativeLength > negativeTargetLength
                || positiveLength > positiveTargetLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(negativeLength),
                    "Barrier growth cannot exceed its target boundary.");
            }

            Id = id;
            ParentRoomId = parentRoomId;
            Origin = origin;
            Orientation = orientation;
            NegativeLength = negativeLength;
            PositiveLength = positiveLength;
            NegativeTargetLength = negativeTargetLength;
            PositiveTargetLength = positiveTargetLength;
            GrowthSpeed = growthSpeed;
            CollisionHalfWidth = collisionHalfWidth;
            Lifecycle = lifecycle;
        }

        public BarrierId Id { get; }
        public RoomId ParentRoomId { get; }
        public LogicalPoint Origin { get; }
        public BarrierOrientation Orientation { get; }
        public float NegativeLength { get; }
        public float PositiveLength { get; }
        public float NegativeTargetLength { get; }
        public float PositiveTargetLength { get; }
        public float GrowthSpeed { get; }
        public float CollisionHalfWidth { get; }
        public BarrierLifecycle Lifecycle { get; }

        public bool NegativeComplete => NegativeLength == NegativeTargetLength;
        public bool PositiveComplete => PositiveLength == PositiveTargetLength;
        public bool IsComplete => NegativeComplete && PositiveComplete;
        public bool IsVulnerable => Lifecycle == BarrierLifecycle.Growing;

        public LogicalPoint NegativeEndpoint =>
            Orientation == BarrierOrientation.Horizontal
                ? new LogicalPoint(Origin.X - NegativeLength, Origin.Y)
                : new LogicalPoint(Origin.X, Origin.Y - NegativeLength);

        public LogicalPoint PositiveEndpoint =>
            Orientation == BarrierOrientation.Horizontal
                ? new LogicalPoint(Origin.X + PositiveLength, Origin.Y)
                : new LogicalPoint(Origin.X, Origin.Y + PositiveLength);

        public BarrierState AdvanceGrowth(
            float elapsedTime,
            GeometryTolerancePolicy tolerance)
        {
            if (float.IsNaN(elapsedTime)
                || float.IsInfinity(elapsedTime)
                || elapsedTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedTime));
            }

            if (!IsVulnerable || elapsedTime == 0f)
            {
                return this;
            }

            float growth = GrowthSpeed * elapsedTime;
            float negative = Math.Min(
                NegativeTargetLength,
                NegativeLength + growth);
            float positive = Math.Min(
                PositiveTargetLength,
                PositiveLength + growth);
            if (tolerance.IsDistanceApproximatelyEqual(
                    negative,
                    NegativeTargetLength))
            {
                negative = NegativeTargetLength;
            }

            if (tolerance.IsDistanceApproximatelyEqual(
                    positive,
                    PositiveTargetLength))
            {
                positive = PositiveTargetLength;
            }

            BarrierLifecycle lifecycle =
                negative == NegativeTargetLength
                && positive == PositiveTargetLength
                    ? BarrierLifecycle.Locked
                    : BarrierLifecycle.Growing;
            return With(negative, positive, lifecycle);
        }

        public BarrierState Fail() =>
            With(NegativeLength, PositiveLength, BarrierLifecycle.Failed);

        public bool Equals(BarrierState other) =>
            Id == other.Id
            && ParentRoomId == other.ParentRoomId
            && Origin == other.Origin
            && Orientation == other.Orientation
            && NegativeLength.Equals(other.NegativeLength)
            && PositiveLength.Equals(other.PositiveLength)
            && NegativeTargetLength.Equals(other.NegativeTargetLength)
            && PositiveTargetLength.Equals(other.PositiveTargetLength)
            && GrowthSpeed.Equals(other.GrowthSpeed)
            && CollisionHalfWidth.Equals(other.CollisionHalfWidth)
            && Lifecycle == other.Lifecycle;

        public override bool Equals(object obj) =>
            obj is BarrierState other && Equals(other);

        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(BarrierState left, BarrierState right) =>
            left.Equals(right);

        public static bool operator !=(BarrierState left, BarrierState right) =>
            !left.Equals(right);

        private BarrierState With(
            float negativeLength,
            float positiveLength,
            BarrierLifecycle lifecycle) =>
            new BarrierState(
                Id,
                ParentRoomId,
                Origin,
                Orientation,
                negativeLength,
                positiveLength,
                NegativeTargetLength,
                PositiveTargetLength,
                GrowthSpeed,
                CollisionHalfWidth,
                lifecycle);

        private static void ValidateLength(
            float value,
            string name,
            bool allowZero)
        {
            if (float.IsNaN(value)
                || float.IsInfinity(value)
                || value < 0f
                || !allowZero && value == 0f)
            {
                throw new ArgumentOutOfRangeException(
                    name, value, "Barrier dimensions must be finite and valid.");
            }
        }
    }
}
