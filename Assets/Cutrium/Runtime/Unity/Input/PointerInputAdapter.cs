using System;
using Cutrium.Gameplay.Geometry;
using Cutrium.Unity.Layout;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cutrium.Unity.Input
{
    [DisallowMultipleComponent]
    public sealed class PointerInputAdapter : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference _pointAction;

        [SerializeField]
        private InputActionReference _pressAction;

        [SerializeField]
        private InputActionReference _cancelAction;

        [SerializeField]
        private MonoBehaviour _uiBlockerComponent;

        [SerializeField]
        private ScreenToLogicalBoardMapper _boardMapper;

        private IPointerUiBlocker _uiBlocker;
        private bool _enabledPointAction;
        private bool _enabledPressAction;
        private bool _enabledCancelAction;
        private bool _interactionActive;
        private bool _startedOverUi;
        private bool _startedInsideBoard;
        private int _activePointerId = -1;

        public event Action<PointerSample> Sampled;

        public InputActionReference PointAction => _pointAction;

        public InputActionReference PressAction => _pressAction;

        public InputActionReference CancelAction => _cancelAction;

        public MonoBehaviour UiBlockerComponent => _uiBlockerComponent;

        public ScreenToLogicalBoardMapper BoardMapper => _boardMapper;

        public Vector2 CurrentScreenPosition { get; private set; }

        public PointerSample LastSample { get; private set; }

        public bool HasActiveInteraction => _interactionActive;

        public void Configure(
            InputActionReference pointAction,
            InputActionReference pressAction,
            InputActionReference cancelAction,
            MonoBehaviour uiBlockerComponent,
            ScreenToLogicalBoardMapper boardMapper)
        {
            _pointAction = pointAction;
            _pressAction = pressAction;
            _cancelAction = cancelAction;
            _uiBlockerComponent = uiBlockerComponent;
            _boardMapper = boardMapper;
            _uiBlocker = uiBlockerComponent as IPointerUiBlocker;
        }

        private void Awake()
        {
            _uiBlocker = _uiBlockerComponent as IPointerUiBlocker;
            if (_uiBlocker == null)
            {
                Debug.LogError(
                    "PointerInputAdapter requires a serialized IPointerUiBlocker component.",
                    this);
            }

            if (_boardMapper == null)
            {
                Debug.LogError(
                    "PointerInputAdapter requires a serialized board mapper.",
                    this);
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!TryGetActions(
                    out InputAction point,
                    out InputAction press,
                    out InputAction cancel))
            {
                return;
            }

            point.performed += OnPointPerformed;
            press.started += OnPressStarted;
            press.canceled += OnPressReleased;
            cancel.performed += OnCancelPerformed;

            _enabledPointAction = EnableIfNeeded(point);
            _enabledPressAction = EnableIfNeeded(press);
            _enabledCancelAction = EnableIfNeeded(cancel);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                ResetInteraction();
                return;
            }

            if (TryGetActions(
                    out InputAction point,
                    out InputAction press,
                    out InputAction cancel))
            {
                point.performed -= OnPointPerformed;
                press.started -= OnPressStarted;
                press.canceled -= OnPressReleased;
                cancel.performed -= OnCancelPerformed;

                DisableIfOwned(point, _enabledPointAction);
                DisableIfOwned(press, _enabledPressAction);
                DisableIfOwned(cancel, _enabledCancelAction);
            }

            _enabledPointAction = false;
            _enabledPressAction = false;
            _enabledCancelAction = false;
            ResetInteraction();
        }

        private void OnPointPerformed(InputAction.CallbackContext context)
        {
            int pointerId = PointerDeviceIdentity.GetPointerId(context.control);
            Vector2 position = context.ReadValue<Vector2>();

            if (_interactionActive && pointerId != _activePointerId)
            {
                return;
            }

            CurrentScreenPosition = position;
            if (_interactionActive)
            {
                Emit(PointerSamplePhase.Moved, position);
            }
        }

        private void OnPressStarted(InputAction.CallbackContext context)
        {
            if (_interactionActive)
            {
                return;
            }

            CurrentScreenPosition = ReadPointerPosition(context);
            _activePointerId = PointerDeviceIdentity.GetPointerId(context.control);
            _startedOverUi =
                _uiBlocker != null
                && _uiBlocker.IsPointerOverUi(CurrentScreenPosition, _activePointerId);
            _startedInsideBoard =
                _boardMapper != null
                && _boardMapper.TryMap(CurrentScreenPosition, out _);
            _interactionActive = true;

            Emit(PointerSamplePhase.Started, CurrentScreenPosition);
        }

        private void OnPressReleased(InputAction.CallbackContext context)
        {
            if (!_interactionActive)
            {
                return;
            }

            int pointerId = PointerDeviceIdentity.GetPointerId(context.control);
            if (pointerId != _activePointerId)
            {
                return;
            }

            CurrentScreenPosition = ReadPointerPosition(context);
            Emit(PointerSamplePhase.Released, CurrentScreenPosition);
            ResetInteraction();
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            if (!_interactionActive)
            {
                return;
            }

            Emit(PointerSamplePhase.Cancelled, CurrentScreenPosition);
            ResetInteraction();
        }

        private void Emit(PointerSamplePhase phase, Vector2 screenPosition)
        {
            LogicalPoint logicalPoint = default;
            bool isInsideBoard =
                _boardMapper != null
                && _boardMapper.TryMap(screenPosition, out logicalPoint);

            var sample = new PointerSample(
                phase,
                screenPosition,
                _activePointerId,
                _startedOverUi,
                _startedInsideBoard,
                isInsideBoard,
                logicalPoint);
            LastSample = sample;
            Sampled?.Invoke(sample);
        }

        private bool TryGetActions(
            out InputAction point,
            out InputAction press,
            out InputAction cancel)
        {
            point = _pointAction != null ? _pointAction.action : null;
            press = _pressAction != null ? _pressAction.action : null;
            cancel = _cancelAction != null ? _cancelAction.action : null;

            if (point != null && press != null && cancel != null)
            {
                return true;
            }

            if (isActiveAndEnabled)
            {
                Debug.LogError(
                    "PointerInputAdapter requires serialized Point, Press, and Cancel actions.",
                    this);
            }

            return false;
        }

        private static Vector2 ReadPointerPosition(InputAction.CallbackContext context)
        {
            if (context.control?.device is Pointer pointer)
            {
                return pointer.position.ReadValue();
            }

            return Vector2.zero;
        }

        private static bool EnableIfNeeded(InputAction action)
        {
            if (action.enabled)
            {
                return false;
            }

            action.Enable();
            return true;
        }

        private static void DisableIfOwned(InputAction action, bool wasEnabledHere)
        {
            if (wasEnabledHere && action.enabled)
            {
                action.Disable();
            }
        }

        private void ResetInteraction()
        {
            _interactionActive = false;
            _startedOverUi = false;
            _startedInsideBoard = false;
            _activePointerId = -1;
        }
    }
}
