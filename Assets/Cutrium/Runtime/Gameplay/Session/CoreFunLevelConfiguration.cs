using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;

namespace Cutrium.Gameplay.Session
{
    public readonly struct CoreFunLevelConfiguration
    {
        private readonly ReadOnlyCollection<ThreatMotionConfiguration>
            _threatMotions;

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
            float maximumExpectedCompletionSeconds,
            string purposeLine = "")
            : this(
                stableId,
                displayNumber,
                new[] { threatMotion },
                barrier,
                capture,
                maximumCatchUpTicks,
                developmentNote,
                maximumExpectedCompletionSeconds,
                purposeLine,
                PowerConfiguration.None)
        {
        }

        public CoreFunLevelConfiguration(
            string stableId,
            int displayNumber,
            IReadOnlyList<ThreatMotionConfiguration> threatMotions,
            BarrierConfiguration barrier,
            CaptureLevelConfiguration capture,
            int maximumCatchUpTicks,
            string developmentNote,
            float maximumExpectedCompletionSeconds,
            string purposeLine = "")
            : this(
                stableId,
                displayNumber,
                threatMotions,
                barrier,
                capture,
                maximumCatchUpTicks,
                developmentNote,
                maximumExpectedCompletionSeconds,
                purposeLine,
                PowerConfiguration.None)
        {
        }

        public CoreFunLevelConfiguration(
            string stableId,
            int displayNumber,
            IReadOnlyList<ThreatMotionConfiguration> threatMotions,
            BarrierConfiguration barrier,
            CaptureLevelConfiguration capture,
            int maximumCatchUpTicks,
            string developmentNote,
            float maximumExpectedCompletionSeconds,
            string purposeLine,
            PowerConfiguration power)
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

            if (threatMotions == null || threatMotions.Count == 0)
            {
                throw new ArgumentException(
                    "A core-fun level needs at least one normal threat.",
                    nameof(threatMotions));
            }

            var copiedThreats =
                new ThreatMotionConfiguration[threatMotions.Count];
            for (int index = 0; index < threatMotions.Count; index++)
            {
                ThreatMotionConfiguration threatMotion = threatMotions[index];
                ValidateThreat(threatMotion, nameof(threatMotions));
                copiedThreats[index] = threatMotion;
            }

            if (barrier.MinimumEdgeMargin * 2f
                >= Math.Min(
                    FixedBoardBounds.Width,
                    FixedBoardBounds.Height))
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
            _threatMotions = Array.AsReadOnly(copiedThreats);
            Barrier = barrier;
            Capture = capture;
            MaximumCatchUpTicks = maximumCatchUpTicks;
            DevelopmentNote = developmentNote ?? string.Empty;
            PurposeLine = purposeLine ?? string.Empty;
            MaximumExpectedCompletionSeconds =
                maximumExpectedCompletionSeconds;
            Power = power;
        }

        public string StableId { get; }

        public int DisplayNumber { get; }

        public ThreatMotionConfiguration ThreatMotion => _threatMotions[0];

        public IReadOnlyList<ThreatMotionConfiguration> ThreatMotions =>
            _threatMotions;

        public BarrierConfiguration Barrier { get; }

        public CaptureLevelConfiguration Capture { get; }

        public int MaximumCatchUpTicks { get; }

        public string DevelopmentNote { get; }

        public string PurposeLine { get; }

        public float MaximumExpectedCompletionSeconds { get; }

        public PowerConfiguration Power { get; }

        private static void ValidateThreat(
            ThreatMotionConfiguration threatMotion,
            string parameterName)
        {
            if (threatMotion.BoardBounds != FixedBoardBounds)
            {
                throw new ArgumentException(
                    "Core-fun levels must use the fixed 10-by-16 board.",
                    parameterName);
            }

            LogicalPoint spawn = threatMotion.InitialPosition;
            float radius = threatMotion.Radius;
            if (spawn.X - radius < threatMotion.BoardBounds.MinX
                || spawn.X + radius > threatMotion.BoardBounds.MaxX
                || spawn.Y - radius < threatMotion.BoardBounds.MinY
                || spawn.Y + radius > threatMotion.BoardBounds.MaxY)
            {
                throw new ArgumentException(
                    "Every complete threat spawn circle must fit inside the board.",
                    parameterName);
            }
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
