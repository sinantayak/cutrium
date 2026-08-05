using System;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;

namespace Cutrium.Gameplay.Session
{
    public readonly struct CoreFunLevelConfiguration
    {
        public static readonly LogicalRect FixedBoardBounds =
            new LogicalRect(0f, 0f, 10f, 16f);

        public CoreFunLevelConfiguration(
            string stableId,
            int displayNumber,
            ThreatMotionConfiguration threatMotion,
            BarrierConfiguration barrier,
            CaptureLevelConfiguration capture,
            int maximumCatchUpTicks,
            string developmentNote,
            float maximumExpectedCompletionSeconds)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                throw new ArgumentException(
                    "A level requires a stable non-empty ID.",
                    nameof(stableId));
            }

            if (displayNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(displayNumber));
            }

            if (threatMotion.BoardBounds != FixedBoardBounds)
            {
                throw new ArgumentException(
                    "Core-fun levels must use the fixed 10-by-16 board.",
                    nameof(threatMotion));
            }

            LogicalPoint spawn = threatMotion.InitialPosition;
            float radius = threatMotion.Radius;
            if (spawn.X - radius < threatMotion.BoardBounds.MinX
                || spawn.X + radius > threatMotion.BoardBounds.MaxX
                || spawn.Y - radius < threatMotion.BoardBounds.MinY
                || spawn.Y + radius > threatMotion.BoardBounds.MaxY)
            {
                throw new ArgumentException(
                    "The complete threat spawn circle must fit inside the board.",
                    nameof(threatMotion));
            }

            if (barrier.MinimumEdgeMargin * 2f
                >= Math.Min(
                    threatMotion.BoardBounds.Width,
                    threatMotion.BoardBounds.Height))
            {
                throw new ArgumentException(
                    "The minimum cut margin leaves no legal cut span.",
                    nameof(barrier));
            }

            if (maximumCatchUpTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumCatchUpTicks));
            }

            if (!IsFinite(maximumExpectedCompletionSeconds)
                || maximumExpectedCompletionSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumExpectedCompletionSeconds));
            }

            StableId = stableId;
            DisplayNumber = displayNumber;
            ThreatMotion = threatMotion;
            Barrier = barrier;
            Capture = capture;
            MaximumCatchUpTicks = maximumCatchUpTicks;
            DevelopmentNote = developmentNote ?? string.Empty;
            MaximumExpectedCompletionSeconds =
                maximumExpectedCompletionSeconds;
        }

        public string StableId { get; }

        public int DisplayNumber { get; }

        public ThreatMotionConfiguration ThreatMotion { get; }

        public BarrierConfiguration Barrier { get; }

        public CaptureLevelConfiguration Capture { get; }

        public int MaximumCatchUpTicks { get; }

        public string DevelopmentNote { get; }

        public float MaximumExpectedCompletionSeconds { get; }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
