using UnityEngine;
using ArtifactsPlus; // Add this using directive
using System.Linq;   // For LINQ usage
using System.Collections.Generic; // Add this using directive
using Klei.AI; // Add this using directive for AttributeModifier

namespace ArtifactsPlus
{
    public class ArtifactHotkeyListener : MonoBehaviour
    {
        void Start()
        {
            CustomLogger.Log("[HOTKEY] ArtifactHotkeyListener attached and Start() called.");
        }

        void Update()
        {
            // F9: Print artifact-induced modifiers on all minions
            if (UnityEngine.Input.GetKeyDown(KeyCode.F9))
            {
                foreach (var minion in GetAllMinions()) // Use the local GetAllMinions method
                {
                    string summary = ArtifactEffectTracker.GetActiveArtifactEffectsSummary(minion);
                    CustomLogger.Log($"[HOTKEY][{minion.name}] {summary}");
                }
                Debug.Log("[ArtifactsPlus] F9 pressed: Logged artifact effects summary for all minions.");
            }
            // F10: Print all active artifacts
            else if (UnityEngine.Input.GetKeyDown(KeyCode.F10))
            {
                PrintAllActiveArtifacts();
            }
            // F11: Strip all artifact modifiers and status effects from all minions
            else if (UnityEngine.Input.GetKeyDown(KeyCode.F11))
            {
                StripAllArtifactEffectsFromAllMinions();
                Debug.Log("[ArtifactsPlus] F11 pressed: Stripped all artifact modifiers and status effects from all minions.");
            }
        }

        private IEnumerable<GameObject> GetAllMinions()
        {
            return UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && kp.HasTag("Minion"))
                .Select(kp => kp.gameObject);
        }

        private void PrintAllActiveArtifacts()
        {
            CustomLogger.Log("[HOTKEY] Printing all active artifacts:");
            foreach (var artifact in ArtifactsPlus.ArtifactStateTracker.ArtifactsOnPedestals)
            {
                if (artifact == null) continue;
                string name = artifact.GetComponent<KSelectable>()?.GetProperName() ?? artifact.name;
                CustomLogger.Log($"[HOTKEY] Active Artifact: {name} (ID={artifact.GetInstanceID()})");
            }
        }

        private void PrintArtifactModifiersOnAllMinions()
        {
            CustomLogger.Log("[HOTKEY] Printing artifact-induced modifiers on all minions:");
            var minions = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && kp.HasTag("Minion"))
                .Select(kp => kp.gameObject);

            foreach (var minion in minions)
            {
                var minionModifiers = minion.GetComponent<MinionModifiers>();
                if (minionModifiers == null) continue;

                var attribs = minionModifiers.attributes;
                if (attribs == null) continue;

                bool anyArtifactMods = false;
                foreach (var attr in attribs)
                {
                    var attrInstance = attr;
                    var artifactMods = new List<AttributeModifier>();
                    for (int i = 0; i < attrInstance.Modifiers.Count; i++)
                    {
                        var mod = attrInstance.Modifiers[i];
                        if (mod != null && mod.Description != null && mod.Description.StartsWith("Artifact Effect:"))
                        {
                            artifactMods.Add(mod);
                        }
                    }

                    if (artifactMods.Count > 0)
                    {
                        if (!anyArtifactMods)
                        {
                            string minionName = minion.GetComponent<KSelectable>()?.GetProperName() ?? minion.name;
                            CustomLogger.Log($"[HOTKEY] Minion: {minionName} (ID={minion.GetInstanceID()})");
                            anyArtifactMods = true;
                        }
                        foreach (var mod in artifactMods)
                        {
                            CustomLogger.Log($"    Attribute: {attrInstance.Attribute.Name}, Modifier: {mod.Value} ({mod.Description})");
                        }
                    }
                }
            }
        }

        private void DumpAllEffects()
        {
            foreach (var effect in Db.Get().effects.resources)
            {
                CustomLogger.Log($"Effect: {effect.Id} - {effect.Name} (Duration: {effect.duration})");
            }
            Debug.Log("[ArtifactsPlus] Dumped all effects to log.");
        }

        private void StripAllArtifactEffectsFromAllMinions()
        {
            var allArtifacts = ArtifactsPlus.ArtifactStateTracker.ArtifactsOnPedestals;
            foreach (var minion in GetAllMinions())
            {
                foreach (var artifact in allArtifacts)
                {
                    if (artifact == null) continue;
                    string artifactId = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                    ArtifactEffectTracker.ApplyOrRemoveArtifactModifiersToMinion(minion, artifactId, false);
                    ArtifactEffectTracker.ApplyOrRemoveArtifactStatusEffectsToMinion(minion, artifactId, false);
                }
                CustomLogger.Log($"[HOTKEY] Stripped all artifact effects from minion '{minion.name}'");
            }
        }
    }
}