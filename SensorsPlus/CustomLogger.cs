using System;
using System.IO;
using UnityEngine;

namespace SensorsPlus
{
    public static class CustomLogger
    {
        private static string logFilePath;
        private static bool initialized = false;
        public static bool WriteToPlayerLog = false; // Set this flag to true to also write to player.log

        public static void Init(string moduleName)
        {
            logFilePath = Path.Combine(Application.persistentDataPath, $"{moduleName}_log.txt");
            try
            {
                File.WriteAllText(logFilePath, $"[{DateTime.Now}] {moduleName} log started.\n");
                initialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{moduleName}] Failed to create log file: {ex}");
            }
        }

        public static void Log(string moduleName, string message)
        {
            if (!initialized)
                Init(moduleName);

            string logLine = $"[{DateTime.Now}] [{moduleName}] {message}\n";
            try
            {
                File.AppendAllText(logFilePath, logLine);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{moduleName}] Failed to write to log file: {ex}");
            }

            if (WriteToPlayerLog)
            {
                Debug.Log($"[{moduleName}] {message}");
            }
        }
    }
}