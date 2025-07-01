using System;
using System.IO;
using UnityEngine; // Add this for Debug.Log

namespace HLib
{
    /// <summary>
    /// A simple, reusable logger for writing text messages to a file.
    /// </summary>
    public static class CustomLogger
    {
        /// <summary>
        /// Path to the log file. Automatically set based on the mod name.
        /// If null, logging is silently discarded.
        /// </summary>
        public static string LogPath { get; private set; }

        /// <summary>
        /// Sets the log path using the mod name. The internal path structure is hidden.
        /// </summary>
        /// <param name="modName">The name of the mod.</param>
        public static void SetLogPath(string modName)
        {
            if (string.IsNullOrWhiteSpace(modName))
            {
                // Silently discard invalid mod names
                LogPath = null;
                return;
            }

            try
            {
                var basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "AppData", "LocalLow", "Klei", "Oxygen Not Included"
                );

                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                LogPath = Path.Combine(basePath, $"{modName}.log");

                // Write the log path to Debug.Log
                Debug.Log($"[{modName}] LogPath set to: {LogPath}");
            }
            catch
            {
                // Silently discard errors during log path setup
                LogPath = null;
            }
        }

        /// <summary>
        /// Writes a message to the log file if LogPath is set.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(LogPath))
                return; // Silently discard logging if LogPath is not set

            try
            {
                var logDir = Path.GetDirectoryName(LogPath);
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                using (var fs = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var sw = new StreamWriter(fs))
                {
                    sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
                }
            }
            catch
            {
                // Silently discard errors during logging
            }
        }

        /// <summary>
        /// Resets the log file by overwriting it with a reset message.
        /// </summary>
        public static void Reset()
        {
            if (string.IsNullOrWhiteSpace(LogPath))
                return; // Silently discard reset operation if LogPath is not set

            try
            {
                File.WriteAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log reset.\n");
            }
            catch
            {
                // Silently discard errors during reset
            }
        }
    }
}