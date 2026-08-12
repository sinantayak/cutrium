using System;
using System.Linq;
using System.Reflection;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Gameplay.Threats;
using Cutrium.Presentation.Landmark;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class FirstTwelveGameplayProgressionTests
    {
        private static readonly string[] ExpectedIds =
        {
            "learn-the-cut",
            "vulnerable-barrier-timing",
            "two-normal-threats",
            "confident-large-capture",
            "hunter-introduction",
            "pulse-introduction",
            "freeze-pulse-window",
            "instant-barrier-window",
            "hunter-normal-pair",
            "pulse-multiple-threats",
            "meaningful-power-choice",
            "first-twelve-mastery",
        };

        [Test]
        public void Catalog_HasExactlyTwelveOrderedUniqueGameplayLevels()
        {
            CoreFunLevelDefinition[] definitions = Definitions();
            CoreFunLevelConfiguration[] configurations = Configurations();
            var catalog = new CoreFunLevelCatalog(configurations);

            Assert.That(definitions, Has.Length.EqualTo(12));
            Assert.That(catalog.Count, Is.EqualTo(12));
            Assert.That(
                definitions.Select(level => level.StableId),
                Is.EqualTo(ExpectedIds));
            Assert.That(
                definitions.Select(level => level.DisplayNumber),
                Is.EqualTo(Enumerable.Range(1, 12)));
            Assert.That(
                definitions.Select(level => level.StableId).Distinct().Count(),
                Is.EqualTo(12));
        }

        [Test]
        public void Catalog_RecordsPurposeDecisionTimeAndDifficultyForEveryLevel()
        {
            CoreFunLevelDefinition[] definitions = Definitions();

            Assert.That(
                definitions.Select(level => level.ExpectedHumanCompletionSeconds),
                Is.EqualTo(new[]
                {
                    15f, 20f, 24f, 26f, 28f, 30f,
                    32f, 32f, 35f, 38f, 42f, 45f,
                }));
            Assert.That(
                definitions.Select(level => level.DifficultyRating),
                Is.EqualTo(new[] { 1, 1, 2, 2, 2, 2, 3, 3, 3, 4, 4, 5 }));
            Assert.That(
                definitions.Skip(1).All(
                    level => level.ExpectedHumanCompletionSeconds >= 20f
                        && level.ExpectedHumanCompletionSeconds <= 45f),
                Is.True);

            foreach (CoreFunLevelDefinition level in definitions)
            {
                Assert.That(level.PurposeLine, Is.Not.Empty, level.StableId);
                Assert.That(level.DevelopmentNote, Is.Not.Empty, level.StableId);
                Assert.That(level.IntendedDecision, Is.Not.Empty, level.StableId);
                Assert.That(level.DifficultyRating, Is.InRange(1, 5));

                CoreFunLevelConfiguration runtime =
                    level.ToRuntimeConfiguration();
                Assert.That(runtime.IntendedDecision,
                    Is.EqualTo(level.IntendedDecision));
                Assert.That(runtime.ExpectedHumanCompletionSeconds,
                    Is.EqualTo(level.ExpectedHumanCompletionSeconds));
                Assert.That(runtime.DifficultyRating,
                    Is.EqualTo(level.DifficultyRating));
            }
        }

        [Test]
        public void Catalog_UsesExpectedThreatAndPowerProgression()
        {
            CoreFunLevelConfiguration[] levels = Configurations();

            Assert.That(
                levels.Select(level => level.ThreatMotions.Count),
                Is.EqualTo(new[] { 1, 1, 2, 1, 1, 1, 1, 2, 2, 2, 2, 3 }));
            AssertKinds(levels[0], ThreatBehaviorKind.Normal);
            AssertKinds(levels[1], ThreatBehaviorKind.Normal);
            AssertKinds(
                levels[2],
                ThreatBehaviorKind.Normal,
                ThreatBehaviorKind.Normal);
            AssertKinds(levels[3], ThreatBehaviorKind.Normal);
            AssertKinds(levels[4], ThreatBehaviorKind.Hunter);
            AssertKinds(levels[5], ThreatBehaviorKind.Pulse);
            AssertKinds(levels[6], ThreatBehaviorKind.Pulse);
            AssertKinds(
                levels[7],
                ThreatBehaviorKind.Normal,
                ThreatBehaviorKind.Normal);
            AssertKinds(
                levels[8],
                ThreatBehaviorKind.Hunter,
                ThreatBehaviorKind.Normal);
            AssertKinds(
                levels[9],
                ThreatBehaviorKind.Pulse,
                ThreatBehaviorKind.Normal);
            AssertKinds(
                levels[10],
                ThreatBehaviorKind.Hunter,
                ThreatBehaviorKind.Pulse);
            AssertKinds(
                levels[11],
                ThreatBehaviorKind.Hunter,
                ThreatBehaviorKind.Pulse,
                ThreatBehaviorKind.Normal);

            Assert.That(levels.Take(6).All(level =>
                level.Power == PowerConfiguration.None), Is.True);
            Assert.That(levels[6].Power.FreezePulseCharges, Is.EqualTo(1));
            Assert.That(levels[6].Power.InstantBarrierCharges, Is.Zero);
            Assert.That(levels[7].Power.FreezePulseCharges, Is.Zero);
            Assert.That(levels[7].Power.InstantBarrierCharges, Is.EqualTo(1));
            Assert.That(levels[8].Power, Is.EqualTo(PowerConfiguration.None));
            Assert.That(levels[9].Power, Is.EqualTo(PowerConfiguration.None));
            Assert.That(levels[10].Power.FreezePulseCharges, Is.EqualTo(1));
            Assert.That(levels[10].Power.InstantBarrierCharges, Is.EqualTo(1));
            Assert.That(levels[11].Power.FreezePulseCharges, Is.EqualTo(1));
            Assert.That(levels[11].Power.InstantBarrierCharges, Is.EqualTo(1));
        }

        [Test]
        public void Catalog_PreservesFixedBoardAndExactAuthoredCoreValues()
        {
            CoreFunLevelConfiguration[] levels = Configurations();

            Assert.That(
                levels.Select(level => level.Capture.TargetCapturedFraction),
                Is.EqualTo(new[]
                {
                    0.75f, 0.78f, 0.78f, 0.84f, 0.8f, 0.8f,
                    0.82f, 0.84f, 0.82f, 0.84f, 0.86f, 0.88f,
                }));
            Assert.That(
                levels.Select(level => level.Barrier.GrowthSpeed),
                Is.EqualTo(new[]
                {
                    3.4f, 2.4f, 3f, 3.2f, 2.85f, 2.8f,
                    2.35f, 2.2f, 2.7f, 2.6f, 2.35f, 2.5f,
                }));

            foreach (CoreFunLevelConfiguration level in levels)
            {
                Assert.That(level.ThreatMotions, Is.Not.Empty);
                foreach (ThreatMotionConfiguration threat in level.ThreatMotions)
                {
                    Assert.That(
                        threat.BoardBounds,
                        Is.EqualTo(new LogicalRect(0f, 0f, 10f, 16f)));
                    Assert.That(threat.Radius, Is.EqualTo(0.35f));
                    Assert.That(threat.MaximumImpactsPerTick, Is.EqualTo(8));
                    Assert.That(threat.InitialDirection.Length,
                        Is.GreaterThan(0f));
                }
            }
        }

        [Test]
        public void Catalog_PreservesExactThreatSpawnsDirectionsAndSpeeds()
        {
            CoreFunLevelDefinition[] levels = Definitions();

            AssertThreat(levels[0], 0, 5f, 8f, 0.8f, 0.6f, 1.6f);
            AssertThreat(levels[1], 0, 4.5f, 3.5f, 0.45f, 0.89f, 2.35f);
            AssertThreat(levels[2], 0, 3f, 5f, 0.9f, 0.44f, 2.05f);
            AssertThreat(levels[2], 1, 7f, 11f, -0.82f, -0.57f, 2.2f);
            AssertThreat(levels[3], 0, 7.6f, 12.2f, -0.72f, -0.69f, 2.25f);
            AssertThreat(levels[4], 0, 5f, 8f, 0.8f, 0.6f, 2f);
            AssertThreat(levels[5], 0, 4f, 10f, 0.65f, -0.76f, 2f);
            AssertThreat(levels[6], 0, 6.2f, 8.5f, -0.74f, 0.67f, 2.15f);
            AssertThreat(levels[7], 0, 3f, 6f, 0.88f, 0.48f, 2.1f);
            AssertThreat(levels[7], 1, 7.2f, 10.8f, -0.68f, -0.73f, 2.15f);
            AssertThreat(levels[8], 0, 3.2f, 5.2f, 0.82f, 0.57f, 1.95f);
            AssertThreat(levels[8], 1, 7f, 11.4f, -0.78f, -0.63f, 2.15f);
            AssertThreat(levels[9], 0, 3.3f, 10.8f, 0.78f, -0.62f, 1.95f);
            AssertThreat(levels[9], 1, 7.1f, 5.2f, -0.84f, 0.54f, 2.15f);
            AssertThreat(levels[10], 0, 3.2f, 6f, 0.82f, 0.57f, 2f);
            AssertThreat(levels[10], 1, 7f, 10.5f, -0.76f, -0.65f, 1.9f);
            AssertThreat(levels[11], 0, 2.8f, 5f, 0.86f, 0.51f, 1.95f);
            AssertThreat(levels[11], 1, 7.2f, 8.3f, -0.72f, 0.69f, 1.9f);
            AssertThreat(levels[11], 2, 4.8f, 12.5f, 0.62f, -0.78f, 2.1f);
        }

        [Test]
        public void DifficultyCurve_IsNotASpeedOnlyEscalation()
        {
            CoreFunLevelConfiguration[] levels = Configurations();
            float levelTwoMaxSpeed = levels[1].ThreatMotions.Max(
                threat => threat.Speed);
            float masteryMaxSpeed = levels[11].ThreatMotions.Max(
                threat => threat.Speed);

            Assert.That(levels[11].DifficultyRating,
                Is.GreaterThan(levels[1].DifficultyRating));
            Assert.That(masteryMaxSpeed, Is.LessThan(levelTwoMaxSpeed));
            Assert.That(levels[11].ThreatMotions.Count,
                Is.GreaterThan(levels[1].ThreatMotions.Count));
            Assert.That(levels[11].Capture.TargetCapturedFraction,
                Is.GreaterThan(levels[1].Capture.TargetCapturedFraction));
        }

        [Test]
        public void GameplayConfigurationTypes_DoNotContainLandmarkReferences()
        {
            AssertNoLandmarkMembers(typeof(CoreFunLevelDefinition));
            AssertNoLandmarkMembers(typeof(CoreFunLevelConfiguration));
            AssertNoLandmarkMembers(typeof(CoreFunLevelCatalogDefinition));
        }

        [Test]
        public void GameplayCatalogAsset_CopiesAndBuildsTwelveDefinitions()
        {
            CoreFunLevelCatalogDefinition asset =
                ScriptableObject.CreateInstance<CoreFunLevelCatalogDefinition>();
            CoreFunLevelDefinition[] source = Definitions();

            asset.ConfigureForSetup(source);
            source[0] = null;

            Assert.That(asset.Levels, Has.Count.EqualTo(12));
            Assert.That(asset.Levels[0], Is.Not.Null);
            Assert.That(asset.BuildRuntimeCatalog().Count, Is.EqualTo(12));
            UnityEngine.Object.DestroyImmediate(asset);
        }

        [Test]
        public void LandmarkCatalog_IsSeparateAndPairsByProgressionIndex()
        {
            LandmarkDefinition first = Landmark("first");
            LandmarkDefinition second = Landmark("second");
            LandmarkDefinition third = Landmark("third");
            LandmarkCatalog catalog =
                ScriptableObject.CreateInstance<LandmarkCatalog>();
            catalog.ConfigureForSetup(new[] { first, second, third });

            Assert.That(catalog.SelectForProgressionIndex(0), Is.SameAs(first));
            Assert.That(catalog.SelectForProgressionIndex(1), Is.SameAs(second));
            Assert.That(catalog.SelectForProgressionIndex(2), Is.SameAs(third));
            Assert.That(catalog.SelectForProgressionIndex(3), Is.SameAs(first));

            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
            UnityEngine.Object.DestroyImmediate(third);
        }

        [Test]
        public void DevelopmentNavigation_JumpsRetriesAndResetsInOneController()
        {
            var root = new GameObject("DevelopmentNavigationTest");
            root.SetActive(false);
            FirstPlayableController controller =
                root.AddComponent<FirstPlayableController>();
            controller.ConfigureLevelsForSetup(Definitions());
            root.SetActive(true);

            Assert.That(controller.LevelCount, Is.EqualTo(12));
            Assert.That(controller.TryJumpToLevelForDevelopment(12), Is.True);
            Assert.That(controller.CurrentLevelNumber, Is.EqualTo(12));
            Assert.That(controller.TryGoToPreviousLevelForDevelopment(), Is.True);
            Assert.That(controller.CurrentLevelNumber, Is.EqualTo(11));
            Assert.That(controller.TryGoToNextLevelForDevelopment(), Is.True);
            Assert.That(controller.CurrentLevelNumber, Is.EqualTo(12));
            Assert.That(controller.TryGoToNextLevelForDevelopment(), Is.False);
            Assert.That(controller.TryJumpToLevelForDevelopment(13), Is.False);
            Assert.That(controller.CurrentLevelNumber, Is.EqualTo(12));

            ThreatMotionSession beforeRetry = controller.Session;
            controller.RetryLevel();
            Assert.That(controller.Session, Is.Not.SameAs(beforeRetry));
            Assert.That(controller.CurrentLevelNumber, Is.EqualTo(12));
            Assert.That(controller.Session.CapturedFraction, Is.Zero);

            controller.RestartSequence();
            Assert.That(controller.CurrentLevelNumber, Is.EqualTo(1));
            Assert.That(controller.CurrentLevelId, Is.EqualTo("learn-the-cut"));
            Assert.That(controller.DevelopmentJumpCount, Is.EqualTo(3));
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static CoreFunLevelDefinition[] Definitions() =>
            FirstTwelveGameplayProgression.CreateDefinitions();

        private static CoreFunLevelConfiguration[] Configurations() =>
            Definitions()
                .Select(definition => definition.ToRuntimeConfiguration())
                .ToArray();

        private static void AssertKinds(
            CoreFunLevelConfiguration level,
            params ThreatBehaviorKind[] kinds)
        {
            Assert.That(
                level.ThreatMotions.Select(threat => threat.Behavior.Kind),
                Is.EqualTo(kinds),
                level.StableId);
        }

        private static void AssertThreat(
            CoreFunLevelDefinition level,
            int threatIndex,
            float x,
            float y,
            float directionX,
            float directionY,
            float speed)
        {
            CoreFunThreatDefinition threat = level.Threats[threatIndex];
            Assert.That(threat.InitialPosition, Is.EqualTo(new Vector2(x, y)));
            Assert.That(
                threat.InitialDirection,
                Is.EqualTo(new Vector2(directionX, directionY)));
            Assert.That(threat.Speed, Is.EqualTo(speed));
        }

        private static void AssertNoLandmarkMembers(Type type)
        {
            Type[] referencedTypes = type
                .GetMembers(
                    BindingFlags.Instance
                    | BindingFlags.Static
                    | BindingFlags.Public
                    | BindingFlags.NonPublic)
                .SelectMany(MemberTypes)
                .Where(candidate => candidate != null)
                .ToArray();
            Assert.That(
                referencedTypes.Any(candidate =>
                    candidate == typeof(LandmarkDefinition)
                    || candidate == typeof(LandmarkCatalog)
                    || candidate.Namespace == "Cutrium.Presentation.Landmark"),
                Is.False,
                type.FullName);
        }

        private static Type[] MemberTypes(MemberInfo member)
        {
            if (member is FieldInfo field)
            {
                return new[] { field.FieldType };
            }

            if (member is PropertyInfo property)
            {
                return new[] { property.PropertyType };
            }

            if (member is MethodInfo method)
            {
                return method.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .Append(method.ReturnType)
                    .ToArray();
            }

            if (member is ConstructorInfo constructor)
            {
                return constructor.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray();
            }

            return Array.Empty<Type>();
        }

        private static LandmarkDefinition Landmark(string id)
        {
            LandmarkDefinition landmark =
                ScriptableObject.CreateInstance<LandmarkDefinition>();
            landmark.ConfigureForSetup(id, id, string.Empty, string.Empty, null);
            return landmark;
        }
    }
}
