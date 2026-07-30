using Cutrium.Gameplay.Geometry;
using UnityEngine;

namespace Cutrium.Unity.Layout
{
    public static class BoardScreenMapper
    {
        public static bool TryMap(
            Rect boardScreenRect,
            Vector2 screenPosition,
            out LogicalPoint logicalPoint)
        {
            if (boardScreenRect.width <= 0f
                || boardScreenRect.height <= 0f
                || screenPosition.x < boardScreenRect.xMin
                || screenPosition.x > boardScreenRect.xMax
                || screenPosition.y < boardScreenRect.yMin
                || screenPosition.y > boardScreenRect.yMax)
            {
                logicalPoint = default;
                return false;
            }

            float normalizedX =
                (screenPosition.x - boardScreenRect.xMin) / boardScreenRect.width;
            float normalizedY =
                (screenPosition.y - boardScreenRect.yMin) / boardScreenRect.height;

            logicalPoint = new LogicalPoint(
                normalizedX * BoardViewportLayout.LogicalWidth,
                normalizedY * BoardViewportLayout.LogicalHeight);
            return true;
        }
    }
}
