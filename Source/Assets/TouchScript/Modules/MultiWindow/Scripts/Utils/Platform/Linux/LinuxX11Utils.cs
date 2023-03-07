#if UNITY_STANDALONE_LINUX
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TouchScript.Utils.Platform
{
    public static class LinuxX11Utils
    {
        private const int None = 0;
        
        // Name of the window manager process id property
        private const string AtomNetWmPID = "_NET_WM_PID";
        private const int AtomXACardinal = 6;

        [DllImport("libX11", EntryPoint = "XOpenDisplay")]
        public static extern IntPtr XOpenDisplay(string displayName);
        
        [DllImport("libX11", EntryPoint = "XCloseDisplay")]
        public static extern int XCloseDisplay(IntPtr display);
        
        [DllImport("libX11")]
        public static extern int XInternAtom(IntPtr display, string atomName, bool onlyIfExists);
        
        [DllImport("libX11")]
        public static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport("libX11")]
        public static extern int XQueryTree(IntPtr display, IntPtr window, out IntPtr rootWindow,
            out IntPtr parentWindow, out IntPtr childWindows, out uint numChildWindows);

        [DllImport("libX11")]
        public static extern int XGetWindowProperty(IntPtr display, IntPtr window, int property, int offset,
            int length, bool delete, int reqType, out int actualType, out int actualFormat, out int nItem,
            out int bytesAfter, out IntPtr prop);

        [DllImport("libX11", CharSet = CharSet.Ansi)]
        public static extern int XFetchName(IntPtr display, IntPtr window, out string windowName);

        [DllImport("libX11")]
        public static extern int XFlush(IntPtr display);

        [DllImport("libX11", EntryPoint = "XFree")]
        public static extern int XFree(IntPtr value);
        
        public static void GetWindowsOfProcess(int pid, List<IntPtr> procWindows)
        {
            
//             // From the current display
//             var displayName = System.Environment.GetEnvironmentVariable("DISPLAY") ?? ":0";
//             
//             // Open display
//             var display = XOpenDisplay(displayName);
//             
//             // Get the default root window
//             var defaultRootWindow = XDefaultRootWindow(display);
//             // Retrieve the identifier associated with the window process id property. This identifier is then used to
//             // get process id of a window. Note that the application must set this property, but unity seems to do that.
//             var atomPID = XInternAtom(display, AtomNetWmPID, true);
//             if (atomPID != None)
//             {
//                 var windows = new List<IntPtr>();
//                 GetChildWindows(display, defaultRootWindow, windows);
//                 
//                 //Debug.Log($"[Linux]: Found {windows.Count} windows");
//                 
//                 // Now check if the pid of the window belongs to this process
//                 foreach (var window in windows)
//                 {
//                     if (XGetWindowProperty(display, window, atomPID, 0, 1, false, 6, out var type,
//                             out var format, out var nItems, out var bytesAfter, out var propPID) == 0) // Here 0 means success
//                     {
//                         if (propPID != IntPtr.Zero)
//                         {
//                             var windowPID = Marshal.ReadInt64(propPID);
//                             
// #if TOUCHSCRIPT_DEBUG
//                             XFetchName(display, window, out string windowName);
//                             if (string.IsNullOrWhiteSpace(windowName))
//                             {
//                                 windowName = "<Unknown>";
//                             }
//                             Debug.Log($"[TouchScript]: Found window: {windowName}: {windowPID}");
// #endif
//                             
//                             if (windowPID == pid)
//                             {
//                                 procWindows.Add(window);
//                             }
//                             
//                             XFree(propPID);    
//                         }
//                     }
//                 }
//             }
//             else
//             {
//                 Debug.LogWarning($"[TouchScript]: Failed to retrieve atom for '{AtomNetWmPID}'");
//             }
//
//             // Close display
//             if (display != IntPtr.Zero)
//             {
//                 XCloseDisplay(display);
//                 display = IntPtr.Zero;
//             }
//             
//             Debug.Log($"[TouchScript]: Found {procWindows.Count} application windows for process {pid}");
        }

        // private static void GetChildWindows(IntPtr display, IntPtr parent, List<IntPtr> result)
        // {
        //     // We found that we need to recurse to gather windows of unity apps
        //     if (XQueryTree(display, parent, out var rootWindow, out var parentWindow, out var childWindows,
        //             out uint numChildWindows) == 0) // Here 0 means failure
        //     {
        //         Debug.LogWarning("[TouchScript]: XQueryTree failed");
        //         return;
        //     }
        //
        //     if (numChildWindows > 0)
        //     {
        //         // Copy the children, so they are pinned in managed space
        //         var children = new IntPtr[numChildWindows];
        //         Marshal.Copy(childWindows, children, 0, children.Length);
        //
        //         result.AddRange(children);
        //
        //         // Cleanup
        //         XFree(childWindows);
        //     }
        // }
    }
}
#endif