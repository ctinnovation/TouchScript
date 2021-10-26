using TouchScript.InputSources.InputHandlers;
using TouchScript.Utils.Attributes;
using UnityEngine;

namespace TouchScript.InputSources
{
    /// <summary>
    /// Input handled for a single Unity application window.
    /// </summary>
    /// <para>
    /// <see cref="StandardInput"/> uses the WindowsTouch.dll to capture the touch input of the first found window with
    /// class name "UnityWndClass". This is the most easy to use input in the case the application is made of one window.
    /// But when multiple windows are created, only this first found window will provide touch input.
    /// </para>
    /// <para>
    /// <see cref="WindowInput"/> provides the means to capture the touch input of the window bound to a specific
    /// display.
    /// </para>
    [AddComponentMenu("TouchScript/Input Sources/Window Input")]
    public sealed class WindowInput : InputSource
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private static readonly Version WIN8_VERSION = new Version(6, 2, 0, 0);
#endif
        
        [SerializeField] private int targetDisplay;
        [ToggleLeft, SerializeField] private bool emulateSecondMousePointer = true;

        public int TargetDisplay
        {
            get => targetDisplay;
            set
            {
                if (value < 1 || value > 8)
                {
                    Debug.LogError($"[{this}]: TargetDisplay must be in range [1,8]");
                    return;
                }

                if (Application.isPlaying)
                {
                    // Check if already created
                }
                else
                {
                    targetDisplay = value;
                }
            }
        }
        
        private MouseHandler mouseHandler;
        private TouchHandler touchHandler;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private Windows8PointerHandlerEx windows8PointerHandler;
#endif
        
#pragma warning disable CS0414

        [SerializeField, HideInInspector] private bool generalProps; // Used in the custom inspector
        [SerializeField, HideInInspector] private bool windowsProps; // Used in the custom inspector

#pragma warning restore CS0414

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            Input.simulateMouseWithTouches = false;

#if UNITY_EDITOR
            enableTouch();
            enableMouse();
#else
# if UNITY_STANDALONE_WIN
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (Environment.OSVersion.Version >= WIN8_VERSION)
                {
                    // Windows 8+
                    enableWindows8Touch();
                }
                else
                {
                    // Some other earlier Windows
                    enableMouse();
                }
            }
            else
            {
                // Some other earlier Windows
                enableMouse();
            }
# else
#  error Unsupported! Use StandardInput in these cases.
# endif
#endif
            if (CoordinatesRemapper != null) updateCoordinatesRemapper(CoordinatesRemapper);
        }
        
        /// <inheritdoc />
        protected override void OnDisable()
        {
            disableMouse();
            disableTouch();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            disableWindows8Touch();
#endif

            base.OnDisable();
        }
        
        private void enableMouse()
        {
            mouseHandler = new MouseHandler(addPointer, updatePointer, pressPointer, releasePointer, removePointer, cancelPointer);
            mouseHandler.EmulateSecondMousePointer = emulateSecondMousePointer;
            Debug.Log("[TouchScript] Initialized Unity mouse input.");
        }

        private void disableMouse()
        {
            if (mouseHandler != null)
            {
                mouseHandler.Dispose();
                mouseHandler = null;
            }
        }

        private void enableTouch()
        {
            touchHandler = new TouchHandler(addPointer, updatePointer, pressPointer, releasePointer, removePointer, cancelPointer);
            Debug.Log("[TouchScript] Initialized Unity touch input.");
        }

        private void disableTouch()
        {
            if (touchHandler != null)
            {
                touchHandler.Dispose();
                touchHandler = null;
            }
        }
        
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private void enableWindows8Touch()
        {
            windows8PointerHandler = new Windows8PointerHandlerEx(addPointer, updatePointer, pressPointer, releasePointer, removePointer, cancelPointer);
            windows8PointerHandler.MouseInPointer = windows8Mouse;
            Debug.Log("[TouchScript] Initialized Windows 8 pointer input.");
        }

        private void disableWindows8Touch()
        {
            if (windows8PointerHandler != null)
            {
                windows8PointerHandler.Dispose();
                windows8PointerHandler = null;
            }
        }
#endif
    }
}