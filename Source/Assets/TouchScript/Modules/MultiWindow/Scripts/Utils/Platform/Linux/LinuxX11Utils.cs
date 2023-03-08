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
        [DllImport("libX11", EntryPoint = "XOpenDisplay")]
        public static extern IntPtr XOpenDisplay(string displayName);
        [DllImport("libX11", EntryPoint = "XCloseDisplay")]
        public static extern int XCloseDisplay(IntPtr display);

        [DllImport("libX11TouchMultiWindow")]
        internal static extern Result XGetWindowsOfProcess(IntPtr display, int pid, out IntPtr windows, out uint numWindows);
        [DllImport("libX11TouchMultiWindow")]
        internal static extern Result XFreeWindowsOfProcess(IntPtr windows);
        
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
        
        public static void GetWindowsOfProcess(IntPtr display, int pid, List<IntPtr> procWindows)
        {
            var result = XGetWindowsOfProcess(display, pid, out var windows, out uint numWindows);
            //Debug.Log($"[TouchScript]: Found {numWindows} application windows for process {pid}");

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