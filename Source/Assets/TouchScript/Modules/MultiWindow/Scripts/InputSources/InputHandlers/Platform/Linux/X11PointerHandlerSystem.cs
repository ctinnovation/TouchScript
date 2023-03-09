#if UNITY_STANDALONE_LINUX

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TouchScript.InputSources.InputHandlers.Interop;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers
{
    public class X11PointerHandlerSystem : IInputSourceSystem
    {
        [DllImport("libX11TouchMultiWindow", EntryPoint = "PointerHandlerSystem_ProcessEventQueue")]
        private static extern Result PointerHandlerSystem_ProcessEventQueue();
        [DllImport("libX11TouchMultiWindow", EntryPoint = "PointerHandlerSystem_GetWindowsOfProcess")]
        private static extern Result PointerHandlerSystem_GetWindowsOfProcess(int pid, out IntPtr windows, out uint numWindows);
        [DllImport("libX11TouchMultiWindow", EntryPoint = "PointerHandlerSystem_FreeWindowsOfProcess")]
        private static extern Result PointerHandlerSystem_FreeWindowsOfProcess(IntPtr windows);

        public X11PointerHandlerSystem()
        {
            
        }
        
        public void Process()
        {
            var result = PointerHandlerSystem_ProcessEventQueue();
#if TOUCHSCRIPT_DEBUG
            ResultHelper.CheckResult(result);
#endif
        }

        public void GetWindowsOfProcess(int pid, List<IntPtr> procWindows)
        {
            var result = PointerHandlerSystem_GetWindowsOfProcess(, pid, out var windows, out uint numWindows);
            ResultHelper.CheckResult(result);
            
            // Copy window handles
            IntPtr[] w = new IntPtr[numWindows];
            Marshal.Copy(windows, w, 0, (int)numWindows);
            
            // Cleanup native side
            PointerHandlerSystem_FreeWindowsOfProcess(handle, windows);
            
            procWindows.AddRange(w);
            
            Debug.Log($"[TouchScript]: Found {procWindows.Count} application windows for process {pid}");
        }
        
        // Attribute used for IL2CPP
        [AOT.MonoPInvokeCallback(typeof(MessageCallback))]
        private void OnNativeMessage(int messageType, string message)
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