#if UNITY_STANDALONE_LINUX
using System;
using System.Runtime.InteropServices;

namespace TouchScript.InputSources.InputHandlers.Interop
{
    sealed class NativeX11PointerHandler : IDisposable
    {
        #region Native Methods
        
        [DllImport("libX11TouchMultiWindow", EntryPoint = "PointerHandler_GetScreenResolution")]
        private static extern Result PointerHandler_GetScreenResolution(IntPtr handle, out int width, out int height);
        [DllImport("libX11TouchMultiWindow", EntryPoint = "PointerHandler_SetScreenParams")]
        private static extern Result PointerHandler_SetScreenParams(IntPtr handle, int width, int height,
            float offsetX, float offsetY, float scaleX, float scaleY);
        
        #endregion
        
        private X11PointerHandlerSystem system;
        private IntPtr handle;

        internal NativeX11PointerHandler(X11PointerHandlerSystem handlerSystem, IntPtr window, PointerCallback pointerCallback)
        {
            this.system = handlerSystem;

            // Create native resources
            handle = new IntPtr();
            var result = X11PointerHandlerSystem.PointerHandlerSystem_CreateHandler(handlerSystem.Handle, window, pointerCallback, ref handle);
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
            if (system.Handle != IntPtr.Zero && handle != IntPtr.Zero)
            {
                X11PointerHandlerSystem.PointerHandlerSystem_DestroyHandler(system.Handle, handle);
            }
            handle = IntPtr.Zero;
        }

        internal void GetScreenResolution(out int width, out int height)
        {
            var result = PointerHandler_GetScreenResolution(handle, out width, out height);
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