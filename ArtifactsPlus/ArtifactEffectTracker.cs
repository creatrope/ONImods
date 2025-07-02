using HLib; // Add this if you want to use 'CustomLogger' directly, or fully qualify as shown below
using Klei.AI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ArtifactsPlus; // Add this namespace to access ArtifactStateTracker

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

        private static IEnumerable<GameObject> GetMinionsInSameWorld(GameObject artifact)
        {
            if (artifact == null)
                return Enumerable.Empty<GameObject>();

            var artifactWorld = artifact.GetComponent<KPrefabID>()?.GetMyWorldId();
            if (artifactWorld == null)
                return Enumerable.Empty<GameObject>();

            return GetAllMinions().Where(minion =>
            {
                var minionWorld = minion.GetComponent<KPrefabID>()?.GetMyWorldId();
                return minionWorld == artifactWorld;
            });
        }

        private static IEnumerable<GameObject> GetMinionsInSameRoom(GameObject artifact)
        {
            if (artifact == null)
                return Enumerable.Empty<GameObject>();

            var artifactRoom = artifact.GetComponent<KPrefabID>()?.GetComponent<RoomTracker>()?.room; // Use 'room' property instead of 'RoomId'
            if (artifactRoom == null)
                return Enumerable.Empty<GameObject>();

            return GetAllMinions().Where(minion =>
            {
                var minionRoom = minion.GetComponent<KPrefabID>()?.GetComponent<RoomTracker>()?.room; // Use 'room' property instead of 'RoomId'
                return minionRoom == artifactRoom;
            });
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
            string artifactInternalName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
            var minionModifiers = minion.GetComponent<MinionModifiers>();
            if (minionModifiers == null)
                return;

            if (TryGetArtifactModifiers(artifactInternalName, out var modifierDict))
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
                        //Patches.Logger.Log($"Attempting to add modifier '{attrName}' with value '{modValue}' for artifact '{artifactInternalName}' (ID: {artifactInstanceId}) to minion '{minion.name}'.");

                        // Enhanced existence check
                        bool modifierExists = minionModifierArtifactMap.TryGetValue(minion, out var modMap) && modMap.ContainsKey(modifierKey);

                        if (modifierExists)
                        {
                            Patches.Logger.Log($"Modifier '{attrName}' with value '{modValue}' for artifact '{artifactInternalName}' (ID: {artifactInstanceId}) already exists for minion '{minion.name}'. Skipping.");
                            continue;
                        }

                        // Create a unique identifier for the modifier
                        var modifier = new AttributeModifier(attribute.Id, modValue, "Skill Level");
                        modifier.DescriptionCB = () => artifactInstanceId.ToString(); // Convert int to string for callback
                        attrInstance.Add(modifier);

                        // Track which artifact applied this modifier
                        if (!minionModifierArtifactMap.TryGetValue(minion, out modMap))
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
            {
                Patches.Logger.Log("RemoveArtifactModifiersToMinion: Minion or artifact is null.");
                return;
            }

            int artifactInstanceId = artifact.GetInstanceID();

            var minionModifiers = minion.GetComponent<MinionModifiers>();
            if (minionModifiers == null)
            {
                Patches.Logger.Log($"RemoveArtifactModifiersToMinion: Minion '{minion.name}' does not have a MinionModifiers component.");
                return;
            }

            if (minionModifiers.attributes == null)
            {
                Patches.Logger.Log($"RemoveArtifactModifiersToMinion: Minion '{minion.name}' does not have any attributes.");
                return;
            }

            foreach (var attribute in Db.Get().Attributes.resources)
            {
                var attrInstance = minionModifiers.attributes.Get(attribute.Id);
                if (attrInstance == null)
                {
                    continue;
                }

                for (int i = attrInstance.Modifiers.size - 1; i >= 0; i--) // Iterate in reverse to safely remove items
                {
                    var currentModifier = attrInstance.Modifiers[i];
                    string descriptionCB = currentModifier.DescriptionCB?.Invoke();
                    if (descriptionCB != null)
                    {
                        if (descriptionCB.Equals(artifactInstanceId.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            Patches.Logger.Log($"Removing Modifier: ID='{currentModifier.AttributeId}', Value='{currentModifier.Value}', Description='{currentModifier.Description}', DescriptionCB='{descriptionCB}'");
                            attrInstance.Remove(currentModifier); // Safely remove the modifier
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
                    Patches.Logger.Log($"Attempting to add effect '{effectId}' for artifact '{artifactInstanceId}'.");

                    if (minionEffectArtifactMap.TryGetValue(minion, out var effectMap) && effectMap.TryGetValue(effectId, out var appliedArtifactInstanceId))
                    {
                        if (appliedArtifactInstanceId == artifactInstanceId)
                        {
                            Patches.Logger.Log($"Effect '{effectId}' for artifact '{artifactInstanceId}' already exists. Skipping.");
                            continue;
                        }
                    }

                    if (!effectsComponent.HasEffect(effectId))
                    {
                        effectsComponent.Add(new HashedString(effectId), true);

                        // Debugging: Effect added
                        Patches.Logger.Log($"Effect '{effectId}' for artifact '{artifactInstanceId}' added successfully.");
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
                    Patches.Logger.Log($"Attempting to delete effect '{effectId}' for artifact '{artifactInstanceId}'.");

                    if (minionEffectArtifactMap.TryGetValue(minion, out var effectMap) && effectMap.TryGetValue(effectId, out var appliedArtifactInstanceId))
                    {
                        if (appliedArtifactInstanceId == artifactInstanceId)
                        {
                            effectsComponent.Remove(new HashedString(effectId));

                            // Debugging: Effect deleted
                            Patches.Logger.Log($"Effect '{effectId}' for artifact '{artifactInstanceId}' deleted successfully.");

                            effectMap.Remove(effectId);
                            if (effectMap.Count == 0)
                                minionEffectArtifactMap.Remove(minion);
                        }
                    }
                }
            }
        }

        private static bool ActiveAndInScope(GameObject minion, GameObject artifact)
        {
            if (minion == null || artifact == null)
                return false;

            var artifactId = artifact.GetInstanceID();
            if (!ArtifactStateTracker.ArtifactStates.TryGetValue(artifactId, out var state) || !state.IsActive)
                return false;

            var config = ArtifactStateTracker.GetArtifactConfig(artifact.GetComponent<KPrefabID>()?.PrefabTag.Name);
            if (config == null)
                return false;

            switch (config.Scope)
            {
                case "All":
                    return true;
                case "InRoom":
                    return GetMinionsInSameRoom(artifact).Contains(minion);
                case "InWorld":
                    return GetMinionsInSameWorld(artifact).Contains(minion);
                default:
                    return false;
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
            var minionModifiers = minion.GetComponent<MinionModifiers>();

            if (minionModifiers == null || minionModifiers.attributes == null)
            {
                Patches.Logger.Log($"GetMinionArtifactInfusions: Minion '{minion.name}' does not have a MinionModifiers component or attributes.");
                return summary.ToString();
            }

            foreach (var attribute in Db.Get().Attributes.resources)
            {
                var attrInstance = minionModifiers.attributes.Get(attribute.Id);
                if (attrInstance == null)
                {
                    continue;
                }

                for (int i = 0; i < attrInstance.Modifiers.size; i++) // Regular iteration
                {
                    var modifier = attrInstance.Modifiers[i];
                    string descriptionCBResult = modifier.DescriptionCB?.Invoke();

                    if (int.TryParse(descriptionCBResult, out int artifactInstanceId))
                    {
                        string artifactProperName = GetArtifactProperName(artifactInstanceId);
                        summary.AppendLine($"{attribute.Name}: {(modifier.Value > 0 ? "+" : "")}{modifier.Value} ({artifactProperName})");
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