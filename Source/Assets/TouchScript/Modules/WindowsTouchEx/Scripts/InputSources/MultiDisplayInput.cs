#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
#endif
using System.Collections.Generic;
using TouchScript.InputSources.InputHandlers;
using TouchScript.Pointers;
using TouchScript.Utils.Attributes;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using TouchScript.Utils.Platform;
#endif
using UnityEngine;

namespace TouchScript.InputSources
{
    /// <summary>
    /// <see cref="MultiDisplayInput"/> tries to handle touch input received from multiple displays. After activation of
    /// the displays it is important to call <see cref="RefreshInputHandlers"/> as from that point the
    /// windows are available.
    /// </summary>
    /// <para>
    /// <see cref="StandardInput"/> uses the WindowsTouch.dll to capture the touch input of the first found window with
    /// class name "UnityWndClass". This is the most easy to use input in the case the application is made of one window.
    /// But when multiple windows are created, only this first found window will provide touch input.
    /// </para>
    /// <para>
    /// <see cref="MultiDisplayInput"/> provides the means to capture the touch input of the window bound to a specific
    /// display.
    /// </para>
    [AddComponentMenu("TouchScript/Input Sources/Multi Display Input")]
    public sealed class MultiDisplayInput : InputSource
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
                foreach (var inputHandler in inputHandlers)
                {
                    inputHandler.EmulateSecondMousePointer = value;
                }
            }
        }
        
        private static MultiDisplayInput instance;

        [ToggleLeft, SerializeField] private bool emulateSecondMousePointer = true;
        
        private List<MultiDisplayInputHandler> inputHandlers = new List<MultiDisplayInputHandler>();
        
#pragma warning disable CS0414

        [SerializeField, HideInInspector] private bool generalProps; // Used in the custom inspector
        [SerializeField, HideInInspector] private bool windowsProps; // Used in the custom inspector
        
#pragma warning restore CS0414

        /// <inheritdoc />
        public override bool UpdateInput()
        {
            if (base.UpdateInput()) return true;

            var handled = false;
            foreach (var inputHandler in inputHandlers)
            {
                handled |= inputHandler.UpdateInput();
            }
            
            return handled;
        }

        /// <inheritdoc />
        public override void UpdateResolution()
        {
            foreach (var inputHandler in inputHandlers)
            {
                inputHandler.UpdateResolution();
            }
        }

        /// <inheritdoc />
        public override bool CancelPointer(Pointer pointer, bool shouldReturn)
        {
            base.CancelPointer(pointer, shouldReturn);

            var handled = false;
            foreach (var inputHandler in inputHandlers)
            {
                handled = inputHandler.CancelPointer(pointer, shouldReturn);
                if (handled)
                {
                    break;
                }
            }

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
            
            foreach (var inputHandler in inputHandlers)
            {
                inputHandler.Enable(emulateSecondMousePointer);
            }
            
            if (CoordinatesRemapper != null) updateCoordinatesRemapper(CoordinatesRemapper);
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            foreach (var inputHandler in inputHandlers)
            {
                inputHandler.Disable();
            }

            base.OnDisable();
        }
        
        [ContextMenu("Basic Editor")]
        private void switchToBasicEditor()
        {
            basicEditor = true;
        }

        public void RefreshInputHandlers()
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

            foreach (var inputHandler in inputHandlers)
            {
                inputHandler.UpdateCoordinatesRemapper(remapper);
            }
        }
    }
}