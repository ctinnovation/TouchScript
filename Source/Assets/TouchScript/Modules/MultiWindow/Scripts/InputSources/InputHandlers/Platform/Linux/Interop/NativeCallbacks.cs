#if UNITY_STANDALONE_LINUX
using System.Runtime.InteropServices;

namespace TouchScript.InputSources.InputHandlers.Interop
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="messageType"></param>
    /// <param name="message"></param>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void MessageCallback(int messageType, string message);

    /// <summary>
    /// 
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void PointerCallback();
}
#endif