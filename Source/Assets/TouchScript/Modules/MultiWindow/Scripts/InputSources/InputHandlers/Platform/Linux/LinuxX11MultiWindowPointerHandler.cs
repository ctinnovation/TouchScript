#if UNITY_STANDALONE_LINUX

using System;
using TouchScript.InputSources.InputHandlers.Interop;
using TouchScript.Pointers;
using TouchScript.Utils;
using TouchScript.Utils.Platform;
using TouchScript.Utils.Platform.Interop;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers
{
    sealed class LinuxX11MultiWindowPointerHandler : MultiWindowPointerHandler
    {
        // /// <summary>
        // /// Should the primary pointer also dispatch a mouse pointer.
        // /// </summary>
        // public bool MouseInPointer
        // {
        //     get { return mouseInPointer; }
        //     set
        //     {
        //         //WindowsUtils.EnableMouseInPointer(value);
        //         
        //         mouseInPointer = value;
        //         if (mouseInPointer)
        //         {
        //             if (mousePointer == null) mousePointer = internalAddMousePointer(Vector3.zero);
        //         }
        //         else
        //         {
        //             if (mousePointer != null)
        //             {
        //                 if ((mousePointer.Buttons & Pointer.PointerButtonState.AnyButtonPressed) != 0)
        //                 {
        //                     mousePointer.Buttons = PointerUtils.UpPressedButtons(mousePointer.Buttons);
        //                     releasePointer(mousePointer);
        //                 }
        //                 removePointer(mousePointer);
        //             }
        //         }
        //     }
        // }
        //
        // private bool mouseInPointer = true;

        private NativeX11PointerHandler pointerHandler;
        private readonly MessageCallback messageCallback;
        private readonly TouchEventCallback touchEventCallback;
        
        public LinuxX11MultiWindowPointerHandler(IntPtr display, IntPtr window, PointerDelegate addPointer,
            PointerDelegate updatePointer,
            PointerDelegate pressPointer, PointerDelegate releasePointer, PointerDelegate removePointer,
            PointerDelegate cancelPointer)
            : base(addPointer, updatePointer, pressPointer, releasePointer, removePointer, cancelPointer)
        {
            mousePool = new ObjectPool<MousePointer>(4, () => new MousePointer(this), null, resetPointer);
            penPool = new ObjectPool<PenPointer>(2, () => new PenPointer(this), null, resetPointer);

            mousePointer = internalAddMousePointer(Vector3.zero);

            messageCallback = LinuxX11Utils.OnNativeMessage;
            touchEventCallback = onNativeTouchEvent;
            
            pointerHandler = new NativeX11PointerHandler();
            pointerHandler.Initialize(messageCallback, display, window);
            
            disablePressAndHold();
            setScaling();
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

            enablePressAndHold();
            
            pointerHandler.Dispose();
            pointerHandler = null;
        }
        
        /// <inheritdoc />
        public override bool UpdateInput()
        {
            pointerHandler.ProcessEventQueue(messageCallback, touchEventCallback);
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

            return false;
        }

        /// <inheritdoc />
        public override void INTERNAL_DiscardPointer(Pointer pointer)
        {
            if (pointer is MousePointer) mousePool.Release(pointer as MousePointer);
            else if (pointer is PenPointer) penPool.Release(pointer as PenPointer);
            else base.INTERNAL_DiscardPointer(pointer);
        }

        protected void disablePressAndHold()
        {
            
        }

        protected void enablePressAndHold()
        {
            
        }

        protected override void setScaling()
        {
            int width, height;

            pointerHandler.GetScreenResolution(messageCallback, out width, out height);
            pointerHandler.SetScreenParams(messageCallback, width, height, 0, 0, 1, 1);
        }

        private void onNativeTouchEvent()
        {
            
        }
    }
}
#endif