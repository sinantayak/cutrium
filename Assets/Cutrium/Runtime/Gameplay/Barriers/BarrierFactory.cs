using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;

namespace Cutrium.Gameplay.Barriers
{
    public static class BarrierFactory
    {
        public static BarrierStartResult TryCreate(
            BarrierId id,
            RoomState room,
            BarrierIntent intent,
            BarrierConfiguration configuration,
            GeometryTolerancePolicy tolerance)
        {
            if (intent.Orientation != BarrierOrientation.Horizontal
                && intent.Orientation != BarrierOrientation.Vertical)
            {
                return Reject(BarrierRejectionReason.InvalidOrientation);
            }

            if (!tolerance.Contains(room.Bounds, intent.Origin))
            {
                return Reject(BarrierRejectionReason.OriginOutsideActiveRoom);
            }

            float negativeTarget = intent.Orientation == BarrierOrientation.Horizontal
                ? intent.Origin.X - room.Bounds.MinX
                : intent.Origin.Y - room.Bounds.MinY;
            float positiveTarget = intent.Orientation == BarrierOrientation.Horizontal
                ? room.Bounds.MaxX - intent.Origin.X
                : room.Bounds.MaxY - intent.Origin.Y;
            if (tolerance.IsLessThanOrApproximatelyEqualDistance(
                    negativeTarget,
                    configuration.MinimumEdgeMargin)
                || tolerance.IsLessThanOrApproximatelyEqualDistance(
                    positiveTarget,
                    configuration.MinimumEdgeMargin))
            {
                return Reject(BarrierRejectionReason.TooCloseToRoomEdge);
            }

            var barrier = new BarrierState(
                id,
                room.Id,
                intent.Origin,
                intent.Orientation,
                0f,
                0f,
                negativeTarget,
                positiveTarget,
                configuration.GrowthSpeed,
                configuration.CollisionHalfWidth,
                BarrierLifecycle.Growing);
            return new BarrierStartResult(
                true,
                BarrierRejectionReason.None,
                barrier);
        }

        private static BarrierStartResult Reject(
            BarrierRejectionReason reason) =>
            new BarrierStartResult(false, reason, default);
    }
}
