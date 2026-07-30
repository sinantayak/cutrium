using Cutrium.Gameplay.Geometry;
using UnityEngine;

namespace Cutrium.Unity.Input
{
    public enum PointerSamplePhase
    {
        None = 0,
        Started = 1,
        Moved = 2,
        Released = 3,
        Cancelled = 4
    }

    public readonly struct PointerSample
    {
        public PointerSample(
            PointerSamplePhase phase,
            Vector2 screenPosition,
            int pointerId,
            bool startedOverUi,
            bool startedInsideBoard,
            bool isInsideBoard,
            LogicalPoint logicalPoint)
        {
            Phase = phase;
            ScreenPosition = screenPosition;
            PointerId = pointerId;
            StartedOverUi = startedOverUi;
            StartedInsideBoard = startedInsideBoard;
            IsInsideBoard = isInsideBoard;
            LogicalPoint = logicalPoint;
        }

        public PointerSamplePhase Phase { get; }

        public Vector2 ScreenPosition { get; }

        public int PointerId { get; }

        public bool StartedOverUi { get; }

        public bool StartedInsideBoard { get; }

        public bool IsInsideBoard { get; }

        public LogicalPoint LogicalPoint { get; }

        public bool IsAcceptedBoardStart =>
            Phase == PointerSamplePhase.Started
            && !StartedOverUi
            && StartedInsideBoard;
    }
}
