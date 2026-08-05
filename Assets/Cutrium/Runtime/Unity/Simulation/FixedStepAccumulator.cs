using System;
using Cutrium.Gameplay.Geometry;

namespace Cutrium.Unity.Simulation
{
    public readonly struct FixedStepAdvanceResult
    {
        public FixedStepAdvanceResult(
            int processedTicks,
            int droppedTicks,
            float droppedTime,
            float remainingTime)
        {
            ProcessedTicks = processedTicks;
            DroppedTicks = droppedTicks;
            DroppedTime = droppedTime;
            RemainingTime = remainingTime;
        }

        public int ProcessedTicks { get; }

        public int DroppedTicks { get; }

        public float DroppedTime { get; }

        public float RemainingTime { get; }

        public bool WasCatchUpCapped => DroppedTicks > 0;
    }

    public sealed class FixedStepAccumulator
    {
        private readonly float _step;
        private readonly int _maximumTicksPerAdvance;
        private readonly GeometryTolerancePolicy _tolerance;
        private float _accumulatedTime;

        public FixedStepAccumulator(
            float step,
            int maximumTicksPerAdvance,
            GeometryTolerancePolicy tolerance)
        {
            if (!IsFinite(step) || step <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(step),
                    step,
                    "The fixed simulation step must be finite and positive.");
            }

            if (maximumTicksPerAdvance <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumTicksPerAdvance),
                    maximumTicksPerAdvance,
                    "The catch-up tick limit must be positive.");
            }

            _step = step;
            _maximumTicksPerAdvance = maximumTicksPerAdvance;
            _tolerance = tolerance;
        }

        public float Step => _step;

        public int MaximumTicksPerAdvance => _maximumTicksPerAdvance;

        public float AccumulatedTime => _accumulatedTime;

        public FixedStepAdvanceResult Advance(
            float renderDeltaTime,
            Action<float> tick)
        {
            if (!IsFinite(renderDeltaTime) || renderDeltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(renderDeltaTime),
                    renderDeltaTime,
                    "Render delta time must be finite and non-negative.");
            }

            if (tick == null)
            {
                throw new ArgumentNullException(nameof(tick));
            }

            _accumulatedTime += renderDeltaTime;
            int availableTicks = (int)Math.Floor(
                (_accumulatedTime + _tolerance.TimeTolerance) / _step);
            int processedTicks = Math.Min(
                availableTicks,
                _maximumTicksPerAdvance);

            for (int index = 0; index < processedTicks; index++)
            {
                tick(_step);
            }

            _accumulatedTime -= processedTicks * _step;
            int droppedTicks = Math.Max(0, availableTicks - processedTicks);
            float droppedTime = droppedTicks * _step;
            _accumulatedTime -= droppedTime;

            if (_accumulatedTime < 0f
                && _tolerance.IsTimeApproximatelyEqual(
                    _accumulatedTime,
                    0f))
            {
                _accumulatedTime = 0f;
            }

            return new FixedStepAdvanceResult(
                processedTicks,
                droppedTicks,
                droppedTime,
                _accumulatedTime);
        }

        public void Reset()
        {
            _accumulatedTime = 0f;
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
