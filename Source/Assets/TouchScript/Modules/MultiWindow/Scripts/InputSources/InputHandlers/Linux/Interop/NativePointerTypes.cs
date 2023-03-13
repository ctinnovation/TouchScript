#if UNITY_STANDALONE_LINUX

using System;
using System.Runtime.InteropServices;

namespace TouchScript.InputSources.InputHandlers.Interop
{
    enum PointerEvent : uint
    {
        None = 0,
        Down = 1,
        Update = 2,
        Up = 3
    }
    
    enum PointerType
    {
        None = 0,
        Mouse = 1,
        Touch = 2
    }

    [Flags]
    enum PointerFlags
    {
        None = 0x00000000,
        New = 0x00000001,
        FirstButton = 0x00000010,
        SecondButton = 0x00000020,
        ThirdButton = 0x00000040,
        FourthButton = 0x00000080,
        FifthButton = 0x00000100,
        Down = 0x00010000,
        Update = 0x00020000,
        Up = 0x00040000
    };

    enum ButtonChangeType
    {
        None,
        FirstDown,
        FirstUp,
        SecondDown,
        SecondUp,
        ThirdDown,
        ThirdUp,
        FourthDown,
        FourthUp,
        FifthDown,
        FifthUp
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PointerData
    {
        public PointerFlags PointerFlags;
        public ButtonChangeType ChangedButtons;
    }
}
#endif