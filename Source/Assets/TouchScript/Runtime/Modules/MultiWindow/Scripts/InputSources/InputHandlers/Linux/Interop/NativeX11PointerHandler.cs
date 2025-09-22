#if UNITY_STANDALONE_LINUX
using System;
using System.Runtime.InteropServices;

namespace TouchScript.InputSources.InputHandlers.Interop
{
    sealed class NativeX11PointerHandler : IDisposable
    {
        #region Native Methods

        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_Create(int targetDisplay, IntPtr window,
            PointerCallback pointerCallback, ref IntPtr handle);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_Destroy(IntPtr handle);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_SetTargetDisplay(IntPtr handle, int targetDisplay);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_GetScreenParams(IntPtr handle, out int x, out int y, out int width,
            out int height, out int screenWidth, out int screenHeight);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_SetScreenParams(IntPtr handle, int width, int height,
            float offsetX, float offsetY, float scaleX, float scaleY);
        
        #endregion
        
        private IntPtr handle;

        internal NativeX11PointerHandler(int targetDisplay, IntPtr window, PointerCallback pointerCallback)
        {
            // Create native resources
            handle = new IntPtr();
            var result = PointerHandler_Create(targetDisplay, window, pointerCallback, ref handle);
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

        internal void SetTargetDisplay(int value)
        {
            var result = PointerHandler_SetTargetDisplay(handle, value);
#if TOUCHSCRIPT_DEBUG
            ResultHelper.CheckResult(result);
#endif
        }

        internal void GetScreenResolution(out int x, out int y, out int width, out int height, out int screenWidth, out int screenHeight)
        {
            var result = PointerHandler_GetScreenParams(handle, out x, out y, out width, out height, out screenWidth, out screenHeight);
#if TOUCHSCRIPT_DEBUG
            ResultHelper.CheckResult(result);
#endif
        }
        
        internal void SetScreenParams(int width, int height,
            float offsetX, float offsetY, float scaleX, float scaleY)
        {
            var result = PointerHandler_SetScreenParams(handle, width, height, offsetX, offsetY, scaleX, scaleY);
#if TOUCHSCRIPT_DEBUG
            ResultHelper.CheckResult(result);
#endif
        }
    }
}
#endif