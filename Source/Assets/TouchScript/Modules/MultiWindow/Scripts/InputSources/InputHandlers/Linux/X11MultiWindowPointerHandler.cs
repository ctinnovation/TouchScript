#if UNITY_STANDALONE_LINUX

using System;
using System.Collections.Generic;
using TouchScript.InputSources.InputHandlers.Interop;
using TouchScript.Pointers;
using TouchScript.Utils;
using UnityEngine;
using PointerType = TouchScript.InputSources.InputHandlers.Interop.PointerType;

namespace TouchScript.InputSources.InputHandlers
{
    sealed class X11MultiWindowPointerHandler : MultiWindowPointerHandler
    {
        private NativeX11PointerHandler pointerHandler;
        private readonly Dictionary<int, TouchPointer> x11TouchToInternalId = new Dictionary<int, TouchPointer>(10);
        
        public X11MultiWindowPointerHandler(X11PointerHandlerSystem handlerSystem, IntPtr window, PointerDelegate addPointer, PointerDelegate updatePointer,
            PointerDelegate pressPointer, PointerDelegate releasePointer, PointerDelegate removePointer,
            PointerDelegate cancelPointer)
            : base(addPointer, updatePointer, pressPointer, releasePointer, removePointer, cancelPointer)
        {
            mousePool = new ObjectPool<MousePointer>(4, () => new MousePointer(this), null, resetPointer);
            mousePointer = internalAddMousePointer(Vector3.zero);

            pointerHandler = new NativeX11PointerHandler(window, OnNativePointerEvent);
            
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

            foreach (var i in x11TouchToInternalId) cancelPointer(i.Value);
            x11TouchToInternalId.Clear();

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
            var touch = pointer as TouchPointer;
            if (touch == null) return false;

            int internalTouchId = -1;
            foreach (var t in x11TouchToInternalId)
            {
                if (t.Value == touch)
                {
                    internalTouchId = t.Key;
                    break;
                }
            }
            if (internalTouchId > -1)
            {
                cancelPointer(touch);
                x11TouchToInternalId.Remove(internalTouchId);
                if (shouldReturn) x11TouchToInternalId[internalTouchId] = internalReturnTouchPointer(touch);
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

        private void OnNativePointerEvent(int id, PointerEvent evt, PointerType type, Vector2 position, PointerData data)
        {
            switch (type)
            {
                case PointerType.Mouse:
                    switch (evt)
                    {
                        case PointerEvent.Down:
                            mousePointer.Buttons = updateButtons(mousePointer.Buttons, data.PointerFlags, data.ChangedButtons);
                            pressPointer(mousePointer);
                            break;
                        case PointerEvent.Update:
                            mousePointer.Position = position;
                            mousePointer.Buttons = updateButtons(mousePointer.Buttons, data.PointerFlags, data.ChangedButtons);
                            updatePointer(mousePointer);
                            break;
                        case PointerEvent.Up:
                            mousePointer.Buttons = updateButtons(mousePointer.Buttons, data.PointerFlags, data.ChangedButtons);
                            releasePointer(mousePointer);
                            break;
                    }
                    break;
                case PointerType.Touch:
                    TouchPointer touchPointer;
                    switch (evt)
                    {
                        case PointerEvent.Down:
                            touchPointer = internalAddTouchPointer(position);
                            touchPointer.Pressure = getTouchPressure(ref data);
                            touchPointer.Rotation = getTouchRotation(ref data);
                            x11TouchToInternalId.Add(id, touchPointer);
                            break;
                        case PointerEvent.Update:
                            if (!x11TouchToInternalId.TryGetValue(id, out touchPointer)) return;
                            touchPointer.Position = position;
                            touchPointer.Pressure = getTouchPressure(ref data);
                            touchPointer.Rotation = getTouchRotation(ref data);
                            updatePointer(touchPointer);
                            break;
                        case PointerEvent.Up:
                            if (x11TouchToInternalId.TryGetValue(id, out touchPointer))
                            {
                                x11TouchToInternalId.Remove(id);
                                internalRemoveTouchPointer(touchPointer);
                            }
                            break;
                    }
                    break;
            }
        }

        private Pointer.PointerButtonState updateButtons(Pointer.PointerButtonState current, PointerFlags flags, ButtonChangeType change)
        {
            var currentUpDown = ((uint)current) & 0xFFFFFC00;
            var pressed = ((uint) flags >> 4) & 0x1F;
            var newUpDown = 0U;
            if (change != ButtonChangeType.None) newUpDown = 1U << (10 + (int)change);
            var combined = (Pointer.PointerButtonState)(pressed | newUpDown | currentUpDown);
            return combined;
        }

        private float getTouchPressure(ref PointerData data)
        {
            // var reliable = (data.Mask & (uint) TouchMask.Pressure) > 0;
            // if (reliable) return data.Pressure / 1024f;
            return TouchPointer.DEFAULT_PRESSURE;
        }

        private float getTouchRotation(ref PointerData data)
        {
            // var reliable = (data.Mask & (uint) TouchMask.Orientation) > 0;
            // if (reliable) return data.Rotation / 180f * Mathf.PI;
            return TouchPointer.DEFAULT_ROTATION;
        }
    }
}
#endif