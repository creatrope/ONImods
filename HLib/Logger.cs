using System;
using System.IO;
using System.Text.RegularExpressions;
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
            // Enable logging if the executable path contains "/Dev/", "\Dev\", "/Local/", or "\Local\"
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string modDir = Path.GetDirectoryName(exePath);
            isLoggingEnabled = false;
            if (modDir != null)
            {
                // Split the path into directory names and check for "Dev" or "Local"
                foreach (var part in modDir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                {
                    if (part.Equals("Dev", StringComparison.OrdinalIgnoreCase) ||
                        part.Equals("Local", StringComparison.OrdinalIgnoreCase))
                    {
                        isLoggingEnabled = true;
                        break;
                    }
                }
            }
            Debug.Log($"[{modName}] Logging is {(isLoggingEnabled ? "ENABLED" : "DISABLED")}.");
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