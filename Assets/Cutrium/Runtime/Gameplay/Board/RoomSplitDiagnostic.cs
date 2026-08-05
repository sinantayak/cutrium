namespace Cutrium.Gameplay.Board
{
    public enum RoomSplitDiagnostic
    {
        None = 0,
        ThreatTieFallback = 1,
        BarrierNotLocked = 2,
        StaleParentRoom = 3,
        BarrierAlreadyApplied = 4,
        InvalidSplit = 5,
        ThreatStraddlesSplit = 6,
        InvariantViolation = 7
    }
}
