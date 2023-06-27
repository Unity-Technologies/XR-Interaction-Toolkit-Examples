/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Text;
using System.Diagnostics;
using UnityEngine;

namespace Meta.WitAi
{
    public static class VLog
    {
        #if UNITY_EDITOR
        /// <summary>
        /// Ignores logs in editor if less than log level (Error = 0, Warning = 2, Log = 3)
        /// </summary>
        public static LogType EditorLogLevel
        {
            get
            {
                if (_editorLogLevel == (LogType) (-1))
                {
                    string editorLogLevel = UnityEditor.EditorPrefs.GetString(EDITOR_LOG_LEVEL_KEY, EDITOR_LOG_LEVEL_DEFAULT.ToString());
                    if (!Enum.TryParse(editorLogLevel, out _editorLogLevel))
                    {
                        _editorLogLevel = EDITOR_LOG_LEVEL_DEFAULT;
                    }
                }
                return _editorLogLevel;
            }
            set
            {
                _editorLogLevel = value;
                UnityEditor.EditorPrefs.SetString(EDITOR_LOG_LEVEL_KEY, _editorLogLevel.ToString());
            }
        }
        private static LogType _editorLogLevel = (LogType)(-1);
        private const string EDITOR_LOG_LEVEL_KEY = "VSDK_EDITOR_LOG_LEVEL";
        private const LogType EDITOR_LOG_LEVEL_DEFAULT = LogType.Warning;
        #endif

        /// <summary>
        /// Hides all errors from the console
        /// </summary>
        public static bool SuppressLogs { get; set; } = false;

        /// <summary>
        /// Event for appending custom data to a log before logging to console
        /// </summary>
        public static event Action<StringBuilder, string, LogType> OnPreLog;

        /// <summary>
        /// Performs a Debug.Log with custom categorization and using the global log level
        /// </summary>
        /// <param name="log">The text to be debugged</param>
        /// <param name="logCategory">The category of the log</param>
        public static void D(object log) => Log(LogType.Log, null, log);
        public static void D(string logCategory, object log) => Log(LogType.Log, logCategory, log);

        /// <summary>
        /// Performs a Debug.LogWarning with custom categorization and using the global log level
        /// </summary>
        /// <param name="log">The text to be debugged</param>
        /// <param name="logCategory">The category of the log</param>
        public static void W(object log) => Log(LogType.Warning, null, log);
        public static void W(string logCategory, object log) => Log(LogType.Warning, logCategory, log);

        /// <summary>
        /// Performs a Debug.LogError with custom categorization and using the global log level
        /// </summary>
        /// <param name="log">The text to be debugged</param>
        /// <param name="logCategory">The category of the log</param>
        public static void E(object log) => Log(LogType.Error, null, log);
        public static void E(string logCategory, object log) => Log(LogType.Error, logCategory, log);

        /// <summary>
        /// Filters out unwanted logs, appends category information
        /// and performs UnityEngine.Debug.Log as desired
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="log"></param>
        /// <param name="category"></param>
        private static void Log(LogType logType, string logCategory, object log)
        {
            #if UNITY_EDITOR
            // Skip logs with higher log type then global log level
            if ((int) logType > (int)EditorLogLevel)
            {
                return;
            }
            #endif
            // Suppress logs if desired
            if (SuppressLogs)
            {
                return;
            }

            // Use calling category if null
            string category = logCategory;
            if (string.IsNullOrEmpty(category))
            {
                category = GetCallingCategory();
            }

            // String builder
            StringBuilder result = new StringBuilder();

            #if !UNITY_EDITOR && !UNITY_ANDROID
            {
                // Start with datetime if not done so automatically
                DateTime now = DateTime.Now;
                result.Append($"[{now.ToShortDateString()} {now.ToShortTimeString()}] ");
            }
            #endif

            // Insert log type
            int start = result.Length;
            result.Append($"[{logType.ToString().ToUpper()}] ");
            WrapWithLogColor(result, start, logType);

            // Append VDSK & Category
            start = result.Length;
            result.Append("[VSDK");
            if (!string.IsNullOrEmpty(category))
            {
                result.Append($" {category}");
            }
            result.Append("] ");
            WrapWithCallingLink(result, start);

            // Append the actual log
            result.Append(log == null ? string.Empty : log.ToString());

            // Final log append
            OnPreLog?.Invoke(result, logCategory, logType);

            // Log
            switch (logType)
            {
                case LogType.Error:
                    UnityEngine.Debug.LogError(result);
                    break;
                case LogType.Warning:
                    UnityEngine.Debug.LogWarning(result);
                    break;
                default:
                    UnityEngine.Debug.Log(result);
                    break;
            }
        }

        /// <summary>
        /// Determines a category from the script name that called the previous method
        /// </summary>
        /// <returns>Assembly name</returns>
        private static string GetCallingCategory()
        {
            // Get stack trace method
            string path = new StackTrace()?.GetFrame(3)?.GetMethod().DeclaringType.Name;
            if (string.IsNullOrEmpty(path))
            {
                return "NoStacktrace";
            }
            // Return path
            return path;
        }

        /// <summary>
        /// Determines a category from the script name that called the previous method
        /// </summary>
        /// <returns>Assembly name</returns>
        private static void WrapWithCallingLink(StringBuilder builder, int startIndex)
        {
            #if UNITY_EDITOR && UNITY_2021_2_OR_NEWER
            StackTrace stackTrace = new StackTrace(true);
            StackFrame stackFrame = stackTrace.GetFrame(3);
            string callingFileName = stackFrame.GetFileName().Replace('\\', '/');
            int callingFileLine = stackFrame.GetFileLineNumber();
            builder.Insert(startIndex, $"<a href=\"{callingFileName}\" line=\"{callingFileLine}\">");
            builder.Append("</a>");
            #endif
        }

        /// <summary>
        /// Get hex value for each log type
        /// </summary>
        private static void WrapWithLogColor(StringBuilder builder, int startIndex, LogType logType)
        {
            #if UNITY_EDITOR
            string hex;
            switch (logType)
            {
                case LogType.Error:
                    hex = "FF0000";
                    break;
                case LogType.Warning:
                    hex = "FFFF00";
                    break;
                default:
                    hex = "00FF00";
                    break;
            }
            builder.Insert(startIndex, $"<color=#{hex}>");
            builder.Append("</color>");
            #endif
        }
    }
}

