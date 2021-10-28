using TouchScript.Pointers;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers
{
    /// <summary>
    /// A display specific input handler. Holds a <see cref="MouseHandler"/> or a <see cref="MultiDisplayTouchHandler"/>.
    /// </summary>
    // TODO MouseHandler is not capable of being routed to a display. As such we need a MultiDisplayMouseHandler
    public class MultiDisplayInputHandler : IMultiDisplayInputHandler
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const string unityWindowClassName = "UnityWndClass";
        private static readonly Version WIN8_VERSION = new Version(6, 2, 0, 0);
#endif
        
        public int TargetDisplay { get; set; }
        
        /// <summary>
        /// Use emulated second mouse pointer with ALT or not.
        /// </summary>
        public bool EmulateSecondMousePointer
        {
            get
            {
                if (mouseHandler != null)
                    return mouseHandler.EmulateSecondMousePointer;
                return false;
            }
            set
            {
                if (mouseHandler != null) mouseHandler.EmulateSecondMousePointer = value;
            }
        }
        
        public ICoordinatesRemapper CoordinatesRemapper { get; set; }
        
        private bool emulateSecondMousePointer = true;
        private MultiDisplayMouseHandler mouseHandler;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private MultiDisplayTouchHandler touchHandler;
#endif
        
        private readonly PointerDelegate addPointer;
        private readonly PointerDelegate updatePointer;
        private readonly PointerDelegate pressPointer;
        private readonly PointerDelegate releasePointer;
        private readonly PointerDelegate removePointer;
        private readonly PointerDelegate cancelPointer;

        public MultiDisplayInputHandler(PointerDelegate addPointer, PointerDelegate updatePointer,
            PointerDelegate pressPointer, PointerDelegate releasePointer, PointerDelegate removePointer,
            PointerDelegate cancelPointer)
        {
            this.addPointer = addPointer;
            this.updatePointer = updatePointer;
            this.pressPointer = pressPointer;
            this.releasePointer = releasePointer;
            this.removePointer = removePointer;
            this.cancelPointer = cancelPointer;
        }

        public void Enable(bool emulateSecondMousePointer)
        {
#if UNITY_EDITOR
            EnableMouse(emulateSecondMousePointer);
#else
# if UNITY_STANDALONE_WIN
            if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                Environment.OSVersion.Version >= WIN8_VERSION)
            {
                // Windows 8+
                EnableTouch();
            }
            else
            {
                // Other windows
                EnableMouse();
            }
# else
            EnableMouse(emulateSecondMousePointer);
# endif
#endif
        }

        public void Disable()
        {
            DisableMouse();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            DisableTouch();
#endif
        }

        private void EnableMouse(bool emulateSecondMousePointer)
        {
            mouseHandler = new MultiDisplayMouseHandler(addPointer, updatePointer, pressPointer, releasePointer, removePointer,
                cancelPointer);
            mouseHandler.EmulateSecondMousePointer = emulateSecondMousePointer;
            mouseHandler.TargetDisplay = TargetDisplay;
            
            Debug.Log($"[TouchScript] Initialized Unity mouse input for {TargetDisplay}.");
        }

        private void DisableMouse()
        {
            mouseHandler?.Dispose();
            mouseHandler = null;
        }

        public bool UpdateInput()
        {
            var handled = false;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (touchHandler != null)
            {
                handled = touchHandler.UpdateInput();
            }
#endif
            if (mouseHandler != null)
            {
                if (handled) mouseHandler.CancelMousePointer();
                else handled = mouseHandler.UpdateInput();
            }
            
            return handled;
        }
        
        /// <inheritdoc />
        public void UpdateResolution()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (touchHandler != null) touchHandler.UpdateResolution();
#endif
            if (mouseHandler != null) mouseHandler.UpdateResolution();
        }
        
        /// <inheritdoc />
        public bool CancelPointer(Pointer pointer, bool shouldReturn)
        {
            var handled = false;
            
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (touchHandler != null) handled = touchHandler.CancelPointer(pointer, shouldReturn);
#endif
            if (mouseHandler != null && !handled) handled = mouseHandler.CancelPointer(pointer, shouldReturn);
            
            return handled;
        }
        
        public void UpdateCoordinatesRemapper(ICoordinatesRemapper remapper)
        {
            if (mouseHandler != null) mouseHandler.CoordinatesRemapper = remapper;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (touchHandler != null) touchHandler.CoordinatesRemapper = remapper;
#endif
        }
        
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
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

                var pointerHandler = new Windows8MultiDisplayTouchHandler(window, addPointer, updatePointer, pressPointer,
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
        /// <inheritdoc />
        public virtual void INTERNAL_DiscardPointer(Pointer pointer)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (touchHandler != null) touchHandler.INTERNAL_DiscardPointer(pointer);
#endif
            if (mouseHandler != null) mouseHandler.INTERNAL_DiscardPointer(pointer);
        }
    }
}