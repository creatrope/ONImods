using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Klei.AI;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;
using HLib; // Add this if you want to use 'CustomLogger' directly, or fully qualify as shown below

namespace ArtifactsPlus
{
    public class ArtifactEffectTracker : MonoBehaviour
    {
        // Map of minion to effect and the artifact that applied it
        private static readonly Dictionary<GameObject, Dictionary<string, int>> minionEffectArtifactMap = new Dictionary<GameObject, Dictionary<string, int>>();

        // Map of minion to modifier and the artifact that applied it
        private static readonly Dictionary<GameObject, Dictionary<(string attrName, float value, int artifactInstanceId), int>> minionModifierArtifactMap = new Dictionary<GameObject, Dictionary<(string attrName, float value, int artifactInstanceId), int>>();

        public static bool TryGetArtifactModifiers(string artifactId, out Dictionary<string, float> modifiers)
        {
            return ArtifactStateTracker.TryGetArtifactAttributes(artifactId, out modifiers);
        }

        private static IEnumerable<GameObject> GetAllMinions()
        {
            foreach (var minion in UnityEngine.Object.FindObjectsOfType<KPrefabID>())
            {
                if (minion != null && minion.HasTag("Minion"))
                    yield return minion.gameObject;
            }
        }

        public static void AddStatusToAllMinions(string statusId)
        {
            foreach (var minion in GetAllMinions())
            {
                var effects = minion.GetComponent<Effects>();
                if (effects != null && !effects.HasEffect(statusId))
                    effects.Add(statusId, true);
            }
        }

        public static void RemoveStatusFromAllMinions(string statusId)
        {
            foreach (var minion in GetAllMinions())
            {
                var effects = minion.GetComponent<Effects>();
                if (effects != null && effects.HasEffect(statusId))
                    effects.Remove(statusId);
            }
        }

        public static void StripAllArtifactEffectsFromAllMinions()
        {
            foreach (var minion in GetAllMinions())
            {
                foreach (var artifact in ArtifactStateTracker.ArtifactsOnPedestals)
                {
                    if (artifact == null) continue;
                    string artifactId = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                    RemoveArtifactModifiersToMinion(minion, artifact);
                    RemoveArtifactStatusEffectsToMinion(minion, artifact);
                }
            }
        }

        public static void ApplyArtifactModifiersToMinion(GameObject minion, GameObject artifact)
        {
            if (minion == null || artifact == null)
                return;

            int artifactInstanceId = artifact.GetInstanceID();
            var minionModifiers = minion.GetComponent<MinionModifiers>();
            if (minionModifiers == null)
                return;

            if (TryGetArtifactModifiers(artifact.GetComponent<KPrefabID>()?.PrefabTag.Name, out var modifierDict))
            {
                foreach (var kvp in modifierDict)
                {
                    string attrName = kvp.Key;
                    float modValue = kvp.Value;

                    Klei.AI.Attribute attribute = Db.Get().Attributes.resources
                        .FirstOrDefault(a => string.Equals(a.Id, attrName, StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(a.Name, attrName, StringComparison.OrdinalIgnoreCase));

                    if (attribute == null)
                        continue;

                    var attrInstance = minionModifiers.attributes?.Get(attribute);
                    if (attrInstance != null)
                    {
                        var modifierKey = (attrName, modValue, artifactInstanceId);

                        // Debugging: Attempting to add a modifier
                        CustomLogger.Log($"Attempting to add modifier '{attrName}' with value '{modValue}' for artifact '{artifactInstanceId}'.");

                        bool modifierExists = false;
                        for (int i = 0; i < attrInstance.Modifiers.size; i++)
                        {
                            var mod = attrInstance.Modifiers[i];
                            if (mod.Description == $"Artifact Modifier: {artifactInstanceId}" && mod.Value == modValue)
                            {
                                modifierExists = true;
                                break;
                            }
                        }

                        if (modifierExists)
                        {
                            CustomLogger.Log($"Modifier '{attrName}' with value '{modValue}' for artifact '{artifactInstanceId}' already exists. Skipping.");
                            continue;
                        }

                        // Add a new modifier for stacking
                        var modifier = new AttributeModifier(attribute.Id, modValue, $"Artifact Modifier: {artifactInstanceId}");
                        attrInstance.Add(modifier);

                        // Debugging: Modifier added
                        CustomLogger.Log($"Modifier '{attrName}' with value '{modValue}' for artifact '{artifactInstanceId}' added successfully.");

                        // Track which artifact applied this modifier
                        if (!minionModifierArtifactMap.TryGetValue(minion, out var modMap))
                        {
                            modMap = new Dictionary<(string attrName, float value, int artifactInstanceId), int>();
                            minionModifierArtifactMap[minion] = modMap;
                        }
                        modMap[modifierKey] = artifactInstanceId;
                    }
                }
            }
        }

        public static void RemoveArtifactModifiersToMinion(GameObject minion, GameObject artifact)
        {
            if (minion == null || artifact == null)
                return;

            int artifactInstanceId = artifact.GetInstanceID();
            var minionModifiers = minion.GetComponent<MinionModifiers>();
            if (minionModifiers == null)
                return;

            if (TryGetArtifactModifiers(artifact.GetComponent<KPrefabID>()?.PrefabTag.Name, out var modifierDict))
            {
                foreach (var kvp in modifierDict)
                {
                    string attrName = kvp.Key;
                    float modValue = kvp.Value;

                    Klei.AI.Attribute attribute = Db.Get().Attributes.resources
                        .FirstOrDefault(a => string.Equals(a.Id, attrName, StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(a.Name, attrName, StringComparison.OrdinalIgnoreCase));

                    if (attribute == null)
                        continue;

                    var attrInstance = minionModifiers.attributes?.Get(attribute);
                    if (attrInstance != null)
                    {
                        var modifierKey = (attrName, modValue, artifactInstanceId);

                        // Debugging: Attempting to delete a modifier
                        CustomLogger.Log($"Attempting to delete modifier '{attrName}' with value '{modValue}' for artifact '{artifactInstanceId}'.");

                        var toRemove = new List<AttributeModifier>();
                        for (int i = 0; i < attrInstance.Modifiers.size; i++)
                        {
                            var mod = attrInstance.Modifiers[i];
                            if (mod.Description == $"Artifact Modifier: {artifactInstanceId}" && mod.Value == modValue)
                            {
                                toRemove.Add(mod);
                            }
                        }
                        for (int i = 0; i < toRemove.Count; i++)
                        {
                            attrInstance.Remove(toRemove[i]);

                            // Debugging: Modifier deleted
                            CustomLogger.Log($"Modifier '{attrName}' with value '{modValue}' for artifact '{artifactInstanceId}' deleted successfully.");
                        }

                        // Remove tracking
                        if (minionModifierArtifactMap.TryGetValue(minion, out var modMap))
                        {
                            modMap.Remove(modifierKey);
                            if (modMap.Count == 0)
                                minionModifierArtifactMap.Remove(minion);
                        }
                    }
                }
            }
        }

        public static void ApplyArtifactStatusEffectsToMinion(GameObject minion, GameObject artifact)
        {
            if (artifact == null)
                return;

            int artifactInstanceId = artifact.GetInstanceID();
            if (ArtifactStateTracker.TryGetArtifactEffects(artifact.GetComponent<KPrefabID>()?.PrefabTag.Name, out var effects) && effects != null)
            {
                foreach (var effect in effects)
                {
                    string effectId = effect.Key;
                    var effectsComponent = minion.GetComponent<Effects>();
                    if (effectsComponent == null)
                        continue;

                    // Debugging: Attempting to add an effect
                    CustomLogger.Log($"Attempting to add effect '{effectId}' for artifact '{artifactInstanceId}'.");

                    if (minionEffectArtifactMap.TryGetValue(minion, out var effectMap) && effectMap.TryGetValue(effectId, out var appliedArtifactInstanceId))
                    {
                        if (appliedArtifactInstanceId == artifactInstanceId)
                        {
                            CustomLogger.Log($"Effect '{effectId}' for artifact '{artifactInstanceId}' already exists. Skipping.");
                            continue;
                        }
                    }

                    if (!effectsComponent.HasEffect(effectId))
                    {
                        effectsComponent.Add(new HashedString(effectId), true);

                        // Debugging: Effect added
                        CustomLogger.Log($"Effect '{effectId}' for artifact '{artifactInstanceId}' added successfully.");
                    }

                    if (!minionEffectArtifactMap.TryGetValue(minion, out var newEffectMap))
                    {
                        newEffectMap = new Dictionary<string, int>();
                        minionEffectArtifactMap[minion] = newEffectMap;
                    }
                    newEffectMap[effectId] = artifactInstanceId;
                }
            }
        }

        public static void RemoveArtifactStatusEffectsToMinion(GameObject minion, GameObject artifact)
        {
            if (artifact == null)
                return;

            int artifactInstanceId = artifact.GetInstanceID();
            if (ArtifactStateTracker.TryGetArtifactEffects(artifact.GetComponent<KPrefabID>()?.PrefabTag.Name, out var effects) && effects != null)
            {
                foreach (var effect in effects)
                {
                    string effectId = effect.Key;
                    var effectsComponent = minion.GetComponent<Effects>();
                    if (effectsComponent == null)
                        continue;

                    // Debugging: Attempting to delete an effect
                    CustomLogger.Log($"Attempting to delete effect '{effectId}' for artifact '{artifactInstanceId}'.");

                    if (minionEffectArtifactMap.TryGetValue(minion, out var effectMap) && effectMap.TryGetValue(effectId, out var appliedArtifactInstanceId))
                    {
                        if (appliedArtifactInstanceId == artifactInstanceId)
                        {
                            effectsComponent.Remove(new HashedString(effectId));

                            // Debugging: Effect deleted
                            CustomLogger.Log($"Effect '{effectId}' for artifact '{artifactInstanceId}' deleted successfully.");

                            effectMap.Remove(effectId);
                            if (effectMap.Count == 0)
                                minionEffectArtifactMap.Remove(minion);
                        }
                    }
                }
            }
        }

        public static HashSet<GameObject> GetMinionsForArtifact(GameObject artifact)
        {
            var result = new HashSet<GameObject>();
            if (artifact == null) return result;
            string artifactId = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
            foreach (var minion in GetAllMinions())
            {
                if (MinionHasArtifact(minion, artifactId))
                    result.Add(minion);
            }
            return result;
        }

        private static bool MinionHasArtifact(GameObject minion, string artifactId)
        {
            // Check status effects
            if (minionEffectArtifactMap.TryGetValue(minion, out var effectMap))
            {
                if (effectMap.Values.Contains(artifactId.GetHashCode()))
                    return true;
            }

            // Check attribute modifiers
            if (minionModifierArtifactMap.TryGetValue(minion, out var modMap))
            {
                if (modMap.Keys.Any(k => k.artifactInstanceId == artifactId.GetHashCode()))
                    return true;
            }
            return false;
        }

        public static string GetMinionArtifactInfusions(GameObject minion)
        {
            var summary = new System.Text.StringBuilder();
            var listedEffects = new HashSet<string>();
            var listedModifiers = new HashSet<(string attrName, float value, int artifactInstanceId)>();

            // Effects
            if (minionEffectArtifactMap.TryGetValue(minion, out var effectMap))
            {
                foreach (var kvp in effectMap)
                {
                    if (listedEffects.Add(kvp.Key))
                    {
                        string properName = GetArtifactProperName(kvp.Value);
                        summary.AppendLine($"{kvp.Key} ({properName})");
                    }
                }
            }

            // Modifiers
            if (minionModifierArtifactMap.TryGetValue(minion, out var modMap))
            {
                foreach (var kvp in modMap)
                {
                    string attrName = kvp.Key.attrName;
                    float val = kvp.Key.value;
                    int artifactInstanceId = kvp.Key.artifactInstanceId;
                    if (listedModifiers.Add((attrName, val, artifactInstanceId)))
                    {
                        string properName = GetArtifactProperName(artifactInstanceId);
                        summary.AppendLine($"{attrName} {(val >= 0 ? "+" : "")}{val} ({properName})");
                    }
                }
            }

            return summary.ToString();
        }

        private static string GetArtifactProperName(int artifactInstanceId)
        {
            // Try to find the artifact GameObject in the world using the instance ID
            var artifact = ArtifactsPlus.ArtifactStateTracker.ArtifactsOnPedestals
                .FirstOrDefault(a => a != null && a.GetInstanceID() == artifactInstanceId);
            if (artifact != null)
            {
                var selectable = artifact.GetComponent<KSelectable>();
                if (selectable != null)
                    return selectable.GetProperName();
            }
            // Fallback to instance ID as a string if not found
            return artifactInstanceId.ToString();
        }
    }
}