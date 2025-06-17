using System;
using System.IO;

namespace HLib
{
    /// <summary>
    /// A simple, reusable logger for writing text messages to a file.
    /// </summary>
    public static class CustomLogger
    {
        /// <summary>
        /// Enable or disable logging globally.
        /// </summary>
        public static bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Enable or disable custom logging.
        /// </summary>
        public static bool Enabled { get; set; } = true;

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
            if (!EnableLogging || !Enabled)
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

                using (var fs = new FileStream(logPathToUse, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var sw = new StreamWriter(fs))
                {
                    sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CustomLogger] Failed to write log: {ex}");
            }
        }

        /// <summary>
        /// Overwrites the log file with a new header and resets the logger.
        /// Optionally specify a log file name (overrides LogPath's filename).
        /// </summary>
        /// <param name="logName">Optional log file name (e.g., "MyLog.log").</param>
        public static void ResetLog(string logName = null)
        {
            if (!Enabled)
                return;

            try
            {
                string logPathToUse = LogPath;
                if (!string.IsNullOrWhiteSpace(logName))
                {
                    var dir = Path.GetDirectoryName(LogPath);
                    logPathToUse = Path.Combine(dir, logName);
                }

                using (var fs = new FileStream(logPathToUse, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var sw = new StreamWriter(fs))
                {
                    sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log reset.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CustomLogger] Failed to reset log: {ex}");
            }
        }

        /// <summary>
        /// Resets the log file by overwriting it with an empty string or a reset message.
        /// </summary>
        public static void Reset()
        {
            if (!Enabled)
                return;

            try
            {
                File.WriteAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log reset.\n");
            }
            catch (Exception)
            {
                // Ignore errors on reset
            }
        }
    }
}