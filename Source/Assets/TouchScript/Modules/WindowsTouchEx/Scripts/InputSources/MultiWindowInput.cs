#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
#endif
using TouchScript.InputSources.InputHandlers;
using TouchScript.Pointers;
using TouchScript.Utils.Attributes;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using TouchScript.Utils.Platform;
#endif
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TouchScript.InputSources
{
    /// <summary>
    /// <see cref="MultiWindowInput"/> tries to handle touch input received from multiple windows. After activation of
    /// the displays it is important to call <see cref="MultiWindowInput.RefreshTouchSources()"/> as from that point the
    /// windows are available.
    /// </summary>
    /// <para>
    /// <see cref="StandardInput"/> uses the WindowsTouch.dll to capture the touch input of the first found window with
    /// class name "UnityWndClass". This is the most easy to use input in the case the application is made of one window.
    /// But when multiple windows are created, only this first found window will provide touch input.
    /// </para>
    /// <para>
    /// <see cref="MultiWindowInput"/> provides the means to capture the touch input of the window bound to a specific
    /// display.
    /// </para>
    [AddComponentMenu("TouchScript/Input Sources/Multi Window Input")]
    public sealed class MultiWindowInput : InputSource
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const string unityWindowClassName = "UnityWndClass";
        private static readonly Version WIN8_VERSION = new Version(6, 2, 0, 0);
#endif
        
        /// <summary>
        /// Use emulated second mouse pointer with ALT or not.
        /// </summary>
        public bool EmulateSecondMousePointer
        {
            get { return emulateSecondMousePointer; }
            set
            {
                emulateSecondMousePointer = value;
                if (mouseHandler != null) mouseHandler.EmulateSecondMousePointer = value;
            }
        }
        
        private static MultiWindowInput instance;

        [ToggleLeft, SerializeField] private bool emulateSecondMousePointer = true;
        
        private MouseHandler mouseHandler;
        private TouchHandler touchHandler;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private List<Windows8PointerHandlerEx> windows8PointerHandlers = new List<Windows8PointerHandlerEx>();
#endif
        
#pragma warning disable CS0414

        [SerializeField, HideInInspector] private bool generalProps; // Used in the custom inspector
        [SerializeField, HideInInspector] private bool windowsProps; // Used in the custom inspector
        
#pragma warning restore CS0414

        /// <inheritdoc />
        public override bool UpdateInput()
        {
            if (base.UpdateInput()) return true;

            var handled = false;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (windows8PointerHandlers.Count > 0) 
            {
                foreach (var pointerHandler in windows8PointerHandlers)
                {
                    handled |= pointerHandler.UpdateInput();
                }
            } 
            else
            {
#endif
                if (touchHandler != null)
                {
                    handled = touchHandler.UpdateInput();
                }
                if (mouseHandler != null)
                {
                    if (handled) mouseHandler.CancelMousePointer();
                    else handled = mouseHandler.UpdateInput();
                }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            }
#endif
            return handled;
        }

        /// <inheritdoc />
        public override void UpdateResolution()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            foreach (var pointerHandler in windows8PointerHandlers)
            {
                pointerHandler.UpdateResolution();
            }
#endif
            if (touchHandler != null) touchHandler.UpdateResolution();
            if (mouseHandler != null) mouseHandler.UpdateResolution();
        }

        /// <inheritdoc />
        public override bool CancelPointer(Pointer pointer, bool shouldReturn)
        {
            base.CancelPointer(pointer, shouldReturn);

            var handled = false;
            if (touchHandler != null) handled = touchHandler.CancelPointer(pointer, shouldReturn);
            if (mouseHandler != null && !handled) handled = mouseHandler.CancelPointer(pointer, shouldReturn);
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (!handled && windows8PointerHandlers.Count > 0) 
            {
                foreach (var pointerHandler in windows8PointerHandlers)
                {
                    handled = pointerHandler.CancelPointer(pointer, shouldReturn);
                    if (handled)
                    {
                        break;
                    }
                }
            }
#endif

            return handled;
        }

#pragma warning restore CS0414

        /// <inheritdoc />
        protected override void OnEnable()
        {
            if (instance != null) Destroy(instance);
            instance = this;
            
            base.OnEnable();

            Input.simulateMouseWithTouches = false;

#if UNITY_EDITOR
            enableTouch();
            enableMouse();
#else
# if UNITY_STANDALONE_WIN
            if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                Environment.OSVersion.Version >= WIN8_VERSION)
            {
                // Windows 8+
                enableWindows8Touch();
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
        
        [ContextMenu("Basic Editor")]
        private void switchToBasicEditor()
        {
            basicEditor = true;
        }

        public void RefreshTouchSources()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            disableWindows8Touch();

            if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                Environment.OSVersion.Version >= WIN8_VERSION)
            {
                // Windows 8+
                enableWindows8Touch();
            }
#endif
        }

        /// <inheritdoc />
        protected override void updateCoordinatesRemapper(ICoordinatesRemapper remapper)
        {
            base.updateCoordinatesRemapper(remapper);
            if (mouseHandler != null) mouseHandler.CoordinatesRemapper = remapper;
            if (touchHandler != null) touchHandler.CoordinatesRemapper = remapper;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (windows8PointerHandler != null) windows8PointerHandler.CoordinatesRemapper = remapper;
#endif
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
        
#if !UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private void enableWindows8Touch()
        {
            // For every window of the current process, we check if it is of the unity window class, if so
            var classNameBuilder = new StringBuilder(64);
            var windows = WindowsUtilsEx.GetRootWindowsOfProcess(Process.GetCurrentProcess().Id);
            
            foreach (var window in windows)
            {
                classNameBuilder.Clear();
                WindowsUtilsEx.GetClassName(window, classNameBuilder, 64);

                var className = classNameBuilder.ToString();
                if (className != unityWindowClassName)
                {
                    continue;
                }

                var pointerHandler = new Windows8PointerHandlerEx(window, addPointer, updatePointer, pressPointer,
                    releasePointer, removePointer, cancelPointer);
                pointerHandler.MouseInPointer = true;
                
                windows8PointerHandlers.Add(pointerHandler);
            }
            
            Debug.Log($"[TouchScript] Initialized {windows8PointerHandlers.Count} Windows 8 pointer inputs.");
        }

        private void disableWindows8Touch()
        {
            foreach (var pointerHandler in windows8PointerHandlers)
            {
                pointerHandler.Dispose();
            }
            windows8PointerHandlers.Clear();
        }
#endif
    }
}