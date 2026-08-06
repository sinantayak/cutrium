using System;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Unity.Input;
using NUnit.Framework;
using UnityEngine;

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

        [TestCase(0f)]
        [TestCase(0.00005f)]
        public void Factory_RejectsHorizontalSplitAtOrWithinToleranceOfEdge(
            float y)
        {
            BarrierStartResult result = BarrierFactory.TryCreate(
                new BarrierId(1),
                CreateRoom(),
                new BarrierIntent(
                    new LogicalPoint(5f, y),
                    BarrierOrientation.Horizontal),
                CreateConfiguration(),
                Tolerance);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(BarrierRejectionReason.TooCloseToRoomEdge));
        }

        [TestCase(0f)]
        [TestCase(0.00005f)]
        public void Factory_RejectsVerticalSplitAtOrWithinToleranceOfEdge(
            float x)
        {
            BarrierStartResult result = BarrierFactory.TryCreate(
                new BarrierId(1),
                CreateRoom(),
                new BarrierIntent(
                    new LogicalPoint(x, 8f),
                    BarrierOrientation.Vertical),
                CreateConfiguration(),
                Tolerance);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(BarrierRejectionReason.TooCloseToRoomEdge));
        }

        [Test]
        public void Factory_PerpendicularMarginDoesNotRejectShortGrowthSpan()
        {
            var child = new RoomState(
                new RoomId(7),
                new LogicalRect(0f, 10f, 10f, 6f));
            BarrierStartResult result = BarrierFactory.TryCreate(
                new BarrierId(4),
                child,
                new BarrierIntent(
                    new LogicalPoint(5f, 13f),
                    BarrierOrientation.Vertical),
                new BarrierConfiguration(8f, 0.08f, 3f, 16),
                Tolerance);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Barrier.ParentRoomId, Is.EqualTo(child.Id));
            Assert.That(result.Barrier.NegativeTargetLength, Is.EqualTo(3f));
            Assert.That(result.Barrier.PositiveTargetLength, Is.EqualTo(3f));
        }

        [TestCase(BarrierOrientation.Horizontal, 5f, 0.1f)]
        [TestCase(BarrierOrientation.Vertical, 0.1f, 8f)]
        public void Factory_AcceptsEveryInteriorPointInsideConfiguredMargin(
            BarrierOrientation orientation,
            float x,
            float y)
        {
            BarrierStartResult result = BarrierFactory.TryCreate(
                new BarrierId(8),
                CreateRoom(),
                new BarrierIntent(new LogicalPoint(x, y), orientation),
                new BarrierConfiguration(3f, 0.08f, 3f, 16),
                Tolerance);

            Assert.That(result.Accepted, Is.True,
                result.RejectionReason.ToString());
            Assert.That(result.Barrier.Orientation, Is.EqualTo(orientation));
            Assert.That(result.Barrier.ParentRoomId, Is.EqualTo(new RoomId(1)));
        }

        [TestCase(BarrierOrientation.Horizontal, 10f, 8f)]
        [TestCase(BarrierOrientation.Vertical, 5f, 16f)]
        public void Factory_RoomGrowthBoundaryRejectsWithoutThrowing(
            BarrierOrientation orientation,
            float x,
            float y)
        {
            BarrierStartResult result = default;

            Assert.DoesNotThrow(() => result = BarrierFactory.TryCreate(
                new BarrierId(1),
                CreateRoom(),
                new BarrierIntent(new LogicalPoint(x, y), orientation),
                CreateConfiguration(),
                Tolerance));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(BarrierRejectionReason.NoGrowthSpan));
        }

        [TestCase(BarrierOrientation.Horizontal)]
        [TestCase(BarrierOrientation.Vertical)]
        public void Factory_TerminalSmallRoomKeepsBothOrientationsAvailable(
            BarrierOrientation orientation)
        {
            var terminalRoom = new RoomState(
                new RoomId(9),
                new LogicalRect(2f, 4f, 3f, 3f));

            BarrierStartResult result = BarrierFactory.TryCreate(
                new BarrierId(5),
                terminalRoom,
                new BarrierIntent(
                    terminalRoom.Bounds.Center,
                    orientation),
                new BarrierConfiguration(2.8f, 0.08f, 1.8f, 16),
                Tolerance);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Barrier.Orientation, Is.EqualTo(orientation));
            Assert.That(result.Barrier.ParentRoomId,
                Is.EqualTo(terminalRoom.Id));
            Assert.That(result.Barrier.NegativeTargetLength,
                Is.EqualTo(1.5f));
            Assert.That(result.Barrier.PositiveTargetLength,
                Is.EqualTo(1.5f));
        }

        [Test]
        public void Factory_ConfiguredMarginDoesNotRestrictInteriorOrientations()
        {
            var narrowRoom = new RoomState(
                new RoomId(10),
                new LogicalRect(2f, 4f, 3f, 6f));
            var configuration =
                new BarrierConfiguration(2.8f, 0.08f, 1.8f, 16);

            BarrierStartResult horizontal = BarrierFactory.TryCreate(
                new BarrierId(5),
                narrowRoom,
                new BarrierIntent(
                    narrowRoom.Bounds.Center,
                    BarrierOrientation.Horizontal),
                configuration,
                Tolerance);
            BarrierStartResult vertical = BarrierFactory.TryCreate(
                new BarrierId(6),
                narrowRoom,
                new BarrierIntent(
                    narrowRoom.Bounds.Center,
                    BarrierOrientation.Vertical),
                configuration,
                Tolerance);

            Assert.That(horizontal.Accepted, Is.True);
            Assert.That(vertical.Accepted, Is.True);
        }

        [Test]
        public void Session_ValidationIsNonMutatingAndMatchesAcceptedStart()
        {
            ThreatMotionSession session = CreateSession();
            var intent = new BarrierIntent(
                new LogicalPoint(5f, 8f),
                BarrierOrientation.Horizontal);

            BarrierStartResult validation =
                session.ValidateBarrierStart(intent);

            Assert.That(validation.Accepted, Is.True);
            Assert.That(validation.Barrier.Id, Is.EqualTo(new BarrierId(1)));
            Assert.That(session.ActiveBarrier.HasValue, Is.False);
            Assert.That(session.TickCount, Is.Zero);

            BarrierStartResult started = session.TryStartBarrier(intent);
            Assert.That(started.Accepted, Is.True);
            Assert.That(started.Barrier, Is.EqualTo(validation.Barrier));
            Assert.That(session.ActiveBarrier.Value,
                Is.EqualTo(started.Barrier));
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
                    new LogicalPoint(5f, 0f),
                    BarrierOrientation.Horizontal));

            Assert.That(result.Accepted, Is.False);
            Assert.That(session.ActiveBarrier.HasValue, Is.False);
            Assert.That(session.Threat, Is.EqualTo(threatBefore));
            Assert.That(session.TickCount, Is.Zero);
        }

        [Test]
        public void Session_HorizontalLockThenVerticalStart_UsesCurrentChildBounds()
        {
            ThreatMotionSession session = CreateAlternatingSession(
                new LogicalPoint(8f, 14f));
            BarrierStartResult horizontal = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(5f, 10f),
                    BarrierOrientation.Horizontal));

            Assert.That(horizontal.Accepted, Is.True);
            session.Tick(0.1f);

            Assert.That(session.ActiveBarrier.HasValue, Is.False);
            Assert.That(session.LockedBarrierCount, Is.EqualTo(1));
            Assert.That(session.Board.TryGetActiveRoomAt(
                new LogicalPoint(5f, 13f),
                out RoomState child), Is.True);

            BarrierStartResult vertical = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(5f, 13f),
                    BarrierOrientation.Vertical));

            Assert.That(vertical.Accepted, Is.True);
            Assert.That(vertical.RejectionReason,
                Is.EqualTo(BarrierRejectionReason.None));
            Assert.That(vertical.Barrier.ParentRoomId, Is.EqualTo(child.Id));
            Assert.That(vertical.Barrier.Orientation,
                Is.EqualTo(BarrierOrientation.Vertical));
            Assert.That(vertical.Barrier.NegativeTargetLength,
                Is.EqualTo(3f).Within(Tolerance.DistanceTolerance));
            Assert.That(vertical.Barrier.PositiveTargetLength,
                Is.EqualTo(3f).Within(Tolerance.DistanceTolerance));
        }

        [Test]
        public void Session_VerticalLockThenHorizontalStart_UsesCurrentChildBounds()
        {
            ThreatMotionSession session = CreateAlternatingSession(
                new LogicalPoint(8f, 14f));
            BarrierStartResult vertical = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(6f, 8f),
                    BarrierOrientation.Vertical));

            Assert.That(vertical.Accepted, Is.True);
            session.Tick(0.1f);

            Assert.That(session.ActiveBarrier.HasValue, Is.False);
            Assert.That(session.LockedBarrierCount, Is.EqualTo(1));
            Assert.That(session.Board.TryGetActiveRoomAt(
                new LogicalPoint(8f, 8f),
                out RoomState child), Is.True);

            BarrierStartResult horizontal = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(8f, 8f),
                    BarrierOrientation.Horizontal));

            Assert.That(horizontal.Accepted, Is.True);
            Assert.That(horizontal.RejectionReason,
                Is.EqualTo(BarrierRejectionReason.None));
            Assert.That(horizontal.Barrier.ParentRoomId, Is.EqualTo(child.Id));
            Assert.That(horizontal.Barrier.Orientation,
                Is.EqualTo(BarrierOrientation.Horizontal));
            Assert.That(horizontal.Barrier.NegativeTargetLength,
                Is.EqualTo(2f).Within(Tolerance.DistanceTolerance));
            Assert.That(horizontal.Barrier.PositiveTargetLength,
                Is.EqualTo(2f).Within(Tolerance.DistanceTolerance));
        }

        [Test]
        public void Session_AlternatesHorizontalVerticalAcrossFourRoomSplits()
        {
            ThreatMotionSession session = CreateMultiSplitSession();

            LockBarrier(
                session,
                new LogicalPoint(5f, 8f),
                BarrierOrientation.Horizontal);
            LockBarrier(
                session,
                new LogicalPoint(5f, 12f),
                BarrierOrientation.Vertical);
            LockBarrier(
                session,
                new LogicalPoint(7.5f, 11f),
                BarrierOrientation.Horizontal);
            LockBarrier(
                session,
                new LogicalPoint(7f, 13f),
                BarrierOrientation.Vertical);

            Assert.That(session.LockedBarrierCount, Is.EqualTo(4));
            Assert.That(session.FailedBarrierCount, Is.Zero);
            Assert.That(session.ActiveBarrier.HasValue, Is.False);
        }

        [Test]
        public void Session_AlternatesVerticalHorizontalAcrossFourRoomSplits()
        {
            ThreatMotionSession session = CreateMultiSplitSession();

            LockBarrier(
                session,
                new LogicalPoint(5f, 8f),
                BarrierOrientation.Vertical);
            LockBarrier(
                session,
                new LogicalPoint(7.5f, 8f),
                BarrierOrientation.Horizontal);
            LockBarrier(
                session,
                new LogicalPoint(7f, 12f),
                BarrierOrientation.Vertical);
            LockBarrier(
                session,
                new LogicalPoint(8.5f, 11f),
                BarrierOrientation.Horizontal);

            Assert.That(session.LockedBarrierCount, Is.EqualTo(4));
            Assert.That(session.FailedBarrierCount, Is.Zero);
            Assert.That(session.ActiveBarrier.HasValue, Is.False);
        }

        [Test]
        public void Session_FailedBarrierClearsBeforePerpendicularRequest()
        {
            ThreatMotionSession session = CreateAlternatingSession(
                new LogicalPoint(5f, 8f));
            BarrierStartResult failedStart = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(5f, 8f),
                    BarrierOrientation.Horizontal));

            Assert.That(failedStart.Accepted, Is.True);
            session.Tick(0.01f);

            Assert.That(session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(session.ActiveBarrier.HasValue, Is.False);
            BarrierStartResult perpendicular = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(6f, 8f),
                    BarrierOrientation.Vertical));
            Assert.That(perpendicular.Accepted, Is.True);
            Assert.That(perpendicular.Barrier.Orientation,
                Is.EqualTo(BarrierOrientation.Vertical));
        }

        [Test]
        public void Session_RejectionReportsDiagnosticAndPreservesCompleteState()
        {
            ThreatMotionSession session = CreateAlternatingSession(
                new LogicalPoint(8f, 14f));
            var threatsBefore = new[] { session.Threat };
            int roomsBefore = session.Board.ActiveRooms.Count;
            int completedBefore = session.Board.CompletedBarriers.Count;
            float capturedBefore = session.CapturedFraction;

            BarrierStartResult rejected = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(5f, 0f),
                    BarrierOrientation.Horizontal));

            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RejectionReason,
                Is.EqualTo(BarrierRejectionReason.TooCloseToRoomEdge));
            Assert.That(session.ActiveBarrier.HasValue, Is.False);
            Assert.That(session.Threats, Is.EqualTo(threatsBefore));
            Assert.That(session.Board.ActiveRooms.Count, Is.EqualTo(roomsBefore));
            Assert.That(session.Board.CompletedBarriers.Count,
                Is.EqualTo(completedBefore));
            Assert.That(session.CapturedFraction, Is.EqualTo(capturedBefore));
            Assert.That(session.TickCount, Is.Zero);

            BarrierStartResult accepted = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(5f, 8f),
                    BarrierOrientation.Horizontal));
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.Barrier.Id, Is.EqualTo(new BarrierId(1)));
        }

        [Test]
        public void Gesture_CommitCancelShortReleaseAndRetryClearTransientAxisState()
        {
            var gameObject = new GameObject("BarrierGestureEditModeTest");
            try
            {
                var gesture = gameObject.AddComponent<BarrierGestureAdapter>();
                gesture.Configure(null, 0.35f, 0.1f);
                BarrierIntent committed = default;
                int commits = 0;
                gesture.IntentCommitted += intent =>
                {
                    committed = intent;
                    commits++;
                };

                ProcessGesture(
                    gesture,
                    new LogicalPoint(5f, 8f),
                    new LogicalPoint(6f, 8f));

                Assert.That(commits, Is.EqualTo(1));
                Assert.That(committed.Orientation,
                    Is.EqualTo(BarrierOrientation.Horizontal));
                AssertGestureCleared(gesture);

                gesture.ProcessSample(AcceptedSample(
                    PointerSamplePhase.Started,
                    new LogicalPoint(5f, 8f)));
                gesture.ProcessSample(AcceptedSample(
                    PointerSamplePhase.Moved,
                    new LogicalPoint(5f, 9f)));
                Assert.That(gesture.SelectedOrientation,
                    Is.EqualTo(BarrierOrientation.Vertical));
                gesture.ProcessSample(AcceptedSample(
                    PointerSamplePhase.Cancelled,
                    new LogicalPoint(5f, 9f)));
                AssertGestureCleared(gesture);

                gesture.ProcessSample(AcceptedSample(
                    PointerSamplePhase.Started,
                    new LogicalPoint(4f, 7f)));
                gesture.ProcessSample(AcceptedSample(
                    PointerSamplePhase.Released,
                    new LogicalPoint(4.1f, 7.1f)));
                AssertGestureCleared(gesture);

                gesture.ResetForRetry();
                AssertGestureCleared(gesture);
                Assert.That(gesture.CommittedIntentCount, Is.Zero);
                Assert.That(gesture.CancelledInteractionCount, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
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

        private static ThreatMotionSession CreateAlternatingSession(
            LogicalPoint threatPosition)
        {
            var threat = new ThreatMotionConfiguration(
                new LogicalRect(0f, 0f, 10f, 16f),
                threatPosition,
                new LogicalVector(1f, 0f),
                0.1f,
                0.35f,
                8);
            var barrier = new BarrierConfiguration(100f, 0.08f, 3f, 16);
            var capture = new CaptureLevelConfiguration(1f);
            return new ThreatMotionSession(
                threat,
                barrier,
                capture,
                Tolerance);
        }

        private static ThreatMotionSession CreateMultiSplitSession()
        {
            var threat = new ThreatMotionConfiguration(
                new LogicalRect(0f, 0f, 10f, 16f),
                new LogicalPoint(8.5f, 14f),
                new LogicalVector(1f, 0f),
                0.1f,
                0.25f,
                8);
            return new ThreatMotionSession(
                threat,
                new BarrierConfiguration(100f, 0.05f, 0.5f, 16),
                new CaptureLevelConfiguration(1f),
                Tolerance);
        }

        private static void LockBarrier(
            ThreatMotionSession session,
            LogicalPoint origin,
            BarrierOrientation orientation)
        {
            Assert.That(session.Board.TryGetActiveRoomAt(
                origin,
                out RoomState parent), Is.True);
            int previousLocks = session.LockedBarrierCount;
            BarrierStartResult started = session.TryStartBarrier(
                new BarrierIntent(origin, orientation));

            Assert.That(started.Accepted, Is.True,
                $"{orientation} at {origin} was rejected as "
                + started.RejectionReason + ".");
            Assert.That(started.Barrier.ParentRoomId, Is.EqualTo(parent.Id));
            Assert.That(started.Barrier.Orientation, Is.EqualTo(orientation));
            if (orientation == BarrierOrientation.Horizontal)
            {
                Assert.That(started.Barrier.NegativeTargetLength,
                    Is.EqualTo(origin.X - parent.Bounds.MinX)
                        .Within(Tolerance.DistanceTolerance));
                Assert.That(started.Barrier.PositiveTargetLength,
                    Is.EqualTo(parent.Bounds.MaxX - origin.X)
                        .Within(Tolerance.DistanceTolerance));
            }
            else
            {
                Assert.That(started.Barrier.NegativeTargetLength,
                    Is.EqualTo(origin.Y - parent.Bounds.MinY)
                        .Within(Tolerance.DistanceTolerance));
                Assert.That(started.Barrier.PositiveTargetLength,
                    Is.EqualTo(parent.Bounds.MaxY - origin.Y)
                        .Within(Tolerance.DistanceTolerance));
            }

            session.Tick(0.1f);

            Assert.That(session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Locked));
            Assert.That(session.LockedBarrierCount, Is.EqualTo(previousLocks + 1));
            Assert.That(session.ActiveBarrier.HasValue, Is.False);
            BarrierState locked = session.Board.CompletedBarriers[
                session.Board.CompletedBarriers.Count - 1];
            Assert.That(locked.ParentRoomId, Is.EqualTo(parent.Id));
            Assert.That(locked.Orientation, Is.EqualTo(orientation));
        }

        private static void ProcessGesture(
            BarrierGestureAdapter gesture,
            LogicalPoint origin,
            LogicalPoint end)
        {
            gesture.ProcessSample(AcceptedSample(
                PointerSamplePhase.Started,
                origin));
            gesture.ProcessSample(AcceptedSample(
                PointerSamplePhase.Moved,
                end));
            gesture.ProcessSample(AcceptedSample(
                PointerSamplePhase.Released,
                end));
        }

        private static PointerSample AcceptedSample(
            PointerSamplePhase phase,
            LogicalPoint point) =>
            new PointerSample(
                phase,
                Vector2.zero,
                41,
                false,
                true,
                true,
                point);

        private static void AssertGestureCleared(
            BarrierGestureAdapter gesture)
        {
            Assert.That(gesture.IsTracking, Is.False);
            Assert.That(gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.None));
            Assert.That(gesture.Origin, Is.EqualTo(default(LogicalPoint)));
            Assert.That(gesture.CurrentPoint, Is.EqualTo(default(LogicalPoint)));
        }
    }
}
