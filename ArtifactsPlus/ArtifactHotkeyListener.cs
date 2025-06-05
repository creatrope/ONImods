using UnityEngine;
using ArtifactsPlus;
using System.Linq;
using System.Collections.Generic;
using Klei.AI;
using System.Reflection;
using System;
using System.IO;
using Newtonsoft.Json;

namespace ArtifactsPlus
{
    public class ArtifactHotkeyListener : MonoBehaviour
    {
        // List of all supported hotkeys and their actions
        private readonly Dictionary<KeyCode, System.Action> hotkeyActions = new Dictionary<KeyCode, System.Action>();
        private KeyCode? _lastHotkey = null;

        void Awake()
        {
            Debug.Log("[ArtifactsPlus] ArtifactHotkeyListener Awake called.");
            if (FindObjectsOfType<ArtifactHotkeyListener>().Length > 1)
            {
                Debug.Log("[ArtifactsPlus] Duplicate ArtifactHotkeyListener found, destroying.");
                Destroy(this);
                return;
            }
            DontDestroyOnLoad(this.gameObject);

            // Register hotkey actions
            hotkeyActions[KeyCode.F1] = () =>
            {
                Debug.Log("[ArtifactsPlus] F1 detected in Update.");
                CustomLogger.Log("[ArtifactsPlus] F1 pressed: Printing hotkey summary.");
                PrintHotkeySummary();
            };
            hotkeyActions[KeyCode.F8] = () =>
            {
                Debug.Log("[ArtifactsPlus] F8 detected in Update.");
                DumpAllEffectsToLog();
            };
            hotkeyActions[KeyCode.F9] = () =>
            {
                Debug.Log("[ArtifactsPlus] F9 detected in Update.");
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
            };
            hotkeyActions[KeyCode.F11] = () =>
            {
                Debug.Log("[ArtifactsPlus] F11 detected in Update.");
                CustomLogger.Log("[ArtifactsPlus] F11 pressed: Stripping all artifact modifiers and status effects from all minions.");
                ArtifactEffectTracker.StripAllArtifactEffectsFromAllMinions();
            };
        }

        void Start()
        {
            Debug.Log("[ArtifactsPlus] ArtifactHotkeyListener Start called.");
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

        private void PrintHotkeySummary()
        {
            string summary =
                "[ArtifactsPlus] Hotkey Summary:\n" +
                "F1  - Show this summary of all hotkey functions.\n" +
                "F8  - Dump all effects to log as JSON.\n" +
                "F9  - Print artifact infusions for all minions.\n" +
                "F11 - Strip all artifact modifiers and status effects from all minions.";
            CustomLogger.Log(summary);
        }

        private void DumpAllEffectsToLog()
        {
            try
            {
                var effectsDb = Db.Get().effects;
                var effectList = new List<object>();

                foreach (var effect in effectsDb.resources)
                {
                    if (effect == null) continue;
                    effectList.Add(new
                    {
                        Id = effect.Id,
                        Name = effect.Name,
                        Duration = effect.duration,
                        Description = effect.description,
                        SelfModifiers = effect.SelfModifiers != null ? effect.SelfModifiers.Count : 0,
                        ShowInUI = effect.showInUI,
                    });
                }

                string json = JsonConvert.SerializeObject(effectList, Formatting.Indented);
                CustomLogger.Log("[EFFECTS DUMP]\n" + json);
                Debug.Log("[ArtifactsPlus] Effects dump written to log.");
            }
            catch (Exception ex)
            {
                CustomLogger.Log("[ERROR] Failed to dump effects: " + ex);
            }
        }
    }
}
