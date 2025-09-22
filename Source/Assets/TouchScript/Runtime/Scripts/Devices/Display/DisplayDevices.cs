using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TouchScript.Utils.Platform;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TouchScript.Devices.Display
{
    using Display = UnityEngine.Display;

    public class DisplayDevices
    {
        private static DisplayDevices _instance;

        private int processID = -1;
        private (Display, bool)[] _displays;
        private IntPtr[] _windowHandles = new IntPtr[0];
        private bool _checkForWindows;

        public Action<Display[]> OnDisplaysConnected;
        public Action<Display[]> OnDisplaysDisconnected;
        //public Action<Display[]> OnDisplaysDeactivated;  // impossibile, Unity non lo permette
        public Action<Display[]> OnDisplaysActivated;
        public Action<IntPtr[]> OnWindowsDeactivated;
        public Action<IntPtr[]> OnWindowsActivated;

        public static DisplayDevices Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DisplayDevices();
                }

                return _instance;
            }
        }
        public IntPtr[] WindowHandles => _windowHandles;

        public DisplayDevices()
        {
            processID = -1;
            _displays = null;
            _windowHandles = new IntPtr[0];
            _checkForWindows = false;
            OnDisplaysConnected = null;
            OnDisplaysDisconnected = null;
            //OnDisplaysDeactivated = null;
            OnDisplaysActivated = null;
            OnWindowsDeactivated = null;
            OnWindowsActivated = null;
        }

        public void Init()
        {
            processID = Process.GetCurrentProcess().Id;
            onDisplaysUpdated();
            // a causa di un bug di Unity, questo evento non serve a niente dopo la fase di boot dell'app
            // manteniamo cmq la logica in caso di un fix
            // https://issuetracker.unity3d.com/issues/display-dot-displays-does-not-update-when-connecting-slash-disconnecting-an-external-display-on-a-build
            Display.onDisplaysUpdated += onDisplaysUpdated;
        }

        /// <summary>
        /// Invoked when Unity detects that a new display has been connected (not necessarily activated)<br/>
        /// It retrieves all the connected displays and notifies the connection or disconnection of any of them. 
        /// </summary>
        private void onDisplaysUpdated()
        {
            var newDisplays = GetConnectedDisplays();

            if (newDisplays == null && _displays == null)
            {
                // no displays connected
                // impossible
            }
            else if (newDisplays == null && _displays != null)
            {
                // no more displays connected
                var disconnected = _displays.Select(d => d.Item1).ToArray();

                for (var k = 0; k < disconnected.Length; k++) Debug.Log($"Display ({k}) disconnected: {disconnected[k].systemWidth}x{disconnected[k].systemHeight}");

                OnDisplaysDisconnected?.Invoke(disconnected);
                _displays = null;
            }
            else if (newDisplays != null && _displays == null)
            {
                // (init) displays connected
                // we want to be able to notify the activation state of each display connected and active at start
                // through the OnWindowsActivated action despite this being the class initialization
                _displays = newDisplays.Select(d => (d, false)).ToArray();

                for (var k = 0; k < _displays.Length; k++) Debug.Log($"Display ({k}) connected: {_displays[k].Item1.systemWidth}x{_displays[k].Item1.systemHeight}");

                OnDisplaysConnected?.Invoke(_displays.Select(d => d.Item1).ToArray());
            }
            else
            {
                // displays connected
                var toRemove = new List<Display>();
                for (var i = 0; i < _displays.Length; i++)
                {
                    var removed = true;
                    for (var j = 0; j < newDisplays.Length; j++)
                    {
                        if (_displays[i].Item1 == newDisplays[j])
                        {
                            removed = false;
                            break;
                        }
                    }

                    if (removed)
                    {
                        toRemove.Add(_displays[i].Item1);

                        Debug.Log($"Display ({i}) disconnected: {_displays[i].Item1.systemWidth}x{_displays[i].Item1.systemHeight}");
                    }
                }

                var toAdd = new List<Display>();
                for (var i = 0; i < newDisplays.Length; i++)
                {
                    var added = true;
                    for (var j = 0; j < _displays.Length; j++)
                    {
                        if (_displays[j].Item1 == newDisplays[i])
                        {
                            added = false;
                            break;
                        }
                    }

                    if (added)
                    {
                        toAdd.Add(newDisplays[i]);

                        Debug.Log($"Display ({i}) connected: {newDisplays[i].systemWidth}x{newDisplays[i].systemHeight}");
                    }
                }

                _displays = newDisplays.Select(d => (d, d.active)).ToArray();
                OnDisplaysDisconnected?.Invoke(toRemove.ToArray());
                OnDisplaysConnected?.Invoke(toAdd.ToArray());
            }
        }

        /// <summary>
        /// Invoked when we detect that a new display (previously connected) has now been activated<br/>
        /// Checks if the build or UnityEditor process window hierarchy has been modified.<br/>
        /// It retrieves all the window handles associated to every <c>Display.Activate</c> and notifies the creation or destruction of any of them. 
        /// </summary>
        /// <returns>True if at least a window associated to a <c>Display.Activate</c> has been created or destroyed, False otherwise</returns>
        private bool onWindowsUpdated()
        {
            var precWindowHandles = (IntPtr[])_windowHandles.Clone();

#if UNITY_EDITOR
            var windows = WindowsUtils.GetGameWindowHandlesForEditorProcess(processID);
#else
            var windows = WindowsUtils.GetMainWindowHandlesForBuildProcess(processID);
#endif
            if (windows != null)
            {
                // handle delle window rimosse
                var toRemove = new List<IntPtr>();
                for (var j = 0; j < _windowHandles.Length; j++)
                {
                    var removed = true;
                    for (var k = 0; k < windows.Count; k++)
                    {
                        if (windows[k] == _windowHandles[j])
                        {
                            removed = false;
                            break;
                        }
                    }

                    if (removed)
                    {
                        toRemove.Add(_windowHandles[j]);

                        Debug.Log($"Window deactivated: {_windowHandles[j].ToString("X")}");
                    }
                }
                // aggiungo gli handle delle window aggiunte e attivate
                var toAdd = new List<IntPtr>();
                for (var j = 0; j < windows.Count; j++)
                {
                    var added = true;
                    for (var k = 0; k < _windowHandles.Length; k++)
                    {
                        if (windows[j] == _windowHandles[k])
                        {
                            added = false;
                            break;
                        }
                    }

                    if (added)
                    {
                        toAdd.Add(windows[j]);

                        Debug.Log($"Window activated: {windows[j].ToString("X")}");
                    }
                }

                _windowHandles = windows.ToArray();
                OnWindowsDeactivated?.Invoke(toRemove.ToArray());
                OnWindowsActivated?.Invoke(toAdd.ToArray());
            }

            // controlliamo se la gerarchia delle window ha subito cambiamenti
            var changed = precWindowHandles.Length != _windowHandles.Length;
            if (changed)
            {
                // se il numero di window corrente � diverso da quello precedente,
                // notifichiamo subito del cambiamento
                return true;
            }

            for (var i = 0; i < precWindowHandles.Length; i++)
            {
                var found = false;
                for (var j = 0; j < _windowHandles.Length; j++)
                {
                    if (precWindowHandles[i] == _windowHandles[j])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // se l'array di windows corrente non contiene anche solo una delle window dell'array precedente,
                    // notifichiamo subito del cambiamento
                    return true;
                }
            }

            // se l'array di windows corrente contiene tutte le window dell'array precedente,
            // notifichiamo che non c'� stato alcun cambiamento
            return false;
        }

        /// <summary>
        /// Invoked at every frame<br/>
        /// Detects the activation of previously connected displays,<br/>
        /// when this happen it triggers the detection of window changes every 30 frames
        /// </summary>
        public void manualUpdate()
        {
            for (var i = 0; i < _displays.Length; i++)
            {
                if (!_displays[i].Item2 && _displays[i].Item1.active) // I need to listen only for Display activation, since Unity doesn't support Display deactivation
                {
                    _displays[i].Item2 = true;

                    Debug.Log($"Display ({i}) activated: {_displays[i].Item1.systemWidth}x{_displays[i].Item1.systemHeight}");

                    OnDisplaysActivated?.Invoke(new[] { _displays[i].Item1 });

                    _checkForWindows = true;
                }
            }

            if (_checkForWindows && Time.frameCount % 30 == 0)
            {
                Debug.Log("Checking window changes");
                // termino di controllare le Window solo se c'� stato un cambiamento nella
                // gerarchia delle Window nel SO (aggiunta/rimozione di window)
                if(onWindowsUpdated())
                {
                    _checkForWindows = false;
                }
            }
        }

        /// <summary>
        /// Returns all the displays connected, detected by the UnityEngine
        /// </summary>
        /// <returns></returns>
        private Display[] GetConnectedDisplays() => Display.displays;

        /// <summary>
        /// Returns all the displays activated, detected by the UnityEngine
        /// </summary>
        /// <returns></returns>
        private Display[] GetActivatedDisplays() => GetConnectedDisplays().Where(d => d.active).ToArray();
    }
}