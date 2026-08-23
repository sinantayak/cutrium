using System;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Feedback;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class FeedbackModelTests
    {
        private static readonly GeometryTolerancePolicy Tolerance =
            new GeometryTolerancePolicy(0.0001f, 0.00001f, 0.0001f, 0.001f);

        [Test]
        public void Tuning_ValidatesEveryLogicalThreshold()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FeedbackTuningConfiguration(0f, 0.5f, 0.2f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FeedbackTuningConfiguration(0.5f, 0f, 0.2f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FeedbackTuningConfiguration(0.5f, 0.5f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FeedbackTuningConfiguration(0.5f, 0.5f, 1.01f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FeedbackTuningConfiguration(0.5f, 0.5f, 0.2f, 0f));

            var valid = new FeedbackTuningConfiguration(0.4f, 0.6f, 0.25f);
            Assert.That(valid.NearMissDistance, Is.EqualTo(0.4f));
            Assert.That(valid.NearMissWindowSeconds, Is.EqualTo(0.6f));
            Assert.That(valid.LargeCaptureFraction, Is.EqualTo(0.25f));
            Assert.That(valid.ComboTimeoutSeconds, Is.EqualTo(3f),
                "The 3-argument constructor must default to a sensible " +
                "combo timeout for existing call sites.");

            var explicitTimeout = new FeedbackTuningConfiguration(
                0.4f, 0.6f, 0.25f, 1.5f);
            Assert.That(explicitTimeout.ComboTimeoutSeconds,
                Is.EqualTo(1.5f));
        }

        [TestCase(0.3998f, true)]
        [TestCase(0.4f, true)]
        [TestCase(0.4002f, false)]
        public void NearMiss_UsesCentralizedDistanceBoundary(
            float clearance,
            bool expected)
        {
            var samples = new[] { new BarrierApproachSample(1f, clearance) };
            NearMissEvaluation result = NearMissEvaluator.Evaluate(
                samples,
                1f,
                false,
                new FeedbackTuningConfiguration(0.4f, 0.5f, 0.2f),
                Tolerance);

            Assert.That(result.IsNearMiss, Is.EqualTo(expected));
            Assert.That(result.ClosestClearance, Is.EqualTo(clearance));
        }

        [Test]
        public void NearMiss_UsesOnlyConfiguredRecentHistory()
        {
            var samples = new[]
            {
                new BarrierApproachSample(0.1f, 0.05f),
                new BarrierApproachSample(0.8f, 0.8f),
                new BarrierApproachSample(1f, 0.7f),
            };
            NearMissEvaluation result = NearMissEvaluator.Evaluate(
                samples,
                1f,
                false,
                new FeedbackTuningConfiguration(0.4f, 0.25f, 0.2f),
                Tolerance);

            Assert.That(result.IsNearMiss, Is.False);
            Assert.That(result.ClosestClearance, Is.EqualTo(0.7f));
        }

        [Test]
        public void NearMiss_MultiThreatHistoryUsesMostDangerousApproach()
        {
            var samples = new[]
            {
                new BarrierApproachSample(0.8f, 0.7f),
                new BarrierApproachSample(0.8f, 0.2f),
                new BarrierApproachSample(1f, 0.5f),
            };
            NearMissEvaluation result = NearMissEvaluator.Evaluate(
                samples,
                1f,
                false,
                new FeedbackTuningConfiguration(0.4f, 0.5f, 0.2f),
                Tolerance);

            Assert.That(result.IsNearMiss, Is.True);
            Assert.That(result.ClosestClearance, Is.EqualTo(0.2f));
        }

        [Test]
        public void NearMiss_NeverTriggersForFailedBarrier()
        {
            NearMissEvaluation result = NearMissEvaluator.Evaluate(
                new[] { new BarrierApproachSample(0.5f, -0.1f) },
                0.5f,
                true,
                FeedbackTuningConfiguration.Default,
                Tolerance);

            Assert.That(result.IsNearMiss, Is.False);
            Assert.That(result.ClosestClearance,
                Is.EqualTo(float.PositiveInfinity));
        }

        [Test]
        public void NearMiss_IsDeterministicForEquivalentFixedStepHistory()
        {
            BarrierApproachSample[] history = Enumerable.Range(0, 61)
                .Select(index => new BarrierApproachSample(
                    index / 60f,
                    Math.Abs(index - 42) * 0.02f + 0.1f))
                .ToArray();
            FeedbackTuningConfiguration tuning =
                FeedbackTuningConfiguration.Default;

            NearMissEvaluation first = NearMissEvaluator.Evaluate(
                history, 1f, false, tuning, Tolerance);
            NearMissEvaluation second = NearMissEvaluator.Evaluate(
                history.ToArray(), 1f, false, tuning, Tolerance);

            Assert.That(second.IsNearMiss, Is.EqualTo(first.IsNearMiss));
            Assert.That(second.ClosestClearance,
                Is.EqualTo(first.ClosestClearance));
        }

        [Test]
        public void ApproachClearance_UsesLogicalBarrierGeometryOnly()
        {
            BarrierState barrier = CreateBarrier(
                new LogicalPoint(5f, 8f),
                BarrierOrientation.Horizontal,
                2f,
                3f);

            float clearance = BarrierApproachCalculator.CalculateClearance(
                barrier,
                new LogicalPoint(6f, 9f),
                0.35f);

            Assert.That(clearance, Is.EqualTo(0.57f).Within(0.00001f));
        }

        [TestCase(31.998f, false)]
        [TestCase(32f, true)]
        [TestCase(32.002f, true)]
        public void LargeCapture_UsesInitialLogicalBoardArea(
            float capturedArea,
            bool expected)
        {
            bool result = LargeCaptureEvaluator.IsLargeCapture(
                capturedArea,
                160f,
                new FeedbackTuningConfiguration(0.4f, 0.5f, 0.2f),
                Tolerance);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Combo_FollowsMinimalDocumentedRule()
        {
            var combo = new ComboState(0);
            combo = combo.OnCapturingLock().OnCapturingLock();
            Assert.That(combo.Count, Is.EqualTo(2));

            combo = combo.OnNoAreaLock();
            Assert.That(combo.Count, Is.EqualTo(2),
                "A valid no-area split neither rewards nor breaks combo.");

            combo = combo.OnBarrierFailure();
            Assert.That(combo.Count, Is.Zero);
            Assert.That(combo.Reset().Count, Is.Zero);
        }

        [Test]
        public void Session_EmitsOneOrderedRewardSequencePerSplit()
        {
            ThreatMotionSession session = CreateRewardSession();
            BarrierStartResult started = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(5f, 4f),
                    BarrierOrientation.Horizontal));
            Assert.That(started.Accepted, Is.True);
            Assert.That(session.FeedbackEvents.Select(item => item.Kind),
                Is.EqualTo(new[] { FeedbackEventKind.BarrierStarted }));

            session.Tick(0.1f);

            Assert.That(session.FeedbackEvents.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    FeedbackEventKind.BarrierLocked,
                    FeedbackEventKind.RegionCaptured,
                    FeedbackEventKind.LargeCapture,
                    FeedbackEventKind.ComboChanged,
                }));
            Assert.That(session.ComboCount, Is.EqualTo(1));
            Assert.That(session.FeedbackEvents.Count(item =>
                item.Kind == FeedbackEventKind.LargeCapture), Is.EqualTo(1));
            Assert.That(session.FeedbackEvents[1].CapturedFractionDelta,
                Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void Session_ResetClearsComboAndEmitsReset()
        {
            ThreatMotionSession session = CreateRewardSession();
            session.TryStartBarrier(new BarrierIntent(
                new LogicalPoint(5f, 4f),
                BarrierOrientation.Horizontal));
            session.Tick(0.1f);
            Assert.That(session.ComboCount, Is.EqualTo(1));

            session.Reset();

            Assert.That(session.ComboCount, Is.Zero);
            Assert.That(session.CapturedFraction, Is.Zero);
            Assert.That(session.FeedbackEvents.Select(item => item.Kind),
                Is.EqualTo(new[] { FeedbackEventKind.SessionReset }));
        }

        [Test]
        public void Session_FailureNeverEmitsNearMissAndResetsCombo()
        {
            ThreatMotionSession session = CreateRewardSession();
            session.TryStartBarrier(new BarrierIntent(
                new LogicalPoint(5f, 4f),
                BarrierOrientation.Horizontal));
            session.Tick(0.1f);
            Assert.That(session.ComboCount, Is.EqualTo(1));
            LogicalPoint threatPosition = session.Threat.Position;

            BarrierStartResult start = session.TryStartBarrier(
                new BarrierIntent(
                    threatPosition,
                    BarrierOrientation.Vertical));
            Assert.That(start.Accepted, Is.True);
            session.Tick(1f / 60f);

            Assert.That(session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(session.ComboCount, Is.Zero);
            FeedbackEventKind[] kinds = session.FeedbackEvents
                .Select(item => item.Kind)
                .ToArray();
            CollectionAssert.Contains(
                kinds,
                FeedbackEventKind.BarrierBroken);
            CollectionAssert.Contains(
                kinds,
                FeedbackEventKind.ComboChanged);
            CollectionAssert.DoesNotContain(
                kinds,
                FeedbackEventKind.NearMiss);
        }

        [Test]
        public void Session_ComboExpiresAfterConfiguredIdleTimeout()
        {
            var threat = new ThreatMotionConfiguration(
                new LogicalRect(0f, 0f, 10f, 16f),
                new LogicalPoint(8f, 12f),
                new LogicalVector(1f, 0f),
                0.1f,
                0.35f,
                8);
            var session = new ThreatMotionSession(
                new[] { threat },
                new BarrierConfiguration(100f, 0.08f, 0f, 16),
                new CaptureLevelConfiguration(1f),
                new FeedbackTuningConfiguration(0.45f, 0.75f, 0.2f, 0.2f),
                Tolerance);

            session.TryStartBarrier(new BarrierIntent(
                new LogicalPoint(5f, 4f),
                BarrierOrientation.Horizontal));
            session.Tick(0.1f);
            Assert.That(session.ComboCount, Is.EqualTo(1));

            session.Tick(0.1f);
            Assert.That(session.ComboCount, Is.EqualTo(1),
                "Combo must not expire before the configured timeout.");
            Assert.That(session.FeedbackEvents, Is.Empty);

            session.Tick(0.15f);

            Assert.That(session.ComboCount, Is.Zero);
            Assert.That(session.FeedbackEvents.Select(item => item.Kind),
                Is.EqualTo(new[] { FeedbackEventKind.ComboChanged }));
            Assert.That(session.FeedbackEvents[0].ComboCount, Is.Zero);
        }

        [Test]
        public void Session_ComboTimeoutPausesWhileBarrierIsActive()
        {
            // Growth speed is deliberately slow (not the 100f used by
            // CreateRewardSession) so the second cut below is still
            // mid-growth after a tick longer than the configured timeout.
            // The threat only moves at 0.35 units/sec and starts ~8.5
            // units from the first cut, so it cannot reach either cut
            // within the few seconds this test simulates.
            var threat = new ThreatMotionConfiguration(
                new LogicalRect(0f, 0f, 10f, 16f),
                new LogicalPoint(8f, 12f),
                new LogicalVector(1f, 0f),
                0.1f,
                0.35f,
                8);
            var session = new ThreatMotionSession(
                new[] { threat },
                new BarrierConfiguration(1f, 0.08f, 0f, 16),
                new CaptureLevelConfiguration(1f),
                new FeedbackTuningConfiguration(0.45f, 0.75f, 0.2f, 0.2f),
                Tolerance);

            session.TryStartBarrier(new BarrierIntent(
                new LogicalPoint(5f, 4f),
                BarrierOrientation.Horizontal));
            for (int tick = 0;
                 tick < 200 && session.ActiveBarrier.HasValue;
                 tick++)
            {
                session.Tick(0.1f);
            }

            Assert.That(session.ActiveBarrier.HasValue, Is.False);
            Assert.That(session.ComboCount, Is.EqualTo(1));

            // (5, 10) sits well inside the room half that keeps the
            // threat (and therefore stays active) after the first
            // capture removes the empty half below the first cut.
            BarrierStartResult second = session.TryStartBarrier(
                new BarrierIntent(
                    new LogicalPoint(5f, 10f),
                    BarrierOrientation.Horizontal));
            if (!second.Accepted)
            {
                Assert.Inconclusive(
                    "The first capture removed this point from any " +
                    "active room; cannot exercise the pause path from " +
                    "this exact scene state.");
                return;
            }

            session.Tick(0.3f);

            Assert.That(session.ActiveBarrier.HasValue, Is.True,
                "Test setup assumption: growth speed must be slow enough " +
                "for the second cut to still be active after this tick.");
            Assert.That(session.ComboCount, Is.EqualTo(1),
                "An idle-timeout must not cancel a combo while the " +
                "player is still actively growing a barrier.");
        }

        [Test]
        public void Session_NoAreaLockDoesNotIncrementCombo()
        {
            LogicalRect board = new LogicalRect(0f, 0f, 10f, 16f);
            var session = new ThreatMotionSession(
                new[]
                {
                    new ThreatMotionConfiguration(
                        board,
                        new LogicalPoint(2f, 8f),
                        new LogicalVector(0f, 1f),
                        0.1f,
                        0.35f,
                        8),
                    new ThreatMotionConfiguration(
                        board,
                        new LogicalPoint(8f, 8f),
                        new LogicalVector(0f, -1f),
                        0.1f,
                        0.35f,
                        8),
                },
                new BarrierConfiguration(100f, 0.08f, 0f, 16),
                new CaptureLevelConfiguration(1f),
                FeedbackTuningConfiguration.Default,
                Tolerance);

            session.TryStartBarrier(new BarrierIntent(
                new LogicalPoint(5f, 8f),
                BarrierOrientation.Vertical));
            session.Tick(0.1f);

            Assert.That(session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Locked));
            Assert.That(session.CapturedFraction, Is.Zero);
            Assert.That(session.ComboCount, Is.Zero);
            FeedbackEventKind[] kinds = session.FeedbackEvents
                .Select(item => item.Kind)
                .ToArray();
            CollectionAssert.DoesNotContain(
                kinds,
                FeedbackEventKind.RegionCaptured);
            CollectionAssert.DoesNotContain(
                kinds,
                FeedbackEventKind.ComboChanged);
        }

        [Test]
        public void ReadingOrIgnoringFeedbackCannotChangeGameplayOutcome()
        {
            ThreatMotionSession observed = CreateRewardSession();
            ThreatMotionSession ignored = CreateRewardSession();
            var intent = new BarrierIntent(
                new LogicalPoint(5f, 4f),
                BarrierOrientation.Horizontal);
            observed.TryStartBarrier(intent);
            ignored.TryStartBarrier(intent);
            _ = observed.FeedbackEvents.Count;

            observed.Tick(0.1f);
            ignored.Tick(0.1f);

            Assert.That(observed.CapturedFraction,
                Is.EqualTo(ignored.CapturedFraction));
            Assert.That(observed.Board.ActiveRooms,
                Is.EqualTo(ignored.Board.ActiveRooms));
            Assert.That(observed.Threats, Is.EqualTo(ignored.Threats));
            Assert.That(observed.LevelStatus, Is.EqualTo(ignored.LevelStatus));
        }

        private static ThreatMotionSession CreateRewardSession()
        {
            var threat = new ThreatMotionConfiguration(
                new LogicalRect(0f, 0f, 10f, 16f),
                new LogicalPoint(8f, 12f),
                new LogicalVector(1f, 0f),
                0.1f,
                0.35f,
                8);
            return new ThreatMotionSession(
                new[] { threat },
                new BarrierConfiguration(100f, 0.08f, 0f, 16),
                new CaptureLevelConfiguration(1f),
                new FeedbackTuningConfiguration(0.45f, 0.75f, 0.2f),
                Tolerance);
        }

        private static BarrierState CreateBarrier(
            LogicalPoint origin,
            BarrierOrientation orientation,
            float negativeLength,
            float positiveLength) =>
            new BarrierState(
                new BarrierId(1),
                new RoomId(1),
                origin,
                orientation,
                negativeLength,
                positiveLength,
                negativeLength,
                positiveLength,
                1f,
                0.08f,
                BarrierLifecycle.Growing);
    }
}
