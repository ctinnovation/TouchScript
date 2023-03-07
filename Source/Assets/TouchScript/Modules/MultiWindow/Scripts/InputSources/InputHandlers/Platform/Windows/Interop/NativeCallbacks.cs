#if UNITY_STANDALONE_WIN
using System.Runtime.InteropServices;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers.Interop
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void PointerCallback(int id, PointerEvent evt, PointerType type, Vector2 position,
        PointerData data);
}
#endif