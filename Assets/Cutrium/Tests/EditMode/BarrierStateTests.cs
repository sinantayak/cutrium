using System;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class BarrierStateTests
    {
        private static readonly GeometryTolerancePolicy Tolerance =
            new GeometryTolerancePolicy(0.0001f, 0.00001f, 0.0001f, 0.001f);

        [TestCase(BarrierOrientation.Horizontal)]
        [TestCase(BarrierOrientation.Vertical)]
        public void Factory_CreatesValidAxisBarrier(BarrierOrientation orientation)
        {
            BarrierStartResult result = BarrierFactory.TryCreate(
                new BarrierId(1),
                CreateRoom(),
                new BarrierIntent(new LogicalPoint(5f, 8f), orientation),
                CreateConfiguration(),
                Tolerance);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.RejectionReason, Is.EqualTo(BarrierRejectionReason.None));
            Assert.That(result.Barrier.ParentRoomId, Is.EqualTo(new RoomId(1)));
            Assert.That(result.Barrier.Origin, Is.EqualTo(new LogicalPoint(5f, 8f)));
            Assert.That(result.Barrier.NegativeLength, Is.Zero);
            Assert.That(result.Barrier.PositiveLength, Is.Zero);
            Assert.That(result.Barrier.IsVulnerable, Is.True);
        }

        [Test]
        public void Factory_RejectsInvalidOrientationAndOutsideOrigin()
        {
            BarrierStartResult orientation = BarrierFactory.TryCreate(
                new BarrierId(1),
                CreateRoom(),
                new BarrierIntent(new LogicalPoint(5f, 8f), BarrierOrientation.None),
                CreateConfiguration(),
                Tolerance);
            BarrierStartResult outside = BarrierFactory.TryCreate(
                new BarrierId(2),
                CreateRoom(),
                new BarrierIntent(new LogicalPoint(11f, 8f), BarrierOrientation.Horizontal),
                CreateConfiguration(),
                Tolerance);

            Assert.That(orientation.Accepted, Is.False);
            Assert.That(orientation.RejectionReason,
                Is.EqualTo(BarrierRejectionReason.InvalidOrientation));
            Assert.That(outside.Accepted, Is.False);
            Assert.That(outside.RejectionReason,
                Is.EqualTo(BarrierRejectionReason.OriginOutsideActiveRoom));
        }

        [TestCase(0.6f)]
        [TestCase(0.59995f)]
        public void Factory_RejectsOriginAtOrWithinToleranceOfEdge(float x)
        {
            BarrierStartResult result = BarrierFactory.TryCreate(
                new BarrierId(1),
                CreateRoom(),
                new BarrierIntent(new LogicalPoint(x, 8f), BarrierOrientation.Horizontal),
                CreateConfiguration(),
                Tolerance);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(BarrierRejectionReason.TooCloseToRoomEdge));
        }

        [Test]
        public void HorizontalGrowth_CompletesOneHalfThenBothAtExactBoundaries()
        {
            BarrierState barrier = CreateBarrier(
                new LogicalPoint(2f, 8f),
                BarrierOrientation.Horizontal,
                8f);

            BarrierState first = barrier.AdvanceGrowth(0.25f, Tolerance);
            BarrierState locked = first.AdvanceGrowth(0.75f, Tolerance);

            Assert.That(first.NegativeLength, Is.EqualTo(2f));
            Assert.That(first.PositiveLength, Is.EqualTo(2f));
            Assert.That(first.NegativeComplete, Is.True);
            Assert.That(first.PositiveComplete, Is.False);
            Assert.That(first.Lifecycle, Is.EqualTo(BarrierLifecycle.Growing));
            Assert.That(locked.NegativeEndpoint, Is.EqualTo(new LogicalPoint(0f, 8f)));
            Assert.That(locked.PositiveEndpoint, Is.EqualTo(new LogicalPoint(10f, 8f)));
            Assert.That(locked.Lifecycle, Is.EqualTo(BarrierLifecycle.Locked));
        }

        [Test]
        public void VerticalGrowth_StopsAtExactRoomBoundaries()
        {
            BarrierState barrier = CreateBarrier(
                new LogicalPoint(5f, 4f),
                BarrierOrientation.Vertical,
                16f);

            BarrierState locked = barrier.AdvanceGrowth(1f, Tolerance);

            Assert.That(locked.NegativeEndpoint, Is.EqualTo(new LogicalPoint(5f, 0f)));
            Assert.That(locked.PositiveEndpoint, Is.EqualTo(new LogicalPoint(5f, 16f)));
            Assert.That(locked.IsComplete, Is.True);
        }

        [Test]
        public void Configuration_RejectsZeroAndInvalidGrowthData()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BarrierConfiguration(0f, 0.08f, 0.6f, 16));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BarrierConfiguration(8f, 0f, 0.6f, 16));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BarrierConfiguration(float.NaN, 0.08f, 0.6f, 16));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BarrierConfiguration(8f, 0.08f, -1f, 16));
        }

        [Test]
        public void Session_RejectsSecondActiveBarrierWithoutMutatingState()
        {
            ThreatMotionSession session = CreateSession();
            BarrierStartResult first = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(5f, 8f),
                    BarrierOrientation.Horizontal));
            BarrierState stateBefore = session.ActiveBarrier.Value;

            BarrierStartResult second = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(4f, 7f),
                    BarrierOrientation.Vertical));

            Assert.That(first.Accepted, Is.True);
            Assert.That(second.Accepted, Is.False);
            Assert.That(second.RejectionReason,
                Is.EqualTo(BarrierRejectionReason.BarrierAlreadyActive));
            Assert.That(session.ActiveBarrier.Value, Is.EqualTo(stateBefore));
            Assert.That(session.Threat.Id, Is.EqualTo(new Cutrium.Gameplay.Threats.ThreatId(1)));
        }

        [Test]
        public void Session_RejectedIntentDoesNotMutateThreatOrBarrierState()
        {
            ThreatMotionSession session = CreateSession();
            var threatBefore = session.Threat;

            BarrierStartResult result = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(0.1f, 8f),
                    BarrierOrientation.Horizontal));

            Assert.That(result.Accepted, Is.False);
            Assert.That(session.ActiveBarrier.HasValue, Is.False);
            Assert.That(session.Threat, Is.EqualTo(threatBefore));
            Assert.That(session.TickCount, Is.Zero);
        }

        private static BarrierState CreateBarrier(
            LogicalPoint origin,
            BarrierOrientation orientation,
            float growthSpeed)
        {
            BarrierStartResult result = BarrierFactory.TryCreate(
                new BarrierId(1),
                CreateRoom(),
                new BarrierIntent(origin, orientation),
                new BarrierConfiguration(growthSpeed, 0.08f, 0f, 16),
                Tolerance);
            Assert.That(result.Accepted, Is.True);
            return result.Barrier;
        }

        private static RoomState CreateRoom() =>
            new RoomState(new RoomId(1), new LogicalRect(0f, 0f, 10f, 16f));

        private static BarrierConfiguration CreateConfiguration() =>
            new BarrierConfiguration(8f, 0.08f, 0.6f, 16);

        private static ThreatMotionSession CreateSession()
        {
            var threat = new ThreatMotionConfiguration(
                new LogicalRect(0f, 0f, 10f, 16f),
                new LogicalPoint(5f, 12f),
                new LogicalVector(1f, 0f),
                1f,
                0.35f,
                8);
            return new ThreatMotionSession(threat, CreateConfiguration(), Tolerance);
        }
    }
}
