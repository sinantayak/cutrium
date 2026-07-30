using Cutrium.Unity.Input;
using Cutrium.Unity.Layout;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Cutrium.Unity.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class SceneCompositionRoot : MonoBehaviour
    {
        [SerializeField]
        private Camera _boardCamera;

        [SerializeField]
        private Canvas _canvas;

        [SerializeField]
        private SafeAreaFitter _safeAreaFitter;

        [SerializeField]
        private BoardCameraFitter _boardCameraFitter;

        [SerializeField]
        private ScreenToLogicalBoardMapper _boardMapper;

        [SerializeField]
        private EventSystem _eventSystem;

        [SerializeField]
        private InputSystemUIInputModule _uiInputModule;

        [SerializeField]
        private EventSystemPointerUiBlocker _uiBlocker;

        [SerializeField]
        private PointerInputAdapter _pointerInput;

        public Camera BoardCamera => _boardCamera;

        public Canvas Canvas => _canvas;

        public SafeAreaFitter SafeAreaFitter => _safeAreaFitter;

        public BoardCameraFitter BoardCameraFitter => _boardCameraFitter;

        public ScreenToLogicalBoardMapper BoardMapper => _boardMapper;

        public EventSystem EventSystem => _eventSystem;

        public InputSystemUIInputModule UiInputModule => _uiInputModule;

        public EventSystemPointerUiBlocker UiBlocker => _uiBlocker;

        public PointerInputAdapter PointerInput => _pointerInput;

        public void Configure(
            Camera boardCamera,
            Canvas canvas,
            SafeAreaFitter safeAreaFitter,
            BoardCameraFitter boardCameraFitter,
            ScreenToLogicalBoardMapper boardMapper,
            EventSystem eventSystem,
            InputSystemUIInputModule uiInputModule,
            EventSystemPointerUiBlocker uiBlocker,
            PointerInputAdapter pointerInput)
        {
            _boardCamera = boardCamera;
            _canvas = canvas;
            _safeAreaFitter = safeAreaFitter;
            _boardCameraFitter = boardCameraFitter;
            _boardMapper = boardMapper;
            _eventSystem = eventSystem;
            _uiInputModule = uiInputModule;
            _uiBlocker = uiBlocker;
            _pointerInput = pointerInput;
        }

        private void Awake()
        {
            if (_boardCamera == null
                || _canvas == null
                || _safeAreaFitter == null
                || _boardCameraFitter == null
                || _boardMapper == null
                || _eventSystem == null
                || _uiInputModule == null
                || _uiBlocker == null
                || _pointerInput == null)
            {
                Debug.LogError(
                    "SceneCompositionRoot has one or more missing serialized Milestone 1B references.",
                    this);
            }
        }
    }
}
