namespace Cutrium.Gameplay.Board
{
    public readonly struct RoomSplitApplyResult
    {
        public RoomSplitApplyResult(
            bool applied,
            RoomSplitDiagnostic diagnostic,
            RoomState negativeChild,
            RoomState positiveChild)
        {
            Applied = applied;
            Diagnostic = diagnostic;
            NegativeChild = negativeChild;
            PositiveChild = positiveChild;
        }

        public bool Applied { get; }

        public RoomSplitDiagnostic Diagnostic { get; }

        public RoomState NegativeChild { get; }

        public RoomState PositiveChild { get; }
    }
}
