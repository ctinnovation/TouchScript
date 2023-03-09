#if UNITY_STANDALONE_LINUX

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TouchScript.InputSources.InputHandlers.Interop;
using TouchScript.Utils.InputHandlers.Interop;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers
{
    public class X11PointerSystem : IInputSourceSystem, IDisposable
    {
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerSystem_Create(MessageCallback messageCallback, ref IntPtr handle);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerSystem_Destroy(IntPtr handle);
        [DllImport("libX11TouchMultiWindow")]
        internal static extern Result PointerSystem_CreateHandler(IntPtr handle, IntPtr window,
            PointerCallback pointerCallback, ref IntPtr handlerHandle);
        [DllImport("libX11TouchMultiWindow")]
        internal static extern Result PointerSystem_DestroyHandler(IntPtr handle, IntPtr handlerHandle);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerSystem_ProcessEventQueue(IntPtr handle);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerSystem_GetWindowsOfProcess(IntPtr handle, int pid, out IntPtr windows, out uint numWindows);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerSystem_FreeWindowsOfProcess(IntPtr handle, IntPtr windows);

        internal IntPtr Handle => handle;
        
        private IntPtr handle;

        public X11PointerSystem()
        {
            // Create native resources
            handle = new IntPtr();
            var result = PointerSystem_Create(OnNativeMessage, ref handle);
            if (result != Result.Ok)
            {
                handle = IntPtr.Zero;
                ResultHelper.CheckResult(result);
            }
        }
        
        ~X11PointerSystem()
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
            if (handle != IntPtr.Zero)
            {
                PointerSystem_Destroy(handle);
                handle = IntPtr.Zero;
            }
        }
        
        public void Process()
        {
            var result = PointerSystem_ProcessEventQueue(handle);
#if TOUCHSCRIPT_DEBUG
            ResultHelper.CheckResult(result);
#endif
        }

        public void GetWindowsOfProcess(int pid, List<IntPtr> procWindows)
        {
            var result = PointerSystem_GetWindowsOfProcess(handle, pid, out var windows, out uint numWindows);
            ResultHelper.CheckResult(result);
            
            // Copy window handles
            IntPtr[] w = new IntPtr[numWindows];
            Marshal.Copy(windows, w, 0, (int)numWindows);
            
            // Cleanup native side
            PointerSystem_FreeWindowsOfProcess(handle, windows);
            
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