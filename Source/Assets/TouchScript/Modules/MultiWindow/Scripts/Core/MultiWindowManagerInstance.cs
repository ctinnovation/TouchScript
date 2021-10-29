using System;
using System.Collections;
using System.Collections.Generic;
using TouchScript.InputSources.InputHandlers;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Diagnostics;
using System.Text;
using TouchScript.Utils.Platform;
#endif

namespace TouchScript.Core
{
    /// <summary>
    /// Default implementation of <see cref="IMultiWindowManager"/>.
    /// </summary>
    public class MultiWindowManagerInstance : DebuggableMonoBehaviour, IMultiWindowManager
    {
        private const string unityWindowClassName = "UnityWndClass";
        
        /// <summary>
        /// Gets the instance of MultiWindowManager singleton.
        /// </summary>
        public static MultiWindowManagerInstance Instance
        {
            get
            {
                if (shuttingDown) return null;
                if (instance == null)
                {
                    if (!Application.isPlaying) return null;
                    var objects = FindObjectsOfType<MultiWindowManagerInstance>();
                    if (objects.Length == 0)
                    {
                        var go = new GameObject("MultiWindowManager Instance");
                        instance = go.AddComponent<MultiWindowManagerInstance>();
                    }
                    else if (objects.Length >= 1)
                    {
                        instance = objects[0];
                    }
                }
                return instance;
            }
        }

        public bool ShouldActivateDisplays
        {
            get => shouldActivateDisplays;
            set => shouldActivateDisplays = value;
        }

        public bool ShouldUpdateInputHandlers
        {
            get => shouldUpdateInputHandlers;
            set => shouldUpdateInputHandlers = value;
        }
        
        private static bool shuttingDown = false;
        private static MultiWindowManagerInstance instance;

        private bool shouldActivateDisplays = true;
        private bool shouldUpdateInputHandlers = true;
        
        private Dictionary<int, IntPtr> targetDisplayWindows = new Dictionary<int, IntPtr>();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private List<IntPtr> unityWindows = new List<IntPtr>();
#endif

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(this);
                return;
            }
            
            gameObject.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(gameObject);
            
            Input.simulateMouseWithTouches = false;
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StopAllCoroutines();
            StartCoroutine(LateAwake());
        }

        private IEnumerator LateAwake()
        {
            // First display is always activated
            OnDisplayActivated(1);
            
            // Wait 2 frames:
            // Frame 0: TouchManager prepares, inputs add themselves and optionally activate the screen
            // Frame 1: Displays are activated, we can retrieve handles and update the input handlers
            yield return null;
            
            if (ShouldUpdateInputHandlers)
            {
                UpdateInputHandlers();
            }
        }
        
        private void OnApplicationQuit()
        {
            shuttingDown = true;
        }

        public IntPtr OnDisplayActivated(int targetDisplay)
        {
            if (targetDisplayWindows.TryGetValue(targetDisplay, out var windowHandle))
            {
                return windowHandle;
            }
            
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            RefreshWindowHandles();

            // Now we check for every pointer, if it is present in the dictionary
            foreach (var window in unityWindows)
            {
                if (!targetDisplayWindows.ContainsValue(window))
                {
                    targetDisplayWindows.Add(targetDisplay, window);

                    Debug.Log($"[TouchScript]: Registered window handle for display {targetDisplay}.");
                    
                    return window;
                }
            }
#endif
            
            return IntPtr.Zero;
        }
        
        private void RefreshWindowHandles()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            unityWindows.Clear();

            // For every window of the current process, we check if it is of the unity window class, if so
            var classNameBuilder = new StringBuilder(33);
            var windows = WindowsUtilsEx.GetRootWindowsOfProcess(Process.GetCurrentProcess().Id);
            
            foreach (var window in windows)
            {
                classNameBuilder.Clear();
                WindowsUtilsEx.GetClassName(window, classNameBuilder, 33);

                var className = classNameBuilder.ToString();
                if (className != unityWindowClassName)
                {
                    continue;
                }

                unityWindows.Add(window);
            }
# if TOUCHSCRIPT_DEBUG
            Debug.Log($"[TouchScript]: Found {unityWindows.Count} windows.");
# endif
#endif
        }
        
        public IntPtr GetWindowHandle(int targetDisplay)
        {
            return targetDisplayWindows.TryGetValue(targetDisplay, out var windowHandle) ? windowHandle : IntPtr.Zero;
        }

        public void UpdateInputHandlers()
        {
            var inputs = TouchManager.Instance.Inputs;
            foreach (var input in inputs)
            {
                if (input is MultiWindowStandardInput multiWindowInput &&
                    multiWindowInput.isActiveAndEnabled)
                {
                    multiWindowInput.UpdateInputHandlers();
                }
            }
        }
    }
}