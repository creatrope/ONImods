using UnityEngine;
using ArtifactsPlus;
using System.Linq;
using System.Collections.Generic;
using Klei.AI;
using System.Reflection;
using System;
using System.IO;

namespace ArtifactsPlus
{
    public class ArtifactHotkeyListener : MonoBehaviour
    {
        // Track which hotkeys are currently pressed to debounce
        private readonly Dictionary<KeyCode, bool> hotkeyDown = new Dictionary<KeyCode, bool>
        {
            { KeyCode.F1, false },
            { KeyCode.F9, false },
            { KeyCode.F10, false },
            { KeyCode.F11, false }
        };

        private KeyCode? _lastHotkey = null;

        void Awake()
        {
            if (FindObjectsOfType<ArtifactHotkeyListener>().Length > 1)
            {
                Destroy(this);
                return;
            }
            DontDestroyOnLoad(this.gameObject); // Optional: persist across scenes
        }

        void Start()
        {
            Debug.Log("[ArtifactsPlus] Custom log location: " + CustomLogger.LogPath);
            CustomLogger.Log("[HOTKEY] ArtifactHotkeyListener attached and Start() called.");
            Debug.Log("[ArtifactsPlus] ArtifactHotkeyListener instance count: " + FindObjectsOfType<ArtifactHotkeyListener>().Length);
        }

        void Update()
        {
            HandleDebouncedHotkey(KeyCode.F1, () =>
            {
                CustomLogger.Log("[ArtifactsPlus] F1 pressed: Printing hotkey summary.");
                PrintHotkeySummary();
            });

            HandleDebouncedHotkey(KeyCode.F9, () =>
            {
                CustomLogger.Log("[ArtifactsPlus] F9 pressed: Printing artifact infusions for all minions.");
                var minions = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                    .Where(kp => kp != null && kp.HasTag("Minion"))
                    .Select(kp => kp.gameObject)
                    .ToList();
                foreach (var minion in minions)
                {
                    string minionName = minion.GetComponent<KSelectable>()?.GetProperName() ?? minion.name;
                    string infusions = ArtifactEffectTracker.GetMinionArtifactInfusions(minion);
                    string singleLine = string.IsNullOrWhiteSpace(infusions)
                        ? "(no artifact infusions)"
                        : string.Join("; ", infusions
                            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()));
                    CustomLogger.Log($"[HOTKEY] {minionName}: {singleLine}");
                }
            });

            HandleDebouncedHotkey(KeyCode.F11, () =>
            {
                CustomLogger.Log("[ArtifactsPlus] F11 pressed: Stripping all artifact modifiers and status effects from all minions.");
                ArtifactEffectTracker.StripAllArtifactEffectsFromAllMinions();
            });
        }

        private void HandleDebouncedHotkey(KeyCode key, System.Action action)
        {
            if (UnityEngine.Input.GetKeyDown(key))
            {
                if (_lastHotkey.HasValue && _lastHotkey.Value == key)
                {
                    // Absorb and ignore if same as last hotkey
                    return;
                }
                _lastHotkey = key;
                action();
            }
            else if (_lastHotkey.HasValue && _lastHotkey.Value == key && !UnityEngine.Input.GetKey(key))
            {
                // Reset when key is released
                _lastHotkey = null;
            }
        }

        private void PrintHotkeySummary()
        {
            string summary =
                "[ArtifactsPlus] Hotkey Summary:\n" +
                "F1  - Show this summary of all hotkey functions.\n" +
                "F9  - Print artifact infusions for all minions.\n" +
                "F11 - Strip all artifact modifiers and status effects from all minions.";
            CustomLogger.Log(summary);
        }
    }
}
