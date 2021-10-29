using System;

namespace TouchScript
{
    public interface IMultiWindowManager
    {
        /// <summary>
        /// 
        /// </summary>
        bool ShouldActivateDisplays { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        bool ShouldUpdateInputHandlers { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetDisplay"></param>
        IntPtr OnDisplayActivated(int targetDisplay);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetDisplay"></param>
        /// <returns></returns>
        IntPtr GetWindowHandle(int targetDisplay);
        
        /// <summary>
        /// 
        /// </summary>
        void UpdateInputHandlers();
    }
}