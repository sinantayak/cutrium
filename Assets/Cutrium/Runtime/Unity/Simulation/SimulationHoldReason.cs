using System;

namespace Cutrium.Unity.Simulation
{
    [Flags]
    public enum SimulationHoldReason
    {
        None = 0,
        Legacy = 1 << 0,
        PreLevelIntro = 1 << 1,
        FrontEnd = 1 << 2,
        Settings = 1 << 3,
    }
}
