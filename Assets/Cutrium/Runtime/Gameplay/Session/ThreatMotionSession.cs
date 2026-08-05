using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Threats;

namespace Cutrium.Gameplay.Session
{
    public sealed class ThreatMotionSession
    {
        private readonly ThreatMotionConfiguration _configuration;
        private readonly GeometryTolerancePolicy _tolerance;

        public ThreatMotionSession(
            ThreatMotionConfiguration configuration,
            GeometryTolerancePolicy tolerance)
        {
            _configuration = configuration;
            _tolerance = tolerance;
            InitialRoom = new RoomState(new RoomId(1), configuration.BoardBounds);
            Reset();
        }

        public RoomState InitialRoom { get; }

        public ThreatState Threat { get; private set; }

        public ThreatMotionDiagnostic LastDiagnostic { get; private set; }

        public int TickCount { get; private set; }

        public void Tick(float elapsedTime)
        {
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
            TickCount = 0;
        }
    }
}
