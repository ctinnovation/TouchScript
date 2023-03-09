#if UNITY_STANDALONE_LINUX

using System;
using TouchScript.InputSources.InputHandlers.Interop;
using TouchScript.Pointers;
using TouchScript.Utils;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers
{
    sealed class X11MultiWindowPointerHandler : MultiWindowPointerHandler
    {
        private NativeX11PointerHandler pointerHandler;
        
        public X11MultiWindowPointerHandler(X11PointerSystem system, IntPtr window, PointerDelegate addPointer, PointerDelegate updatePointer,
            PointerDelegate pressPointer, PointerDelegate releasePointer, PointerDelegate removePointer,
            PointerDelegate cancelPointer)
            : base(addPointer, updatePointer, pressPointer, releasePointer, removePointer, cancelPointer)
        {
            mousePool = new ObjectPool<MousePointer>(4, () => new MousePointer(this), null, resetPointer);
            penPool = new ObjectPool<PenPointer>(2, () => new PenPointer(this), null, resetPointer);

            mousePointer = internalAddMousePointer(Vector3.zero);

            pointerHandler = new NativeX11PointerHandler(system, window, OnNativePointerEvent);
            
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

            pointerHandler.GetScreenResolution(out width, out height);
            pointerHandler.SetScreenParams(width, height, 0, 0, 1, 1);
        }

        private void OnNativePointerEvent()
        {
            
        }
    }
}
#endif