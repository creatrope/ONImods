using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HLib
{
    /// <summary>
    /// Standalone, reusable hotkey listener. Allows registration of hotkey actions.
    /// </summary>
    public class HotkeyListener
    {
        private readonly Dictionary<string, Action> hotkeyActions = new Dictionary<string, Action>();
        private readonly HashSet<string> pressedKeys = new HashSet<string>();
        private readonly Dictionary<string, float> lastExecutionTimes = new Dictionary<string, float>(); // Track last execution times
        private const float DebounceInterval = 0.5f; // Debounce interval in seconds

        /// <summary>
        /// Register a hotkey or combo key and its associated action.
        /// </summary>
        public void RegisterHotkey(string comboKey, Action action)
        {
            hotkeyActions[comboKey] = action;
        }

        /// <summary>
        /// Unregister a hotkey or combo key.
        /// </summary>
        public void UnregisterHotkey(string comboKey)
        {
            if (hotkeyActions.ContainsKey(comboKey))
                hotkeyActions.Remove(comboKey);
        }

        /// <summary>
        /// Call this method from your application's main loop to detect and invoke hotkey actions.
        /// </summary>
        public void Update()
        {
            // Clear previously tracked pressed keys
            pressedKeys.Clear();

            // Detect all currently pressed keys
            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKey(keyCode))
                {
                    pressedKeys.Add(keyCode.ToString());
                }
            }

            // Check registered hotkeys
            foreach (var hotkey in hotkeyActions)
            {
                var comboKeys = hotkey.Key.Split('+'); // Split combo keys (e.g., "Ctrl+F11")
                bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool altPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                bool allKeysPressed = comboKeys.All(key =>
                    key.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) && ctrlPressed ||
                    key.Equals("Alt", StringComparison.OrdinalIgnoreCase) && altPressed ||
                    key.Equals("Shift", StringComparison.OrdinalIgnoreCase) && shiftPressed ||
                    pressedKeys.Contains(key)
                );

                if (allKeysPressed)
                {
                    // Check debounce
                    if (!lastExecutionTimes.TryGetValue(hotkey.Key, out float lastExecution) ||
                        Time.time - lastExecution >= DebounceInterval)
                    {
                        hotkey.Value.Invoke();
                        lastExecutionTimes[hotkey.Key] = Time.time; // Update last execution time
                    }
                }
            }
        }
    }
}