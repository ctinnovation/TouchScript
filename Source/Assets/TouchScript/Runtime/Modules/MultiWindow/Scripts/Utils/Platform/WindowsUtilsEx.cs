#if UNITY_STANDALONE_WIN
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using UnityEngine;

namespace TouchScript.Utils.Platform
{
    public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);
    
    public static class WindowsUtilsEx
    {
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(Win32Callback callback, IntPtr lParam);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);
        
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public static List<IntPtr> GetRootWindowsOfProcess(int pid)
        {
            var rootWindows = GetChildWindows(IntPtr.Zero);
            var dsProcRootWindows = new List<IntPtr>();
            foreach (var hWnd in rootWindows)
            {
                GetWindowThreadProcessId(hWnd, out var lpdwProcessId);
                if (lpdwProcessId == pid)
                {
                    dsProcRootWindows.Add(hWnd);
                }
            }
            return dsProcRootWindows;
        }
        
        /// <summary>
        /// Retrieves the native monitor resolution.
        /// </summary>
        /// <param name="width">Output width.</param>
        /// <param name="height">Output height.</param>
        public static void GetNativeMonitorResolution(IntPtr hWindow, out int width, out int height)
        {
            var monitor = WindowsUtils.MonitorFromWindow(hWindow, WindowsUtils.MONITOR_DEFAULTTONEAREST);
            var monitorInfo = new WindowsUtils.MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
            if (!WindowsUtils.GetMonitorInfo(monitor, ref monitorInfo))
            {
                width = Screen.width;
                height = Screen.height;
            }
            else
            {
                width = monitorInfo.rcMonitor.Width;
                height = monitorInfo.rcMonitor.Height;
            }
        }

        // Attribute used for IL2CPP
        [MonoPInvokeCallback(typeof(Win32Callback))]
        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            var gch = GCHandle.FromIntPtr(pointer);
            var list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            //  You can modify this to check to see if you want to cancel the operation, then return a null here
            return true;
        }
        
        private static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            var result = new List<IntPtr>();
            var listHandle = GCHandle.Alloc(result);
            try
            {
                EnumChildWindows(parent, EnumWindow, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }
    }
}
#endif