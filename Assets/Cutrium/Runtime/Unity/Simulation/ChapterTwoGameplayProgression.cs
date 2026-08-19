using UnityEngine;

namespace Cutrium.Unity.Simulation
{
    /// Chapter 2 introduces motion variants and Gravity Well without adding
    /// another visual threat identity. Comet is a small, fast Normal; Heavy
    /// is a larger, slow Normal, so both keep the normal threat artwork.
    public static class ChapterTwoGameplayProgression
    {
        public const int LevelCount = 12;

        private const float NormalRadius = 0.35f;
        private const float CometRadius = 0.29f;
        private const float HeavyRadius = 0.52f;
        private const float BarrierCollisionHalfWidth = 0.08f;
        private const int MaximumImpactsPerTick = 8;
        private const int MaximumBarrierSolverIterations = 16;
        private const int MaximumCatchUpTicks = 8;

        public static CoreFunLevelDefinition[] CreateDefinitions() =>
            new[]
            {
                Level(
                    "motion-reset", 13,
                    new[] { Normal(5f, 8f, 0.78f, 0.63f, 2f) },
                    0.76f, 3.1f, 2.7f,
                    "READ THE ROOM",
                    "A calm opener resets the rhythm before motion variants arrive.",
                    "Make confident medium cuts and re-establish the grouping read.",
                    22f, 2, maximumAcceptedBarrierBreaks: 5),
                Level(
                    "parallel-pair", 14,
                    new[]
                    {
                        Normal(3f, 4.6f, 0.82f, 0.57f, 1.9f),
                        Normal(7f, 11.4f, 0.82f, 0.57f, 1.95f),
                    },
                    0.78f, 2.9f, 2.5f,
                    "MOVE WITH THEM",
                    "Two parallel Normals create a readable shared movement window.",
                    "Use their shared direction to place a larger safe cut.",
                    27f, 2, maximumAcceptedBarrierBreaks: 5),
                Level(
                    "crossing-pair", 15,
                    new[]
                    {
                        Normal(2.8f, 5.2f, 0.82f, 0.57f, 2.05f),
                        Normal(7.2f, 10.8f, -0.82f, -0.57f, 2.05f),
                    },
                    0.8f, 2.65f, 2.25f,
                    "WAIT FOR THE CROSS",
                    "Crossing paths vary the safe window without raising raw speed.",
                    "Let the threats cross, then cut behind their shared position.",
                    30f, 3, maximumAcceptedBarrierBreaks: 4),
                Level(
                    "comet-introduction", 16,
                    new[] { Comet(5f, 8f, 0.74f, 0.67f, 3.25f) },
                    0.78f, 3f, 2.4f,
                    "MEET THE COMET",
                    "A smaller, faster Normal introduces speed with generous growth.",
                    "Track the quick pass and commit as the Comet moves away.",
                    29f, 3,
                    introTitle: "COMET",
                    introMessage: "SMALL AND FAST",
                    maximumAcceptedBarrierBreaks: 4),
                Level(
                    "heavy-introduction", 17,
                    new[] { Heavy(5f, 8f, 0.66f, 0.75f, 1.45f) },
                    0.8f, 2.8f, 2.25f,
                    "GIVE IT SPACE",
                    "A slow Heavy occupies more room and changes edge clearance.",
                    "Use its calm pace, but respect the larger collision radius.",
                    30f, 3,
                    introTitle: "HEAVY",
                    introMessage: "SLOW BUT LARGE",
                    maximumAcceptedBarrierBreaks: 4),
                Level(
                    "comet-normal-pair", 18,
                    new[]
                    {
                        Comet(3f, 5f, 0.86f, 0.51f, 3.1f),
                        Normal(7f, 11f, -0.72f, -0.69f, 1.95f),
                    },
                    0.82f, 2.7f, 2.05f,
                    "READ TWO SPEEDS",
                    "Comet and Normal separate timing from grouping.",
                    "Wait until the faster threat rejoins the safer shared side.",
                    34f, 3, maximumAcceptedBarrierBreaks: 4),
                Level(
                    "heavy-normal-pair", 19,
                    new[]
                    {
                        Heavy(3.1f, 5.3f, 0.72f, 0.69f, 1.4f),
                        Normal(7f, 11f, -0.78f, -0.63f, 2.15f),
                    },
                    0.82f, 2.6f, 2f,
                    "BALANCE THE PAIR",
                    "Heavy clearance and Normal pace produce a gentle spatial puzzle.",
                    "Keep both in one room without shaving too close to the Heavy.",
                    35f, 3, maximumAcceptedBarrierBreaks: 4),
                Level(
                    "gravity-well-introduction", 20,
                    new[]
                    {
                        Normal(3f, 5f, 0.84f, 0.54f, 2f),
                        Normal(7f, 11f, -0.8f, -0.6f, 2f),
                    },
                    0.8f, 2.8f, 2.2f,
                    "BEND THEIR PATH",
                    "One Gravity Well gently gathers nearby threats in its active room.",
                    "Place the well where both paths can curve into a shared safe side.",
                    34f, 3,
                    GravityPower(),
                    introTitle: "GRAVITY WELL",
                    introMessage: "TAP A POINT TO PULL",
                    maximumAcceptedBarrierBreaks: 4),
                Level(
                    "gravity-heavy-pair", 21,
                    new[]
                    {
                        Heavy(3f, 5f, 0.7f, 0.71f, 1.45f),
                        Normal(7f, 11f, -0.76f, -0.65f, 2.15f),
                    },
                    0.83f, 2.55f, 1.9f,
                    "SHAPE THE GROUP",
                    "Gravity helps align a large Heavy with a quicker Normal.",
                    "Pull early, then use the gathered room for one valuable cut.",
                    38f, 4, GravityPower(), maximumAcceptedBarrierBreaks: 4),
                Level(
                    "pulse-comet-window", 22,
                    new[]
                    {
                        Pulse(3.1f, 10.7f, 0.78f, -0.62f, 1.9f),
                        Comet(7f, 5.2f, -0.84f, 0.54f, 3.05f),
                    },
                    0.84f, 2.55f, 1.8f,
                    "CONTROL THE BURST",
                    "Pulse and Comet make Freeze useful without adding another rule.",
                    "Spend Freeze when their fast windows overlap around a long cut.",
                    40f, 4,
                    new CoreFunPowerDefinition(1, 3.5f, 0.1f, 0, 600f),
                    maximumAcceptedBarrierBreaks: 3),
                Level(
                    "four-threat-gravity", 23,
                    new[]
                    {
                        Normal(2.2f, 4f, 0.78f, 0.62f, 1.55f),
                        Normal(7.8f, 4.5f, -0.74f, 0.67f, 1.55f),
                        Normal(2.5f, 12f, 0.7f, -0.71f, 1.5f),
                        Normal(7.5f, 11.5f, -0.68f, -0.73f, 1.5f),
                    },
                    0.82f, 2.9f, 1.9f,
                    "GATHER THE CROWD",
                    "Four slow Normals add spectacle while Gravity keeps the board manageable.",
                    "Gather a useful pair rather than trying to control every threat.",
                    42f, 4, GravityPower(), maximumAcceptedBarrierBreaks: 4),
                Level(
                    "motion-and-gravity-mastery", 24,
                    new[]
                    {
                        Hunter(2.7f, 4.8f, 0.84f, 0.54f, 1.9f),
                        Comet(7.3f, 6.3f, -0.82f, 0.57f, 3.05f),
                        Heavy(5f, 11.8f, 0.55f, -0.84f, 1.4f),
                    },
                    0.86f, 2.55f, 1.65f,
                    "MASTER THE MOTION",
                    "Hunter, Comet, and Heavy combine with two deliberate power choices.",
                    "Use Gravity to group, then save Instant for the final exposed cut.",
                    45f, 5,
                    new CoreFunPowerDefinition(
                        0, 3f, 0.12f, 1, 600f,
                        1, 4.25f, 4.5f, 105f),
                    maximumAcceptedCuts: 10,
                    expectedReasonableCutUsage: 8,
                    introTitle: "CHAPTER MASTERY",
                    introMessage: "SHAPE THEN CUT",
                    maximumAcceptedBarrierBreaks: 3),
            };

        private static CoreFunPowerDefinition GravityPower() =>
            new CoreFunPowerDefinition(
                0, 3f, 0.12f, 0, 600f,
                1, 4.25f, 4.5f, 105f);

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
            float x, float y, float dx, float dy, float speed) =>
            Threat(x, y, dx, dy, speed, NormalRadius, null);

        private static CoreFunThreatDefinition Comet(
            float x, float y, float dx, float dy, float speed) =>
            Threat(x, y, dx, dy, speed, CometRadius, null);

        private static CoreFunThreatDefinition Heavy(
            float x, float y, float dx, float dy, float speed) =>
            Threat(x, y, dx, dy, speed, HeavyRadius, null);

        private static CoreFunThreatDefinition Hunter(
            float x, float y, float dx, float dy, float speed) =>
            Threat(
                x, y, dx, dy, speed, NormalRadius,
                new CoreFunThreatBehaviorDefinition(0.68f, 52f));

        private static CoreFunThreatDefinition Pulse(
            float x, float y, float dx, float dy, float speed) =>
            Threat(
                x, y, dx, dy, speed, NormalRadius,
                new CoreFunThreatBehaviorDefinition(
                    0.45f, 1.7f, 1.15f, 0.85f));

        private static CoreFunThreatDefinition Threat(
            float x,
            float y,
            float dx,
            float dy,
            float speed,
            float radius,
            CoreFunThreatBehaviorDefinition behavior) =>
            new CoreFunThreatDefinition(
                new Vector2(x, y),
                new Vector2(dx, dy),
                speed,
                radius,
                MaximumImpactsPerTick,
                behavior);
    }
}
