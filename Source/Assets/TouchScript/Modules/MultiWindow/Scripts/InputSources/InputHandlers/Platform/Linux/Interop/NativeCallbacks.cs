using System.Runtime.InteropServices;

namespace TouchScript.InputSources.InputHandlers.Interop
{
    /// <summary>
    /// 
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void PointerCallback();
}