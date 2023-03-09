#if UNITY_STANDALONE_LINUX

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TouchScript.InputSources.InputHandlers.Interop;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers
{
    public class X11PointerHandlerSystem : IInputSourceSystem, IDisposable
    {
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandlerSystem_ProcessEventQueue();
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandlerSystem_GetWindowsOfProcess(MessageCallback messageCallback,
            int pid, out IntPtr windows, out uint numWindows);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandlerSystem_FreeWindowsOfProcess(IntPtr windows);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandlerSystem_Destroy();
        
        ~X11PointerHandlerSystem()
        {
            Dispose(false);
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Free managed resources
            }

            // Free native resources
            PointerHandlerSystem_Destroy();
        }

        public void PrepareInputs()
        {
            var result = PointerHandlerSystem_ProcessEventQueue();
#if TOUCHSCRIPT_DEBUG
            ResultHelper.CheckResult(result);
#endif
        }

        public void GetWindowsOfProcess(int pid, List<IntPtr> procWindows)
        {
            var result =
                PointerHandlerSystem_GetWindowsOfProcess(OnNativeMessage, pid, out var windows, out uint numWindows);
            ResultHelper.CheckResult(result);
            
            // Copy window handles
            IntPtr[] w = new IntPtr[numWindows];
            Marshal.Copy(windows, w, 0, (int)numWindows);
            
            // Cleanup native side
            PointerHandlerSystem_FreeWindowsOfProcess(windows);
            
            procWindows.AddRange(w);
            
            Debug.Log($"[TouchScript]: Found {procWindows.Count} application windows for process {pid}");
        }
        
        // Attribute used for IL2CPP
        [AOT.MonoPInvokeCallback(typeof(MessageCallback))]
        public static void OnNativeMessage(int messageType, string message)
        {
            switch (messageType)
            {
                case 2:
                    Debug.LogWarning("[libX11TouchMultiWindow.so]: " + message);
                    break;
                case 3:
                    Debug.LogError("[libX11TouchMultiWindow.so]: " + message);
                    break;
                default:
                    Debug.Log("[libX11TouchMultiWindow.so]: " + message);
                    break;
            }
        }
    }
}
#endif