namespace Cutrium.Gameplay.Threats
{
    public enum ThreatMotionDiagnostic
    {
        None = 0,
        ImpactLimitReached = 1
    }

    public readonly struct ThreatMotionResult
    {
        public ThreatMotionResult(
            ThreatState threat,
            int impactCount,
            float simulatedTime,
            ThreatMotionDiagnostic diagnostic)
        {
            Threat = threat;
            ImpactCount = impactCount;
            SimulatedTime = simulatedTime;
            Diagnostic = diagnostic;
        }

        public ThreatState Threat { get; }

        public int ImpactCount { get; }

        public float SimulatedTime { get; }

        public ThreatMotionDiagnostic Diagnostic { get; }
    }
}
