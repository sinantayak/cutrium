using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Threats;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class GrowingBarrierMotionSolverTests
    {
        private static readonly GeometryTolerancePolicy Tolerance =
            new GeometryTolerancePolicy(0.0001f, 0.00001f, 0.0001f, 0.001f);

        [Test]
        public void Move_DetectsReachedBodyContact()
        {
            BarrierSimulationResult result = Move(
                Threat(new LogicalPoint(5f, 10f), new LogicalVector(0f, -4f)),
                Barrier(3f, 3f, 1f),
                1f);

            Assert.That(result.SimulationEvent, Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(result.ContactKind, Is.EqualTo(BarrierContactKind.Body));
            Assert.That(result.Barrier.Lifecycle, Is.EqualTo(BarrierLifecycle.Failed));
            AssertInside(result.Threat);
        }

        [Test]
        public void Move_DetectsNegativeMovingTipContact()
        {
            BarrierSimulationResult result = Move(
                Threat(new LogicalPoint(3.5f, 8.5f), new LogicalVector(0.01f, 0f)),
                Barrier(0f, 0f, 2f),
                1f);

            Assert.That(result.SimulationEvent, Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(result.ContactKind, Is.EqualTo(BarrierContactKind.NegativeTip));
        }

        [Test]
        public void Move_DetectsPositiveMovingTipContact()
        {
            BarrierSimulationResult result = Move(
                Threat(new LogicalPoint(6.5f, 8.5f), new LogicalVector(-0.01f, 0f)),
                Barrier(0f, 0f, 2f),
                1f);

            Assert.That(result.SimulationEvent, Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(result.ContactKind, Is.EqualTo(BarrierContactKind.PositiveTip));
        }

        [Test]
        public void Move_OrdersWallImpactBeforeLaterBarrierContact()
        {
            BarrierState vertical = Barrier(
                0f,
                0f,
                10f,
                BarrierOrientation.Vertical);
            BarrierSimulationResult result = Move(
                Threat(new LogicalPoint(8f, 8f), new LogicalVector(10f, 0f)),
                vertical,
                0.6f);

            Assert.That(result.SimulationEvent, Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(result.Threat.Velocity.X, Is.LessThan(0f));
            Assert.That(result.ElapsedUntilEvent,
                Is.InRange(0.539f, 0.541f));
            AssertInside(result.Threat);
        }

        [Test]
        public void Move_LocksWhenCompletionPrecedesContact()
        {
            BarrierSimulationResult result = Move(
                Threat(new LogicalPoint(5f, 12f), new LogicalVector(1f, 0f)),
                Barrier(4.9f, 4.9f, 10f),
                0.02f);

            Assert.That(result.SimulationEvent, Is.EqualTo(BarrierSimulationEvent.Locked));
            Assert.That(result.Barrier.Lifecycle, Is.EqualTo(BarrierLifecycle.Locked));
            Assert.That(result.ContactKind, Is.EqualTo(BarrierContactKind.None));
            Assert.That(result.ElapsedUntilEvent,
                Is.EqualTo(0.01f).Within(Tolerance.TimeTolerance));
        }

        [Test]
        public void Move_FailsWhenContactPrecedesCompletion()
        {
            BarrierSimulationResult result = Move(
                Threat(new LogicalPoint(5f, 8.7f), new LogicalVector(0f, -1f)),
                Barrier(1f, 1f, 1f),
                0.2f);

            Assert.That(result.SimulationEvent, Is.EqualTo(BarrierSimulationEvent.Failed));
        }

        [Test]
        public void Move_LockWinsContactTieWithinTimeTolerance()
        {
            BarrierSimulationResult result = Move(
                Threat(new LogicalPoint(5f, 8.61f), new LogicalVector(0f, -1f)),
                Barrier(4.9f, 4.9f, 10f),
                0.01f);

            Assert.That(result.SimulationEvent, Is.EqualTo(BarrierSimulationEvent.Locked));
            Assert.That(result.Barrier.Lifecycle, Is.EqualTo(BarrierLifecycle.Locked));
        }

        [Test]
        public void Move_HighSpeedThreatCannotTunnelThroughBody()
        {
            BarrierSimulationResult result = Move(
                Threat(new LogicalPoint(5f, 14f), new LogicalVector(0f, -100f)),
                Barrier(4f, 4f, 0.5f),
                0.1f);

            Assert.That(result.SimulationEvent, Is.EqualTo(BarrierSimulationEvent.Failed));
            AssertInside(result.Threat);
        }

        [Test]
        public void Move_HighGrowthSpeedTipCannotTunnelPastThreat()
        {
            BarrierSimulationResult result = Move(
                Threat(new LogicalPoint(2f, 8.4f), new LogicalVector(0.01f, 0f)),
                Barrier(0f, 0f, 100f),
                0.05f);

            Assert.That(result.SimulationEvent, Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(result.ContactKind, Is.EqualTo(BarrierContactKind.NegativeTip));
        }

        [Test]
        public void Move_IterationCapReportsDiagnosticAndPreservesRoomState()
        {
            BarrierSimulationResult result = GrowingBarrierMotionSolver.Move(
                Room(),
                Threat(new LogicalPoint(5f, 4f), new LogicalVector(100f, 0f)),
                Barrier(0f, 0f, 0.1f),
                0.2f,
                1,
                8,
                Tolerance);

            Assert.That(result.Diagnostic,
                Is.EqualTo(BarrierSimulationDiagnostic.IterationLimitReached));
            Assert.That(result.IterationCount, Is.EqualTo(1));
            AssertInside(result.Threat);
        }

        [Test]
        public void Move_RepeatedExecutionIsDeterministic()
        {
            ThreatState threat = Threat(
                new LogicalPoint(2f, 12f),
                new LogicalVector(3f, -2f));
            BarrierState barrier = Barrier(0f, 0f, 1.5f);
            BarrierSimulationResult left = Move(threat, barrier, 0.4f);
            BarrierSimulationResult right = Move(threat, barrier, 0.4f);

            Assert.That(left.Threat, Is.EqualTo(right.Threat));
            Assert.That(left.Barrier, Is.EqualTo(right.Barrier));
            Assert.That(left.SimulationEvent, Is.EqualTo(right.SimulationEvent));
            Assert.That(left.ContactKind, Is.EqualTo(right.ContactKind));
        }

        private static BarrierSimulationResult Move(
            ThreatState threat,
            BarrierState barrier,
            float elapsedTime) =>
            GrowingBarrierMotionSolver.Move(
                Room(), threat, barrier, elapsedTime, 16, 16, Tolerance);

        private static RoomState Room() =>
            new RoomState(new RoomId(1), new LogicalRect(0f, 0f, 10f, 16f));

        private static ThreatState Threat(
            LogicalPoint position,
            LogicalVector velocity) =>
            new ThreatState(
                new ThreatId(1),
                new RoomId(1),
                position,
                velocity,
                0.5f);

        private static BarrierState Barrier(
            float negativeLength,
            float positiveLength,
            float growthSpeed,
            BarrierOrientation orientation = BarrierOrientation.Horizontal)
        {
            float negativeTarget = orientation == BarrierOrientation.Horizontal
                ? 5f
                : 8f;
            float positiveTarget = negativeTarget;
            return new BarrierState(
                new BarrierId(1),
                new RoomId(1),
                new LogicalPoint(5f, 8f),
                orientation,
                negativeLength,
                positiveLength,
                negativeTarget,
                positiveTarget,
                growthSpeed,
                0.1f,
                BarrierLifecycle.Growing);
        }

        private static void AssertInside(ThreatState threat)
        {
            Assert.That(threat.Position.X, Is.InRange(0.5f, 9.5f));
            Assert.That(threat.Position.Y, Is.InRange(0.5f, 15.5f));
        }
    }
}
