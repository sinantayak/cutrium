using System;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Feedback;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Gameplay.Threats;
using Cutrium.Presentation.Threats;
using Cutrium.Unity.Simulation;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class GameplayIdentityRevisionTests
    {
        private static readonly LogicalRect Board =
            CoreFunLevelConfiguration.FixedBoardBounds;

        private static readonly GeometryTolerancePolicy Tolerance =
            new GeometryTolerancePolicy(0.0001f, 0.00001f, 0.0001f, 0.001f);

        [Test]
        public void HunterReaction_IsMateriallyDifferentBoundedAndNotPerfectHoming()
        {
            ThreatMotionConfiguration normal = Configuration(
                ThreatBehaviorConfiguration.Normal,
                new LogicalPoint(5f, 8f),
                new LogicalVector(1f, 0f));
            ThreatMotionConfiguration hunter = Configuration(
                ThreatBehaviorConfiguration.CreateHunter(0.72f, 55f),
                new LogicalPoint(5f, 8f),
                new LogicalVector(1f, 0f));
            var normalSession = new ThreatMotionSession(normal, Tolerance);
            var hunterSession = new ThreatMotionSession(hunter, Tolerance);
            var intent = new BarrierIntent(
                new LogicalPoint(5f, 13f),
                BarrierOrientation.Vertical);

            Assert.That(normalSession.TryStartBarrier(intent).Accepted, Is.True);
            Assert.That(hunterSession.TryStartBarrier(intent).Accepted, Is.True);

            float hunterTurn = AngleDegrees(
                new LogicalVector(1f, 0f),
                hunterSession.Threat.Velocity);
            LogicalVector perfectDirection =
                (intent.Origin - hunterSession.Threat.Position)
                / (intent.Origin - hunterSession.Threat.Position).Length;
            float remainingError = AngleDegrees(
                hunterSession.Threat.Velocity,
                perfectDirection);
            Assert.That(hunterTurn, Is.GreaterThan(35f));
            Assert.That(hunterTurn, Is.LessThanOrEqualTo(55.001f));
            Assert.That(remainingError, Is.GreaterThan(1f));
            Assert.That(normalSession.Threat.Velocity.Y, Is.EqualTo(0f));
            Assert.That(hunterSession.FeedbackEvents.Any(item =>
                item.Kind == FeedbackEventKind.HunterReacted), Is.True);
        }

        [Test]
        public void HunterReaction_IsDeterministicAcrossRenderDeltaChunking()
        {
            ThreatMotionConfiguration hunter = Configuration(
                ThreatBehaviorConfiguration.CreateHunter(0.72f, 55f),
                new LogicalPoint(4f, 7f),
                new LogicalVector(0.8f, 0.6f));
            var first = new ThreatMotionSession(hunter, Tolerance);
            var second = new ThreatMotionSession(hunter, Tolerance);
            var intent = new BarrierIntent(
                new LogicalPoint(8f, 12f),
                BarrierOrientation.Horizontal);
            first.TryStartBarrier(intent);
            second.TryStartBarrier(intent);

            var accumulatorA = new FixedStepAccumulator(
                1f / 60f,
                8,
                Tolerance);
            var accumulatorB = new FixedStepAccumulator(
                1f / 60f,
                8,
                Tolerance);
            for (int index = 0; index < 120; index++)
            {
                accumulatorA.Advance(1f / 60f, first.Tick);
            }

            for (int index = 0; index < 60; index++)
            {
                accumulatorB.Advance(1f / 30f, second.Tick);
            }

            Assert.That(second.Threat.Position.X,
                Is.EqualTo(first.Threat.Position.X).Within(0.0005f));
            Assert.That(second.Threat.Position.Y,
                Is.EqualTo(first.Threat.Position.Y).Within(0.0005f));
            Assert.That(second.Threat.Velocity.X,
                Is.EqualTo(first.Threat.Velocity.X).Within(0.0005f));
            Assert.That(second.Threat.Velocity.Y,
                Is.EqualTo(first.Threat.Velocity.Y).Within(0.0005f));
        }

        [Test]
        public void HunterReaction_MirrorsDeterministicallyAtExactOppositeDirection()
        {
            ThreatBehaviorConfiguration behavior =
                ThreatBehaviorConfiguration.CreateHunter(0.72f, 55f);
            LogicalVector first =
                ThreatMotionSession.CalculateHunterReactionDirection(
                    new LogicalVector(1f, 0f),
                    new LogicalVector(-1f, 0f),
                    behavior);
            LogicalVector second =
                ThreatMotionSession.CalculateHunterReactionDirection(
                    new LogicalVector(1f, 0f),
                    new LogicalVector(-1f, 0f),
                    behavior);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.Y, Is.GreaterThan(0f));
            Assert.That(
                AngleDegrees(new LogicalVector(1f, 0f), first),
                Is.EqualTo(55f).Within(0.001f));
        }

        [Test]
        public void MultipleHunters_ReactWithinBoundsAndRemainSolverSafe()
        {
            var session = new ThreatMotionSession(
                new[]
                {
                    Configuration(
                        ThreatBehaviorConfiguration.CreateHunter(0.7f, 52f),
                        new LogicalPoint(3f, 5f),
                        new LogicalVector(1f, 0.2f)),
                    Configuration(
                        ThreatBehaviorConfiguration.CreateHunter(0.7f, 52f),
                        new LogicalPoint(7f, 11f),
                        new LogicalVector(-1f, -0.2f)),
                },
                Tolerance);

            Assert.That(session.TryStartBarrier(new BarrierIntent(
                new LogicalPoint(5f, 8f),
                BarrierOrientation.Vertical)).Accepted, Is.True);
            for (int tick = 0; tick < 300; tick++)
            {
                session.Tick(1f / 60f);
                Assert.That(session.Board.ValidateCurrentInvariants(), Is.True);
                Assert.That(session.LastDiagnostic,
                    Is.Not.EqualTo(ThreatMotionDiagnostic.ImpactLimitReached));
            }
        }

        [Test]
        public void CutLimit_AcceptedSuccessConsumesAndExhaustsOnlyAfterResolution()
        {
            var session = LimitedSession(
                1,
                new LogicalPoint(2f, 2f),
                new LogicalVector(1f, 0f),
                0.99f,
                600f);

            BarrierStartResult accepted = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(8f, 8f),
                    BarrierOrientation.Vertical));
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(session.AcceptedCutCount, Is.EqualTo(1));
            Assert.That(session.LevelStatus, Is.EqualTo(CaptureLevelStatus.Playing));

            session.Tick(1f / 60f);

            Assert.That(session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Locked));
            Assert.That(session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.OutOfCuts));
            Assert.That(session.FeedbackEvents.Any(item =>
                item.Kind == FeedbackEventKind.CutLimitExhausted), Is.True);
        }

        [Test]
        public void CutLimit_FailedGrownBarrierConsumesButRejectedAttemptDoesNot()
        {
            var session = LimitedSession(
                1,
                new LogicalPoint(5f, 8f),
                new LogicalVector(0f, 1f),
                0.99f,
                8f);

            BarrierStartResult rejected = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(20f, 20f),
                    BarrierOrientation.Vertical));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(session.AcceptedCutCount, Is.Zero);

            Assert.That(session.TryStartBarrier(new BarrierIntent(
                new LogicalPoint(5f, 8f),
                BarrierOrientation.Vertical)).Accepted, Is.True);
            session.Tick(1f / 60f);

            Assert.That(session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(session.AcceptedCutCount, Is.EqualTo(1));
            Assert.That(session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.OutOfCuts));
        }

        [Test]
        public void CutLimit_NoEleventhAcceptedCutAndResetRestoresAllTen()
        {
            var session = LimitedSession(
                10,
                new LogicalPoint(5f, 8f),
                new LogicalVector(0f, 1f),
                0.99f,
                8f);

            for (int attempt = 0; attempt < 10; attempt++)
            {
                Assert.That(session.TryStartBarrier(new BarrierIntent(
                    new LogicalPoint(5f, 8f),
                    BarrierOrientation.Vertical)).Accepted, Is.True);
                session.Tick(1f / 60f);
            }

            Assert.That(session.AcceptedCutCount, Is.EqualTo(10));
            Assert.That(session.CutsRemaining, Is.Zero);
            BarrierStartResult eleventh = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(5f, 8f),
                    BarrierOrientation.Vertical));
            Assert.That(eleventh.Accepted, Is.False);
            Assert.That(eleventh.RejectionReason,
                Is.EqualTo(BarrierRejectionReason.CutLimitReached));

            session.Reset();
            Assert.That(session.AcceptedCutCount, Is.Zero);
            Assert.That(session.CutsRemaining, Is.EqualTo(10));
            Assert.That(session.LevelStatus, Is.EqualTo(CaptureLevelStatus.Playing));
        }

        [Test]
        public void UnlimitedCaptureConfiguration_RemainsUnlimitedAfterManyAttempts()
        {
            var session = new ThreatMotionSession(
                Configuration(
                    ThreatBehaviorConfiguration.Normal,
                    new LogicalPoint(5f, 8f),
                    new LogicalVector(0f, 1f)),
                new BarrierConfiguration(8f, 0.08f, 0.6f, 16),
                new CaptureLevelConfiguration(0.99f),
                Tolerance);

            for (int attempt = 0; attempt < 12; attempt++)
            {
                Assert.That(session.TryStartBarrier(new BarrierIntent(
                    new LogicalPoint(5f, 8f),
                    BarrierOrientation.Vertical)).Accepted, Is.True);
                session.Tick(1f / 60f);
            }

            Assert.That(session.HasCutLimit, Is.False);
            Assert.That(session.AcceptedCutCount, Is.EqualTo(12));
            Assert.That(session.LevelStatus, Is.EqualTo(CaptureLevelStatus.Playing));
        }

        [Test]
        public void TrailIdentity_UsesGeometryAndPhaseWithoutTouchingCollisionRadius()
        {
            ThreatTrailTreatment normal = ThreatTrailTreatment.Resolve(
                ThreatBehaviorKind.Normal, 1f, false);
            ThreatTrailTreatment hunter = ThreatTrailTreatment.Resolve(
                ThreatBehaviorKind.Hunter, 1f, false);
            ThreatTrailTreatment hunterReacting = ThreatTrailTreatment.Resolve(
                ThreatBehaviorKind.Hunter, 1f, true);
            ThreatTrailTreatment pulseSlow = ThreatTrailTreatment.Resolve(
                ThreatBehaviorKind.Pulse, 0.45f, false);
            ThreatTrailTreatment pulseFast = ThreatTrailTreatment.Resolve(
                ThreatBehaviorKind.Pulse, 1.75f, false);

            Assert.That(hunter.ScaleInDiameters,
                Is.GreaterThan(normal.ScaleInDiameters));
            Assert.That(hunterReacting.ScaleInDiameters,
                Is.GreaterThan(hunter.ScaleInDiameters));
            Assert.That(pulseFast.ScaleInDiameters,
                Is.GreaterThan(pulseSlow.ScaleInDiameters));
            Assert.That(pulseFast.Intensity, Is.GreaterThan(pulseSlow.Intensity));

            ThreatMotionConfiguration logical = Configuration(
                ThreatBehaviorConfiguration.CreateHunter(0.7f, 52f),
                new LogicalPoint(5f, 8f),
                new LogicalVector(1f, 0f));
            Assert.That(logical.Radius, Is.EqualTo(0.35f));
        }

        private static ThreatMotionSession LimitedSession(
            int cutLimit,
            LogicalPoint position,
            LogicalVector direction,
            float target,
            float growthSpeed) =>
            new ThreatMotionSession(
                Configuration(
                    ThreatBehaviorConfiguration.Normal,
                    position,
                    direction),
                new BarrierConfiguration(growthSpeed, 0.08f, 0.6f, 16),
                new CaptureLevelConfiguration(target, cutLimit),
                Tolerance);

        private static ThreatMotionConfiguration Configuration(
            ThreatBehaviorConfiguration behavior,
            LogicalPoint position,
            LogicalVector direction) =>
            new ThreatMotionConfiguration(
                Board,
                position,
                direction,
                2f,
                0.35f,
                16,
                behavior);

        private static float AngleDegrees(
            LogicalVector first,
            LogicalVector second)
        {
            LogicalVector normalizedFirst = first / first.Length;
            LogicalVector normalizedSecond = second / second.Length;
            float dot = Math.Max(-1f, Math.Min(
                1f,
                normalizedFirst.X * normalizedSecond.X
                + normalizedFirst.Y * normalizedSecond.Y));
            return (float)(Math.Acos(dot) * 180d / Math.PI);
        }
    }
}
