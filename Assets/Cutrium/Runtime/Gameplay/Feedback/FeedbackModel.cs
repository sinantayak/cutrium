using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;

namespace Cutrium.Gameplay.Feedback
{
    public enum FeedbackEventKind
    {
        None = 0,
        SessionReset = 1,
        BarrierStarted = 2,
        BarrierGrowing = 3,
        BarrierLocked = 4,
        BarrierBroken = 5,
        RegionCaptured = 6,
        LargeCapture = 7,
        NearMiss = 8,
        ComboChanged = 9,
        LevelCompleted = 10,
        Ui = 11,
        PowerFreezePulseActivated = 12,
        PowerInstantBarrierArmed = 13,
        PowerInstantBarrierConsumed = 14,
        PowerUnavailable = 15,
        HunterReacted = 16,
        CutLimitExhausted = 17,
        BurnLimitExhausted = 18,
        PowerGravityWellActivated = 19,
    }

    public readonly struct FeedbackEvent
    {
        public FeedbackEvent(
            FeedbackEventKind kind,
            BarrierId barrierId,
            float capturedFractionDelta,
            float capturedFraction,
            float closestClearance,
            int comboCount)
        {
            Kind = kind;
            BarrierId = barrierId;
            CapturedFractionDelta = capturedFractionDelta;
            CapturedFraction = capturedFraction;
            ClosestClearance = closestClearance;
            ComboCount = comboCount;
        }

        public FeedbackEventKind Kind { get; }

        public BarrierId BarrierId { get; }

        public float CapturedFractionDelta { get; }

        public float CapturedFraction { get; }

        public float ClosestClearance { get; }

        public int ComboCount { get; }
    }

    public readonly struct FeedbackTuningConfiguration
    {
        public FeedbackTuningConfiguration(
            float nearMissDistance,
            float nearMissWindowSeconds,
            float largeCaptureFraction)
        {
            ValidatePositive(nearMissDistance, nameof(nearMissDistance));
            ValidatePositive(
                nearMissWindowSeconds,
                nameof(nearMissWindowSeconds));
            if (!IsFinite(largeCaptureFraction)
                || largeCaptureFraction <= 0f
                || largeCaptureFraction > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(largeCaptureFraction));
            }

            NearMissDistance = nearMissDistance;
            NearMissWindowSeconds = nearMissWindowSeconds;
            LargeCaptureFraction = largeCaptureFraction;
        }

        public float NearMissDistance { get; }

        public float NearMissWindowSeconds { get; }

        public float LargeCaptureFraction { get; }

        public static FeedbackTuningConfiguration Default =>
            new FeedbackTuningConfiguration(0.45f, 0.75f, 0.2f);

        private static void ValidatePositive(float value, string name)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public readonly struct BarrierApproachSample
    {
        public BarrierApproachSample(float elapsedSeconds, float clearance)
        {
            if (!IsFinite(elapsedSeconds) || elapsedSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            }

            if (!IsFinite(clearance))
            {
                throw new ArgumentOutOfRangeException(nameof(clearance));
            }

            ElapsedSeconds = elapsedSeconds;
            Clearance = clearance;
        }

        public float ElapsedSeconds { get; }

        public float Clearance { get; }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public readonly struct NearMissEvaluation
    {
        public NearMissEvaluation(bool isNearMiss, float closestClearance)
        {
            IsNearMiss = isNearMiss;
            ClosestClearance = closestClearance;
        }

        public bool IsNearMiss { get; }

        public float ClosestClearance { get; }
    }

    public static class NearMissEvaluator
    {
        public static NearMissEvaluation Evaluate(
            IReadOnlyList<BarrierApproachSample> samples,
            float lockTime,
            bool barrierFailed,
            FeedbackTuningConfiguration configuration,
            GeometryTolerancePolicy tolerance)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (float.IsNaN(lockTime)
                || float.IsInfinity(lockTime)
                || lockTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(lockTime));
            }

            if (barrierFailed)
            {
                return new NearMissEvaluation(false, float.PositiveInfinity);
            }

            float windowStart = Math.Max(
                0f,
                lockTime - configuration.NearMissWindowSeconds);
            float closest = float.PositiveInfinity;
            for (int index = 0; index < samples.Count; index++)
            {
                BarrierApproachSample sample = samples[index];
                if (sample.ElapsedSeconds > lockTime
                    && !tolerance.IsTimeApproximatelyEqual(
                        sample.ElapsedSeconds,
                        lockTime))
                {
                    continue;
                }

                if (sample.ElapsedSeconds < windowStart
                    && !tolerance.IsTimeApproximatelyEqual(
                        sample.ElapsedSeconds,
                        windowStart))
                {
                    continue;
                }

                closest = Math.Min(closest, sample.Clearance);
            }

            bool didNotContact = closest >= 0f
                || tolerance.IsDistanceApproximatelyEqual(closest, 0f);
            bool withinThreshold = closest < configuration.NearMissDistance
                || tolerance.IsDistanceApproximatelyEqual(
                    closest,
                    configuration.NearMissDistance);
            return new NearMissEvaluation(
                didNotContact && withinThreshold,
                closest);
        }
    }

    public static class BarrierApproachCalculator
    {
        public static float CalculateClearance(
            BarrierState barrier,
            LogicalPoint threatPosition,
            float threatRadius)
        {
            if (float.IsNaN(threatRadius)
                || float.IsInfinity(threatRadius)
                || threatRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(threatRadius));
            }

            LogicalPoint start = barrier.NegativeEndpoint;
            LogicalPoint end = barrier.PositiveEndpoint;
            float closestX = barrier.Orientation == BarrierOrientation.Horizontal
                ? Clamp(threatPosition.X, start.X, end.X)
                : start.X;
            float closestY = barrier.Orientation == BarrierOrientation.Vertical
                ? Clamp(threatPosition.Y, start.Y, end.Y)
                : start.Y;
            float deltaX = threatPosition.X - closestX;
            float deltaY = threatPosition.Y - closestY;
            float centerDistance = (float)Math.Sqrt(
                deltaX * deltaX + deltaY * deltaY);
            return centerDistance - threatRadius - barrier.CollisionHalfWidth;
        }

        private static float Clamp(float value, float minimum, float maximum) =>
            Math.Max(minimum, Math.Min(maximum, value));
    }

    public static class LargeCaptureEvaluator
    {
        public static bool IsLargeCapture(
            float capturedArea,
            float initialBoardArea,
            FeedbackTuningConfiguration configuration,
            GeometryTolerancePolicy tolerance)
        {
            if (!IsFinite(capturedArea) || capturedArea < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(capturedArea));
            }

            if (!IsFinite(initialBoardArea) || initialBoardArea <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(initialBoardArea));
            }

            float threshold =
                configuration.LargeCaptureFraction * initialBoardArea;
            return capturedArea > threshold
                || tolerance.IsAreaApproximatelyEqual(capturedArea, threshold);
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public readonly struct ComboState
    {
        public ComboState(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Count = count;
        }

        public int Count { get; }

        public ComboState OnCapturingLock() => new ComboState(Count + 1);

        public ComboState OnNoAreaLock() => this;

        public ComboState OnBarrierFailure() => new ComboState(0);

        public ComboState Reset() => new ComboState(0);
    }
}
