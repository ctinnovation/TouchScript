#if UNITY_STANDALONE_LINUX
using System.Runtime.InteropServices;

namespace TouchScript.Utils.Platform.Interop
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="messageType"></param>
    /// <param name="message"></param>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void MessageCallback(int messageType, string message);
}
#endif