#if UNITY_STANDALONE_LINUX
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TouchScript.Utils.Platform.Interop;
using UnityEngine;

namespace TouchScript.Utils.Platform
{
    public static class LinuxX11Utils
    {
        [DllImport("libX11")]
        private static extern IntPtr XOpenDisplay(string displayName);
        [DllImport("libX11")]
        private static extern int XCloseDisplay(IntPtr display);
        [DllImport("libX11")]
        private static extern bool XQueryExtension(IntPtr display, string extension, out int opcode, out int evt,
            out int err);
        
        [DllImport("libXi")]
        private static extern int XIQueryVersion(IntPtr display, ref int majorVersion, ref int minorVersion);

        [DllImport("libX11TouchMultiWindow")]
        private static extern Result XGetWindowsOfProcess(IntPtr display, int pid, out IntPtr windows, out uint numWindows);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result XFreeWindowsOfProcess(IntPtr windows);
        
        // Attribute used for IL2CPP
        [AOT.MonoPInvokeCallback(typeof(MessageCallback))]
        internal static void OnNativeMessage(int messageType, string message)
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

        public static IntPtr OpenDisplayConnection()
        {
            var display = XOpenDisplay(null);
            if (display == IntPtr.Zero)
            {
                Debug.LogError($"[TouchScript] Failed to open X11 display connection.");
                return IntPtr.Zero;
            }

            // Check if the XInput extension is available
            if (!XQueryExtension(display, "XInputExtension", out var opcode, out var evt, out var err))
            {
                Debug.LogError($"[TouchScript]: Failed to get the XInput extension");
             
                XCloseDisplay(display);
                return IntPtr.Zero;
            }

            // Check the XInput version, which we expect to be 2.0
            var majorVersion = 2;
            var minorVersion = 0;
            if (XIQueryVersion(display, ref majorVersion, ref minorVersion) != 0)
            {
                Debug.LogError($"[TouchScript]: Unsupported XInput extension version {majorVersion}.{minorVersion}");
            
                XCloseDisplay(display);
                return IntPtr.Zero;
            }

            return display;
        }

        public static void CloseDisplayConnection(IntPtr display)
        {
            XCloseDisplay(display);
        }
        
        public static void GetWindowsOfProcess(IntPtr display, int pid, List<IntPtr> procWindows)
        {
            var result = XGetWindowsOfProcess(display, pid, out var windows, out uint numWindows);
            ResultHelper.CheckResult(result);
            
            // Copy window handles
            IntPtr[] w = new IntPtr[numWindows];
            Marshal.Copy(windows, w, 0, (int)numWindows);
            
            // Cleanup native side
            XFreeWindowsOfProcess(windows);
            
            procWindows.AddRange(w);
            
            Debug.Log($"[TouchScript]: Found {procWindows.Count} application windows for process {pid}");
        }
    }
}
#endif