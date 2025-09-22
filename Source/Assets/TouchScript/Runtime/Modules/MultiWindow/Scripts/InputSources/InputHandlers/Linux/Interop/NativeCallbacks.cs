#if UNITY_STANDALONE_LINUX
using System.Runtime.InteropServices;
using UnityEngine;

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
    delegate void PointerCallback(int id, PointerEvent evt, PointerType type, Vector2 position, PointerData pointerData);
}
#endif