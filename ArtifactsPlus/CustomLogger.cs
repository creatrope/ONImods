using System;
using System.IO;
using UnityEngine;

namespace ArtifactsPlus
{
    public static class CustomLogger
    {
        private static string logPath;
        private static bool useCustomLog = true;
        private static bool initialized = false;

        public static string LogPath
        {
            get
            {
                EnsureInitialized();
                return logPath;
            }
        }

        public static void Log(string message)
        {
            EnsureInitialized();
            if (useCustomLog && !string.IsNullOrEmpty(logPath))
            {
                try
                {
                    File.AppendAllText(logPath, $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
                }
                catch (Exception ex)
                {
                    Debug.Log($"[ArtifactsPlus] Failed to write to custom log: {ex.Message}");
                    useCustomLog = false;
                    Debug.Log($"[ArtifactsPlus] {message}");
                }
            }
            else
            {
                Debug.Log($"[ArtifactsPlus] {message}");
            }
        }

        private static void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            try
            {
                string configPath = ModInit.ArtifactPowersConfigPath;
                if (File.Exists(configPath))
                {
                    var configText = File.ReadAllText(configPath);
                    var configJson = Newtonsoft.Json.Linq.JObject.Parse(configText);
                    var logPathToken = configJson["DebugLogPath"];
                    if (logPathToken != null && !string.IsNullOrWhiteSpace(logPathToken.ToString()))
                    {
                        logPath = logPathToken.ToString();
                        useCustomLog = true;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[ArtifactsPlus] Error reading config for log path: {ex.Message}");
            }

            useCustomLog = false;
            logPath = null;
            Debug.Log("[ArtifactsPlus] No custom log path found in config. Using Debug.Log for logging.");
        }
    }
}