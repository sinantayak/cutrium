using Cutrium.Gameplay.Threats;

namespace Cutrium.Gameplay.Barriers
{
    public enum BarrierSimulationEvent
    {
        None = 0,
        Failed = 1,
        Locked = 2
    }

    public enum BarrierContactKind
    {
        None = 0,
        Body = 1,
        NegativeTip = 2,
        PositiveTip = 3
    }

    public enum BarrierSimulationDiagnostic
    {
        None = 0,
        IterationLimitReached = 1,
        ThreatImpactLimitReached = 2
    }

    public readonly struct BarrierSimulationResult
    {
        public BarrierSimulationResult(
            ThreatState threat,
            BarrierState barrier,
            BarrierSimulationEvent simulationEvent,
            BarrierContactKind contactKind,
            BarrierSimulationDiagnostic diagnostic,
            int iterationCount)
        {
            Threat = threat;
            Barrier = barrier;
            SimulationEvent = simulationEvent;
            ContactKind = contactKind;
            Diagnostic = diagnostic;
            IterationCount = iterationCount;
        }

        public ThreatState Threat { get; }
        public BarrierState Barrier { get; }
        public BarrierSimulationEvent SimulationEvent { get; }
        public BarrierContactKind ContactKind { get; }
        public BarrierSimulationDiagnostic Diagnostic { get; }
        public int IterationCount { get; }
    }
}
