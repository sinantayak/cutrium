using System;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Gameplay.Threats;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class CoreFunLevelAndMetricsTests
    {
        private static readonly GeometryTolerancePolicy Tolerance =
            new GeometryTolerancePolicy(0.0001f, 0.00001f, 0.0001f, 0.001f);

        [Test]
        public void Defaults_ConvertToOrderedThreeLevelCatalog()
        {
            CoreFunLevelConfiguration[] levels = Defaults();
            var catalog = new CoreFunLevelCatalog(levels);

            Assert.That(catalog.Count, Is.EqualTo(3));
            Assert.That(levels.Select(level => level.StableId), Is.EqualTo(
                new[]
                {
                    "learn-the-cut",
                    "timing-and-failure",
                    "confident-capture",
                }));
            Assert.That(levels.Select(level => level.DisplayNumber),
                Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(levels.Select(
                    level => level.Capture.TargetCapturedFraction),
                Is.EqualTo(new[] { 0.825f, 0.85f, 0.9f }));
            Assert.That(levels.Select(level => level.ThreatMotion.Speed),
                Is.EqualTo(new[] { 1.6f, 3.1f, 2.7f }));
            Assert.That(levels.Select(level => level.Barrier.GrowthSpeed),
                Is.EqualTo(new[] { 3f, 2.4f, 2.8f }));
            Assert.That(levels.Select(level => level.Barrier.MinimumEdgeMargin),
                Is.EqualTo(new[] { 3f, 2.5f, 1.8f }));
            Assert.That(levels.Select(level => level.ThreatMotions.Count),
                Is.EqualTo(new[] { 1, 1, 2 }));
            Assert.That(levels.Select(level => level.PurposeLine),
                Is.EqualTo(new[]
                {
                    "LEARN THE CUT",
                    "WATCH THE THREAT",
                    "KEEP THEM TOGETHER",
                }));
            Assert.That(levels[0].ThreatMotion.InitialPosition,
                Is.EqualTo(new LogicalPoint(5f, 8f)));
            Assert.That(levels[0].ThreatMotion.Radius, Is.EqualTo(0.35f));
            Assert.That(levels[1].ThreatMotion.InitialPosition,
                Is.EqualTo(new LogicalPoint(4.5f, 3.5f)));
            Assert.That(levels[1].ThreatMotion.Radius, Is.EqualTo(0.38f));
            Assert.That(levels[2].ThreatMotions.Select(
                    threat => threat.InitialPosition),
                Is.EqualTo(new[]
                {
                    new LogicalPoint(3f, 5f),
                    new LogicalPoint(7f, 11f),
                }));
            Assert.That(levels[2].ThreatMotions.Select(threat => threat.Speed),
                Is.EqualTo(new[] { 2.7f, 2.9f }));
            Assert.That(levels[2].ThreatMotions.Select(threat => threat.Radius),
                Is.EqualTo(new[] { 0.35f, 0.35f }));
        }

        [Test]
        public void Defaults_UseFixedBoardAndLegalNormalizedThreatSpawns()
        {
            foreach (CoreFunLevelConfiguration level in Defaults())
            {
                Assert.That(level.ThreatMotion.BoardBounds,
                    Is.EqualTo(CoreFunLevelConfiguration.FixedBoardBounds));
                foreach (ThreatMotionConfiguration threat in
                         level.ThreatMotions)
                {
                    Assert.That(threat.InitialDirection.Length,
                        Is.EqualTo(1f).Within(0.00001f));
                    Assert.That(threat.BoardBounds.Contains(
                        threat.InitialPosition), Is.True);
                    Assert.That(threat.Radius, Is.GreaterThan(0f));
                }

                Assert.That(level.MaximumCatchUpTicks, Is.EqualTo(8));
            }

            Assert.That(Defaults().Select(
                    level => level.MaximumExpectedCompletionSeconds),
                Is.EqualTo(new[] { 15f, 30f, 45f }));
        }

        [Test]
        public void AuthoredTuning_OrdinaryLevelOneCutDoesNotCompleteAndLevelTwoDiffers()
        {
            CoreFunLevelConfiguration[] levels = Defaults();
            CoreFunLevelConfiguration first = levels[0];
            CoreFunLevelConfiguration second = levels[1];
            var board = new CaptureBoardState(
                first.ThreatMotion.BoardBounds,
                new[]
                {
                    new ThreatState(
                        new ThreatId(1),
                        new RoomId(1),
                        first.ThreatMotion.InitialPosition,
                        first.ThreatMotion.InitialDirection
                            * first.ThreatMotion.Speed,
                        first.ThreatMotion.Radius),
                },
                Tolerance);
            var ordinaryCut = new BarrierState(
                new BarrierId(1),
                new RoomId(1),
                new LogicalPoint(3f, 8f),
                BarrierOrientation.Vertical,
                8f,
                8f,
                8f,
                8f,
                first.Barrier.GrowthSpeed,
                first.Barrier.CollisionHalfWidth,
                BarrierLifecycle.Locked);

            Assert.That(board.TryApplyLockedBarrier(ordinaryCut).Applied,
                Is.True);
            Assert.That(board.CapturedFraction,
                Is.LessThan(first.Capture.TargetCapturedFraction));
            Assert.That(second.Capture.TargetCapturedFraction,
                Is.Not.EqualTo(first.Capture.TargetCapturedFraction));
            Assert.That(second.ThreatMotion.InitialPosition,
                Is.Not.EqualTo(first.ThreatMotion.InitialPosition));
            Assert.That(second.ThreatMotion.InitialDirection,
                Is.Not.EqualTo(first.ThreatMotion.InitialDirection));
            Assert.That(second.ThreatMotion.Speed,
                Is.Not.EqualTo(first.ThreatMotion.Speed));
            Assert.That(second.Barrier.GrowthSpeed,
                Is.LessThan(first.Barrier.GrowthSpeed));
            Assert.That(second.Barrier.MinimumEdgeMargin,
                Is.Not.EqualTo(first.Barrier.MinimumEdgeMargin));
        }

        [Test]
        public void LevelTwo_CarelessCrossingBreaksButWaitingCreatesSafeLockWindow()
        {
            CoreFunLevelConfiguration level = Defaults()[1];
            var careless = new ThreatMotionSession(
                level.ThreatMotions,
                level.Barrier,
                level.Capture,
                Tolerance);

            BarrierStartResult carelessStart = careless.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(5f, 8f),
                    BarrierOrientation.Horizontal));
            careless.Tick(2f);

            Assert.That(carelessStart.Accepted, Is.True);
            Assert.That(careless.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(careless.FailedBarrierCount, Is.EqualTo(1));
            Assert.That(careless.ActiveBarrier.HasValue, Is.False);

            var deliberate = new ThreatMotionSession(
                level.ThreatMotions,
                level.Barrier,
                level.Capture,
                Tolerance);
            deliberate.Tick(1.85f);
            Assert.That(deliberate.Threat.Position.Y, Is.GreaterThan(8.46f));
            Assert.That(deliberate.Threat.Velocity.Y, Is.GreaterThan(0f));

            BarrierStartResult deliberateStart = deliberate.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(5f, 8f),
                    BarrierOrientation.Horizontal));
            deliberate.Tick(2.1f);

            Assert.That(deliberateStart.Accepted, Is.True);
            Assert.That(deliberate.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Locked));
            Assert.That(deliberate.FailedBarrierCount, Is.Zero);
            Assert.That(deliberate.LockedBarrierCount, Is.EqualTo(1));
        }

        [Test]
        public void LevelThree_TwoNormalThreatsMoveAndSplitWithoutCapture()
        {
            CoreFunLevelConfiguration level = Defaults()[2];
            var session = new ThreatMotionSession(
                level.ThreatMotions,
                level.Barrier,
                level.Capture,
                Tolerance);

            Assert.That(session.Threats.Count, Is.EqualTo(2));
            Assert.That(session.Threats.Select(threat => threat.Id.Value),
                Is.EqualTo(new[] { 1, 2 }));
            LogicalPoint[] before = session.Threats
                .Select(threat => threat.Position)
                .ToArray();

            session.Tick(0.5f);

            Assert.That(session.Threats[0].Position, Is.Not.EqualTo(before[0]));
            Assert.That(session.Threats[1].Position, Is.Not.EqualTo(before[1]));
            var split = new BarrierState(
                new BarrierId(1),
                new RoomId(1),
                new LogicalPoint(5f, 8f),
                BarrierOrientation.Vertical,
                8f,
                8f,
                8f,
                8f,
                level.Barrier.GrowthSpeed,
                level.Barrier.CollisionHalfWidth,
                BarrierLifecycle.Locked);
            RoomSplitApplyResult result =
                session.Board.TryApplyLockedBarrier(split);

            Assert.That(result.Applied, Is.True);
            Assert.That(session.Board.ActiveRooms.Count, Is.EqualTo(2));
            Assert.That(session.Board.CapturedRooms, Is.Empty);
            Assert.That(session.Board.CapturedArea, Is.Zero);
            Assert.That(session.Board.Threats.Select(threat => threat.RoomId)
                .Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void LevelThree_ResetRestoresBothStableThreats()
        {
            CoreFunLevelConfiguration level = Defaults()[2];
            var session = new ThreatMotionSession(
                level.ThreatMotions,
                level.Barrier,
                level.Capture,
                Tolerance);
            ThreatState[] initial = session.Threats.ToArray();

            session.Tick(1f);
            session.Reset();

            Assert.That(session.Threats, Is.EqualTo(initial));
            Assert.That(session.ActiveBarrier.HasValue, Is.False);
            Assert.That(session.CapturedFraction, Is.Zero);
        }

        [Test]
        public void LevelThree_EarliestThreatBreaksSharedBarrierAndBothMove()
        {
            CoreFunLevelConfiguration level = Defaults()[2];
            var session = new ThreatMotionSession(
                level.ThreatMotions,
                level.Barrier,
                level.Capture,
                Tolerance);
            ThreatState[] before = session.Threats.ToArray();
            BarrierStartResult start = session.TryStartBarrier(
                new BarrierIntent(
                    before[0].Position,
                    BarrierOrientation.Horizontal));

            session.Tick(1f / 60f);

            Assert.That(start.Accepted, Is.True);
            Assert.That(session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(session.ActiveBarrier.HasValue, Is.False);
            Assert.That(session.FailedBarrierCount, Is.EqualTo(1));
            Assert.That(session.Threats[0].Position,
                Is.Not.EqualTo(before[0].Position));
            Assert.That(session.Threats[1].Position,
                Is.Not.EqualTo(before[1].Position));
            Assert.That(session.Board.ValidateCurrentInvariants(), Is.True);
        }

        [Test]
        public void Catalog_RejectsDuplicateIdsAndOutOfOrderNumbers()
        {
            CoreFunLevelConfiguration first = Level("same", 1);
            CoreFunLevelConfiguration duplicate = Level("same", 2);
            CoreFunLevelConfiguration outOfOrder = Level("second", 3);

            Assert.Throws<ArgumentException>(() =>
                new CoreFunLevelCatalog(new[] { first, duplicate }));
            Assert.Throws<ArgumentException>(() =>
                new CoreFunLevelCatalog(new[] { first, outOfOrder }));
            Assert.Throws<ArgumentException>(() =>
                new CoreFunLevelCatalog(Array.Empty<CoreFunLevelConfiguration>()));
        }

        [Test]
        public void RuntimeLevel_RejectsMissingIdAndNonFixedBoard()
        {
            Assert.Throws<ArgumentException>(() => Level(" ", 1));

            var wrongBoard = new ThreatMotionConfiguration(
                new LogicalRect(0f, 0f, 11f, 16f),
                new LogicalPoint(5f, 8f),
                new LogicalVector(1f, 0f),
                2.6f,
                0.35f,
                8);
            Assert.Throws<ArgumentException>(() =>
                RuntimeLevel("wrong-board", 1, wrongBoard));
        }

        [TestCase(-0.1f, 8f)]
        [TestCase(10.1f, 8f)]
        [TestCase(5f, -0.1f)]
        [TestCase(5f, 16.1f)]
        public void RuntimeLevel_RejectsSpawnCircleOutsideBoard(float x, float y)
        {
            var threat = new ThreatMotionConfiguration(
                CoreFunLevelConfiguration.FixedBoardBounds,
                new LogicalPoint(x, y),
                new LogicalVector(1f, 0f),
                2.6f,
                0.35f,
                8);

            Assert.Throws<ArgumentException>(() =>
                RuntimeLevel("outside", 1, threat));
        }

        [Test]
        public void SerializedDefinition_RejectsInvalidTargetSpeedRadiusAndDirection()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Definition(speed: 0f).ToRuntimeConfiguration());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Definition(radius: 0f).ToRuntimeConfiguration());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Definition(target: 0f).ToRuntimeConfiguration());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Definition(direction: Vector2.zero).ToRuntimeConfiguration());
        }

        [Test]
        public void SameLevelConfiguration_InitializesIdenticalSessions()
        {
            CoreFunLevelConfiguration level = Defaults()[2];
            var left = new ThreatMotionSession(
                level.ThreatMotions,
                level.Barrier,
                level.Capture,
                Tolerance);
            var right = new ThreatMotionSession(
                level.ThreatMotions,
                level.Barrier,
                level.Capture,
                Tolerance);

            Assert.That(left.InitialRoom, Is.EqualTo(right.InitialRoom));
            Assert.That(left.Threat, Is.EqualTo(right.Threat));
            Assert.That(left.Threats, Is.EqualTo(right.Threats));
            Assert.That(left.Threats.Count, Is.EqualTo(2));
            Assert.That(left.TargetCapturedFraction,
                Is.EqualTo(right.TargetCapturedFraction));
            Assert.That(left.CapturedFraction, Is.Zero);
            Assert.That(right.CapturedFraction, Is.Zero);
        }

        [Test]
        public void Metrics_AccumulateFailuresSuccessAndLargestCapture()
        {
            var tracker = new CoreFunMetricsTracker();
            tracker.StartSequence(Defaults()[0]);
            tracker.AdvanceTime(12.5f);
            tracker.RecordBarrierAttempt();
            tracker.RecordBarrierFailure(0f);
            tracker.RecordBarrierAttempt();
            tracker.RecordBarrierSuccess(0.2f, 0.2f);
            tracker.RecordBarrierAttempt();
            tracker.RecordBarrierSuccess(0.425f, 0.625f);
            tracker.RecordCompletion(0.625f);

            CoreFunLevelMetrics metrics = tracker.Current;
            Assert.That(metrics.ElapsedSeconds, Is.EqualTo(12.5f));
            Assert.That(metrics.BarrierAttempts, Is.EqualTo(3));
            Assert.That(metrics.FailedBarriers, Is.EqualTo(1));
            Assert.That(metrics.SuccessfulBarriers, Is.EqualTo(2));
            Assert.That(metrics.LargestSingleCapturedFraction,
                Is.EqualTo(0.425f));
            Assert.That(metrics.FinalCapturedFraction, Is.EqualTo(0.625f));
        }

        [Test]
        public void Metrics_RetryResetsRunAndIncrementsLevelRetryCount()
        {
            var tracker = new CoreFunMetricsTracker();
            tracker.StartSequence(Defaults()[0]);
            tracker.AdvanceTime(9f);
            tracker.RecordBarrierAttempt();
            tracker.RecordBarrierFailure(0f);

            tracker.RetryCurrentLevel();

            CoreFunLevelMetrics metrics = tracker.Current;
            Assert.That(metrics.ElapsedSeconds, Is.Zero);
            Assert.That(metrics.LevelStartTimeSeconds, Is.EqualTo(9f));
            Assert.That(metrics.BarrierAttempts, Is.Zero);
            Assert.That(metrics.FailedBarriers, Is.Zero);
            Assert.That(metrics.SuccessfulBarriers, Is.Zero);
            Assert.That(metrics.FinalCapturedFraction, Is.Zero);
            Assert.That(metrics.RetryCount, Is.EqualTo(1));
        }

        [Test]
        public void Metrics_AccumulatesPerformanceBonusSignals()
        {
            var tracker = new CoreFunMetricsTracker();
            tracker.StartSequence(Defaults()[0]);

            tracker.RecordNearMiss();
            tracker.RecordNearMiss();
            tracker.RecordPerfectCut();
            tracker.RecordPowerUpUsed();

            CoreFunLevelMetrics metrics = tracker.Current;
            Assert.That(metrics.NearMissCount, Is.EqualTo(2));
            Assert.That(metrics.PerfectCutCount, Is.EqualTo(1));
            Assert.That(metrics.AnyPowerUpUsed, Is.True);
        }

        [Test]
        public void Metrics_RetryResetsPerformanceBonusSignals()
        {
            var tracker = new CoreFunMetricsTracker();
            tracker.StartSequence(Defaults()[0]);
            tracker.RecordNearMiss();
            tracker.RecordPerfectCut();
            tracker.RecordPowerUpUsed();

            tracker.RetryCurrentLevel();

            CoreFunLevelMetrics metrics = tracker.Current;
            Assert.That(metrics.NearMissCount, Is.Zero);
            Assert.That(metrics.PerfectCutCount, Is.Zero);
            Assert.That(metrics.AnyPowerUpUsed, Is.False);
        }

        [Test]
        public void Metrics_AdvanceToNextLevelResetsPerformanceBonusSignals()
        {
            CoreFunLevelConfiguration[] levels = Defaults();
            var tracker = new CoreFunMetricsTracker();
            tracker.StartSequence(levels[0]);
            tracker.RecordNearMiss();
            tracker.RecordPowerUpUsed();

            tracker.AdvanceTo(levels[1]);

            CoreFunLevelMetrics metrics = tracker.Current;
            Assert.That(metrics.NearMissCount, Is.Zero);
            Assert.That(metrics.AnyPowerUpUsed, Is.False);
            Assert.That(tracker.SequenceRuns[0].NearMissCount,
                Is.EqualTo(1),
                "The completed run's own snapshot must keep its earned " +
                "bonus signals even after the next level resets the " +
                "live tracker.");
            Assert.That(tracker.SequenceRuns[0].AnyPowerUpUsed, Is.True);
        }

        [Test]
        public void Metrics_NextAndFinalRestartRecordCompleteSequence()
        {
            CoreFunLevelConfiguration[] levels = Defaults();
            var tracker = new CoreFunMetricsTracker();
            tracker.StartSequence(levels[0]);
            tracker.AdvanceTime(5f);
            tracker.RecordCompletion(0.625f);
            tracker.AdvanceTo(levels[1]);
            tracker.AdvanceTime(7f);
            tracker.RecordCompletion(0.7f);
            tracker.AdvanceTo(levels[2]);
            tracker.RecordCompletion(0.75f);

            tracker.CompleteSequenceAndRestart(levels[0]);

            Assert.That(tracker.SequenceCompletionCount, Is.EqualTo(1));
            Assert.That(tracker.LastCompletedSequence.Count, Is.EqualTo(3));
            Assert.That(tracker.LastCompletedSequence[0].NextPressed, Is.True);
            Assert.That(tracker.LastCompletedSequence[1].NextPressed, Is.True);
            Assert.That(tracker.LastCompletedSequence[2].NextPressed, Is.False);
            Assert.That(tracker.LastCompletedSequence
                .Select(run => run.LevelStartTimeSeconds),
                Is.EqualTo(new[] { 0f, 5f, 12f }));
            Assert.That(tracker.Current.LevelNumber, Is.EqualTo(1));
            Assert.That(tracker.Current.FinalCapturedFraction, Is.Zero);
        }

        [TestCase(120f, 87f, 3.5f)]
        [TestCase(-131f, 109f, 5f)]
        public void Solver_HighSpeedRepeatedTicksStayInsideInsetBoard(
            float velocityX,
            float velocityY,
            float seconds)
        {
            var room = new RoomState(
                new RoomId(1),
                CoreFunLevelConfiguration.FixedBoardBounds);
            var threat = new ThreatState(
                new ThreatId(1),
                room.Id,
                new LogicalPoint(5f, 8f),
                new LogicalVector(velocityX, velocityY),
                0.35f);
            int ticks = Mathf.RoundToInt(seconds * 60f);
            for (int tick = 0; tick < ticks; tick++)
            {
                threat = ThreatMotionSolver.Move(
                    room,
                    threat,
                    1f / 60f,
                    16,
                    Tolerance).Threat;
                Assert.That(threat.Position.X, Is.InRange(0.35f, 9.65f));
                Assert.That(threat.Position.Y, Is.InRange(0.35f, 15.65f));
            }
        }

        [Test]
        public void RepeatedNarrowRoomSplitsPreserveAreaAndMonotonicProgress()
        {
            var threat = new ThreatState(
                new ThreatId(1),
                new RoomId(1),
                new LogicalPoint(9.6f, 8f),
                new LogicalVector(1f, 0f),
                0.1f);
            var board = new CaptureBoardState(
                CoreFunLevelConfiguration.FixedBoardBounds,
                new[] { threat },
                Tolerance);
            float previous = 0f;
            float[] splits = { 2f, 4f, 6f, 8f, 9f };
            for (int index = 0; index < splits.Length; index++)
            {
                RoomState parent = board.ActiveRooms.Single();
                float split = splits[index];
                var barrier = new BarrierState(
                    new BarrierId(index + 1),
                    parent.Id,
                    new LogicalPoint(split, 8f),
                    BarrierOrientation.Vertical,
                    8f,
                    8f,
                    8f,
                    8f,
                    8f,
                    0.08f,
                    BarrierLifecycle.Locked);

                Assert.That(board.TryApplyLockedBarrier(barrier).Applied,
                    Is.True);
                Assert.That(board.CapturedFraction, Is.GreaterThan(previous));
                Assert.That(board.ValidateCurrentInvariants(), Is.True);
                Assert.That(board.ActiveArea + board.CapturedArea,
                    Is.EqualTo(160f).Within(Tolerance.AreaTolerance));
                previous = board.CapturedFraction;
            }

            Assert.That(board.ActiveRooms.Single().Bounds,
                Is.EqualTo(new LogicalRect(9f, 0f, 1f, 16f)));
            Assert.That(board.CapturedFraction, Is.EqualTo(0.9f));
            Assert.That(board.Threats.Single().RoomId,
                Is.EqualTo(board.ActiveRooms.Single().Id));
        }

        private static CoreFunLevelConfiguration[] Defaults() =>
            CoreFunLevelDefinition.CreateMilestone3Defaults()
                .Select(definition => definition.ToRuntimeConfiguration())
                .ToArray();

        private static CoreFunLevelConfiguration Level(string id, int number) =>
            RuntimeLevel(id, number, new ThreatMotionConfiguration(
                CoreFunLevelConfiguration.FixedBoardBounds,
                new LogicalPoint(5f, 8f),
                new LogicalVector(1f, 0f),
                2.6f,
                0.35f,
                8));

        private static CoreFunLevelConfiguration RuntimeLevel(
            string id,
            int number,
            ThreatMotionConfiguration threat) =>
            new CoreFunLevelConfiguration(
                id,
                number,
                threat,
                new BarrierConfiguration(9.5f, 0.08f, 0.75f, 16),
                new CaptureLevelConfiguration(0.625f),
                8,
                string.Empty,
                45f);

        private static CoreFunLevelDefinition Definition(
            float speed = 2.6f,
            float radius = 0.35f,
            float target = 0.625f,
            Vector2? direction = null) =>
            new CoreFunLevelDefinition(
                "test-level",
                1,
                new Vector2(5f, 8f),
                direction ?? Vector2.right,
                speed,
                radius,
                target,
                9.5f,
                0.08f,
                0.75f,
                8,
                16,
                8,
                string.Empty,
                45f);
    }
}
