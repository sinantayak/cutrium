using System;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Gameplay.Threats;
using Cutrium.Unity.Simulation;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class FixedStepAccumulatorTests
    {
        private static readonly GeometryTolerancePolicy Tolerance =
            new GeometryTolerancePolicy(0.0001f, 0.00001f, 0.0001f, 0.001f);

        [Test]
        public void Accumulator_UsesExactSixtiethStepAndIgnoresPartialTime()
        {
            var accumulator = new FixedStepAccumulator(
                FirstPlayableController.SimulationStep,
                8,
                Tolerance);
            int ticks = 0;

            FixedStepAdvanceResult partial = accumulator.Advance(
                FirstPlayableController.SimulationStep * 0.5f,
                _ => ticks++);
            FixedStepAdvanceResult complete = accumulator.Advance(
                FirstPlayableController.SimulationStep * 0.5f,
                _ => ticks++);

            Assert.That(partial.ProcessedTicks, Is.Zero);
            Assert.That(complete.ProcessedTicks, Is.EqualTo(1));
            Assert.That(ticks, Is.EqualTo(1));
            Assert.That(accumulator.Step, Is.EqualTo(1f / 60f));
        }

        [Test]
        public void Accumulator_BoundsCatchUpAndReportsDroppedTime()
        {
            var accumulator = new FixedStepAccumulator(
                1f / 60f,
                3,
                Tolerance);
            int ticks = 0;

            FixedStepAdvanceResult result = accumulator.Advance(
                10f / 60f,
                _ => ticks++);

            Assert.That(ticks, Is.EqualTo(3));
            Assert.That(result.ProcessedTicks, Is.EqualTo(3));
            Assert.That(result.DroppedTicks, Is.EqualTo(7));
            Assert.That(result.DroppedTime, Is.EqualTo(7f / 60f).Within(0.00001f));
            Assert.That(result.WasCatchUpCapped, Is.True);
        }

        [Test]
        public void Accumulator_DifferentRenderDeltaSequencesProduceSameState()
        {
            ThreatMotionSession manyFrames = CreateSession();
            ThreatMotionSession fewFrames = CreateSession();
            var manyAccumulator = new FixedStepAccumulator(
                1f / 60f, 120, Tolerance);
            var fewAccumulator = new FixedStepAccumulator(
                1f / 60f, 120, Tolerance);

            for (int index = 0; index < 60; index++)
            {
                manyAccumulator.Advance(1f / 60f, manyFrames.Tick);
            }

            for (int index = 0; index < 10; index++)
            {
                fewAccumulator.Advance(0.1f, fewFrames.Tick);
            }

            Assert.That(manyFrames.TickCount, Is.EqualTo(60));
            Assert.That(fewFrames.TickCount, Is.EqualTo(60));
            Assert.That(
                Tolerance.AreApproximatelyEqual(
                    manyFrames.Threat.Position,
                    fewFrames.Threat.Position),
                Is.True);
            Assert.That(
                Tolerance.AreApproximatelyEqual(
                    manyFrames.Threat.Velocity,
                    fewFrames.Threat.Velocity),
                Is.True);
        }

        [Test]
        public void Accumulator_ResetClearsRemainder()
        {
            var accumulator = new FixedStepAccumulator(1f / 60f, 8, Tolerance);
            accumulator.Advance(0.01f, _ => { });

            accumulator.Reset();

            Assert.That(accumulator.AccumulatedTime, Is.Zero);
        }

        [Test]
        public void Accumulator_RejectsInvalidConfigurationAndInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FixedStepAccumulator(0f, 8, Tolerance));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FixedStepAccumulator(1f / 60f, 0, Tolerance));

            var accumulator = new FixedStepAccumulator(1f / 60f, 8, Tolerance);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                accumulator.Advance(float.NaN, _ => { }));
            Assert.Throws<ArgumentNullException>(() =>
                accumulator.Advance(0f, null));
        }

        private static ThreatMotionSession CreateSession()
        {
            var configuration = new ThreatMotionConfiguration(
                new LogicalRect(0f, 0f, 10f, 16f),
                new LogicalPoint(5f, 8f),
                new LogicalVector(0.8f, 0.6f),
                3f,
                0.35f,
                8);
            return new ThreatMotionSession(configuration, Tolerance);
        }
    }
}
