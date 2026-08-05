using System;
using System.Collections.Generic;

namespace Cutrium.Gameplay.Session
{
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
            bool nextPressed)
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
                nextPressed);

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
