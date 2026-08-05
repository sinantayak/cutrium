using System;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Gameplay.Threats;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class CaptureBoardStateTests
    {
        private static readonly GeometryTolerancePolicy Tolerance =
            new GeometryTolerancePolicy(0.0001f, 0.00001f, 0.0001f, 0.001f);
        private static readonly LogicalRect BoardBounds =
            new LogicalRect(0f, 0f, 10f, 16f);

        [Test]
        public void VerticalSplit_CreatesExactChildrenAndCapturesEmptySide()
        {
            CaptureBoardState board = Board(Threat(1, 8f, 8f));

            RoomSplitApplyResult result = board.TryApplyLockedBarrier(
                LockedBarrier(1, 1, 5f, BarrierOrientation.Vertical, 0.08f));

            Assert.That(result.Applied, Is.True);
            Assert.That(result.NegativeChild.Bounds,
                Is.EqualTo(new LogicalRect(0f, 0f, 5f, 16f)));
            Assert.That(result.PositiveChild.Bounds,
                Is.EqualTo(new LogicalRect(5f, 0f, 5f, 16f)));
            Assert.That(board.CapturedRooms.Single(),
                Is.EqualTo(result.NegativeChild));
            Assert.That(board.ActiveRooms.Single(),
                Is.EqualTo(result.PositiveChild));
            Assert.That(board.Threats.Single().RoomId,
                Is.EqualTo(result.PositiveChild.Id));
            Assert.That(board.CapturedFraction, Is.EqualTo(0.5f));
        }

        [Test]
        public void HorizontalSplit_PreservesParentAreaAndExactBounds()
        {
            CaptureBoardState board = Board(Threat(1, 5f, 12f));

            RoomSplitApplyResult result = board.TryApplyLockedBarrier(
                LockedBarrier(1, 1, 4f, BarrierOrientation.Horizontal, 0.08f));

            Assert.That(result.Applied, Is.True);
            Assert.That(result.NegativeChild.Bounds,
                Is.EqualTo(new LogicalRect(0f, 0f, 10f, 4f)));
            Assert.That(result.PositiveChild.Bounds,
                Is.EqualTo(new LogicalRect(0f, 4f, 10f, 12f)));
            Assert.That(
                result.NegativeChild.Bounds.Area
                + result.PositiveChild.Bounds.Area,
                Is.EqualTo(BoardBounds.Area));
            Assert.That(board.CapturedFraction, Is.EqualTo(0.25f));
        }

        [Test]
        public void ThreatsOnBothSides_KeepBothChildrenActive()
        {
            CaptureBoardState board = Board(
                Threat(1, 2f, 8f),
                Threat(2, 8f, 8f));

            RoomSplitApplyResult result = board.TryApplyLockedBarrier(
                LockedBarrier(1, 1, 5f, BarrierOrientation.Vertical, 0.08f));

            Assert.That(result.Applied, Is.True);
            Assert.That(board.ActiveRooms, Has.Count.EqualTo(2));
            Assert.That(board.CapturedRooms, Is.Empty);
            Assert.That(
                board.Threats.Select(value => value.RoomId).Distinct().Count(),
                Is.EqualTo(2));
        }

        [Test]
        public void ToleranceTie_ReportsDiagnosticAndUsesVelocityFallback()
        {
            var tinyThreat = new ThreatState(
                new ThreatId(1),
                new RoomId(1),
                new LogicalPoint(5f, 8f),
                new LogicalVector(-1f, 0f),
                0.00005f);
            CaptureBoardState board = Board(tinyThreat);

            RoomSplitApplyResult result = board.TryApplyLockedBarrier(
                LockedBarrier(1, 1, 5f, BarrierOrientation.Vertical, 0.08f));

            Assert.That(result.Applied, Is.True);
            Assert.That(result.Diagnostic,
                Is.EqualTo(RoomSplitDiagnostic.ThreatTieFallback));
            Assert.That(board.Threats.Single().RoomId,
                Is.EqualTo(result.NegativeChild.Id));
        }

        [Test]
        public void ThreatStraddlingBeyondTolerance_IsRejectedAtomically()
        {
            CaptureBoardState board = Board(Threat(1, 5f, 8f));

            RoomSplitApplyResult result = board.TryApplyLockedBarrier(
                LockedBarrier(1, 1, 5f, BarrierOrientation.Vertical, 0.08f));

            Assert.That(result.Applied, Is.False);
            Assert.That(result.Diagnostic,
                Is.EqualTo(RoomSplitDiagnostic.ThreatStraddlesSplit));
            Assert.That(board.ActiveRooms.Single().Id, Is.EqualTo(new RoomId(1)));
            Assert.That(board.CapturedRooms, Is.Empty);
            Assert.That(board.CompletedBarriers, Is.Empty);
        }

        [Test]
        public void StaleRoomAndRepeatedBarrier_AreRejectedWithoutMutation()
        {
            CaptureBoardState board = Board(Threat(1, 8f, 8f));
            BarrierState applied = LockedBarrier(
                1, 1, 5f, BarrierOrientation.Vertical, 0.08f);
            Assert.That(board.TryApplyLockedBarrier(applied).Applied, Is.True);
            float fraction = board.CapturedFraction;

            RoomSplitApplyResult repeated = board.TryApplyLockedBarrier(applied);
            RoomSplitApplyResult stale = board.TryApplyLockedBarrier(
                LockedBarrier(2, 1, 4f, BarrierOrientation.Vertical, 0.08f));

            Assert.That(repeated.Diagnostic,
                Is.EqualTo(RoomSplitDiagnostic.BarrierAlreadyApplied));
            Assert.That(stale.Diagnostic,
                Is.EqualTo(RoomSplitDiagnostic.StaleParentRoom));
            Assert.That(board.CapturedFraction, Is.EqualTo(fraction));
            Assert.That(board.CompletedBarriers, Has.Count.EqualTo(1));
        }

        [Test]
        public void LongSplitSequence_PreservesAreaNonOverlapAndMonotonicCapture()
        {
            CaptureBoardState board = Board(Threat(1, 8f, 12f));
            float previous = board.CapturedFraction;
            RoomSplitApplyResult first = board.TryApplyLockedBarrier(
                LockedBarrier(1, 1, 5f, BarrierOrientation.Vertical, 0.08f));
            Assert.That(first.Applied, Is.True);
            Assert.That(board.CapturedFraction, Is.GreaterThanOrEqualTo(previous));
            previous = board.CapturedFraction;

            RoomSplitApplyResult second = board.TryApplyLockedBarrier(
                LockedBarrier(
                    2,
                    first.PositiveChild.Id.Value,
                    8f,
                    BarrierOrientation.Horizontal,
                    0.08f,
                    new LogicalPoint(7f, 8f),
                    first.PositiveChild.Bounds));

            Assert.That(second.Applied, Is.True);
            Assert.That(board.CapturedFraction, Is.EqualTo(0.75f));
            Assert.That(board.CapturedFraction, Is.GreaterThanOrEqualTo(previous));
            Assert.That(board.ActiveArea + board.CapturedArea,
                Is.EqualTo(BoardBounds.Area).Within(Tolerance.AreaTolerance));
            Assert.That(board.ValidateCurrentInvariants(), Is.True);
            Assert.That(board.Threats, Has.Count.EqualTo(1));
            Assert.That(board.ActiveRooms.Single().Id,
                Is.EqualTo(board.Threats.Single().RoomId));
        }

        [Test]
        public void VisualCollisionWidth_IsExcludedFromCapturedPercentage()
        {
            CaptureBoardState thin = Board(Threat(1, 8f, 8f));
            CaptureBoardState thick = Board(Threat(1, 8f, 8f));

            thin.TryApplyLockedBarrier(
                LockedBarrier(1, 1, 4f, BarrierOrientation.Vertical, 0.01f));
            thick.TryApplyLockedBarrier(
                LockedBarrier(1, 1, 4f, BarrierOrientation.Vertical, 1f));

            Assert.That(thin.CapturedFraction, Is.EqualTo(0.4f));
            Assert.That(thick.CapturedFraction, Is.EqualTo(0.4f));
        }

        [TestCase(0f)]
        [TestCase(-0.1f)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        public void CaptureConfiguration_RejectsInvalidTarget(float target)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CaptureLevelConfiguration(target));
        }

        [Test]
        public void Session_ReachesTargetBlocksInputAndRetryIsDeterministic()
        {
            ThreatMotionSession session = Session(0.5f);
            ThreatState initialThreat = session.Threat;

            Assert.That(session.TryStartBarrier(new BarrierIntent(
                new LogicalPoint(5f, 8f),
                BarrierOrientation.Vertical)).Accepted, Is.True);
            session.Tick(0.02f);

            Assert.That(session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Completed));
            Assert.That(session.CapturedFraction, Is.EqualTo(0.5f));
            Assert.That(session.TryStartBarrier(new BarrierIntent(
                new LogicalPoint(7f, 8f),
                BarrierOrientation.Horizontal)).RejectionReason,
                Is.EqualTo(BarrierRejectionReason.LevelCompleted));

            session.Reset();

            Assert.That(session.LevelStatus, Is.EqualTo(CaptureLevelStatus.Playing));
            Assert.That(session.CapturedFraction, Is.Zero);
            Assert.That(session.Board.ActiveRooms.Single(),
                Is.EqualTo(session.InitialRoom));
            Assert.That(session.Board.CapturedRooms, Is.Empty);
            Assert.That(session.Board.CompletedBarriers, Is.Empty);
            Assert.That(session.Threat, Is.EqualTo(initialThreat));
            Assert.That(session.ActiveBarrier.HasValue, Is.False);
        }

        private static CaptureBoardState Board(params ThreatState[] threats) =>
            new CaptureBoardState(BoardBounds, threats, Tolerance);

        private static ThreatState Threat(int id, float x, float y) =>
            new ThreatState(
                new ThreatId(id),
                new RoomId(1),
                new LogicalPoint(x, y),
                new LogicalVector(1f, 0f),
                0.35f);

        private static BarrierState LockedBarrier(
            int barrierId,
            int parentRoomId,
            float split,
            BarrierOrientation orientation,
            float collisionHalfWidth,
            LogicalPoint? originOverride = null,
            LogicalRect? parentBoundsOverride = null)
        {
            LogicalRect parent = parentBoundsOverride ?? BoardBounds;
            LogicalPoint origin = originOverride ??
                (orientation == BarrierOrientation.Vertical
                    ? new LogicalPoint(split, 8f)
                    : new LogicalPoint(5f, split));
            float negativeTarget = orientation == BarrierOrientation.Vertical
                ? origin.Y - parent.MinY
                : origin.X - parent.MinX;
            float positiveTarget = orientation == BarrierOrientation.Vertical
                ? parent.MaxY - origin.Y
                : parent.MaxX - origin.X;
            return new BarrierState(
                new BarrierId(barrierId),
                new RoomId(parentRoomId),
                origin,
                orientation,
                negativeTarget,
                positiveTarget,
                negativeTarget,
                positiveTarget,
                1000f,
                collisionHalfWidth,
                BarrierLifecycle.Locked);
        }

        private static ThreatMotionSession Session(float target)
        {
            var motion = new ThreatMotionConfiguration(
                BoardBounds,
                new LogicalPoint(8f, 12f),
                new LogicalVector(1f, 0f),
                1f,
                0.35f,
                8);
            var barrier = new BarrierConfiguration(1000f, 0.08f, 0.6f, 16);
            return new ThreatMotionSession(
                motion,
                barrier,
                new CaptureLevelConfiguration(target),
                Tolerance);
        }
    }
}
