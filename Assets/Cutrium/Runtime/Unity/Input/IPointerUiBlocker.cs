using UnityEngine;

namespace Cutrium.Unity.Input
{
    public interface IPointerUiBlocker
    {
        bool IsPointerOverUi(Vector2 screenPosition, int pointerId);
    }
}
