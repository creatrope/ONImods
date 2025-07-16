using System;
using UnityEngine;

namespace HLib
{
    public class Logger
    {
        private readonly string modName;
        private bool isLoggingEnabled = false;

        public Logger(string modName)
        {
            this.modName = modName;
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