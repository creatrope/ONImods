using System;
using System.IO;
using UnityEngine;

namespace HLib
{
    public class Logger
    {
        private readonly string modName;
        private bool isLoggingEnabled;

        public Logger(string modName)
        {
            this.modName = modName;
            // Enable logging if the executable is running from a "Local" directory
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string modDir = Path.GetDirectoryName(exePath);
            isLoggingEnabled = modDir != null && modDir.IndexOf("Local", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void SetLoggingState(bool enabled)
        {
            isLoggingEnabled = enabled;
        }

        public void LogDebug(string message)
        {
            if (isLoggingEnabled)
            {
                UnityEngine.Debug.Log($"[{modName}] {message}");
            }
        }
    }
}