using System;
using System.IO;
using UnityEngine;

namespace HLib
{
    /// <summary>
    /// A reusable logger for writing text messages to a file, supporting instance-based usage for multi-mod logging.
    /// </summary>
    public class CustomLogger
    {
        private readonly string logPath;
        private bool isLoggingEnabled = false; // Tracks whether logging is enabled

        /// <summary>
        /// Initializes a new instance of the CustomLogger class for a specific mod.
        /// </summary>
        /// <param name="modName">The name of the mod.</param>
        public CustomLogger(string modName) // Fixed constructor name
        {
            if (string.IsNullOrWhiteSpace(modName))
                throw new ArgumentException("Mod name cannot be null or empty.", nameof(modName));

            try
            {
                var basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "AppData", "LocalLow", "Klei", "Oxygen Not Included"
                );

                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                logPath = Path.Combine(basePath, $"{modName}.log");

                Debug.Log($"[{modName}] LogPath set to: {logPath}");
            }
            catch
            {
                throw new InvalidOperationException("Failed to initialize log path.");
            }
        }

        /// <summary>
        /// Enables or disables logging output.
        /// </summary>
        /// <param name="enabled">True to enable logging, false to disable.</param>
        public void SetLoggingEnabled(bool enabled)
        {
            isLoggingEnabled = enabled;
        }

        /// <summary>
        /// Writes a message to the log file if logging is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Log(string message)
        {
            if (!isLoggingEnabled || string.IsNullOrWhiteSpace(logPath))
                return;

            try
            {
                var logDir = Path.GetDirectoryName(logPath);
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                using (var fs = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read))
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
        public void Reset()
        {
            if (string.IsNullOrWhiteSpace(logPath))
                return;

            try
            {
                File.WriteAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log reset.\n");
            }
            catch
            {
                // Silently discard errors during reset
            }
        }
    }
}