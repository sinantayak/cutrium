using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Feedback;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Threats;

namespace Cutrium.Gameplay.Session
{
    public sealed class ThreatMotionSession
    {
        private readonly ThreatMotionConfiguration[] _configurations;
        private readonly BarrierSimulationResult[] _barrierResults;
        private readonly GeometryTolerancePolicy _tolerance;
        private readonly BarrierConfiguration _barrierConfiguration;
        private readonly CaptureLevelConfiguration _captureConfiguration;
        private readonly FeedbackTuningConfiguration _feedbackConfiguration;
        private readonly List<BarrierApproachSample> _approachSamples =
            new List<BarrierApproachSample>(128);
        private readonly List<FeedbackEvent> _feedbackEvents =
            new List<FeedbackEvent>(8);
        private int _nextBarrierId;
        private float _barrierElapsed;
        private ComboState _combo;

        public ThreatMotionSession(
            ThreatMotionConfiguration configuration,
            GeometryTolerancePolicy tolerance)
            : this(
                new[] { configuration },
                new BarrierConfiguration(8f, 0.08f, 0.6f, 16),
                new CaptureLevelConfiguration(0.75f),
                tolerance)
        {
        }

        public ThreatMotionSession(
            ThreatMotionConfiguration configuration,
            BarrierConfiguration barrierConfiguration,
            GeometryTolerancePolicy tolerance)
            : this(
                new[] { configuration },
                barrierConfiguration,
                new CaptureLevelConfiguration(0.75f),
                tolerance)
        {
        }

        public ThreatMotionSession(
            ThreatMotionConfiguration configuration,
            BarrierConfiguration barrierConfiguration,
            CaptureLevelConfiguration captureConfiguration,
            GeometryTolerancePolicy tolerance)
            : this(
                new[] { configuration },
                barrierConfiguration,
                captureConfiguration,
                tolerance)
        {
        }

        public ThreatMotionSession(
            IReadOnlyList<ThreatMotionConfiguration> configurations,
            GeometryTolerancePolicy tolerance)
            : this(
                configurations,
                new BarrierConfiguration(8f, 0.08f, 0.6f, 16),
                new CaptureLevelConfiguration(0.75f),
                tolerance)
        {
        }

        public ThreatMotionSession(
            IReadOnlyList<ThreatMotionConfiguration> configurations,
            BarrierConfiguration barrierConfiguration,
            CaptureLevelConfiguration captureConfiguration,
            GeometryTolerancePolicy tolerance)
            : this(
                configurations,
                barrierConfiguration,
                captureConfiguration,
                FeedbackTuningConfiguration.Default,
                tolerance)
        {
        }

        public ThreatMotionSession(
            IReadOnlyList<ThreatMotionConfiguration> configurations,
            BarrierConfiguration barrierConfiguration,
            CaptureLevelConfiguration captureConfiguration,
            FeedbackTuningConfiguration feedbackConfiguration,
            GeometryTolerancePolicy tolerance)
        {
            if (configurations == null || configurations.Count == 0)
            {
                throw new ArgumentException(
                    "A motion session needs at least one normal threat.",
                    nameof(configurations));
            }

            _configurations =
                new ThreatMotionConfiguration[configurations.Count];
            LogicalRect boardBounds = configurations[0].BoardBounds;
            for (int index = 0; index < configurations.Count; index++)
            {
                ThreatMotionConfiguration configuration = configurations[index];
                if (configuration.BoardBounds != boardBounds)
                {
                    throw new ArgumentException(
                        "All threats in a session must use the same board.",
                        nameof(configurations));
                }

                _configurations[index] = configuration;
            }

            _barrierResults =
                new BarrierSimulationResult[_configurations.Length];
            _barrierConfiguration = barrierConfiguration;
            _captureConfiguration = captureConfiguration;
            _feedbackConfiguration = feedbackConfiguration;
            _tolerance = tolerance;
            InitialRoom = new RoomState(new RoomId(1), boardBounds);
            Reset();
        }

        public RoomState InitialRoom { get; }

        public ThreatState Threat => Board.Threats[0];

        public IReadOnlyList<ThreatState> Threats => Board.Threats;

        public CaptureBoardState Board { get; private set; }

        public CaptureLevelStatus LevelStatus { get; private set; }

        public float TargetCapturedFraction =>
            _captureConfiguration.TargetCapturedFraction;

        public float CapturedFraction => Board.CapturedFraction;

        public int ComboCount => _combo.Count;

        public IReadOnlyList<FeedbackEvent> FeedbackEvents => _feedbackEvents;

        public RoomSplitApplyResult LastRoomSplitResult { get; private set; }

        public ThreatMotionDiagnostic LastDiagnostic { get; private set; }

        public BarrierState? ActiveBarrier { get; private set; }

        public BarrierState? LastBarrierSnapshot { get; private set; }

        public BarrierSimulationEvent LastBarrierEvent { get; private set; }

        public BarrierContactKind LastBarrierContact { get; private set; }

        public BarrierSimulationDiagnostic LastBarrierDiagnostic
        {
            get;
            private set;
        }

        public int FailedBarrierCount { get; private set; }

        public int LockedBarrierCount { get; private set; }

        public int TickCount { get; private set; }

        public void Tick(float elapsedTime)
        {
            _feedbackEvents.Clear();
            LastBarrierEvent = BarrierSimulationEvent.None;
            LastBarrierContact = BarrierContactKind.None;
            LastBarrierDiagnostic = BarrierSimulationDiagnostic.None;
            LastDiagnostic = ThreatMotionDiagnostic.None;
            if (LevelStatus == CaptureLevelStatus.Completed)
            {
                return;
            }

            if (ActiveBarrier.HasValue)
            {
                TickActiveBarrier(elapsedTime);
            }
            else
            {
                MoveAllThreats(elapsedTime);
            }

            TickCount++;
        }

        public BarrierStartResult TryStartBarrier(BarrierIntent intent)
        {
            _feedbackEvents.Clear();
            BarrierStartResult result = ValidateBarrierStart(intent);
            if (result.Accepted)
            {
                ActiveBarrier = result.Barrier;
                LastBarrierSnapshot = result.Barrier;
                _barrierElapsed = 0f;
                _approachSamples.Clear();
                RecordApproachSamples(result.Barrier, 0f);
                AddFeedback(
                    FeedbackEventKind.BarrierStarted,
                    result.Barrier.Id,
                    0f,
                    float.PositiveInfinity);
                _nextBarrierId++;
            }

            return result;
        }

        public BarrierStartResult ValidateBarrierStart(BarrierIntent intent)
        {
            if (LevelStatus == CaptureLevelStatus.Completed)
            {
                return new BarrierStartResult(
                    false,
                    BarrierRejectionReason.LevelCompleted,
                    default);
            }

            if (ActiveBarrier.HasValue)
            {
                return new BarrierStartResult(
                    false,
                    BarrierRejectionReason.BarrierAlreadyActive,
                    default);
            }

            if (!Board.TryGetActiveRoomAt(intent.Origin, out RoomState room))
            {
                return new BarrierStartResult(
                    false,
                    BarrierRejectionReason.OriginOutsideActiveRoom,
                    default);
            }

            return BarrierFactory.TryCreate(
                new BarrierId(_nextBarrierId),
                room,
                intent,
                _barrierConfiguration,
                _tolerance);
        }

        public void Reset()
        {
            var initialThreats = new ThreatState[_configurations.Length];
            for (int index = 0; index < _configurations.Length; index++)
            {
                ThreatMotionConfiguration configuration =
                    _configurations[index];
                var threat = new ThreatState(
                    new ThreatId(index + 1),
                    InitialRoom.Id,
                    configuration.InitialPosition,
                    configuration.InitialDirection * configuration.Speed,
                    configuration.Radius);
                ThreatMotionSolver.Move(
                    InitialRoom,
                    threat,
                    0f,
                    configuration.MaximumImpactsPerTick,
                    _tolerance);
                initialThreats[index] = threat;
            }

            Board = new CaptureBoardState(
                InitialRoom.Bounds,
                initialThreats,
                _tolerance);
            LevelStatus = CaptureLevelStatus.Playing;
            LastRoomSplitResult = default;
            LastDiagnostic = ThreatMotionDiagnostic.None;
            ActiveBarrier = null;
            LastBarrierSnapshot = null;
            LastBarrierEvent = BarrierSimulationEvent.None;
            LastBarrierContact = BarrierContactKind.None;
            LastBarrierDiagnostic = BarrierSimulationDiagnostic.None;
            FailedBarrierCount = 0;
            LockedBarrierCount = 0;
            _nextBarrierId = 1;
            TickCount = 0;
            _barrierElapsed = 0f;
            _approachSamples.Clear();
            _combo = _combo.Reset();
            _feedbackEvents.Clear();
            AddFeedback(
                FeedbackEventKind.SessionReset,
                default,
                0f,
                float.PositiveInfinity);
        }

        private void TickActiveBarrier(float elapsedTime)
        {
            BarrierState initialBarrier = ActiveBarrier.Value;
            if (!Board.TryGetActiveRoom(
                    initialBarrier.ParentRoomId,
                    out RoomState barrierRoom))
            {
                throw new InvalidOperationException(
                    "The active barrier parent room is no longer active.");
            }

            int firstParentThreatIndex = -1;
            int lockedResultIndex = -1;
            int earliestFailureIndex = -1;
            float earliestFailureTime = float.PositiveInfinity;
            for (int index = 0; index < Board.Threats.Count; index++)
            {
                ThreatState threat = Board.Threats[index];
                if (threat.RoomId != barrierRoom.Id)
                {
                    continue;
                }

                if (firstParentThreatIndex < 0)
                {
                    firstParentThreatIndex = index;
                }

                ThreatMotionConfiguration configuration =
                    ConfigurationFor(threat.Id);
                BarrierSimulationResult result =
                    GrowingBarrierMotionSolver.Move(
                        barrierRoom,
                        threat,
                        initialBarrier,
                        elapsedTime,
                        _barrierConfiguration.MaximumSolverIterations,
                        configuration.MaximumImpactsPerTick,
                        _tolerance);
                _barrierResults[index] = result;
                if (result.SimulationEvent == BarrierSimulationEvent.Locked)
                {
                    lockedResultIndex = index;
                }

                if (result.SimulationEvent != BarrierSimulationEvent.Failed)
                {
                    continue;
                }

                if (earliestFailureIndex < 0
                    || result.ElapsedUntilEvent < earliestFailureTime
                    && !_tolerance.IsTimeApproximatelyEqual(
                        result.ElapsedUntilEvent,
                        earliestFailureTime))
                {
                    earliestFailureIndex = index;
                    earliestFailureTime = result.ElapsedUntilEvent;
                }
            }

            if (firstParentThreatIndex < 0)
            {
                throw new InvalidOperationException(
                    "An active room must retain at least one threat.");
            }

            if (earliestFailureIndex >= 0)
            {
                ApplyEarliestBarrierFailure(
                    barrierRoom,
                    initialBarrier,
                    elapsedTime,
                    earliestFailureIndex,
                    earliestFailureTime);
                return;
            }

            int representativeIndex = lockedResultIndex >= 0
                ? lockedResultIndex
                : firstParentThreatIndex;
            BarrierSimulationResult representative =
                _barrierResults[representativeIndex];
            if (representative.SimulationEvent == BarrierSimulationEvent.Locked)
            {
                RecordApproachSamplesAtLock(
                    barrierRoom,
                    initialBarrier,
                    representative.ElapsedUntilEvent,
                    _barrierElapsed + representative.ElapsedUntilEvent);
            }
            else
            {
                RecordResultApproachSamples(
                    barrierRoom.Id,
                    _barrierElapsed + elapsedTime);
            }

            for (int index = 0; index < Board.Threats.Count; index++)
            {
                ThreatState threat = Board.Threats[index];
                if (threat.RoomId == barrierRoom.Id)
                {
                    BarrierSimulationResult result = _barrierResults[index];
                    Board.UpdateThreat(result.Threat);
                    IncludeBarrierDiagnostic(result.Diagnostic);
                }
                else
                {
                    MoveThreat(threat, elapsedTime);
                }
            }

            ActiveBarrier = representative.Barrier;
            LastBarrierSnapshot = representative.Barrier;
            LastBarrierEvent = representative.SimulationEvent;
            if (representative.SimulationEvent != BarrierSimulationEvent.Locked)
            {
                _barrierElapsed += elapsedTime;
                AddFeedback(
                    FeedbackEventKind.BarrierGrowing,
                    representative.Barrier.Id,
                    0f,
                    float.PositiveInfinity);
                return;
            }

            float capturedAreaBefore = Board.CapturedArea;
            LockedBarrierCount++;
            LastRoomSplitResult =
                Board.TryApplyLockedBarrier(representative.Barrier);
            if (!LastRoomSplitResult.Applied)
            {
                throw new InvalidOperationException(
                    "A locked barrier could not split its active room: "
                    + LastRoomSplitResult.Diagnostic + ".");
            }

            ActiveBarrier = null;
            float capturedAreaDelta = Board.CapturedArea - capturedAreaBefore;
            float capturedFractionDelta =
                capturedAreaDelta / InitialRoom.Bounds.Area;
            AddFeedback(
                FeedbackEventKind.BarrierLocked,
                representative.Barrier.Id,
                capturedFractionDelta,
                float.PositiveInfinity);
            if (capturedAreaDelta > _tolerance.AreaTolerance)
            {
                AddFeedback(
                    FeedbackEventKind.RegionCaptured,
                    representative.Barrier.Id,
                    capturedFractionDelta,
                    float.PositiveInfinity);
                if (LargeCaptureEvaluator.IsLargeCapture(
                        capturedAreaDelta,
                        InitialRoom.Bounds.Area,
                        _feedbackConfiguration,
                        _tolerance))
                {
                    AddFeedback(
                        FeedbackEventKind.LargeCapture,
                        representative.Barrier.Id,
                        capturedFractionDelta,
                        float.PositiveInfinity);
                }

                _combo = _combo.OnCapturingLock();
                AddFeedback(
                    FeedbackEventKind.ComboChanged,
                    representative.Barrier.Id,
                    capturedFractionDelta,
                    float.PositiveInfinity);
            }
            else
            {
                _combo = _combo.OnNoAreaLock();
            }

            float lockTime =
                _barrierElapsed + representative.ElapsedUntilEvent;
            NearMissEvaluation nearMiss = NearMissEvaluator.Evaluate(
                _approachSamples,
                lockTime,
                false,
                _feedbackConfiguration,
                _tolerance);
            if (nearMiss.IsNearMiss)
            {
                AddFeedback(
                    FeedbackEventKind.NearMiss,
                    representative.Barrier.Id,
                    capturedFractionDelta,
                    nearMiss.ClosestClearance);
            }

            _approachSamples.Clear();
            _barrierElapsed = 0f;
            float capturedTargetArea =
                TargetCapturedFraction * InitialRoom.Bounds.Area;
            if (Board.CapturedArea > capturedTargetArea
                || _tolerance.IsAreaApproximatelyEqual(
                    Board.CapturedArea,
                    capturedTargetArea))
            {
                LevelStatus = CaptureLevelStatus.Completed;
                AddFeedback(
                    FeedbackEventKind.LevelCompleted,
                    representative.Barrier.Id,
                    capturedFractionDelta,
                    nearMiss.ClosestClearance);
            }
        }

        private void ApplyEarliestBarrierFailure(
            RoomState barrierRoom,
            BarrierState initialBarrier,
            float elapsedTime,
            int failureIndex,
            float failureTime)
        {
            BarrierSimulationResult failure = _barrierResults[failureIndex];
            float remaining = Math.Max(0f, elapsedTime - failureTime);
            for (int index = 0; index < Board.Threats.Count; index++)
            {
                ThreatState threat = Board.Threats[index];
                if (threat.RoomId != barrierRoom.Id)
                {
                    MoveThreat(threat, elapsedTime);
                    continue;
                }

                ThreatMotionConfiguration configuration =
                    ConfigurationFor(threat.Id);
                BarrierSimulationResult prefix =
                    GrowingBarrierMotionSolver.Move(
                        barrierRoom,
                        threat,
                        initialBarrier,
                        failureTime,
                        _barrierConfiguration.MaximumSolverIterations,
                        configuration.MaximumImpactsPerTick,
                        _tolerance);
                ThreatMotionResult continuation = ThreatMotionSolver.Move(
                    barrierRoom,
                    prefix.Threat,
                    remaining,
                    configuration.MaximumImpactsPerTick,
                    _tolerance);
                Board.UpdateThreat(continuation.Threat);
                IncludeBarrierDiagnostic(prefix.Diagnostic);
                IncludeThreatDiagnostic(continuation.Diagnostic);
            }

            LastBarrierEvent = BarrierSimulationEvent.Failed;
            LastBarrierContact = failure.ContactKind;
            IncludeBarrierDiagnostic(failure.Diagnostic);
            LastBarrierSnapshot = failure.Barrier;
            FailedBarrierCount++;
            ActiveBarrier = null;
            _approachSamples.Clear();
            _barrierElapsed = 0f;
            AddFeedback(
                FeedbackEventKind.BarrierBroken,
                failure.Barrier.Id,
                0f,
                float.PositiveInfinity);
            if (_combo.Count > 0)
            {
                _combo = _combo.OnBarrierFailure();
                AddFeedback(
                    FeedbackEventKind.ComboChanged,
                    failure.Barrier.Id,
                    0f,
                    float.PositiveInfinity);
            }
        }

        private void RecordApproachSamples(
            BarrierState barrier,
            float elapsedSeconds)
        {
            for (int index = 0; index < Board.Threats.Count; index++)
            {
                ThreatState threat = Board.Threats[index];
                if (threat.RoomId != barrier.ParentRoomId)
                {
                    continue;
                }

                _approachSamples.Add(new BarrierApproachSample(
                    elapsedSeconds,
                    BarrierApproachCalculator.CalculateClearance(
                        barrier,
                        threat.Position,
                        threat.Radius)));
            }

            PruneApproachSamples(elapsedSeconds);
        }

        private void RecordResultApproachSamples(
            RoomId parentRoomId,
            float elapsedSeconds)
        {
            for (int index = 0; index < Board.Threats.Count; index++)
            {
                ThreatState threat = Board.Threats[index];
                if (threat.RoomId != parentRoomId)
                {
                    continue;
                }

                BarrierSimulationResult result = _barrierResults[index];
                _approachSamples.Add(new BarrierApproachSample(
                    elapsedSeconds,
                    BarrierApproachCalculator.CalculateClearance(
                        result.Barrier,
                        result.Threat.Position,
                        result.Threat.Radius)));
            }

            PruneApproachSamples(elapsedSeconds);
        }

        private void RecordApproachSamplesAtLock(
            RoomState room,
            BarrierState initialBarrier,
            float elapsedUntilLock,
            float lockTime)
        {
            for (int index = 0; index < Board.Threats.Count; index++)
            {
                ThreatState threat = Board.Threats[index];
                if (threat.RoomId != room.Id)
                {
                    continue;
                }

                ThreatMotionConfiguration configuration =
                    ConfigurationFor(threat.Id);
                BarrierSimulationResult atLock =
                    GrowingBarrierMotionSolver.Move(
                        room,
                        threat,
                        initialBarrier,
                        elapsedUntilLock,
                        _barrierConfiguration.MaximumSolverIterations,
                        configuration.MaximumImpactsPerTick,
                        _tolerance);
                _approachSamples.Add(new BarrierApproachSample(
                    lockTime,
                    BarrierApproachCalculator.CalculateClearance(
                        atLock.Barrier,
                        atLock.Threat.Position,
                        atLock.Threat.Radius)));
            }

            PruneApproachSamples(lockTime);
        }

        private void PruneApproachSamples(float elapsedSeconds)
        {
            float oldest = Math.Max(
                0f,
                elapsedSeconds - _feedbackConfiguration.NearMissWindowSeconds
                    - _tolerance.TimeTolerance);
            int removeCount = 0;
            while (removeCount < _approachSamples.Count
                && _approachSamples[removeCount].ElapsedSeconds < oldest)
            {
                removeCount++;
            }

            if (removeCount > 0)
            {
                _approachSamples.RemoveRange(0, removeCount);
            }
        }

        private void AddFeedback(
            FeedbackEventKind kind,
            BarrierId barrierId,
            float capturedFractionDelta,
            float closestClearance)
        {
            _feedbackEvents.Add(new FeedbackEvent(
                kind,
                barrierId,
                capturedFractionDelta,
                Board?.CapturedFraction ?? 0f,
                closestClearance,
                _combo.Count));
        }

        private void MoveAllThreats(float elapsedTime)
        {
            for (int index = 0; index < Board.Threats.Count; index++)
            {
                MoveThreat(Board.Threats[index], elapsedTime);
            }
        }

        private void MoveThreat(ThreatState threat, float elapsedTime)
        {
            if (!Board.TryGetActiveRoom(threat.RoomId, out RoomState room))
            {
                throw new InvalidOperationException(
                    "A live threat has no active room.");
            }

            ThreatMotionConfiguration configuration =
                ConfigurationFor(threat.Id);
            ThreatMotionResult result = ThreatMotionSolver.Move(
                room,
                threat,
                elapsedTime,
                configuration.MaximumImpactsPerTick,
                _tolerance);
            Board.UpdateThreat(result.Threat);
            IncludeThreatDiagnostic(result.Diagnostic);
        }

        private ThreatMotionConfiguration ConfigurationFor(ThreatId id)
        {
            int index = id.Value - 1;
            if (index < 0 || index >= _configurations.Length)
            {
                throw new InvalidOperationException(
                    $"No motion configuration exists for {id}.");
            }

            return _configurations[index];
        }

        private void IncludeBarrierDiagnostic(
            BarrierSimulationDiagnostic diagnostic)
        {
            if (diagnostic != BarrierSimulationDiagnostic.None)
            {
                LastBarrierDiagnostic = diagnostic;
            }

            if (diagnostic
                == BarrierSimulationDiagnostic.ThreatImpactLimitReached)
            {
                LastDiagnostic = ThreatMotionDiagnostic.ImpactLimitReached;
            }
        }

        private void IncludeThreatDiagnostic(ThreatMotionDiagnostic diagnostic)
        {
            if (diagnostic != ThreatMotionDiagnostic.None)
            {
                LastDiagnostic = diagnostic;
            }
        }
    }
}
