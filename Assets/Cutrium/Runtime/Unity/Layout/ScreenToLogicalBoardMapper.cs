using Cutrium.Gameplay.Geometry;
using UnityEngine;

namespace Cutrium.Unity.Layout
{
    [DisallowMultipleComponent]
    public sealed class ScreenToLogicalBoardMapper : MonoBehaviour
    {
        [SerializeField]
        private BoardCameraFitter _boardCameraFitter;

        public BoardCameraFitter BoardCameraFitter => _boardCameraFitter;

        public void Configure(BoardCameraFitter boardCameraFitter)
        {
            _boardCameraFitter = boardCameraFitter;
        }

        public bool TryMap(Vector2 screenPosition, out LogicalPoint logicalPoint)
        {
            if (_boardCameraFitter == null)
            {
                logicalPoint = default;
                return false;
            }

            return BoardScreenMapper.TryMap(
                _boardCameraFitter.BoardScreenRect,
                screenPosition,
                out logicalPoint);
        }
    }
}
