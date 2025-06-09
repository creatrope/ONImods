using UnityEngine;
using ArtifactsPlus;
using System.Linq;
using System.Collections.Generic;
using Klei.AI;
using System.Reflection;
using System;
using System.IO;
using Newtonsoft.Json;
using HarmonyLib;

namespace ArtifactsPlus
{
    public class ArtifactHotkeyListener : MonoBehaviour
    {
        // List of all supported hotkeys and their actions
        private readonly Dictionary<KeyCode, System.Action> hotkeyActions = new Dictionary<KeyCode, System.Action>();
        private KeyCode? _lastHotkey = null;

        void Awake()
        {
            // Debug.Log("[ArtifactsPlus] ArtifactHotkeyListener Awake called.");
            if (FindObjectsOfType<ArtifactHotkeyListener>().Length > 1)
            {
                // Debug.Log("[ArtifactsPlus] Duplicate ArtifactHotkeyListener found, destroying.");
                Destroy(this);
                return;
            }
            DontDestroyOnLoad(this.gameObject);

            hotkeyActions[KeyCode.F9] = () =>
            {
                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                {
                    // Debug.Log("[ArtifactsPlus] ALT+F9 detected in Update.");
                    CustomLogger.Log("[ArtifactsPlus] ALT+F9 pressed: Printing artifact infusions for all minions.");
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
                }
            };
            hotkeyActions[KeyCode.F10] = () =>
            {
                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                {
                    // Debug.Log("[ArtifactsPlus] ALT+F11 detected in Update.");
                    CustomLogger.Log("[ArtifactsPlus] ALT+F11 pressed: Stripping all artifact modifiers and status effects from all minions.");
                    ArtifactEffectTracker.StripAllArtifactEffectsFromAllMinions();
                }
            };
        }

        void Start()
        {
            // Debug.Log("[ArtifactsPlus] ArtifactHotkeyListener Start called.");
        }

        void Update()
        {
            foreach (var kvp in hotkeyActions)
            {
                HandleDebouncedHotkey(kvp.Key, kvp.Value);
            }
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
    }
}
