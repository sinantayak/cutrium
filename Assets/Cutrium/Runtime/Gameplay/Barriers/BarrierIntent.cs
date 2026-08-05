using Cutrium.Gameplay.Geometry;

namespace Cutrium.Gameplay.Barriers
{
    public readonly struct BarrierIntent
    {
        public BarrierIntent(
            LogicalPoint origin,
            BarrierOrientation orientation)
        {
            Origin = origin;
            Orientation = orientation;
        }

        public LogicalPoint Origin { get; }

        public BarrierOrientation Orientation { get; }
    }
}
