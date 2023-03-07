using System;
using System.Collections.Generic;
using System.Diagnostics;
using TouchScript.Utils.Platform;
using UnityEngine;

namespace TouchScript.Examples.MultiWindow
{
    public class LinuxX11Tests : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                GetWindows();
            }
        }
        
        public void GetWindows()
        {
            var procWindows = new List<IntPtr>();
            LinuxX11Utils.GetWindowsOfProcess(Process.GetCurrentProcess().Id, procWindows);
        }
    }
}