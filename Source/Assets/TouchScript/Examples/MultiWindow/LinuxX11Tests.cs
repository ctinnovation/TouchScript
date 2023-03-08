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
            var display = X11Utils.OpenDisplayConnection();
            if (display == IntPtr.Zero)
            {
                return;
            }
            
            var procWindows = new List<IntPtr>();
            X11Utils.GetWindowsOfProcess(display, Process.GetCurrentProcess().Id, procWindows);
            
            X11Utils.CloseDisplayConnection(display);
        }
#endif
    }
}