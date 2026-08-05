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
        private int _nextBarrierId;

        public ThreatMotionSession(
            ThreatMotionConfiguration configuration,
            GeometryTolerancePolicy tolerance)
            : this(
                configuration,
                new BarrierConfiguration(8f, 0.08f, 0.6f, 16),
                tolerance)
        {
        }

        public ThreatMotionSession(
            ThreatMotionConfiguration configuration,
            BarrierConfiguration barrierConfiguration,
            GeometryTolerancePolicy tolerance)
        {
            _configuration = configuration;
            _barrierConfiguration = barrierConfiguration;
            _tolerance = tolerance;
            InitialRoom = new RoomState(new RoomId(1), configuration.BoardBounds);
            Reset();
        }

        public RoomState InitialRoom { get; }

        public ThreatState Threat { get; private set; }

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
            if (ActiveBarrier.HasValue)
            {
                BarrierSimulationResult barrierResult =
                    GrowingBarrierMotionSolver.Move(
                        InitialRoom,
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
                    }
                }

                TickCount++;
                return;
            }

            ThreatMotionResult result = ThreatMotionSolver.Move(
                InitialRoom,
                Threat,
                elapsedTime,
                _configuration.MaximumImpactsPerTick,
                _tolerance);
            Threat = result.Threat;
            LastDiagnostic = result.Diagnostic;
            TickCount++;
        }

        public BarrierStartResult TryStartBarrier(BarrierIntent intent)
        {
            if (ActiveBarrier.HasValue)
            {
                return new BarrierStartResult(
                    false,
                    BarrierRejectionReason.BarrierAlreadyActive,
                    default);
            }

            BarrierStartResult result = BarrierFactory.TryCreate(
                new BarrierId(_nextBarrierId),
                InitialRoom,
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
