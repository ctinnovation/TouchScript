#if UNITY_STANDALONE_LINUX
using System;
using System.Runtime.InteropServices;
using TouchScript.Utils.Platform.Interop;

namespace TouchScript.InputSources.InputHandlers.Interop
{
    sealed class NativeX11PointerHandler : IDisposable
    {
        #region Native Methods
        
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_Create(MessageCallback messageCallback,
            IntPtr display, IntPtr window, PointerCallback pointerCallback, ref IntPtr handle);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_Destroy(IntPtr handle);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_GetScreenResolution(IntPtr handle, MessageCallback messageCallback,
            out int width, out int height);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_SetScreenParams(IntPtr handle, MessageCallback messageCallback,
            int width, int height, float offsetX, float offsetY, float scaleX, float scaleY);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_ProcessEventQueue(IntPtr handle, MessageCallback messageCallback, int frameCount);
        
        #endregion
        
        private IntPtr handle;

        internal NativeX11PointerHandler(MessageCallback messageCallback, IntPtr display, IntPtr window, PointerCallback pointerCallback)
        {
            // Create native resources
            handle = new IntPtr();
            var result = PointerHandler_Create(messageCallback, display, window, pointerCallback, ref handle);
            if (result != Result.Ok)
            {
                handle = IntPtr.Zero;
                ResultHelper.CheckResult(result);
            }
        }

        ~NativeX11PointerHandler()
        {
            Dispose(false);
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Free managed resources
            }

            // Free native resources
            if (handle != IntPtr.Zero)
            {
                PointerHandler_Destroy(handle);
                handle = IntPtr.Zero;
            }
        }

        internal void GetScreenResolution(MessageCallback messageCallback, out int width, out int height)
        {
            var result = PointerHandler_GetScreenResolution(handle, messageCallback, out width, out height);
#if TOUCHSCRIPT_DEBUG
            ResultHelper.CheckResult(result);
#endif
        }
        
        internal void SetScreenParams(MessageCallback messageCallback, int width, int height,
            float offsetX, float offsetY, float scaleX, float scaleY)
        {
            var result = PointerHandler_SetScreenParams(handle, messageCallback, width, height, offsetX, offsetY, scaleX, scaleY);
#if TOUCHSCRIPT_DEBUG
            ResultHelper.CheckResult(result);
#endif
        }

        internal void ProcessEventQueue(MessageCallback messageCallback, int frameCount)
        {
            PointerHandler_ProcessEventQueue(handle, messageCallback, frameCount);
        }
    }
}
#endif