using System;
using System.Collections.Generic;
using System.Diagnostics;
using TouchScript.Utils.Platform;
using UnityEngine;

namespace TouchScript.Examples.MultiWindow
{
    public class LinuxX11Tests : MonoBehaviour
    {
#if UNITY_STANDALONE_LINUX
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                GetWindows();
            }
        }
        
        public void GetWindows()
        {
            var display = LinuxX11Utils.XOpenDisplay(null);
            
            var procWindows = new List<IntPtr>();
            LinuxX11Utils.GetWindowsOfProcess(display, Process.GetCurrentProcess().Id, procWindows);
            
            LinuxX11Utils.XCloseDisplay(display);
        }
#endif
    }
}