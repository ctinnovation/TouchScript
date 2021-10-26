using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TouchScript.Utils.Platform
{
    public static class WindowsUtilsEx
    {
        /// <summary>
        /// Retrieves the native monitor resolution.
        /// </summary>
        /// <param name="width">Output width.</param>
        /// <param name="height">Output height.</param>
        public static void GetNativeMonitorResolution(IntPtr hWindow, out int width, out int height)
        {
            var monitor = WindowsUtils.MonitorFromWindow(hWindow, WindowsUtils.MONITOR_DEFAULTTONEAREST);
            WindowsUtils.MONITORINFO monitorInfo = new WindowsUtils.MONITORINFO();
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
    }
}