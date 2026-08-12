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
                    "learn-the-cut",
                    1,
                    new[]
                    {
                        Normal(5f, 8f, 0.8f, 0.6f, 1.6f),
                    },
                    0.75f,
                    3.4f,
                    3f,
                    "LEARN THE CUT",
                    "A calm single threat teaches that the empty side is captured.",
                    "Read the threat position, then make two safe edge-biased cuts.",
                    15f,
                    1),
                Level(
                    "vulnerable-barrier-timing",
                    2,
                    new[]
                    {
                        Normal(4.5f, 3.5f, 0.45f, 0.89f, 2.35f),
                    },
                    0.78f,
                    2.4f,
                    2.7f,
                    "WATCH THE THREAT",
                    "A crossing trajectory introduces the vulnerable barrier window.",
                    "Wait until the threat is moving away before committing the cut.",
                    20f,
                    1),
                Level(
                    "two-normal-threats",
                    3,
                    new[]
                    {
                        Normal(3f, 5f, 0.9f, 0.44f, 2.05f),
                        Normal(7f, 11f, -0.82f, -0.57f, 2.2f),
                    },
                    0.78f,
                    3f,
                    2.3f,
                    "KEEP THEM TOGETHER",
                    "Two predictable threats make grouping the strategic constraint.",
                    "Choose cuts that leave both threats in the same surviving room.",
                    24f,
                    2),
                Level(
                    "confident-large-capture",
                    4,
                    new[]
                    {
                        Normal(7.6f, 12.2f, -0.72f, -0.69f, 2.25f),
                    },
                    0.84f,
                    3.2f,
                    1.8f,
                    "CUT WITH CONFIDENCE",
                    "Open space and one readable threat invite a large decisive capture.",
                    "Commit farther from the edge and claim a large empty region at once.",
                    26f,
                    2),
                Level(
                    "hunter-introduction",
                    5,
                    new[]
                    {
                        Hunter(5f, 8f, 0.8f, 0.6f, 2f, 0.22f),
                    },
                    0.8f,
                    2.85f,
                    2.2f,
                    "MEET THE HUNTER",
                    "The first Hunter bends toward growing barriers without a speed spike.",
                    "Notice the steering response and commit where the Hunter cannot turn in time.",
                    28f,
                    2),
                Level(
                    "pulse-introduction",
                    6,
                    new[]
                    {
                        Pulse(4f, 10f, 0.65f, -0.76f, 2f, 0.55f, 1.4f, 1.4f, 0.8f),
                    },
                    0.8f,
                    2.8f,
                    2.1f,
                    "READ THE PULSE",
                    "The first Pulse alternates a generous slow phase with a readable burst.",
                    "Start longer cuts during the slow phase instead of reacting to raw speed.",
                    30f,
                    2),
                Level(
                    "freeze-pulse-window",
                    7,
                    new[]
                    {
                        Pulse(6.2f, 8.5f, -0.74f, 0.67f, 2.15f, 0.55f, 1.45f, 1.25f, 0.85f),
                    },
                    0.82f,
                    2.35f,
                    2f,
                    "CREATE A FREEZE WINDOW",
                    "One Freeze Pulse charge turns the Pulse threat's fast phase into a rescue window.",
                    "Commit a valuable long cut, then freeze when the threat accelerates toward it.",
                    32f,
                    3,
                    new CoreFunPowerDefinition(1, 3f, 0.12f, 0, 600f)),
                Level(
                    "instant-barrier-window",
                    8,
                    new[]
                    {
                        Normal(3f, 6f, 0.88f, 0.48f, 2.1f),
                        Normal(7.2f, 10.8f, -0.68f, -0.73f, 2.15f),
                    },
                    0.84f,
                    2.2f,
                    1.9f,
                    "FINISH IT INSTANTLY",
                    "Two threats and slow growth make the single Instant Barrier charge legible.",
                    "Save Instant Barrier for the longest exposed cut or the final target push.",
                    32f,
                    3,
                    new CoreFunPowerDefinition(0, 3f, 0.12f, 1, 600f)),
                Level(
                    "hunter-normal-pair",
                    9,
                    new[]
                    {
                        Hunter(3.2f, 5.2f, 0.82f, 0.57f, 1.95f, 0.22f),
                        Normal(7f, 11.4f, -0.78f, -0.63f, 2.15f),
                    },
                    0.82f,
                    2.7f,
                    1.8f,
                    "TRACK TWO INTENTIONS",
                    "A reactive Hunter and predictable normal threat demand different reads.",
                    "Group both threats while accounting for only one of them steering toward the cut.",
                    35f,
                    3),
                Level(
                    "pulse-multiple-threats",
                    10,
                    new[]
                    {
                        Pulse(3.3f, 10.8f, 0.78f, -0.62f, 1.95f, 0.55f, 1.45f, 1.25f, 0.85f),
                        Normal(7.1f, 5.2f, -0.84f, 0.54f, 2.15f),
                    },
                    0.84f,
                    2.6f,
                    1.7f,
                    "FIND THE SHARED WINDOW",
                    "Pulse timing now matters while a second threat constrains the safe room.",
                    "Wait for the Pulse slow phase and a moment when both threats can remain grouped.",
                    38f,
                    4),
                Level(
                    "meaningful-power-choice",
                    11,
                    new[]
                    {
                        Hunter(3.2f, 6f, 0.82f, 0.57f, 2f, 0.24f),
                        Pulse(7f, 10.5f, -0.76f, -0.65f, 1.9f, 0.55f, 1.5f, 1.2f, 0.8f),
                    },
                    0.86f,
                    2.35f,
                    1.6f,
                    "CHOOSE THE RIGHT POWER",
                    "Hunter and Pulse overlap so Freeze and Instant solve different dangerous moments.",
                    "Use Freeze to create control and reserve Instant for a high-value exposed cut.",
                    42f,
                    4,
                    new CoreFunPowerDefinition(1, 3f, 0.12f, 1, 600f)),
                Level(
                    "first-twelve-mastery",
                    12,
                    new[]
                    {
                        Hunter(2.8f, 5f, 0.86f, 0.51f, 1.95f, 0.25f),
                        Pulse(7.2f, 8.3f, -0.72f, 0.69f, 1.9f, 0.55f, 1.5f, 1.2f, 0.8f),
                        Normal(4.8f, 12.5f, 0.62f, -0.78f, 2.1f),
                    },
                    0.88f,
                    2.5f,
                    1.5f,
                    "MASTER THE BOARD",
                    "Three readable identities combine the approved mechanics without a raw speed wall.",
                    "Group all threats, read the Pulse, respect the Hunter, and spend each power deliberately.",
                    45f,
                    5,
                    new CoreFunPowerDefinition(1, 3f, 0.12f, 1, 600f)),
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
            CoreFunPowerDefinition power = null) =>
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
                difficultyRating);

        private static CoreFunThreatDefinition Normal(
            float x,
            float y,
            float directionX,
            float directionY,
            float speed) =>
            Threat(
                x,
                y,
                directionX,
                directionY,
                speed,
                null);

        private static CoreFunThreatDefinition Hunter(
            float x,
            float y,
            float directionX,
            float directionY,
            float speed,
            float steerFactor) =>
            Threat(
                x,
                y,
                directionX,
                directionY,
                speed,
                new CoreFunThreatBehaviorDefinition(steerFactor));

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
