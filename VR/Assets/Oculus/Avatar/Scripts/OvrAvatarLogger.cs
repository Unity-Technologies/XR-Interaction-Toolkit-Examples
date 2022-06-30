using UnityEngine;

namespace Oculus.Avatar
{
    public static class AvatarLogger
    {
        public const string LogAvatar = "[Avatars] - ";
        public const string Tab = "    ";

        [System.Diagnostics.Conditional("ENABLE_AVATAR_LOGS"),
            System.Diagnostics.Conditional("ENABLE_AVATAR_LOG_BASIC")]
        public static void Log(string logMsg)
        {
            Debug.Log(LogAvatar + logMsg);
        }

        [System.Diagnostics.Conditional("ENABLE_AVATAR_LOGS"),
            System.Diagnostics.Conditional("ENABLE_AVATAR_LOG_BASIC")]
        public static void Log(string logMsg, Object context)
        {
            Debug.Log(LogAvatar + logMsg , context);
        }

        [System.Diagnostics.Conditional("ENABLE_AVATAR_LOGS"), 
            System.Diagnostics.Conditional("ENABLE_AVATAR_LOG_WARNING")]
        public static void LogWarning(string logMsg)
        {
            Debug.LogWarning(LogAvatar + logMsg);
        }

        [System.Diagnostics.Conditional("ENABLE_AVATAR_LOGS"), 
            System.Diagnostics.Conditional("ENABLE_AVATAR_LOG_ERROR")]
        public static void LogError(string logMsg)
        {
            Debug.LogError(LogAvatar + logMsg);
        }

        [System.Diagnostics.Conditional("ENABLE_AVATAR_LOGS"),
         System.Diagnostics.Conditional("ENABLE_AVATAR_LOG_ERROR")]
        public static void LogError(string logMsg, Object context)
        {
            Debug.LogError(LogAvatar + logMsg, context);
        }
    };
}
