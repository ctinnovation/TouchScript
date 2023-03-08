#if UNITY_STANDALONE_LINUX
using System;

namespace TouchScript.Utils.Platform.Interop
{
    public enum Result
    {
        Ok = 0,
        ErrorNullPointer = -101,
        ErrorAPI = -102
    }

    public static class ResultHelper
    {
        public static void CheckResult(Result result)
        {
            if ((int)result >= 0)
            {
                return;
            }
            
            var errorMessage = $"Native error - {result}";
            switch (result)
            {
                case Result.ErrorNullPointer:
                    throw new InvalidOperationException(errorMessage);
                case Result.ErrorAPI:
                    throw new Exception(errorMessage);
            }
        }
    }
}
#endif