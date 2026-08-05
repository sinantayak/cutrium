using System;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Threats;

namespace Cutrium.Gameplay.Session
{
    public sealed class ThreatMotionSession
    {
        private readonly ThreatMotionConfiguration _configuration;
        private readonly GeometryTolerancePolicy _tolerance;
        private readonly BarrierConfiguration _barrierConfiguration;
        private readonly CaptureLevelConfiguration _captureConfiguration;
        private int _nextBarrierId;

        public ThreatMotionSession(
            ThreatMotionConfiguration configuration,
            GeometryTolerancePolicy tolerance)
            : this(
                configuration,
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
                configuration,
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
        {
            _configuration = configuration;
            _barrierConfiguration = barrierConfiguration;
            _captureConfiguration = captureConfiguration;
            _tolerance = tolerance;
            InitialRoom = new RoomState(new RoomId(1), configuration.BoardBounds);
            Reset();
        }

        public RoomState InitialRoom { get; }

        public ThreatState Threat { get; private set; }

        public CaptureBoardState Board { get; private set; }

        public CaptureLevelStatus LevelStatus { get; private set; }

        public float TargetCapturedFraction =>
            _captureConfiguration.TargetCapturedFraction;

        public float CapturedFraction => Board.CapturedFraction;

        public RoomSplitApplyResult LastRoomSplitResult { get; private set; }

        public ThreatMotionDiagnostic LastDiagnostic { get; private set; }

        public BarrierState? ActiveBarrier { get; private set; }

        public BarrierState? LastBarrierSnapshot { get; private set; }

        public BarrierSimulationEvent LastBarrierEvent { get; private set; }

        public BarrierContactKind LastBarrierContact { get; private set; }

        public BarrierSimulationDiagnostic LastBarrierDiagnostic { get; private set; }

        public int FailedBarrierCount { get; private set; }

        public int LockedBarrierCount { get; private set; }

        public int TickCount { get; private set; }

        public void Tick(float elapsedTime)
        {
            LastBarrierEvent = BarrierSimulationEvent.None;
            LastBarrierContact = BarrierContactKind.None;
            LastBarrierDiagnostic = BarrierSimulationDiagnostic.None;
            if (LevelStatus == CaptureLevelStatus.Completed)
            {
                return;
            }

            if (ActiveBarrier.HasValue)
            {
                if (!Board.TryGetActiveRoom(
                        ActiveBarrier.Value.ParentRoomId,
                        out RoomState barrierRoom))
                {
                    throw new InvalidOperationException(
                        "The active barrier parent room is no longer active.");
                }

                BarrierSimulationResult barrierResult =
                    GrowingBarrierMotionSolver.Move(
                        barrierRoom,
                        Threat,
                        ActiveBarrier.Value,
                        elapsedTime,
                        _barrierConfiguration.MaximumSolverIterations,
                        _configuration.MaximumImpactsPerTick,
                        _tolerance);
                Threat = barrierResult.Threat;
                LastBarrierEvent = barrierResult.SimulationEvent;
                LastBarrierContact = barrierResult.ContactKind;
                LastBarrierDiagnostic = barrierResult.Diagnostic;
                LastDiagnostic = barrierResult.Diagnostic
                    == BarrierSimulationDiagnostic.ThreatImpactLimitReached
                        ? ThreatMotionDiagnostic.ImpactLimitReached
                        : ThreatMotionDiagnostic.None;
                LastBarrierSnapshot = barrierResult.Barrier;
                Board.UpdateThreat(Threat);
                if (barrierResult.SimulationEvent
                    == BarrierSimulationEvent.Failed)
                {
                    FailedBarrierCount++;
                    ActiveBarrier = null;
                }
                else
                {
                    ActiveBarrier = barrierResult.Barrier;
                    if (barrierResult.SimulationEvent
                        == BarrierSimulationEvent.Locked)
                    {
                        LockedBarrierCount++;
                        LastRoomSplitResult =
                            Board.TryApplyLockedBarrier(barrierResult.Barrier);
                        if (!LastRoomSplitResult.Applied)
                        {
                            throw new InvalidOperationException(
                                "A locked barrier could not split its active room: "
                                + LastRoomSplitResult.Diagnostic + ".");
                        }

                        Threat = Board.GetThreat(Threat.Id);
                        ActiveBarrier = null;
                        float capturedTargetArea =
                            TargetCapturedFraction * InitialRoom.Bounds.Area;
                        if (Board.CapturedArea > capturedTargetArea
                            || _tolerance.IsAreaApproximatelyEqual(
                                Board.CapturedArea,
                                capturedTargetArea))
                        {
                            LevelStatus = CaptureLevelStatus.Completed;
                        }
                    }
                }

                TickCount++;
                return;
            }

            if (!Board.TryGetActiveRoom(Threat.RoomId, out RoomState room))
            {
                throw new InvalidOperationException(
                    "The live threat has no active room.");
            }

            ThreatMotionResult result = ThreatMotionSolver.Move(
                room,
                Threat,
                elapsedTime,
                _configuration.MaximumImpactsPerTick,
                _tolerance);
            Threat = result.Threat;
            Board.UpdateThreat(Threat);
            LastDiagnostic = result.Diagnostic;
            TickCount++;
        }

        public BarrierStartResult TryStartBarrier(BarrierIntent intent)
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

            BarrierStartResult result = BarrierFactory.TryCreate(
                new BarrierId(_nextBarrierId),
                room,
                intent,
                _barrierConfiguration,
                _tolerance);
            if (result.Accepted)
            {
                ActiveBarrier = result.Barrier;
                LastBarrierSnapshot = result.Barrier;
                _nextBarrierId++;
            }

            return result;
        }

        public void Reset()
        {
            Threat = new ThreatState(
                new ThreatId(1),
                InitialRoom.Id,
                _configuration.InitialPosition,
                _configuration.InitialDirection * _configuration.Speed,
                _configuration.Radius);
            ThreatMotionSolver.Move(
                InitialRoom,
                Threat,
                0f,
                _configuration.MaximumImpactsPerTick,
                _tolerance);
            Board = new CaptureBoardState(
                InitialRoom.Bounds,
                new[] { Threat },
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
        }
    }
}
