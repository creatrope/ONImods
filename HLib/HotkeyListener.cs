using System;
using System.Collections.Generic;

namespace HLib
{
    /// <summary>
    /// Standalone, reusable hotkey listener. Allows registration of hotkey actions.
    /// Platform-agnostic: does not depend on Unity.
    /// </summary>
    public class HotkeyListener
    {
        // Use string for key representation in a non-Unity context
        private readonly Dictionary<string, Action> hotkeyActions = new Dictionary<string, Action>();
        private string _lastHotkey = null;

        /// <summary>
        /// Register a hotkey and its associated action.
        /// </summary>
        public void RegisterHotkey(string key, Action action)
        {
            hotkeyActions[key] = action;
        }

        /// <summary>
        /// Unregister a hotkey.
        /// </summary>
        public void UnregisterHotkey(string key)
        {
            if (hotkeyActions.ContainsKey(key))
                hotkeyActions.Remove(key);
        }

        /// <summary>
        /// Call this method from your application's main loop, passing in the currently pressed keys.
        /// </summary>
        public void Update(IEnumerable<string> currentlyPressedKeys)
        {
            foreach (var kvp in hotkeyActions)
            {
                HandleDebouncedHotkey(kvp.Key, kvp.Value, currentlyPressedKeys);
            }
        }

        private void HandleDebouncedHotkey(string key, Action action, IEnumerable<string> currentlyPressedKeys)
        {
            bool isDown = false;
            foreach (var pressed in currentlyPressedKeys)
            {
                if (string.Equals(pressed, key, StringComparison.OrdinalIgnoreCase))
                {
                    isDown = true;
                    break;
                }
            }

            if (isDown)
            {
                if (_lastHotkey != null && _lastHotkey == key)
                {
                    // Absorb and ignore if same as last hotkey
                    return;
                }
                _lastHotkey = key;
                action();
            }
            else if (_lastHotkey != null && _lastHotkey == key)
            {
                // Reset when key is released
                _lastHotkey = null;
            }
        }
    }
}
