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
        private readonly PowerConfiguration _powerConfiguration;
        private readonly float[] _pulseElapsedSeconds;
        private readonly List<BarrierApproachSample> _approachSamples =
            new List<BarrierApproachSample>(128);
        private readonly List<FeedbackEvent> _feedbackEvents =
            new List<FeedbackEvent>(8);
        private readonly List<ThreatId> _lastHunterReactionThreatIds =
            new List<ThreatId>(4);
        private int _nextBarrierId;
        private float _barrierElapsed;
        private ComboState _combo;
        private float _comboIdleSeconds;

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
            : this(
                configurations,
                barrierConfiguration,
                captureConfiguration,
                feedbackConfiguration,
                PowerConfiguration.None,
                tolerance)
        {
        }

        public ThreatMotionSession(
            IReadOnlyList<ThreatMotionConfiguration> configurations,
            BarrierConfiguration barrierConfiguration,
            CaptureLevelConfiguration captureConfiguration,
            FeedbackTuningConfiguration feedbackConfiguration,
            PowerConfiguration powerConfiguration,
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
            _pulseElapsedSeconds = new float[_configurations.Length];
            _barrierConfiguration = barrierConfiguration;
            _captureConfiguration = captureConfiguration;
            _feedbackConfiguration = feedbackConfiguration;
            _powerConfiguration = powerConfiguration;
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

        public IReadOnlyList<ThreatId> LastHunterReactionThreatIds =>
            _lastHunterReactionThreatIds;

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

        public int AcceptedCutCount { get; private set; }

        public bool HasCutLimit => _captureConfiguration.HasCutLimit;

        public int MaximumAcceptedCuts =>
            _captureConfiguration.MaximumAcceptedCuts;

        public int CutsRemaining => HasCutLimit
            ? Math.Max(0, MaximumAcceptedCuts - AcceptedCutCount)
            : int.MaxValue;

        public bool HasBurnLimit => _captureConfiguration.HasBurnLimit;

        public int MaximumAcceptedBarrierBreaks =>
            _captureConfiguration.MaximumAcceptedBarrierBreaks;

        public int BarrierBreaksRemaining => HasBurnLimit
            ? Math.Max(0, MaximumAcceptedBarrierBreaks - FailedBarrierCount)
            : int.MaxValue;

        public int TickCount { get; private set; }

        public int FreezePulseChargesRemaining { get; private set; }

        public float FreezePulseRemainingSeconds { get; private set; }

        public int InstantBarrierChargesRemaining { get; private set; }

        public bool InstantBarrierArmed { get; private set; }

        public int GravityWellChargesRemaining { get; private set; }

        public float GravityWellRemainingSeconds { get; private set; }

        public LogicalPoint? GravityWellPosition { get; private set; }

        public bool GravityWellActive =>
            GravityWellPosition.HasValue
            && GravityWellRemainingSeconds > 0f;

        public float GravityWellRadius => _powerConfiguration.GravityWellRadius;

        public bool CanActivateGravityWell =>
            LevelStatus == CaptureLevelStatus.Playing
            && GravityWellChargesRemaining > 0
            && !GravityWellActive;

        public void Tick(float elapsedTime)
        {
            _feedbackEvents.Clear();
            LastBarrierEvent = BarrierSimulationEvent.None;
            LastBarrierContact = BarrierContactKind.None;
            LastBarrierDiagnostic = BarrierSimulationDiagnostic.None;
            LastDiagnostic = ThreatMotionDiagnostic.None;
            if (LevelStatus != CaptureLevelStatus.Playing)
            {
                return;
            }

            AdvanceBehaviorState(elapsedTime);

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
                BarrierState startedBarrier = result.Barrier;
                if (InstantBarrierArmed)
                {
                    startedBarrier = startedBarrier.WithGrowthSpeed(
                        _powerConfiguration.InstantBarrierGrowthSpeed);
                    InstantBarrierArmed = false;
                    InstantBarrierChargesRemaining--;
                    AddFeedback(
                        FeedbackEventKind.PowerInstantBarrierConsumed,
                        startedBarrier.Id,
                        0f,
                        float.PositiveInfinity);
                }

                ApplyHunterReaction(
                    startedBarrier.ParentRoomId,
                    intent.Origin);
                AcceptedCutCount++;
                ActiveBarrier = startedBarrier;
                LastBarrierSnapshot = startedBarrier;
                _barrierElapsed = 0f;
                _approachSamples.Clear();
                RecordApproachSamples(startedBarrier, 0f);
                AddFeedback(
                    FeedbackEventKind.BarrierStarted,
                    startedBarrier.Id,
                    0f,
                    float.PositiveInfinity);
                _nextBarrierId++;
                result = new BarrierStartResult(
                    true,
                    BarrierRejectionReason.None,
                    startedBarrier);
            }

            return result;
        }

        public bool TryActivateFreezePulse()
        {
            _feedbackEvents.Clear();
            if (LevelStatus != CaptureLevelStatus.Playing
                || FreezePulseChargesRemaining <= 0)
            {
                AddFeedback(
                    FeedbackEventKind.PowerUnavailable,
                    default,
                    0f,
                    float.PositiveInfinity);
                return false;
            }

            FreezePulseChargesRemaining--;
            FreezePulseRemainingSeconds =
                _powerConfiguration.FreezePulseDurationSeconds;
            AddFeedback(
                FeedbackEventKind.PowerFreezePulseActivated,
                default,
                0f,
                float.PositiveInfinity);
            return true;
        }

        public bool TryArmInstantBarrier()
        {
            _feedbackEvents.Clear();
            if (LevelStatus != CaptureLevelStatus.Playing
                || InstantBarrierArmed
                || InstantBarrierChargesRemaining <= 0)
            {
                AddFeedback(
                    FeedbackEventKind.PowerUnavailable,
                    default,
                    0f,
                    float.PositiveInfinity);
                return false;
            }

            InstantBarrierArmed = true;
            AddFeedback(
                FeedbackEventKind.PowerInstantBarrierArmed,
                default,
                0f,
                float.PositiveInfinity);
            return true;
        }

        public bool TryActivateGravityWell(LogicalPoint position)
        {
            _feedbackEvents.Clear();
            if (!CanActivateGravityWell
                || !Board.TryGetActiveRoomAt(position, out RoomState wellRoom)
                || !HasThreatInGravityWellRange(position, wellRoom.Id))
            {
                AddFeedback(
                    FeedbackEventKind.PowerUnavailable,
                    default,
                    0f,
                    float.PositiveInfinity);
                return false;
            }

            GravityWellChargesRemaining--;
            GravityWellPosition = position;
            GravityWellRemainingSeconds =
                _powerConfiguration.GravityWellDurationSeconds;
            AddFeedback(
                FeedbackEventKind.PowerGravityWellActivated,
                default,
                0f,
                float.PositiveInfinity);
            return true;
        }

        private bool HasThreatInGravityWellRange(
            LogicalPoint position,
            RoomId roomId)
        {
            float radiusSquared = _powerConfiguration.GravityWellRadius
                * _powerConfiguration.GravityWellRadius;
            for (int index = 0; index < Board.Threats.Count; index++)
            {
                ThreatState threat = Board.Threats[index];
                if (threat.RoomId == roomId
                    && (position - threat.Position).LengthSquared
                        <= radiusSquared)
                {
                    return true;
                }
            }

            return false;
        }

        public BarrierStartResult ValidateBarrierStart(BarrierIntent intent)
        {
            if (LevelStatus != CaptureLevelStatus.Playing)
            {
                return new BarrierStartResult(
                    false,
                    LevelStatus == CaptureLevelStatus.Completed
                        ? BarrierRejectionReason.LevelCompleted
                        : BarrierRejectionReason.CutLimitReached,
                    default);
            }

            if (HasCutLimit && AcceptedCutCount >= MaximumAcceptedCuts)
            {
                return new BarrierStartResult(
                    false,
                    BarrierRejectionReason.CutLimitReached,
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
            AcceptedCutCount = 0;
            _nextBarrierId = 1;
            TickCount = 0;
            _barrierElapsed = 0f;
            _approachSamples.Clear();
            _combo = _combo.Reset();
            _comboIdleSeconds = 0f;
            Array.Clear(
                _pulseElapsedSeconds,
                0,
                _pulseElapsedSeconds.Length);
            FreezePulseChargesRemaining = _powerConfiguration.FreezePulseCharges;
            FreezePulseRemainingSeconds = 0f;
            InstantBarrierChargesRemaining =
                _powerConfiguration.InstantBarrierCharges;
            InstantBarrierArmed = false;
            GravityWellChargesRemaining =
                _powerConfiguration.GravityWellCharges;
            GravityWellRemainingSeconds = 0f;
            GravityWellPosition = null;
            _feedbackEvents.Clear();
            _lastHunterReactionThreatIds.Clear();
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
                ThreatState prepared = PrepareForTick(threat);
                BarrierSimulationResult result =
                    GrowingBarrierMotionSolver.Move(
                        barrierRoom,
                        prepared,
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
                // Combo increments before RegionCaptured is added so the
                // capture chime's attached ComboCount already reflects this
                // capture (used to pitch-rise the chime per combo step).
                _combo = _combo.OnCapturingLock();
                _comboIdleSeconds = 0f;
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
            else
            {
                EvaluateCutLimitExhaustion(representative.Barrier.Id);
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
                ThreatState prepared = PrepareForTick(threat);
                BarrierSimulationResult prefix =
                    GrowingBarrierMotionSolver.Move(
                        barrierRoom,
                        prepared,
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
                _comboIdleSeconds = 0f;
                AddFeedback(
                    FeedbackEventKind.ComboChanged,
                    failure.Barrier.Id,
                    0f,
                    float.PositiveInfinity);
            }

            EvaluateCutLimitExhaustion(failure.Barrier.Id);
            EvaluateBurnLimitExhaustion(failure.Barrier.Id);
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
                ThreatState prepared = PrepareForTick(threat);
                BarrierSimulationResult atLock =
                    GrowingBarrierMotionSolver.Move(
                        room,
                        prepared,
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
            ThreatState prepared = PrepareForTick(threat);
            ThreatMotionResult result = ThreatMotionSolver.Move(
                room,
                prepared,
                elapsedTime,
                configuration.MaximumImpactsPerTick,
                _tolerance);
            Board.UpdateThreat(result.Threat);
            IncludeThreatDiagnostic(result.Diagnostic);
        }

        private void AdvanceBehaviorState(float elapsedTime)
        {
            if (_combo.Count > 0 && !ActiveBarrier.HasValue)
            {
                _comboIdleSeconds += elapsedTime;
                if (_comboIdleSeconds
                    >= _feedbackConfiguration.ComboTimeoutSeconds)
                {
                    _combo = _combo.OnTimeout();
                    _comboIdleSeconds = 0f;
                    AddFeedback(
                        FeedbackEventKind.ComboChanged,
                        default,
                        0f,
                        float.PositiveInfinity);
                }
            }

            if (GravityWellActive)
            {
                float influenceSeconds = Math.Min(
                    elapsedTime,
                    GravityWellRemainingSeconds);
                ApplyGravityWellSteering(influenceSeconds);
                GravityWellRemainingSeconds = Math.Max(
                    0f,
                    GravityWellRemainingSeconds - elapsedTime);
                if (GravityWellRemainingSeconds <= 0f)
                {
                    GravityWellPosition = null;
                }
            }

            if (FreezePulseRemainingSeconds > 0f)
            {
                FreezePulseRemainingSeconds = Math.Max(
                    0f,
                    FreezePulseRemainingSeconds - elapsedTime);
            }

            for (int index = 0; index < _configurations.Length; index++)
            {
                if (_configurations[index].Behavior.Kind
                    == ThreatBehaviorKind.Pulse)
                {
                    _pulseElapsedSeconds[index] += elapsedTime;
                }
            }
        }

        private void ApplyGravityWellSteering(float elapsedTime)
        {
            if (!GravityWellPosition.HasValue
                || elapsedTime <= 0f
                || !Board.TryGetActiveRoomAt(
                    GravityWellPosition.Value,
                    out RoomState wellRoom))
            {
                GravityWellRemainingSeconds = 0f;
                GravityWellPosition = null;
                return;
            }

            float radiusSquared = _powerConfiguration.GravityWellRadius
                * _powerConfiguration.GravityWellRadius;
            float maximumTurnDegrees =
                _powerConfiguration.GravityWellTurnDegreesPerSecond
                * elapsedTime;
            for (int index = 0; index < Board.Threats.Count; index++)
            {
                ThreatState threat = Board.Threats[index];
                if (threat.RoomId != wellRoom.Id)
                {
                    continue;
                }

                LogicalVector toWell =
                    GravityWellPosition.Value - threat.Position;
                if (toWell.LengthSquared <= 0f
                    || toWell.LengthSquared > radiusSquared)
                {
                    continue;
                }

                LogicalVector newDirection =
                    CalculateGravityWellDirection(
                        threat.Velocity,
                        toWell,
                        maximumTurnDegrees);
                Board.UpdateThreat(threat.WithMotion(
                    threat.Position,
                    newDirection * threat.Speed));
            }
        }

        public static LogicalVector CalculateGravityWellDirection(
            LogicalVector currentDirection,
            LogicalVector targetDirection,
            float maximumTurnDegrees)
        {
            if (currentDirection.LengthSquared <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentDirection));
            }

            if (targetDirection.LengthSquared <= 0f)
            {
                return currentDirection / currentDirection.Length;
            }

            if (float.IsNaN(maximumTurnDegrees)
                || float.IsInfinity(maximumTurnDegrees)
                || maximumTurnDegrees < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumTurnDegrees));
            }

            LogicalVector current =
                currentDirection / currentDirection.Length;
            LogicalVector target = targetDirection / targetDirection.Length;
            float dot = Math.Max(-1f, Math.Min(
                1f,
                LogicalVector.Dot(current, target)));
            float angle = (float)Math.Acos(dot);
            if (angle <= 0f || maximumTurnDegrees <= 0f)
            {
                return current;
            }

            float maximumTurnRadians = maximumTurnDegrees
                * ((float)Math.PI / 180f);
            float turn = Math.Min(angle, maximumTurnRadians);
            float cross = current.X * target.Y - current.Y * target.X;
            float signedTurn = cross >= 0f ? turn : -turn;
            float cosine = (float)Math.Cos(signedTurn);
            float sine = (float)Math.Sin(signedTurn);
            return new LogicalVector(
                current.X * cosine - current.Y * sine,
                current.X * sine + current.Y * cosine);
        }

        private ThreatState PrepareForTick(ThreatState threat)
        {
            int index = IndexFor(threat.Id);
            ThreatMotionConfiguration configuration = _configurations[index];
            float multiplier = 1f;
            if (configuration.Behavior.Kind == ThreatBehaviorKind.Pulse)
            {
                multiplier *= PulsePhaseMultiplier(
                    configuration.Behavior,
                    _pulseElapsedSeconds[index]);
            }

            if (FreezePulseRemainingSeconds > 0f)
            {
                multiplier *= _powerConfiguration.FreezePulseSpeedMultiplier;
            }

            // Always re-derive magnitude from the authored base speed rather
            // than reusing the threat's currently stored magnitude: once a
            // temporary modifier (Pulse phase, Freeze Pulse) has scaled a
            // threat's speed, a naive "unchanged when multiplier is 1"
            // shortcut would leave it permanently at that scaled magnitude
            // after the modifier ends, since nothing else ever restores it.
            LogicalVector direction = threat.Velocity / threat.Velocity.Length;
            float targetSpeed = configuration.Speed * multiplier;
            return threat.WithMotion(threat.Position, direction * targetSpeed);
        }

        private static float PulsePhaseMultiplier(
            ThreatBehaviorConfiguration behavior,
            float elapsedSeconds)
        {
            float phase = elapsedSeconds % behavior.PulseCycleSeconds;
            return phase < behavior.PulseSlowDurationSeconds
                ? behavior.PulseSlowSpeedMultiplier
                : behavior.PulseFastSpeedMultiplier;
        }

        public ThreatBehaviorConfiguration BehaviorFor(ThreatId id) =>
            ConfigurationFor(id).Behavior;

        public float PulsePhaseMultiplierFor(ThreatId id)
        {
            int index = IndexFor(id);
            ThreatBehaviorConfiguration behavior =
                _configurations[index].Behavior;
            return behavior.Kind == ThreatBehaviorKind.Pulse
                ? PulsePhaseMultiplier(behavior, _pulseElapsedSeconds[index])
                : 1f;
        }

        public static LogicalVector CalculateHunterReactionDirection(
            LogicalVector currentDirection,
            LogicalVector targetDirection,
            ThreatBehaviorConfiguration behavior)
        {
            if (behavior.Kind != ThreatBehaviorKind.Hunter)
            {
                return currentDirection / currentDirection.Length;
            }

            LogicalVector current =
                currentDirection / currentDirection.Length;
            LogicalVector target = targetDirection / targetDirection.Length;
            float dot = Math.Max(-1f, Math.Min(
                1f,
                current.X * target.X + current.Y * target.Y));
            float angle = (float)Math.Acos(dot);
            if (angle <= 0f)
            {
                return current;
            }

            float turnFraction = Math.Min(
                behavior.HunterSteerFactor,
                0.9f);
            float maximumTurnRadians =
                behavior.HunterMaximumTurnDegrees
                * ((float)Math.PI / 180f);
            float turn = Math.Min(angle * turnFraction, maximumTurnRadians);
            float cross = current.X * target.Y - current.Y * target.X;
            float signedTurn = cross >= 0f ? turn : -turn;
            float cosine = (float)Math.Cos(signedTurn);
            float sine = (float)Math.Sin(signedTurn);
            return new LogicalVector(
                current.X * cosine - current.Y * sine,
                current.X * sine + current.Y * cosine);
        }

        private void ApplyHunterReaction(
            RoomId roomId,
            LogicalPoint barrierOrigin)
        {
            _lastHunterReactionThreatIds.Clear();
            bool reacted = false;
            for (int index = 0; index < Board.Threats.Count; index++)
            {
                ThreatState threat = Board.Threats[index];
                if (threat.RoomId != roomId)
                {
                    continue;
                }

                ThreatBehaviorConfiguration behavior =
                    _configurations[IndexFor(threat.Id)].Behavior;
                if (behavior.Kind != ThreatBehaviorKind.Hunter)
                {
                    continue;
                }

                LogicalVector toOrigin = barrierOrigin - threat.Position;
                if (toOrigin.LengthSquared <= 0f)
                {
                    continue;
                }

                LogicalVector targetDirection = toOrigin / toOrigin.Length;
                LogicalVector currentDirection =
                    threat.Velocity / threat.Velocity.Length;
                // A Hunter reacts once, but always keeps some angular error
                // and can never turn farther than its authored fairness cap.
                LogicalVector newDirection =
                    CalculateHunterReactionDirection(
                        currentDirection,
                        targetDirection,
                        behavior);
                if (newDirection == currentDirection)
                {
                    continue;
                }
                float baseSpeed = threat.Speed;
                Board.UpdateThreat(threat.WithMotion(
                    threat.Position,
                    newDirection * baseSpeed));
                _lastHunterReactionThreatIds.Add(threat.Id);
                reacted = true;
            }

            if (reacted)
            {
                AddFeedback(
                    FeedbackEventKind.HunterReacted,
                    default,
                    0f,
                    float.PositiveInfinity);
            }
        }

        private void EvaluateCutLimitExhaustion(BarrierId barrierId)
        {
            if (!HasCutLimit
                || AcceptedCutCount < MaximumAcceptedCuts
                || LevelStatus != CaptureLevelStatus.Playing
                || ActiveBarrier.HasValue)
            {
                return;
            }

            LevelStatus = CaptureLevelStatus.OutOfCuts;
            AddFeedback(
                FeedbackEventKind.CutLimitExhausted,
                barrierId,
                0f,
                float.PositiveInfinity);
        }

        private void EvaluateBurnLimitExhaustion(BarrierId barrierId)
        {
            if (!HasBurnLimit
                || FailedBarrierCount < MaximumAcceptedBarrierBreaks
                || LevelStatus != CaptureLevelStatus.Playing
                || ActiveBarrier.HasValue)
            {
                return;
            }

            LevelStatus = CaptureLevelStatus.OutOfLives;
            AddFeedback(
                FeedbackEventKind.BurnLimitExhausted,
                barrierId,
                0f,
                float.PositiveInfinity);
        }

        private ThreatMotionConfiguration ConfigurationFor(ThreatId id) =>
            _configurations[IndexFor(id)];

        private int IndexFor(ThreatId id)
        {
            int index = id.Value - 1;
            if (index < 0 || index >= _configurations.Length)
            {
                throw new InvalidOperationException(
                    $"No motion configuration exists for {id}.");
            }

            return index;
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
