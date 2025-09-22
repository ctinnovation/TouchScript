/*
 * @author Valentin Simonov / http://va.lent.in/
 * @author Valentin Frolov
 * @author Andrew David Griffiths
 */

#if UNITY_STANDALONE_WIN

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TouchScript.Pointers;
using TouchScript.Utils;
using TouchScript.Utils.Platform;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers
{
    /// <summary>
    /// Windows 8 pointer handling implementation which can be embedded to other (input) classes. Uses WindowsTouch.dll to query native touches with WM_TOUCH or WM_POINTER APIs.
    /// </summary>
    public class Windows8PointerHandler : WindowsPointerHandler
    {
        #region Public properties

        /// <summary>
        /// Should the primary pointer also dispatch a mouse pointer.
        /// </summary>
        public bool MouseInPointer
        {
            get { return mouseInPointer; }
            set
            {
                WindowsUtils.EnableMouseInPointer(value);
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

        #endregion

        #region Private variables

        private bool mouseInPointer = true;

        #endregion

        #region Constructor

        /// <inheritdoc />
        public Windows8PointerHandler(PointerDelegate addPointer, PointerDelegate updatePointer, PointerDelegate pressPointer, PointerDelegate releasePointer, PointerDelegate removePointer, PointerDelegate cancelPointer) : base(addPointer, updatePointer, pressPointer, releasePointer, removePointer, cancelPointer)
        {
            mousePool = new ObjectPool<MousePointer>(4, () => new MousePointer(this), null, resetPointer);
            penPool = new ObjectPool<PenPointer>(2, () => new PenPointer(this), null, resetPointer);

            mousePointer = internalAddMousePointer(Vector3.zero);

            init(TOUCH_API.WIN8);
        }

        #endregion

        #region Public methods

        /// <inheritdoc />
        public override bool UpdateInput()
        {
            base.UpdateInput();
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

            WindowsUtils.EnableMouseInPointer(false);

            base.Dispose();
        }

        #endregion

        #region Internal methods

        /// <inheritdoc />
        public override void INTERNAL_DiscardPointer(Pointer pointer)
        {
            if (pointer is MousePointer) mousePool.Release(pointer as MousePointer);
            else if (pointer is PenPointer) penPool.Release(pointer as PenPointer);
            else base.INTERNAL_DiscardPointer(pointer);
        }

        #endregion
    }

    public class Windows7PointerHandler : WindowsPointerHandler
    {
        /// <inheritdoc />
        public Windows7PointerHandler(PointerDelegate addPointer, PointerDelegate updatePointer, PointerDelegate pressPointer, PointerDelegate releasePointer, PointerDelegate removePointer, PointerDelegate cancelPointer) : base(addPointer, updatePointer, pressPointer, releasePointer, removePointer, cancelPointer)
        {
            init(TOUCH_API.WIN7);
        }

        #region Public methods

        /// <inheritdoc />
        public override bool UpdateInput()
        {
            base.UpdateInput();
            return winTouchToInternalId.Count > 0;
        }

        #endregion
    }

    /// <summary>
    /// Base class for Windows 8 and Windows 7 input handlers.
    /// </summary>
    public abstract class WindowsPointerHandler : IInputSource, IDisposable
    {
        #region Consts

        /// <summary>
        /// Windows constant to turn off press and hold visual effect.
        /// </summary>
        public const string PRESS_AND_HOLD_ATOM = "MicrosoftTabletPenServiceProperty";

        /// <summary>
        /// The method delegate used to pass data from the native DLL.
        /// </summary>
        /// <param name="id">Pointer id.</param>
        /// <param name="evt">Current event.</param>
        /// <param name="type">Pointer type.</param>
        /// <param name="position">Pointer position.</param>
        /// <param name="data">Pointer data.</param>
        protected delegate void NativePointerDelegate(int id, PointerEvent evt, PointerType type, Vector2 position, PointerData data);

        /// <summary>
        /// The method delegate used to pass log messages from the native DLL.
        /// </summary>
        /// <param name="log">The log message.</param>
        protected delegate void NativeLog([MarshalAs(UnmanagedType.BStr)] string log);

        #endregion

        #region Public properties

        /// <inheritdoc />
        public ICoordinatesRemapper CoordinatesRemapper { get; set; }

        public bool WindowsGesturesManagement
        {
            get { return windowsGesturesManagement; }
            set
            {
                windowHandles.Clear();
                windowsGesturesManagement = value;
            }
        }

        #endregion

        #region Private variables

        private NativePointerDelegate nativePointerDelegate;
        private NativeLog nativeLogDelegate;
        private bool windowsGesturesManagement = true;

        protected PointerDelegate addPointer;
        protected PointerDelegate updatePointer;
        protected PointerDelegate pressPointer;
        protected PointerDelegate releasePointer;
        protected PointerDelegate removePointer;
        protected PointerDelegate cancelPointer;

        /// <summary>
        /// Maps the window handle to its pressAndHoldAtomID property
        /// </summary>
        protected List<(IntPtr, ushort)> windowHandles = new();
        protected IntPtr hMainWindow;
        protected ushort pressAndHoldAtomID;
        protected Dictionary<int, TouchPointer> winTouchToInternalId = new(10);

        protected ObjectPool<TouchPointer> touchPool;
        protected ObjectPool<MousePointer> mousePool;
        protected ObjectPool<PenPointer> penPool;
        protected MousePointer mousePointer;
        protected PenPointer penPointer;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsPointerHandler"/> class.
        /// </summary>
        /// <param name="addPointer">A function called when a new pointer is detected.</param>
        /// <param name="updatePointer">A function called when a pointer is moved or its parameter is updated.</param>
        /// <param name="pressPointer">A function called when a pointer touches the surface.</param>
        /// <param name="releasePointer">A function called when a pointer is lifted off.</param>
        /// <param name="removePointer">A function called when a pointer is removed.</param>
        /// <param name="cancelPointer">A function called when a pointer is cancelled.</param>
        public WindowsPointerHandler(PointerDelegate addPointer, PointerDelegate updatePointer, PointerDelegate pressPointer, PointerDelegate releasePointer, PointerDelegate removePointer, PointerDelegate cancelPointer)
        {
            this.addPointer = addPointer;
            this.updatePointer = updatePointer;
            this.pressPointer = pressPointer;
            this.releasePointer = releasePointer;
            this.removePointer = removePointer;
            this.cancelPointer = cancelPointer;

            nativeLogDelegate = nativeLog;
            nativePointerDelegate = nativePointer;

            touchPool = new ObjectPool<TouchPointer>(10, () => new TouchPointer(this), null, resetPointer);
            setScaling();
        }

        #endregion

        #region Public methods

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

            var internalTouchId = -1;
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

        public virtual void UpdateWindowsInput(IntPtr[] hwnds) => UpdateWindowsInput(hwnds, windowHandles);

        public static void UpdateWindowsInput(IntPtr[] hwnds, List<(IntPtr, ushort)> windowHandles)
        {
            Debug.LogError($"UpdateWindowsInput {hwnds.Length} is it? {TouchManager.Instance is MonoBehaviour}");
#if !UNITY_EDITOR
#pragma warning disable CS4014
            if (TouchManager.Instance is MonoBehaviour touchManagerGo)
            {
                touchManagerGo.StartCoroutine(
                    setTouchSettingToWindowCo(() =>
                    {
                        for (int k = 0; k < windowHandles.Count; k++)
                        {
                            ResetTouchSettingToWindow(windowHandles[k].Item1, windowHandles[k].Item2);
                            windowHandles.Remove(windowHandles[k]);
                        }
                        for (int k = 0; k < hwnds.Length; k++)
                        {
                            applyTouchSettingToWindow(hwnds[k], out ushort pressAndHoldAtomID);
                            windowHandles.Add(new(hwnds[k], pressAndHoldAtomID));
                        }
                    })
                );
#pragma warning restore CS4014
            }
#endif
        }

        public static void ResetTouchSettingToWindow(IntPtr hwnd, ushort pressAndHoldAtomID)
        {
            Debug.Log($"[{nameof(WindowsPointerHandler)}] {nameof(ResetTouchSettingToWindow)}: {hwnd.ToString("X")}");

            enableTap(hwnd);
            enableDoubleTap(hwnd);
            enablePressAndTap(hwnd);
            enableRightTap(hwnd);
            enablePressAndHold(hwnd, pressAndHoldAtomID);
            enableEdgeGestures(hwnd);
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        public virtual void Dispose()
        {
            foreach (var i in winTouchToInternalId) cancelPointer(i.Value);
            winTouchToInternalId.Clear();

#if !UNITY_EDITOR
            // it doesn't need the configuration parameter check since if it is disabled "windowHandles" is empty
            foreach (var h in windowHandles) ResetTouchSettingToWindow(h.Item1, h.Item2);
#endif

            DisposePlugin();
        }

        #endregion

        #region Internal methods

        /// <inheritdoc />
        public virtual void INTERNAL_DiscardPointer(Pointer pointer)
        {
            var p = pointer as TouchPointer;
            if (p == null) return;

            touchPool.Release(p);
        }

        #endregion

        #region Protected methods

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

        protected void init(TOUCH_API api)
        {
            Init(api, nativeLogDelegate, nativePointerDelegate);
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

        #endregion

        #region Private functions

        private static void applyTouchSettingToWindow(IntPtr hwnd, out ushort pressAndHoldAtomID)
        {
            Debug.Log($"[{nameof(WindowsPointerHandler)}] {nameof(applyTouchSettingToWindow)}: {hwnd.ToString("X")}");

            disableTap(hwnd);
            disableDoubleTap(hwnd);
            disablePressAndTap(hwnd);
            disableRightTap(hwnd);
            disablePressAndHold(hwnd, out pressAndHoldAtomID);
            disableEdgeGestures(hwnd);
        }

        private static void disableTap(IntPtr hwnd)
        {
            setWindowFeedbackSetting(hwnd, FeedbackType.FeedbackTouchContactVisualization, false);
            setWindowFeedbackSetting(hwnd, FeedbackType.FeedbackTouchTap, false);
        }

        private static void disableDoubleTap(IntPtr hwnd) => setWindowFeedbackSetting(hwnd, FeedbackType.FeedbackTouchDoubleTap, false);

        private static void disablePressAndTap(IntPtr hwnd) => setWindowFeedbackSetting(hwnd, FeedbackType.FeedbackGesturePressAndTap, false);

        private static void disableRightTap(IntPtr hwnd) => setWindowFeedbackSetting(hwnd, FeedbackType.FeedbackTouchRightTap, false);

        private static void disablePressAndHold(IntPtr hwnd, out ushort pressAndHoldAtomID)
        {
            // https://msdn.microsoft.com/en-us/library/bb969148(v=vs.85).aspx
            pressAndHoldAtomID = WindowsUtils.GlobalAddAtom(PRESS_AND_HOLD_ATOM);
            WindowsUtils.SetProp(hwnd, PRESS_AND_HOLD_ATOM,
                WindowsUtils.TABLET_DISABLE_PRESSANDHOLD | // disables press and hold (right-click) gesture
                WindowsUtils.TABLET_DISABLE_PENTAPFEEDBACK | // disables UI feedback on pen up (waves)
                WindowsUtils.TABLET_DISABLE_PENBARRELFEEDBACK | // disables UI feedback on pen button down (circle)
                WindowsUtils.TABLET_DISABLE_FLICKS // disables pen flicks (back, forward, drag down, drag up);
                );

            setWindowFeedbackSetting(hwnd, FeedbackType.FeedbackTouchPressAndHold, false);
        }

        private static void disableEdgeGestures(IntPtr hwnd)
        {
            var hr = WindowsUtils.SHGetPropertyStoreForWindow(hwnd, ref IID_IPropertyStore, out var propStore);
            if (hr != 0 || propStore == null)
            {
                Debug.LogWarning($"Cannot retrieve the property store for window named \"{nameof(DISABLE_TOUCH_WHEN_FULLSCREEN)}\"");
                return;
            }

            var key = new WindowsUtils.PropertyKey { fmtid = DISABLE_TOUCH_WHEN_FULLSCREEN, pid = 2 };
            var value = new WindowsUtils.PropVariant { vt = VT_BOOL, boolVal = -1 }; // -1 = TRUE, 0 = FALSE

            propStore.SetValue(ref key, ref value);
            propStore.Commit(); // shouldn't be needed

            Marshal.ReleaseComObject(propStore);
        }

        private static void enableTap(IntPtr hwnd)
        {
            setWindowFeedbackSetting(hwnd, FeedbackType.FeedbackTouchContactVisualization, true);
            setWindowFeedbackSetting(hwnd, FeedbackType.FeedbackTouchTap, true);
        }

        private static void enableDoubleTap(IntPtr hwnd) => setWindowFeedbackSetting(hwnd, FeedbackType.FeedbackTouchDoubleTap, true);

        private static void enablePressAndTap(IntPtr hwnd) => setWindowFeedbackSetting(hwnd, FeedbackType.FeedbackGesturePressAndTap, true);

        private static void enableRightTap(IntPtr hwnd) => setWindowFeedbackSetting(hwnd, FeedbackType.FeedbackTouchRightTap, true);

        private static void enablePressAndHold(IntPtr hwnd, ushort pressAndHoldAtomID)
        {
            if (pressAndHoldAtomID != 0)
            {
                WindowsUtils.RemoveProp(hwnd, PRESS_AND_HOLD_ATOM);
                WindowsUtils.GlobalDeleteAtom(pressAndHoldAtomID);
            }

            setWindowFeedbackSetting(hwnd, FeedbackType.FeedbackTouchPressAndHold, true);
        }

        private static void enableEdgeGestures(IntPtr hwnd)
        {
            var hr = WindowsUtils.SHGetPropertyStoreForWindow(hwnd, ref IID_IPropertyStore, out var propStore);
            if (hr != 0 || propStore == null)
            {
                Debug.LogWarning($"Cannot retrieve the property store for window named \"{nameof(DISABLE_TOUCH_WHEN_FULLSCREEN)}\"");
                return;
            }

            var key = new WindowsUtils.PropertyKey { fmtid = DISABLE_TOUCH_WHEN_FULLSCREEN, pid = 2 };
            var value = new WindowsUtils.PropVariant { vt = VT_EMPTY };

            propStore.SetValue(ref key, ref value);
            propStore.Commit(); // shouldn't be needed

            Marshal.ReleaseComObject(propStore);
        }

        private static void setWindowFeedbackSetting(IntPtr hwnd, FeedbackType feedback, bool enable)
        {
            var settings = new WindowsUtils.FeedbackTypeSettings { Enable = enable };

            var size = Marshal.SizeOf(settings);
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(settings, ptr, false);

            var result = WindowsUtils.SetWindowFeedbackSetting(hwnd, (uint)feedback, 0, (uint)size, ptr);
            if (!result)
            {
                Debug.LogWarning(
                    $"Cannot change the window feedback setting named {Enum.GetName(typeof(FeedbackType), feedback)}, " +
                    $"Win32Error: {Marshal.GetLastWin32Error().ToString("X")}");
            }

            Marshal.FreeHGlobal(ptr);
        }

        private static IEnumerator setTouchSettingToWindowCo(Action action)
        {
            // [https://ctinnovation.atlassian.net/browse/KALI-7539?atlOrigin=eyJpIjoiODE3NTQ5MmY0YTk0NDk1ZGI4MjI2NjY4NTAzMjRjYzkiLCJwIjoiaiJ9]
            // Lo scopo � "consumare" il touch input gestito da Windows senza che questi
            // interferisca con gesture particolari impostate a livello utente nei Windows Settings
            // In Windows 11 sono state introdotte impostazioni in merito alle gestures touch (edge gestures, 3-4 fingers gestures)
            // che, se abilitate, pregiudicano l'utilizzo di app touch dato che queste vengono gestite direttamente dal sistema operativo
            // Da documentazione Microsoft [https://learn.microsoft.com/en-us/windows/apps/design/input/touch-developer-guide#custom-touch-interactions]
            // non vi � alcun modo "ufficiale" per risolvere il problema a meno della disattivazione da parte dell'utente di queste impostazioni
            // Nonostante ci�, si � trovato un metodo "non ufficiale" che sfrutta API di "shell32.dll" per "simulare" l'utilizzo di Theater
            // in un ambiente Windows di tipo "kiosk"
            // In particolare, andiamo a settare la property [https://learn.microsoft.com/en-us/windows/win32/properties/props-system-edgegesture-disabletouchwhenfullscreen]
            // che disabilita le "Edge Gestures" solo per le Window in Fullscreen.
            // Come effetto collaterale si ottiene anche la disabilitazione delle "3-4 fingers Gestures" solo quando la Window � in focus ed in Foreground
            // e solo in seguito:
            // - ad uno "scambio" di focus tra Theater ed un'altra Window (nello stesso Display)
            // - oppure ad un cambiamento di modalit� della Window da "Windowed" a "FullscreenWindow"
            // Nel primo caso abbiamo il problema che le "3-4 fingers gestures" non vengono disattivate all'avvio dell'app anche se la Window � in Focus ed in Foreground,
            // nel secondo caso invece, forzando il cambio di FullscreenMode, riusciamo sempre ad applicare alla Window l'effetto collaterale
            // Utilizziamo quindi il secondo caso (ottenibile programmaticamente e riguardante solo l'app) stando attenti che Unity
            // permette il cambio di FullscreenMode anche tramite combinazione di tasti ATL + ENTER e che salva il nuovo valore di FullscreenMode
            // nel registro di Windows al path "HKEY_CURRENT_USER\Software\[CompanyName]\[ProductName]\Screenmanager Fullscreen mode_h3630240806"

            Screen.fullScreen = false;
            yield return null;
            Screen.SetResolution(Screen.mainWindowDisplayInfo.width - 1, Screen.mainWindowDisplayInfo.height - 1, FullScreenMode.Windowed);
            yield return null;

            action?.Invoke();

            Screen.fullScreen = true;
            yield return null;
            Screen.SetResolution(Screen.mainWindowDisplayInfo.width, Screen.mainWindowDisplayInfo.height, FullScreenMode.FullScreenWindow);

            setScaling();
        }

        private static void setScaling()
        {
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;

            if (!Screen.fullScreen)
            {
                SetScreenParams(screenWidth, screenHeight, 0, 0, 1, 1);
                return;
            }

            WindowsUtils.GetNativeMonitorResolution(out var width, out var height);
            var scale = Mathf.Max(screenWidth / ((float) width), screenHeight / ((float) height));
            SetScreenParams(screenWidth, screenHeight, (width - screenWidth / scale) * .5f, (height - screenHeight / scale) * .5f, scale, scale);
        }

        #endregion

        #region Pointer callbacks

        private void nativeLog(string log)
        {
            Debug.Log("[WindowsTouch.dll]: " + log);
        }

        private void nativePointer(int id, PointerEvent evt, PointerType type, Vector2 position, PointerData data)
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

        #endregion

        #region p/invoke
        /// <summary>
        /// Windows property store guid to turn off edge gestures and 3-4 fingers gestures
        /// </summary>
        private static readonly Guid DISABLE_TOUCH_WHEN_FULLSCREEN = new("32CE38B2-2C9A-41B1-9BC5-B3784394AA44");
        private static Guid IID_IPropertyStore = new("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
        private const short VT_BOOL = 11;
        private const short VT_EMPTY = 0;

        protected enum TOUCH_API
        {
            WIN7,
            WIN8
        }

        protected enum PointerEvent : uint
        {
            Enter = 0x0249,
            Leave = 0x024A,
            Update = 0x0245,
            Down = 0x0246,
            Up = 0x0247,
            Cancelled = 0x1000
        }

        protected enum PointerType
        {
            Pointer = 0x00000001,
            Touch = 0x00000002,
            Pen = 0x00000003,
            Mouse = 0x00000004,
            TouchPad = 0x00000005
        }

        [Flags]
        protected enum PointerFlags
        {
            None = 0x00000000,
            New = 0x00000001,
            InRange = 0x00000002,
            InContact = 0x00000004,
            FirstButton = 0x00000010,
            SecondButton = 0x00000020,
            ThirdButton = 0x00000040,
            FourthButton = 0x00000080,
            FifthButton = 0x00000100,
            Primary = 0x00002000,
            Confidence = 0x00004000,
            Canceled = 0x00008000,
            Down = 0x00010000,
            Update = 0x00020000,
            Up = 0x00040000,
            Wheel = 0x00080000,
            HWheel = 0x00100000,
            CaptureChanged = 0x00200000,
            HasTransform = 0x00400000
        }

        protected enum ButtonChangeType
        {
            None,
            FirstDown,
            FirstUp,
            SecondDown,
            SecondUp,
            ThirdDown,
            ThirdUp,
            FourthDown,
            FourthUp,
            FifthDown,
            FifthUp
        }

        [Flags]
        protected enum TouchFlags
        {
            None = 0x00000000
        }

        [Flags]
        protected enum TouchMask
        {
            None = 0x00000000,
            ContactArea = 0x00000001,
            Orientation = 0x00000002,
            Pressure = 0x00000004
        }

        [Flags]
        protected enum PenFlags
        {
            None = 0x00000000,
            Barrel = 0x00000001,
            Inverted = 0x00000002,
            Eraser = 0x00000004
        }

        [Flags]
        protected enum PenMask
        {
            None = 0x00000000,
            Pressure = 0x00000001,
            Rotation = 0x00000002,
            TiltX = 0x00000004,
            TiltY = 0x00000008
        }

        private enum FeedbackType : uint
        {
            FeedbackTouchContactVisualization = 1,
            FeedbackPenBarrelVisualization = 2,
            FeedbackPenTap = 3,
            FeedbackDoubleTap = 4,
            FeedbackPenPressAndHold = 5,
            FeedbackPenRightTap = 6,
            FeedbackTouchTap = 7,
            FeedbackTouchDoubleTap = 8,
            FeedbackTouchPressAndHold = 9,
            FeedbackTouchRightTap = 10,
            FeedbackGesturePressAndTap = 11,
            FeedbackMax = 0xFFFFFFFF
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct PointerData
        {
            public PointerFlags PointerFlags;
            public uint Flags;
            public uint Mask;
            public ButtonChangeType ChangedButtons;
            public uint Rotation;
            public uint Pressure;
            public int TiltX;
            public int TiltY;
        }

        [DllImport("WindowsTouch", CallingConvention = CallingConvention.StdCall)]
        private static extern void Init(TOUCH_API api, NativeLog log, NativePointerDelegate pointerDelegate);

        [DllImport("WindowsTouch", EntryPoint = "Dispose", CallingConvention = CallingConvention.StdCall)]
        private static extern void DisposePlugin();

        [DllImport("WindowsTouch", CallingConvention = CallingConvention.StdCall)]
        private static extern void SetScreenParams(int width, int height, float offsetX, float offsetY, float scaleX, float scaleY);

        #endregion
    }
}

#endif