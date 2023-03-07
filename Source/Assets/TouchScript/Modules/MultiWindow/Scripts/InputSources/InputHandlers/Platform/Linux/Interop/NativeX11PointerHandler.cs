#if UNITY_STANDALONE_LINUX
using System;
using System.Runtime.InteropServices;
using TouchScript.Utils.Platform.Interop;

namespace TouchScript.InputSources.InputHandlers.Interop
{
    public class NativeX11PointerHandler : IDisposable
    {
        #region Native Methods
        
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_Create(ref IntPtr handle);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_Destroy(IntPtr handle);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_Initialize(IntPtr handle, MessageCallback messageCallback,
            IntPtr display, IntPtr window);
        [DllImport("libX11TouchMultiWindow")]
        private static extern Result PointerHandler_SetScreenParams(IntPtr handle, MessageCallback messageCallback,
            int width, int height, float offsetX, float offsetY, float scaleX, float scaleY);
        
        #endregion
        
        private IntPtr handle;

        internal NativeX11PointerHandler()
        {
            // Create native resources
            handle = new IntPtr();
            var result = PointerHandler_Create(ref handle);
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
        
        internal void Initialize(MessageCallback messageCallback, IntPtr display, IntPtr window)
        {
            PointerHandler_Initialize(handle, messageCallback, display, window);
        }

        internal void GetNativeScreenResolution(MessageCallback messageCallback, out int width, out int height)
        {
            width = height = 0;
        }
        
        internal void SetScreenParams(MessageCallback messageCallback, int width, int height,
            float offsetX, float offsetY, float scaleX, float scaleY)
        {
            PointerHandler_SetScreenParams(handle, messageCallback, width, height, offsetX, offsetY, scaleX, scaleY);
        }
    }
}
#endif