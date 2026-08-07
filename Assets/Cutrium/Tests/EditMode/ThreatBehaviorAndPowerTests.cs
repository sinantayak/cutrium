using System;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Feedback;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Gameplay.Threats;
using Cutrium.Unity.Simulation;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class ThreatBehaviorAndPowerTests
    {
        private static readonly GeometryTolerancePolicy Tolerance =
            new GeometryTolerancePolicy(0.0001f, 0.00001f, 0.0001f, 0.001f);

        private static readonly LogicalRect Board =
            CoreFunLevelConfiguration.FixedBoardBounds;

        [Test]
        public void ThreatBehaviorConfiguration_NormalIsInertAndValidated()
        {
            Assert.That(
                ThreatBehaviorConfiguration.Normal.Kind,
                Is.EqualTo(ThreatBehaviorKind.Normal));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThreatBehaviorConfiguration.CreateHunter(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThreatBehaviorConfiguration.CreateHunter(1.1f));
            Assert.DoesNotThrow(() =>
                ThreatBehaviorConfiguration.CreateHunter(1f));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThreatBehaviorConfiguration.CreatePulse(0f, 1.5f, 1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThreatBehaviorConfiguration.CreatePulse(0.5f, 1.5f, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThreatBehaviorConfiguration.CreatePulse(0.5f, 1.5f, 1f, 0f));
        }

        [Test]
        public void PowerConfiguration_RejectsInvalidChargedValuesAndNoneIsSafe()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PowerConfiguration(-1, 1f, 0.1f, 0, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PowerConfiguration(0, 1f, 0.1f, -1, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PowerConfiguration(1, 0f, 0.1f, 0, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PowerConfiguration(1, 1f, 1f, 0, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PowerConfiguration(1, 1f, 0f, 0, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PowerConfiguration(0, 1f, 0.1f, 1, 0f));

            Assert.That(PowerConfiguration.None.FreezePulseCharges, Is.Zero);
            Assert.That(PowerConfiguration.None.InstantBarrierCharges, Is.Zero);
        }

        [Test]
        public void Hunter_ReactsOnceWithBoundedBlendAndPreservesSpeed()
        {
            var behavior = ThreatBehaviorConfiguration.CreateHunter(0.5f);
            var configuration = new ThreatMotionConfiguration(
                Board,
                new LogicalPoint(5f, 8f),
                new LogicalVector(1f, 0f),
                2f,
                0.35f,
                8,
                behavior);
            var session = new ThreatMotionSession(configuration, Tolerance);

            BarrierStartResult result = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(5f, 12f),
                    BarrierOrientation.Vertical));

            Assert.That(result.Accepted, Is.True);
            float expected = 2f / MathF.Sqrt(2f);
            Assert.That(session.Threat.Velocity.X, Is.EqualTo(expected).Within(0.0005f));
            Assert.That(session.Threat.Velocity.Y, Is.EqualTo(expected).Within(0.0005f));
            Assert.That(session.Threat.Speed, Is.EqualTo(2f).Within(0.0005f));
        }

        [Test]
        public void Hunter_DoesNotReactToNonHunterThreatsInSameRoom()
        {
            var hunterConfiguration = new ThreatMotionConfiguration(
                Board,
                new LogicalPoint(3f, 5f),
                new LogicalVector(1f, 0f),
                2f,
                0.35f,
                8,
                ThreatBehaviorConfiguration.CreateHunter(0.5f));
            var normalConfiguration = new ThreatMotionConfiguration(
                Board,
                new LogicalPoint(7f, 11f),
                new LogicalVector(-1f, 0f),
                2f,
                0.35f,
                8);
            var session = new ThreatMotionSession(
                new[] { hunterConfiguration, normalConfiguration },
                Tolerance);
            LogicalVector normalVelocityBefore = session.Threats[1].Velocity;

            BarrierStartResult result = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(3f, 12f),
                    BarrierOrientation.Vertical));

            Assert.That(result.Accepted, Is.True);
            Assert.That(session.Threats[0].Velocity, Is.Not.EqualTo(
                hunterConfiguration.InitialDirection
                    * hunterConfiguration.Speed));
            Assert.That(session.Threats[1].Velocity, Is.EqualTo(
                normalVelocityBefore));
        }

        [Test]
        public void Hunter_DoesNotReactToBarrierStartedInAnotherRoom()
        {
            var hunterConfiguration = new ThreatMotionConfiguration(
                Board,
                new LogicalPoint(2f, 8f),
                new LogicalVector(1f, 0f),
                2f,
                0.35f,
                8,
                ThreatBehaviorConfiguration.CreateHunter(0.5f));
            var normalConfiguration = new ThreatMotionConfiguration(
                Board,
                new LogicalPoint(8f, 8f),
                new LogicalVector(-1f, 0f),
                2f,
                0.35f,
                8);
            var session = new ThreatMotionSession(
                new[] { hunterConfiguration, normalConfiguration },
                Tolerance);

            var manualSplit = new BarrierState(
                new BarrierId(1),
                new RoomId(1),
                new LogicalPoint(5f, 8f),
                BarrierOrientation.Vertical,
                8f,
                8f,
                8f,
                8f,
                8f,
                0.08f,
                BarrierLifecycle.Locked);
            RoomSplitApplyResult split =
                session.Board.TryApplyLockedBarrier(manualSplit);
            Assert.That(split.Applied, Is.True);
            LogicalVector hunterVelocityBefore = session.Threats
                .Single(threat => threat.RoomId == split.NegativeChild.Id)
                .Velocity;

            BarrierStartResult result = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(8f, 4f),
                    BarrierOrientation.Horizontal));

            Assert.That(result.Accepted, Is.True);
            Assert.That(session.Threats
                    .Single(threat => threat.RoomId == split.NegativeChild.Id)
                    .Velocity,
                Is.EqualTo(hunterVelocityBefore));
        }

        [Test]
        public void Pulse_AppliesSlowThenFastMultiplierOnScheduleAndPreservesDirection()
        {
            var behavior = ThreatBehaviorConfiguration.CreatePulse(
                0.5f,
                1.5f,
                1f,
                0.5f);
            var configuration = new ThreatMotionConfiguration(
                Board,
                new LogicalPoint(5f, 8f),
                new LogicalVector(1f, 0f),
                2f,
                0.35f,
                8,
                behavior);
            var session = new ThreatMotionSession(configuration, Tolerance);

            Assert.That(session.Threat.Speed, Is.EqualTo(2f).Within(0.0005f));

            session.Tick(0.3f);
            Assert.That(session.Threat.Speed, Is.EqualTo(1f).Within(0.001f));

            session.Tick(0.9f);
            Assert.That(session.Threat.Speed, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void Pulse_PeakSpeedStaysInsetAcrossManyTicks()
        {
            var behavior = ThreatBehaviorConfiguration.CreatePulse(
                0.5f,
                1.5f,
                0.2f,
                0.2f);
            var configuration = new ThreatMotionConfiguration(
                Board,
                new LogicalPoint(5f, 8f),
                new LogicalVector(0.7f, 0.6f),
                2f,
                0.35f,
                16,
                behavior);
            var session = new ThreatMotionSession(configuration, Tolerance);

            for (int tick = 0; tick < 300; tick++)
            {
                session.Tick(1f / 60f);
                Assert.That(session.Threat.Position.X, Is.InRange(0.35f, 9.65f));
                Assert.That(session.Threat.Position.Y, Is.InRange(0.35f, 15.65f));
                Assert.That(session.Threat.Speed, Is.LessThanOrEqualTo(3.001f));
            }
        }

        [Test]
        public void FreezePulse_SlowsThreatsDecaysOverTimeAndDoesNotStack()
        {
            var powerConfiguration = new PowerConfiguration(
                2,
                2f,
                0.1f,
                0,
                1f);
            var configuration = new ThreatMotionConfiguration(
                Board,
                new LogicalPoint(5f, 8f),
                new LogicalVector(1f, 0f),
                2f,
                0.35f,
                8);
            var session = new ThreatMotionSession(
                new[] { configuration },
                new BarrierConfiguration(8f, 0.08f, 0.6f, 16),
                new CaptureLevelConfiguration(0.75f),
                FeedbackTuningConfiguration.Default,
                powerConfiguration,
                Tolerance);

            Assert.That(session.TryActivateFreezePulse(), Is.True);
            Assert.That(session.FreezePulseChargesRemaining, Is.EqualTo(1));
            Assert.That(session.FreezePulseRemainingSeconds,
                Is.EqualTo(2f).Within(0.0001f));

            session.Tick(1f);
            Assert.That(session.Threat.Speed, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(session.Threat.Speed, Is.GreaterThan(0f));
            Assert.That(session.FreezePulseRemainingSeconds,
                Is.EqualTo(1f).Within(0.0001f));

            Assert.That(session.TryActivateFreezePulse(), Is.True);
            Assert.That(session.FreezePulseChargesRemaining, Is.EqualTo(0));
            Assert.That(session.FreezePulseRemainingSeconds,
                Is.EqualTo(2f).Within(0.0001f));

            session.Tick(2.5f);
            Assert.That(session.FreezePulseRemainingSeconds, Is.Zero);
            Assert.That(session.Threat.Speed, Is.EqualTo(2f).Within(0.001f));

            Assert.That(session.TryActivateFreezePulse(), Is.False);
            Assert.That(session.FeedbackEvents.Any(
                    feedbackEvent => feedbackEvent.Kind
                        == FeedbackEventKind.PowerUnavailable),
                Is.True);
        }

        [Test]
        public void InstantBarrier_ArmsConsumesOnlyOnAcceptAndCompletesWithinOneTick()
        {
            var powerConfiguration = new PowerConfiguration(
                0,
                1f,
                0.1f,
                1,
                600f);
            var configuration = new ThreatMotionConfiguration(
                Board,
                new LogicalPoint(2f, 2f),
                new LogicalVector(1f, 0f),
                1f,
                0.35f,
                8);
            var session = new ThreatMotionSession(
                new[] { configuration },
                new BarrierConfiguration(8f, 0.08f, 0.6f, 16),
                new CaptureLevelConfiguration(0.75f),
                FeedbackTuningConfiguration.Default,
                powerConfiguration,
                Tolerance);

            Assert.That(session.TryArmInstantBarrier(), Is.True);
            Assert.That(session.InstantBarrierArmed, Is.True);
            Assert.That(session.InstantBarrierChargesRemaining, Is.EqualTo(1));

            BarrierStartResult rejected = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(20f, 20f),
                    BarrierOrientation.Vertical));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(session.InstantBarrierArmed, Is.True);
            Assert.That(session.InstantBarrierChargesRemaining, Is.EqualTo(1));

            BarrierStartResult accepted = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(8f, 8f),
                    BarrierOrientation.Vertical));
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.Barrier.GrowthSpeed,
                Is.EqualTo(600f).Within(0.001f));
            Assert.That(session.InstantBarrierArmed, Is.False);
            Assert.That(session.InstantBarrierChargesRemaining, Is.EqualTo(0));

            session.Tick(1f / 60f);

            Assert.That(session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Locked));
            Assert.That(session.LockedBarrierCount, Is.EqualTo(1));
            Assert.That(session.ActiveBarrier.HasValue, Is.False);
            Assert.That(session.Board.ValidateCurrentInvariants(), Is.True);
        }

        [Test]
        public void InstantBarrier_StillFailsAgainstAnAlreadyTouchingThreat()
        {
            var powerConfiguration = new PowerConfiguration(
                0,
                1f,
                0.1f,
                1,
                600f);
            var configuration = new ThreatMotionConfiguration(
                Board,
                new LogicalPoint(8f, 8f),
                new LogicalVector(0f, 1f),
                1f,
                0.35f,
                8);
            var session = new ThreatMotionSession(
                new[] { configuration },
                new BarrierConfiguration(8f, 0.08f, 0.6f, 16),
                new CaptureLevelConfiguration(0.75f),
                FeedbackTuningConfiguration.Default,
                powerConfiguration,
                Tolerance);

            Assert.That(session.TryArmInstantBarrier(), Is.True);
            BarrierStartResult accepted = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(8f, 8f),
                    BarrierOrientation.Vertical));
            Assert.That(accepted.Accepted, Is.True);

            session.Tick(1f / 60f);

            Assert.That(session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(session.FailedBarrierCount, Is.EqualTo(1));
            Assert.That(session.ActiveBarrier.HasValue, Is.False);
        }

        [Test]
        public void Reset_RestoresPowerChargesArmStateAndPulsePhase()
        {
            var powerConfiguration = new PowerConfiguration(
                1,
                1f,
                0.1f,
                1,
                600f);
            var behavior = ThreatBehaviorConfiguration.CreatePulse(
                0.5f,
                1.5f,
                1f,
                0.5f);
            var configuration = new ThreatMotionConfiguration(
                Board,
                new LogicalPoint(5f, 8f),
                new LogicalVector(1f, 0f),
                2f,
                0.35f,
                8,
                behavior);
            var session = new ThreatMotionSession(
                new[] { configuration },
                new BarrierConfiguration(8f, 0.08f, 0.6f, 16),
                new CaptureLevelConfiguration(0.75f),
                FeedbackTuningConfiguration.Default,
                powerConfiguration,
                Tolerance);

            session.Tick(1.1f);
            Assert.That(session.TryArmInstantBarrier(), Is.True);
            BarrierStartResult accepted = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(5f, 12f),
                    BarrierOrientation.Vertical));
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(session.InstantBarrierChargesRemaining, Is.Zero);

            session.Reset();

            Assert.That(session.InstantBarrierChargesRemaining, Is.EqualTo(1));
            Assert.That(session.InstantBarrierArmed, Is.False);
            Assert.That(session.FreezePulseChargesRemaining, Is.EqualTo(1));
            Assert.That(session.FreezePulseRemainingSeconds, Is.Zero);

            session.Tick(0.3f);
            Assert.That(session.Threat.Speed, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Milestone6Defaults_ConvertToOrderedFiveLevelCatalogWithExpectedIdentities()
        {
            CoreFunLevelDefinition[] definitions =
                CoreFunLevelDefinition.CreateMilestone6Defaults();
            CoreFunLevelConfiguration[] levels = definitions
                .Select(definition => definition.ToRuntimeConfiguration())
                .ToArray();
            var catalog = new CoreFunLevelCatalog(levels);

            Assert.That(catalog.Count, Is.EqualTo(5));
            Assert.That(levels.Select(level => level.StableId), Is.EqualTo(
                new[]
                {
                    "hunter-alone",
                    "pulse-alone",
                    "freeze-pulse-rescue",
                    "instant-barrier-finish",
                    "identity-mix",
                }));
            Assert.That(levels.Select(level => level.DisplayNumber),
                Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));

            Assert.That(levels[0].ThreatMotion.Behavior.Kind,
                Is.EqualTo(ThreatBehaviorKind.Hunter));
            Assert.That(levels[1].ThreatMotion.Behavior.Kind,
                Is.EqualTo(ThreatBehaviorKind.Pulse));
            Assert.That(levels[2].Power.FreezePulseCharges, Is.EqualTo(1));
            Assert.That(levels[2].Power.InstantBarrierCharges, Is.Zero);
            Assert.That(levels[3].Power.InstantBarrierCharges, Is.EqualTo(1));
            Assert.That(levels[3].Power.FreezePulseCharges, Is.Zero);
            Assert.That(levels[4].ThreatMotions.Count, Is.EqualTo(2));
            Assert.That(levels[4].ThreatMotions[0].Behavior.Kind,
                Is.EqualTo(ThreatBehaviorKind.Hunter));
            Assert.That(levels[4].ThreatMotions[1].Behavior.Kind,
                Is.EqualTo(ThreatBehaviorKind.Pulse));
            Assert.That(levels[4].Power.FreezePulseCharges, Is.EqualTo(1));
            Assert.That(levels[4].Power.InstantBarrierCharges, Is.EqualTo(1));

            foreach (CoreFunLevelConfiguration level in levels)
            {
                Assert.That(level.ThreatMotion.BoardBounds, Is.EqualTo(Board));
            }
        }
    }
}
