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
        // This is included so library dependencies are loaded.
        // TODO Figure out how we can work without this hack...
        [DllImport("libX11")]
        private static extern IntPtr XOpenDisplay(string displayName);
        [DllImport("libX11")]
        private static extern int XCloseDisplay(IntPtr display);
        [DllImport("libX11")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool XQueryExtension(IntPtr display, string extension, out int opcode, out int evt,
            out int err);
        [DllImport("libXi")]
        private static extern int XIQueryVersion(IntPtr display, ref int majorVersion, ref int minorVersion);
        
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandlerSystem_Create(IntPtr display, MessageCallback messageCallback, ref IntPtr handle);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandlerSystem_ProcessEventQueue(IntPtr handle);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandlerSystem_GetWindowsOfProcess(IntPtr handle, int pid, out IntPtr windows, out uint numWindows);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandlerSystem_FreeWindowsOfProcess(IntPtr handle, IntPtr windows);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandlerSystem_Destroy(IntPtr handle);

        private IntPtr display;
        private IntPtr handle;

        public X11PointerHandlerSystem()
        {
            display = XOpenDisplay(null);
            
            // The following checks should be on the native side, as that's where the actual implementation is.
            // But Unity can't load 
            // Check if the XInput extension is available
            if (!XQueryExtension(display, "XInputExtension", out var opcode, out var evt, out var err))
            {
                Debug.LogError($"[TouchScript]: Failed to get the XInput extension");
             
                XCloseDisplay(display);
                display = IntPtr.Zero;
                return;
            }
            
            // Check the minimum XInput extension version, which we expect to be 2.3+
            var majorVersion = 2;
            var minorVersion = 3;
            if (XIQueryVersion(display, ref majorVersion, ref minorVersion) != 0)
            {
                Debug.LogError($"[TouchScript]: Unsupported XInput extension version: expected 2.3+, actual {majorVersion}.{minorVersion}");
            
                XCloseDisplay(display);
                display = IntPtr.Zero;
                return;
            }

            // Create native resources
            handle = new IntPtr();
            var result = PointerHandlerSystem_Create(display, OnNativeMessage, ref handle);
            if (result != Result.Ok)
            {
                handle = IntPtr.Zero;
                ResultHelper.CheckResult(result);
            }
        }
        
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
            if (handle != IntPtr.Zero)
            {
                PointerHandlerSystem_Destroy(handle);
                handle = IntPtr.Zero;
            }

            if (display != IntPtr.Zero)
            {
                XCloseDisplay(display);
                display = IntPtr.Zero;
            }
        }

        public void PrepareInputs()
        {
            var result = PointerHandlerSystem_ProcessEventQueue(handle);
#if TOUCHSCRIPT_DEBUG
            ResultHelper.CheckResult(result);
#endif
        }

        public void GetWindowsOfProcess(int pid, List<IntPtr> procWindows)
        {
            var result =
                PointerHandlerSystem_GetWindowsOfProcess(handle, pid, out var windows, out uint numWindows);
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