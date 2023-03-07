using System;
using TouchScript.InputSources.InputHandlers.Interop;
using TouchScript.Pointers;
using TouchScript.Utils;
using TouchScript.Utils.Platform;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers
{
    class LinuxX11MultiWindowPointerHandler : LinuxMultiWindowPointerHandler
    {
        /// <summary>
        /// Should the primary pointer also dispatch a mouse pointer.
        /// </summary>
        public bool MouseInPointer
        {
            get { return mouseInPointer; }
            set
            {
                //WindowsUtils.EnableMouseInPointer(value);
                
                mouseInPointer = value;
                if (mouseInPointer)
                {
                    if (mousePointer == null) mousePointer = internalAddMousePointer(Vector3.zero);
                }
                else
                {
                    if (mousePointer != null)
                    {
                        if ((mousePointer.Buttons & Pointer.PointerButtonState.AnyButtonPressed) != 0)
                        {
                            mousePointer.Buttons = PointerUtils.UpPressedButtons(mousePointer.Buttons);
                            releasePointer(mousePointer);
                        }
                        removePointer(mousePointer);
                    }
                }
            }
        }
        
        private bool mouseInPointer = true;
        private NativeX11PointerHandler pointerHandler;
        
        public LinuxX11MultiWindowPointerHandler(IntPtr display, IntPtr window, PointerDelegate addPointer,
            PointerDelegate updatePointer,
            PointerDelegate pressPointer, PointerDelegate releasePointer, PointerDelegate removePointer,
            PointerDelegate cancelPointer)
            : base(window, addPointer, updatePointer, pressPointer, releasePointer, removePointer, cancelPointer)
        {
            pointerHandler = new NativeX11PointerHandler();
            pointerHandler.Initialize(LinuxX11Utils.OnNativeMessage, display, window);
        }
        
        /// <inheritdoc />
        public override void Dispose()
        {
            if (mousePointer != null)
            {
                cancelPointer(mousePointer);
                mousePointer = null;
            }
            if (penPointer != null)
            {
                cancelPointer(penPointer);
                penPointer = null;
            }

            //WindowsUtils.EnableMouseInPointer(false);

            base.Dispose();
            
            pointerHandler.Dispose();
            pointerHandler = null;
        }
        
        /// <inheritdoc />
        public override bool UpdateInput()
        {
            base.UpdateInput();

            //pointerHandler.ProcessEventQueue(LinuxX11Utils.OnNativeMessage);
            
            return true;
        }

        /// <inheritdoc />
        public override bool CancelPointer(Pointer pointer, bool shouldReturn)
        {
            if (pointer.Equals(mousePointer))
            {
                cancelPointer(mousePointer);
                if (shouldReturn) mousePointer = internalReturnMousePointer(mousePointer);
                else mousePointer = internalAddMousePointer(pointer.Position); // can't totally cancel mouse pointer
                return true;
            }
            if (pointer.Equals(penPointer))
            {
                cancelPointer(penPointer);
                if (shouldReturn) penPointer = internalReturnPenPointer(penPointer);
                return true;
            }
            return base.CancelPointer(pointer, shouldReturn);
        }

        /// <inheritdoc />
        public override void INTERNAL_DiscardPointer(Pointer pointer)
        {
            if (pointer is MousePointer) mousePool.Release(pointer as MousePointer);
            else if (pointer is PenPointer) penPool.Release(pointer as PenPointer);
            else base.INTERNAL_DiscardPointer(pointer);
        }

        protected override void enablePressAndHold()
        {
            
        }

        protected override void setScaling()
        {
            int width, height;

            pointerHandler.GetNativeScreenResolution(LinuxX11Utils.OnNativeMessage, out width, out height);
            pointerHandler.SetScreenParams(LinuxX11Utils.OnNativeMessage, width, height, 0, 0, 1, 1);
        }
    }
}