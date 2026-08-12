namespace Cutrium.Gameplay.Barriers
{
    public enum BarrierRejectionReason
    {
        None = 0,
        InvalidOrientation = 1,
        OriginOutsideActiveRoom = 2,
        TooCloseToRoomEdge = 3,
        BarrierAlreadyActive = 4,
        LevelCompleted = 5,
        NoGrowthSpan = 6,
        CutLimitReached = 7
    }

    public readonly struct BarrierStartResult
    {
        public BarrierStartResult(
            bool accepted,
            BarrierRejectionReason rejectionReason,
            BarrierState barrier)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Barrier = barrier;
        }

        public bool Accepted { get; }
        public BarrierRejectionReason RejectionReason { get; }
        public BarrierState Barrier { get; }
    }
}
