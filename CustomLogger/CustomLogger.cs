using System;
using System.IO;

namespace CustomLogger
{
    /// <summary>
    /// A simple, reusable logger for writing text messages to a file.
    /// </summary>
    public static class CustomLogger
    {
        private static bool _initialized = false;

        /// <summary>
        /// Enable or disable logging globally.
        /// </summary>
        public static bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Path to the log file. Set this before logging if you want a custom location.
        /// </summary>
        public static string LogPath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", "Klei", "Oxygen Not Included", "CustomLogger.log"
        );

        /// <summary>
        /// Writes a message to the log file if logging is enabled.
        /// Optionally specify a log file name (overrides LogPath's filename).
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="logName">Optional log file name (e.g., "MyLog.log").</param>
        public static void Log(string message, string logName = null)
        {
            if (!EnableLogging)
                return;

            try
            {
                string logPathToUse = LogPath;
                if (!string.IsNullOrWhiteSpace(logName))
                {
                    var dir = Path.GetDirectoryName(LogPath);
                    logPathToUse = Path.Combine(dir, logName);
                }
                else
                {
                    var dir = Path.GetDirectoryName(LogPath);
                }

                var logDir = Path.GetDirectoryName(logPathToUse);
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                // Only initialize once per log file per process
                if (!_initialized || (logName != null && !File.Exists(logPathToUse)))
                {
                    File.WriteAllText(logPathToUse, $"[CustomLogger] Log started at {System.DateTime.Now}\n");
                    _initialized = true;
                }
                File.AppendAllText(logPathToUse, $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
            }
            catch
            {
                // Swallow exceptions to avoid recursive logging or crashing.
            }
        }

        /// <summary>
        /// Overwrites the log file with a new header and resets the logger.
        /// Optionally specify a log file name (overrides LogPath's filename).
        /// </summary>
        /// <param name="logName">Optional log file name (e.g., "MyLog.log").</param>
        public static void ResetLog(string logName = null)
        {
            try
            {
                string logPathToUse = LogPath;
                if (!string.IsNullOrWhiteSpace(logName))
                {
                    var dir = Path.GetDirectoryName(LogPath);
                    logPathToUse = Path.Combine(dir, logName);
                }

                File.WriteAllText(logPathToUse, $"[CustomLogger] Log started at {System.DateTime.Now}\n");
                _initialized = true;
            }
            catch
            {
                // Swallow exceptions to avoid recursive logging or crashing.
            }
        }
    }
}