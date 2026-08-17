using UnityEngine;

namespace Cutrium.Unity.Simulation
{
    /// The deliberately small gameplay catalog used for the first human
    /// progression review. This is gameplay content only: landmarks are
    /// paired externally by the presentation layer.
    public static class FirstTwelveGameplayProgression
    {
        public const int LevelCount = 12;

        private const float ThreatRadius = 0.35f;
        private const float BarrierCollisionHalfWidth = 0.08f;
        private const int MaximumImpactsPerTick = 8;
        private const int MaximumBarrierSolverIterations = 16;
        private const int MaximumCatchUpTicks = 8;

        public static CoreFunLevelDefinition[] CreateDefinitions() =>
            new[]
            {
                Level(
                    "learn-the-cut", 1,
                    new[] { Normal(5f, 8f, 0.8f, 0.6f, 1.6f) },
                    0.75f, 3.4f, 3f,
                    "LEARN THE CUT",
                    "A calm single threat teaches that the empty side is captured.",
                    "Read the threat position, then make two safe edge-biased cuts.",
                    15f, 1,
                    expectedReasonableCutUsage: 3,
                    maximumAcceptedBarrierBreaks: 5),
                Level(
                    "vulnerable-barrier-timing", 2,
                    new[] { Normal(4.5f, 3.5f, 0.45f, 0.89f, 2.35f) },
                    0.78f, 2.15f, 2.7f,
                    "WATCH THE THREAT",
                    "A crossing trajectory and longer exposure punish a repeated blind rhythm.",
                    "Wait until the threat is moving away before committing the cut.",
                    24f, 2,
                    expectedReasonableCutUsage: 5,
                    maximumAcceptedBarrierBreaks: 5),
                Level(
                    "two-normal-threats", 3,
                    new[]
                    {
                        Normal(3f, 5f, 0.9f, 0.44f, 2.05f),
                        Normal(7f, 11f, -0.82f, -0.57f, 2.2f),
                    },
                    0.8f, 2.8f, 2.3f,
                    "KEEP THEM TOGETHER",
                    "Two predictable threats make grouping the strategic constraint.",
                    "Choose cuts that leave both threats in the same surviving room.",
                    28f, 2,
                    expectedReasonableCutUsage: 6,
                    maximumAcceptedBarrierBreaks: 4),
                Level(
                    "confident-large-capture", 4,
                    new[] { Normal(7.6f, 12.2f, -0.72f, -0.69f, 2.25f) },
                    0.84f, 3.2f, 1.8f,
                    "CUT WITH CONFIDENCE",
                    "A forgiving ten-cut budget rewards decisive captures over tiny edge shaving.",
                    "Plan larger empty regions and keep spare attempts for mistakes.",
                    30f, 2,
                    maximumAcceptedCuts: 10,
                    expectedReasonableCutUsage: 7,
                    introTitle: "10 CUTS",
                    introMessage: "MAKE THEM COUNT",
                    maximumAcceptedBarrierBreaks: 4),
                Level(
                    "hunter-introduction", 5,
                    new[] { Hunter(5f, 8f, 0.8f, 0.6f, 2f, 0.72f, 55f) },
                    0.8f, 2.6f, 2.2f,
                    "MEET THE HUNTER",
                    "A lone Hunter makes its bounded steering reaction unmistakable.",
                    "Bait the reaction, then commit where its limited turn cannot recover in time.",
                    30f, 2,
                    expectedReasonableCutUsage: 5,
                    introTitle: "HUNTER",
                    introMessage: "REACTS TO YOUR CUTS",
                    maximumAcceptedBarrierBreaks: 4),
                Level(
                    "pulse-introduction", 6,
                    new[]
                    {
                        Pulse(4f, 10f, 0.65f, -0.76f, 1.9f,
                            0.45f, 1.65f, 1.35f, 0.75f),
                    },
                    0.8f, 2.55f, 2.1f,
                    "READ THE PULSE",
                    "One Pulse alternates a generous slow phase with a clear energetic burst.",
                    "Start longer cuts during the slow phase instead of reacting to average speed.",
                    32f, 2,
                    expectedReasonableCutUsage: 5,
                    introTitle: "PULSE",
                    introMessage: "WATCH ITS SPEED",
                    maximumAcceptedBarrierBreaks: 4),
                Level(
                    "freeze-pulse-window", 7,
                    new[]
                    {
                        Pulse(6.2f, 8.5f, -0.74f, 0.67f, 2f,
                            0.45f, 1.75f, 1.15f, 0.85f),
                    },
                    0.82f, 1.85f, 2f,
                    "CREATE A FREEZE WINDOW",
                    "Slow growth, a strong Pulse burst, and cut economy make Freeze tactically valuable.",
                    "Commit a valuable long cut, then freeze the dangerous acceleration window.",
                    36f, 3,
                    new CoreFunPowerDefinition(1, 3.8f, 0.1f, 0, 600f),
                    10, 7,
                    "FREEZE", "CREATE A SAFE WINDOW",
                    maximumAcceptedBarrierBreaks: 3),
                Level(
                    "instant-barrier-window", 8,
                    new[]
                    {
                        Normal(3f, 6f, 0.88f, 0.48f, 2.1f),
                        Normal(7.2f, 10.8f, -0.68f, -0.73f, 2.15f),
                    },
                    0.84f, 1.75f, 1.9f,
                    "FINISH IT INSTANTLY",
                    "Two threats expose long cuts enough that one saved Instant charge has obvious value.",
                    "Group first, then spend Instant on the longest exposed target push.",
                    38f, 3,
                    new CoreFunPowerDefinition(0, 3f, 0.12f, 1, 600f),
                    9, 6,
                    "INSTANT", "SAVE IT FOR A RISKY CUT",
                    maximumAcceptedBarrierBreaks: 3),
                Level(
                    "hunter-normal-pair", 9,
                    new[]
                    {
                        Hunter(3.2f, 5.2f, 0.82f, 0.57f, 1.95f, 0.68f, 52f),
                        Normal(7f, 11.4f, -0.78f, -0.63f, 2.15f),
                    },
                    0.84f, 2.45f, 1.8f,
                    "TRACK TWO INTENTIONS",
                    "A reactive Hunter and predictable Normal demand different reads while grouping.",
                    "Keep both threats together while accounting for only the Hunter reacting.",
                    38f, 3,
                    expectedReasonableCutUsage: 7,
                    maximumAcceptedBarrierBreaks: 3),
                Level(
                    "pulse-multiple-threats", 10,
                    new[]
                    {
                        Pulse(3.3f, 10.8f, 0.78f, -0.62f, 1.9f,
                            0.45f, 1.7f, 1.15f, 0.85f),
                        Normal(7.1f, 5.2f, -0.84f, 0.54f, 2.15f),
                    },
                    0.84f, 2.35f, 1.7f,
                    "FIND THE SHARED WINDOW",
                    "Pulse timing matters while a Normal constrains the safe grouping window.",
                    "Wait until slow phase and spatial grouping agree before cutting.",
                    40f, 4,
                    expectedReasonableCutUsage: 7,
                    maximumAcceptedBarrierBreaks: 3),
                Level(
                    "meaningful-power-choice", 11,
                    new[]
                    {
                        Hunter(3.2f, 6f, 0.82f, 0.57f, 1.95f, 0.72f, 55f),
                        Pulse(7f, 10.5f, -0.76f, -0.65f, 1.85f,
                            0.45f, 1.75f, 1.1f, 0.8f),
                    },
                    0.87f, 1.85f, 1.6f,
                    "CHOOSE THE RIGHT POWER",
                    "A lower cut budget and two distinct danger patterns reward different power windows.",
                    "Freeze controls a bad phase; Instant secures a high-value exposed cut.",
                    43f, 4,
                    new CoreFunPowerDefinition(1, 3.5f, 0.1f, 1, 600f),
                    8, 6,
                    maximumAcceptedBarrierBreaks: 2),
                Level(
                    "first-twelve-mastery", 12,
                    new[]
                    {
                        Hunter(2.8f, 5f, 0.86f, 0.51f, 1.9f, 0.7f, 52f),
                        Pulse(7.2f, 8.3f, -0.72f, 0.69f, 1.85f,
                            0.45f, 1.75f, 1.1f, 0.8f),
                        Normal(4.8f, 12.5f, 0.62f, -0.78f, 2.1f),
                    },
                    0.9f, 2f, 1.5f,
                    "MASTER THE BOARD",
                    "Three readable identities, the highest target, and a finite cut budget test the full set.",
                    "Group, read phase, bait reaction, and spend both powers deliberately.",
                    45f, 5,
                    new CoreFunPowerDefinition(1, 3.5f, 0.1f, 1, 600f),
                    10, 8,
                    maximumAcceptedBarrierBreaks: 2),
            };

        private static CoreFunLevelDefinition Level(
            string stableId,
            int displayNumber,
            CoreFunThreatDefinition[] threats,
            float targetCapturedFraction,
            float barrierGrowthSpeed,
            float minimumCutMargin,
            string purposeLine,
            string developmentNote,
            string intendedDecision,
            float expectedHumanCompletionSeconds,
            int difficultyRating,
            CoreFunPowerDefinition power = null,
            int maximumAcceptedCuts = 0,
            int expectedReasonableCutUsage = 0,
            string introTitle = "",
            string introMessage = "",
            int maximumAcceptedBarrierBreaks = 0) =>
            new CoreFunLevelDefinition(
                stableId,
                displayNumber,
                threats,
                targetCapturedFraction,
                barrierGrowthSpeed,
                BarrierCollisionHalfWidth,
                minimumCutMargin,
                MaximumBarrierSolverIterations,
                MaximumCatchUpTicks,
                developmentNote,
                expectedHumanCompletionSeconds,
                purposeLine,
                power,
                intendedDecision,
                difficultyRating,
                maximumAcceptedCuts,
                expectedReasonableCutUsage,
                introTitle,
                introMessage,
                maximumAcceptedBarrierBreaks);

        private static CoreFunThreatDefinition Normal(
            float x, float y, float directionX, float directionY, float speed) =>
            Threat(x, y, directionX, directionY, speed, null);

        private static CoreFunThreatDefinition Hunter(
            float x,
            float y,
            float directionX,
            float directionY,
            float speed,
            float steerFactor,
            float maximumTurnDegrees) =>
            Threat(
                x,
                y,
                directionX,
                directionY,
                speed,
                new CoreFunThreatBehaviorDefinition(
                    steerFactor,
                    maximumTurnDegrees));

        private static CoreFunThreatDefinition Pulse(
            float x,
            float y,
            float directionX,
            float directionY,
            float speed,
            float slowMultiplier,
            float fastMultiplier,
            float slowSeconds,
            float fastSeconds) =>
            Threat(
                x,
                y,
                directionX,
                directionY,
                speed,
                new CoreFunThreatBehaviorDefinition(
                    slowMultiplier,
                    fastMultiplier,
                    slowSeconds,
                    fastSeconds));

        private static CoreFunThreatDefinition Threat(
            float x,
            float y,
            float directionX,
            float directionY,
            float speed,
            CoreFunThreatBehaviorDefinition behavior) =>
            new CoreFunThreatDefinition(
                new Vector2(x, y),
                new Vector2(directionX, directionY),
                speed,
                ThreatRadius,
                MaximumImpactsPerTick,
                behavior);
    }
}
