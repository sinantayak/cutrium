using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cutrium.Unity.Input
{
    [DisallowMultipleComponent]
    public sealed class EventSystemPointerUiBlocker : MonoBehaviour, IPointerUiBlocker
    {
        private readonly List<RaycastResult> _raycastResults =
            new List<RaycastResult>(8);

        [SerializeField]
        private EventSystem _eventSystem;

        public EventSystem EventSystem => _eventSystem;

        public void Configure(EventSystem eventSystem)
        {
            _eventSystem = eventSystem;
        }

        public bool IsPointerOverUi(Vector2 screenPosition, int pointerId)
        {
            if (_eventSystem == null)
            {
                return false;
            }

            var eventData = new PointerEventData(_eventSystem)
            {
                position = screenPosition,
                pointerId = pointerId
            };

            _raycastResults.Clear();
            _eventSystem.RaycastAll(eventData, _raycastResults);
            bool isBlocked = _raycastResults.Count > 0;
            _raycastResults.Clear();
            return isBlocked;
        }
    }
}
