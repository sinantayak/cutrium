using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Cutrium.Unity.Input
{
    public static class PointerDeviceIdentity
    {
        public static int GetPointerId(InputControl control)
        {
            if (control == null)
            {
                return -1;
            }

            InputControl current = control;
            while (current != null)
            {
                if (current is TouchControl touchControl)
                {
                    return touchControl.touchId.ReadValue();
                }

                current = current.parent;
            }

            if (control.device is Touchscreen touchscreen)
            {
                return touchscreen.primaryTouch.touchId.ReadValue();
            }

            return control.device != null ? control.device.deviceId : -1;
        }
    }
}
