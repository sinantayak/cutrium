using System;
using System.Collections.Generic;

namespace Cutrium.Gameplay.Session
{
    /// Calculates the 0-3 star result for one completed level run from
    /// deterministic metrics and level-authored content. Stars are
    /// cumulative: earning the third star also requires the second-star
    /// no-life-loss condition.
    public static class LevelStarRatingCalculator
    {
        public const int MaximumStars = 3;

        public static int Calculate(
            bool levelCompleted,
            int failedBarrierCount,
            int acceptedCutCount,
            int expectedReasonableCutUsage)
        {
            if (failedBarrierCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(failedBarrierCount));
            }

            if (acceptedCutCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(acceptedCutCount));
            }

            if (expectedReasonableCutUsage < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedReasonableCutUsage));
            }

            if (!levelCompleted)
            {
                return 0;
            }

            int stars = 1;
            if (failedBarrierCount > 0)
            {
                return stars;
            }

            stars = 2;
            if (expectedReasonableCutUsage > 0
                && acceptedCutCount <= expectedReasonableCutUsage)
            {
                stars = MaximumStars;
            }

            return stars;
        }

        public static int PreserveBest(int storedRating, int runRating)
        {
            ValidateRating(storedRating, nameof(storedRating));
            ValidateRating(runRating, nameof(runRating));
            return Math.Max(storedRating, runRating);
        }

        private static void ValidateRating(int rating, string parameterName)
        {
            if (rating < 0 || rating > MaximumStars)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }

    public readonly struct CoreFunLevelMetrics
    {
        public CoreFunLevelMetrics(
            string levelId,
            int levelNumber,
            float levelStartTimeSeconds,
            float elapsedSeconds,
            int barrierAttempts,
            int failedBarriers,
            int successfulBarriers,
            float largestSingleCapturedFraction,
            float finalCapturedFraction,
            int retryCount,
            bool nextPressed,
            int nearMissCount,
            int perfectCutCount,
            bool anyPowerUpUsed)
        {
            LevelId = levelId;
            LevelNumber = levelNumber;
            LevelStartTimeSeconds = levelStartTimeSeconds;
            ElapsedSeconds = elapsedSeconds;
            BarrierAttempts = barrierAttempts;
            FailedBarriers = failedBarriers;
            SuccessfulBarriers = successfulBarriers;
            LargestSingleCapturedFraction = largestSingleCapturedFraction;
            FinalCapturedFraction = finalCapturedFraction;
            RetryCount = retryCount;
            NextPressed = nextPressed;
            NearMissCount = nearMissCount;
            PerfectCutCount = perfectCutCount;
            AnyPowerUpUsed = anyPowerUpUsed;
        }

        public string LevelId { get; }
        public int LevelNumber { get; }
        public float LevelStartTimeSeconds { get; }
        public float ElapsedSeconds { get; }
        public int BarrierAttempts { get; }
        public int FailedBarriers { get; }
        public int SuccessfulBarriers { get; }
        public float LargestSingleCapturedFraction { get; }
        public float FinalCapturedFraction { get; }
        public int RetryCount { get; }
        public bool NextPressed { get; }

        /// Times a barrier locked close enough to a threat to count as a
        /// near miss this run (see NearMissEvaluator). Reset every retry.
        public int NearMissCount { get; }

        /// Times a single barrier lock captured a "large" fraction of the
        /// room in one cut this run (see LargeCaptureEvaluator). Reset
        /// every retry.
        public int PerfectCutCount { get; }

        /// Whether Freeze Pulse, Instant Barrier, or Gravity Well was
        /// activated at least once this run. Reset every retry.
        public bool AnyPowerUpUsed { get; }
    }

    public sealed class CoreFunMetricsTracker
    {
        private readonly List<CoreFunLevelMetrics> _sequenceRuns =
            new List<CoreFunLevelMetrics>();
        private CoreFunLevelConfiguration _level;
        private float _sequenceClock;
        private float _levelStartTimeSeconds;
        private float _elapsedSeconds;
        private int _barrierAttempts;
        private int _failedBarriers;
        private int _successfulBarriers;
        private float _largestSingleCapturedFraction;
        private float _finalCapturedFraction;
        private int _retryCount;
        private int _nearMissCount;
        private int _perfectCutCount;
        private bool _anyPowerUpUsed;

        public CoreFunLevelMetrics Current => Snapshot(false);

        public IReadOnlyList<CoreFunLevelMetrics> SequenceRuns => _sequenceRuns;

        public IReadOnlyList<CoreFunLevelMetrics> LastCompletedSequence
        {
            get;
            private set;
        } = Array.Empty<CoreFunLevelMetrics>();

        public int SequenceCompletionCount { get; private set; }

        public void StartSequence(CoreFunLevelConfiguration firstLevel)
        {
            _sequenceRuns.Clear();
            _sequenceClock = 0f;
            StartLevel(firstLevel);
        }

        public void StartLevel(CoreFunLevelConfiguration level)
        {
            _level = level;
            ResetRun(0);
        }

        public void AdvanceTime(float elapsedSeconds)
        {
            if (float.IsNaN(elapsedSeconds)
                || float.IsInfinity(elapsedSeconds)
                || elapsedSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            }

            _elapsedSeconds += elapsedSeconds;
            _sequenceClock += elapsedSeconds;
        }

        public void RecordBarrierAttempt()
        {
            _barrierAttempts++;
        }

        public void RecordBarrierFailure(float capturedFraction)
        {
            _failedBarriers++;
            SetFinalCapturedFraction(capturedFraction);
        }

        public void RecordBarrierSuccess(
            float capturedDelta,
            float capturedFraction)
        {
            if (capturedDelta < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(capturedDelta));
            }

            _successfulBarriers++;
            if (capturedDelta > _largestSingleCapturedFraction)
            {
                _largestSingleCapturedFraction = capturedDelta;
            }

            SetFinalCapturedFraction(capturedFraction);
        }

        public void RecordCompletion(float capturedFraction)
        {
            SetFinalCapturedFraction(capturedFraction);
        }

        public void RecordNearMiss()
        {
            _nearMissCount++;
        }

        public void RecordPerfectCut()
        {
            _perfectCutCount++;
        }

        public void RecordPowerUpUsed()
        {
            _anyPowerUpUsed = true;
        }

        public void RetryCurrentLevel()
        {
            ResetRun(_retryCount + 1);
        }

        public void AdvanceTo(CoreFunLevelConfiguration nextLevel)
        {
            _sequenceRuns.Add(Snapshot(true));
            StartLevel(nextLevel);
        }

        public void CompleteSequenceAndRestart(
            CoreFunLevelConfiguration firstLevel)
        {
            _sequenceRuns.Add(Snapshot(false));
            LastCompletedSequence = _sequenceRuns.ToArray();
            SequenceCompletionCount++;
            StartSequence(firstLevel);
        }

        private CoreFunLevelMetrics Snapshot(bool nextPressed) =>
            new CoreFunLevelMetrics(
                _level.StableId,
                _level.DisplayNumber,
                _levelStartTimeSeconds,
                _elapsedSeconds,
                _barrierAttempts,
                _failedBarriers,
                _successfulBarriers,
                _largestSingleCapturedFraction,
                _finalCapturedFraction,
                _retryCount,
                nextPressed,
                _nearMissCount,
                _perfectCutCount,
                _anyPowerUpUsed);

        private void ResetRun(int retryCount)
        {
            _levelStartTimeSeconds = _sequenceClock;
            _elapsedSeconds = 0f;
            _barrierAttempts = 0;
            _failedBarriers = 0;
            _successfulBarriers = 0;
            _largestSingleCapturedFraction = 0f;
            _finalCapturedFraction = 0f;
            _retryCount = retryCount;
            _nearMissCount = 0;
            _perfectCutCount = 0;
            _anyPowerUpUsed = false;
        }

        private void SetFinalCapturedFraction(float capturedFraction)
        {
            if (float.IsNaN(capturedFraction)
                || float.IsInfinity(capturedFraction)
                || capturedFraction < 0f
                || capturedFraction > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(capturedFraction));
            }

            _finalCapturedFraction = capturedFraction;
        }
    }
}
