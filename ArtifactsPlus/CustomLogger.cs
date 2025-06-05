using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace ArtifactsPlus
{
    public static class CustomLogger
    {
        private static bool _initialized = false;
        public static string LogPath { get; private set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", "Klei", "Oxygen Not Included", "ArtifactsPlus.log"
        );

        public static void Log(string message)
        {
            if (!ArtifactsPlus.ArtifactsPlusOptions.Instance.CustomLogging)
                return;

            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!_initialized)
                {
                    // Overwrite the log file on first use (start fresh)
                    File.WriteAllText(LogPath, $"[ArtifactsPlus] Log started at {System.DateTime.Now}\n");
                    _initialized = true;
                }
                File.AppendAllText(LogPath, $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ArtifactsPlus] Failed to write to custom log: {ex}");
            }
        }

        // Call this at the start of each game load to reset the log
        public static void ResetLog()
        {
            File.WriteAllText(LogPath, $"[ArtifactsPlus] Log started at {System.DateTime.Now}\n");
            _initialized = true;
        }

        // No longer reads log path from config
        public static void InitializeFromConfig(string configPath)
        {
            // You may still want to read other config values here, but log path is now hardcoded.
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                // Deserialize or process other config values as needed
            }
        }
    }
}