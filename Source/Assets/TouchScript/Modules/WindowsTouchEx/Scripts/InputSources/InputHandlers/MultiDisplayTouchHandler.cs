#if UNITY_STANDALONE_WIN

using System;
using System.Collections.Generic;
using TouchScript.InputSources.Interop;
using TouchScript.Pointers;
using TouchScript.Utils;
using TouchScript.Utils.Platform;
using UnityEngine;
using PointerType = TouchScript.InputSources.Interop.PointerType;

namespace TouchScript.InputSources.InputHandlers
{
    /// <summary>
    /// Most is copied from WindowsPointerHandler, except we try to retrieve a window for a given display.
    /// </summary>
    class MultiDisplayTouchHandler : IMultiDisplayInputHandler, IDisposable
    {
        public const string PRESS_AND_HOLD_ATOM = "MicrosoftTabletPenServiceProperty";
        
        public int TargetDisplay { get; set; }
        
        /// <inheritdoc />
        public ICoordinatesRemapper CoordinatesRemapper { get; set; }
        
        protected readonly PointerDelegate addPointer;
        protected readonly PointerDelegate updatePointer;
        protected readonly PointerDelegate pressPointer;
        protected readonly PointerDelegate releasePointer;
        protected readonly PointerDelegate removePointer;
        protected readonly PointerDelegate cancelPointer;

        private IntPtr hWindow;
        private ushort pressAndHoldAtomID;
        protected readonly Dictionary<int, TouchPointer> winTouchToInternalId = new Dictionary<int, TouchPointer>(10);

        protected ObjectPool<TouchPointer> touchPool;
        protected ObjectPool<MousePointer> mousePool;
        protected ObjectPool<PenPointer> penPool;
        protected MousePointer mousePointer;
        protected PenPointer penPointer;

        private NativePointerHandler pointerHandler;
        private MessageCallback messageCallback;
        private PointerCallback pointerCallback;
        
        public MultiDisplayTouchHandler(IntPtr hWindow, PointerDelegate addPointer, PointerDelegate updatePointer,
            PointerDelegate pressPointer, PointerDelegate releasePointer, PointerDelegate removePointer,
            PointerDelegate cancelPointer)
        {
            this.hWindow = hWindow;
            
            this.addPointer = addPointer;
            this.updatePointer = updatePointer;
            this.pressPointer = pressPointer;
            this.releasePointer = releasePointer;
            this.removePointer = removePointer;
            this.cancelPointer = cancelPointer;
            
            messageCallback = OnNativeMessage;
            pointerCallback = OnNativePointer;
            
            touchPool = new ObjectPool<TouchPointer>(10, () => new TouchPointer(this), null, resetPointer);

            pointerHandler = new NativePointerHandler();
            disablePressAndHold();
            setScaling();
        }
        
        /// <inheritdoc />
        public virtual bool UpdateInput()
        {
            return false;
        }

        /// <inheritdoc />
        public virtual void UpdateResolution()
        {
            setScaling();
            if (mousePointer != null) TouchManager.Instance.CancelPointer(mousePointer.Id);
        }

        /// <inheritdoc />
        public virtual bool CancelPointer(Pointer pointer, bool shouldReturn)
        {
            var touch = pointer as TouchPointer;
            if (touch == null) return false;

            int internalTouchId = -1;
            foreach (var t in winTouchToInternalId)
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
                winTouchToInternalId.Remove(internalTouchId);
                if (shouldReturn) winTouchToInternalId[internalTouchId] = internalReturnTouchPointer(touch);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        public virtual void Dispose()
        {
            foreach (var i in winTouchToInternalId) cancelPointer(i.Value);
            winTouchToInternalId.Clear();

            enablePressAndHold();
            
            pointerHandler.Dispose();
            pointerHandler = null;
        }
        
        /// <inheritdoc />
        public virtual void INTERNAL_DiscardPointer(Pointer pointer)
        {
            var p = pointer as TouchPointer;
            if (p == null) return;

            touchPool.Release(p);
        }

        protected void Initialize(TOUCH_API api)
        {
            pointerHandler.Initialize(messageCallback, api, hWindow, pointerCallback);
        }
        
        protected TouchPointer internalAddTouchPointer(Vector2 position)
        {
            var pointer = touchPool.Get();
            pointer.Position = remapCoordinates(position);
            pointer.Buttons |= Pointer.PointerButtonState.FirstButtonDown | Pointer.PointerButtonState.FirstButtonPressed;
            addPointer(pointer);
            pressPointer(pointer);
            return pointer;
        }

        protected TouchPointer internalReturnTouchPointer(TouchPointer pointer)
        {
            var newPointer = touchPool.Get();
            newPointer.CopyFrom(pointer);
            pointer.Buttons |= Pointer.PointerButtonState.FirstButtonDown | Pointer.PointerButtonState.FirstButtonPressed;
            newPointer.Flags |= Pointer.FLAG_RETURNED;
            addPointer(newPointer);
            pressPointer(newPointer);
            return newPointer;
        }

        protected void internalRemoveTouchPointer(TouchPointer pointer)
        {
            pointer.Buttons &= ~Pointer.PointerButtonState.FirstButtonPressed;
            pointer.Buttons |= Pointer.PointerButtonState.FirstButtonUp;
            releasePointer(pointer);
            removePointer(pointer);
        }

        protected MousePointer internalAddMousePointer(Vector2 position)
        {
            var pointer = mousePool.Get();
            pointer.Position = remapCoordinates(position);
            addPointer(pointer);
            return pointer;
        }

        protected MousePointer internalReturnMousePointer(MousePointer pointer)
        {
            var newPointer = mousePool.Get();
            newPointer.CopyFrom(pointer);
            newPointer.Flags |= Pointer.FLAG_RETURNED;
            addPointer(newPointer);
            if ((newPointer.Buttons & Pointer.PointerButtonState.AnyButtonPressed) != 0)
            {
                // Adding down state this frame
                newPointer.Buttons = PointerUtils.DownPressedButtons(newPointer.Buttons);
                pressPointer(newPointer);
            }
            return newPointer;
        }

        protected PenPointer internalAddPenPointer(Vector2 position)
        {
            if (penPointer != null) throw new InvalidOperationException("One pen pointer is already registered! Trying to add another one.");
            var pointer = penPool.Get();
            pointer.Position = remapCoordinates(position);
            addPointer(pointer);
            return pointer;
        }

        protected void internalRemovePenPointer(PenPointer pointer)
        {
            removePointer(pointer);
            penPointer = null;
        }

        protected PenPointer internalReturnPenPointer(PenPointer pointer)
        {
            var newPointer = penPool.Get();
            newPointer.CopyFrom(pointer);
            newPointer.Flags |= Pointer.FLAG_RETURNED;
            addPointer(newPointer);
            if ((newPointer.Buttons & Pointer.PointerButtonState.AnyButtonPressed) != 0)
            {
                // Adding down state this frame
                newPointer.Buttons = PointerUtils.DownPressedButtons(newPointer.Buttons);
                pressPointer(newPointer);
            }
            return newPointer;
        }
        
        protected Vector2 remapCoordinates(Vector2 position)
        {
            if (CoordinatesRemapper != null) return CoordinatesRemapper.Remap(position);
            return position;
        }

        protected void resetPointer(Pointer p)
        {
            p.INTERNAL_Reset();
        }
        
        private void disablePressAndHold()
        {
            // https://msdn.microsoft.com/en-us/library/bb969148(v=vs.85).aspx
            pressAndHoldAtomID = WindowsUtils.GlobalAddAtom(PRESS_AND_HOLD_ATOM);
            WindowsUtils.SetProp(hWindow, PRESS_AND_HOLD_ATOM,
                WindowsUtils.TABLET_DISABLE_PRESSANDHOLD | // disables press and hold (right-click) gesture
                WindowsUtils.TABLET_DISABLE_PENTAPFEEDBACK | // disables UI feedback on pen up (waves)
                WindowsUtils.TABLET_DISABLE_PENBARRELFEEDBACK | // disables UI feedback on pen button down (circle)
                WindowsUtils.TABLET_DISABLE_FLICKS // disables pen flicks (back, forward, drag down, drag up);
            );
        }

        private void enablePressAndHold()
        {
            if (pressAndHoldAtomID != 0)
            {
                WindowsUtils.RemoveProp(hWindow, PRESS_AND_HOLD_ATOM);
                WindowsUtils.GlobalDeleteAtom(pressAndHoldAtomID);
            }
        }

        private void setScaling()
        {
            // TODO not fullscreen
            if (!Screen.fullScreen)
            {
                pointerHandler.SetScreenParams(OnNativeMessage, Screen.width, Screen.height, 0, 0, 1, 1);
                return;
            }
            
            int width, height;
            WindowsUtilsEx.GetNativeMonitorResolution(hWindow, out width, out height);
            pointerHandler.SetScreenParams(OnNativeMessage, width, height,
                0, 0, 1, 1);
        }
        
        private void OnNativeMessage(int messageType, string message)
        {
            switch (messageType)
            {
                case 2:
                    Debug.LogWarning("[WindowsTouchEx.dll]: " + message);
                    break;
                case 3:
                    Debug.LogError("[WindowsTouchEx.dll]: " + message);
                    break;
                default:
                    Debug.Log("[WindowsTouchEx.dll]: " + message);
                    break;
            }
        }

        private void OnNativePointer(int id, PointerEvent evt, PointerType type, Vector2 position, PointerData data)
        {
            switch (type)
            {
                case PointerType.Mouse:
                    switch (evt)
                    {
                        // Enter and Exit are not used - mouse is always present
                        // TODO: how does it work with 2+ mice?
                        case PointerEvent.Enter:
                            throw new NotImplementedException("This is not supposed to be called o.O");
                        case PointerEvent.Leave:
                            break;
                        case PointerEvent.Down:
                            mousePointer.Buttons = updateButtons(mousePointer.Buttons, data.PointerFlags, data.ChangedButtons);
                            pressPointer(mousePointer);
                            break;
                        case PointerEvent.Up:
                            mousePointer.Buttons = updateButtons(mousePointer.Buttons, data.PointerFlags, data.ChangedButtons);
                            releasePointer(mousePointer);
                            break;
                        case PointerEvent.Update:
                            mousePointer.Position = position;
                            mousePointer.Buttons = updateButtons(mousePointer.Buttons, data.PointerFlags, data.ChangedButtons);
                            updatePointer(mousePointer);
                            break;
                        case PointerEvent.Cancelled:
                            cancelPointer(mousePointer);
                            // can't cancel the mouse pointer, it is always present
                            mousePointer = internalAddMousePointer(mousePointer.Position);
                            break;
                    }
                    break;
                case PointerType.Touch:
                    TouchPointer touchPointer;
                    switch (evt)
                    {
                        case PointerEvent.Enter:
                            break;
                        case PointerEvent.Leave:
                            // Sometimes Windows might not send Up, so have to execute touch release logic here.
                            // Has been working fine on test devices so far.
                            if (winTouchToInternalId.TryGetValue(id, out touchPointer))
                            {
                                winTouchToInternalId.Remove(id);
                                internalRemoveTouchPointer(touchPointer);
                            }
                            break;
                        case PointerEvent.Down:
                            touchPointer = internalAddTouchPointer(position);
                            touchPointer.Rotation = getTouchRotation(ref data);
                            touchPointer.Pressure = getTouchPressure(ref data);
                            winTouchToInternalId.Add(id, touchPointer);
                            break;
                        case PointerEvent.Up:
                            break;
                        case PointerEvent.Update:
                            if (!winTouchToInternalId.TryGetValue(id, out touchPointer)) return;
                            touchPointer.Position = position;
                            touchPointer.Rotation = getTouchRotation(ref data);
                            touchPointer.Pressure = getTouchPressure(ref data);
                            updatePointer(touchPointer);
                            break;
                        case PointerEvent.Cancelled:
                            if (winTouchToInternalId.TryGetValue(id, out touchPointer))
                            {
                                winTouchToInternalId.Remove(id);
                                cancelPointer(touchPointer);
                            }
                            break;
                    }
                    break;
                case PointerType.Pen:
                    switch (evt)
                    {
                        case PointerEvent.Enter:
                            penPointer = internalAddPenPointer(position);
                            penPointer.Pressure = getPenPressure(ref data);
                            penPointer.Rotation = getPenRotation(ref data);
                            break;
                        case PointerEvent.Leave:
                            if (penPointer == null) break;
                            internalRemovePenPointer(penPointer);
                            break;
                        case PointerEvent.Down:
                            if (penPointer == null) break;
                            penPointer.Buttons = updateButtons(penPointer.Buttons, data.PointerFlags, data.ChangedButtons);
                            penPointer.Pressure = getPenPressure(ref data);
                            penPointer.Rotation = getPenRotation(ref data);
                            pressPointer(penPointer);
                            break;
                        case PointerEvent.Up:
                            if (penPointer == null) break;
                            mousePointer.Buttons = updateButtons(penPointer.Buttons, data.PointerFlags, data.ChangedButtons);
                            releasePointer(penPointer);
                            break;
                        case PointerEvent.Update:
                            if (penPointer == null) break;
                            penPointer.Position = position;
                            penPointer.Pressure = getPenPressure(ref data);
                            penPointer.Rotation = getPenRotation(ref data);
                            penPointer.Buttons = updateButtons(penPointer.Buttons, data.PointerFlags, data.ChangedButtons);
                            updatePointer(penPointer);
                            break;
                        case PointerEvent.Cancelled:
                            if (penPointer == null) break;
                            cancelPointer(penPointer);
                            break;
                    }
                    break;
            }
        }
        
        private Pointer.PointerButtonState updateButtons(Pointer.PointerButtonState current, PointerFlags flags, ButtonChangeType change)
        {
            var currentUpDown = ((uint) current) & 0xFFFFFC00;
            var pressed = ((uint) flags >> 4) & 0x1F;
            var newUpDown = 0U;
            if (change != ButtonChangeType.None) newUpDown = 1U << (10 + (int) change);
            var combined = (Pointer.PointerButtonState) (pressed | newUpDown | currentUpDown);
            return combined;
        }

        private float getTouchPressure(ref PointerData data)
        {
            var reliable = (data.Mask & (uint) TouchMask.Pressure) > 0;
            if (reliable) return data.Pressure / 1024f;
            return TouchPointer.DEFAULT_PRESSURE;
        }

        private float getTouchRotation(ref PointerData data)
        {
            var reliable = (data.Mask & (uint) TouchMask.Orientation) > 0;
            if (reliable) return data.Rotation / 180f * Mathf.PI;
            return TouchPointer.DEFAULT_ROTATION;
        }

        private float getPenPressure(ref PointerData data)
        {
            var reliable = (data.Mask & (uint) PenMask.Pressure) > 0;
            if (reliable) return data.Pressure / 1024f;
            return PenPointer.DEFAULT_PRESSURE;
        }

        private float getPenRotation(ref PointerData data)
        {
            var reliable = (data.Mask & (uint) PenMask.Rotation) > 0;
            if (reliable) return data.Rotation / 180f * Mathf.PI;
            return PenPointer.DEFAULT_ROTATION;
        }
    }
}

#endif