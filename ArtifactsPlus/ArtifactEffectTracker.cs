using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Klei.AI;

namespace ArtifactsPlus
{
    public static class ArtifactEffectTracker
    {
        // For each artifact, track which minions have had its effect applied
        private static readonly Dictionary<GameObject, HashSet<GameObject>> artifactMinionMap = new Dictionary<GameObject, HashSet<GameObject>>();

        // For each artifact, track the history of minions that have had its effect applied
        private static readonly Dictionary<GameObject, HashSet<GameObject>> artifactHistoryMap = new Dictionary<GameObject, HashSet<GameObject>>();

        // Map of artifactId to its modifiers (what the artifact does)
        private static readonly Dictionary<string, Dictionary<string, float>> artifactModifiersMap = new Dictionary<string, Dictionary<string, float>>();

        // Use only this method for retrieving artifact modifiers
        public static bool TryGetArtifactModifiers(string artifactId, out Dictionary<string, float> modifiers)
        {
            return ArtifactStateTracker.TryGetArtifactAttributes(artifactId, out modifiers);
        }

        // Helper to get statuses for an artifact
        private static bool TryGetArtifactStatuses(string artifactId, out List<string> statuses)
        {
            statuses = null;
            var config = ArtifactStateTracker.GetArtifactConfig(artifactId);
            if (config != null && config.Effects != null && config.Effects.Count > 0)
            {
                statuses = config.Effects;
                return true;
            }
            return false;
        }

        // Call this when an artifact changes state
        public static void OnArtifactStateChanged(GameObject artifact, string artifactId, bool isActive, List<GameObject> minionList)
        {
            CustomLogger.Log($"[EffectTracker] Artifact '{artifactId}' state changed. isActive={isActive}");

            if (artifact == null)
            {
                CustomLogger.Log("[DEBUG] OnArtifactStateChanged: artifact is null, returning.");
                return;
            }

            if (isActive)
            {
                // history of minions that have already had this effect applied
                if (!artifactHistoryMap.TryGetValue(artifact, out var minionSet))
                {
                    minionSet = new HashSet<GameObject>();
                    artifactHistoryMap[artifact] = minionSet;
                }

                int minionCount = 0;
                foreach (var minion in minionList)
                {
                    CustomLogger.Log($"[EffectTracker] Processing minion '{minion.name}' for artifact '{artifactId}'");
                    minionCount++;
                    if (!minionSet.Contains(minion))
                    {
                        ApplyOrRemoveArtifactModifiersToMinion(minion, artifactId, true);
                        ApplyOrRemoveArtifactStatusEffectsToMinion(minion, artifactId, true);
                        minionSet.Add(minion);
                    }
                    else
                    {
                        CustomLogger.Log($"[DEBUG] Minion {minion.name} already has effect for artifactId={artifactId}");
                    }
                }
                CustomLogger.Log($"[DEBUG] OnArtifactStateChanged: Processed {minionCount} minions for artifact {artifact.name}");
            }
            else
            {
                if (artifactHistoryMap.TryGetValue(artifact, out var minionSet))
                {
                    CustomLogger.Log($"[DEBUG] Removing effects from {minionSet.Count} minions for artifact {artifact.name}");
                    foreach (var minion in minionSet)
                    {
                        ApplyOrRemoveArtifactModifiersToMinion(minion, artifactId, false);
                        ApplyOrRemoveArtifactStatusEffectsToMinion(minion, artifactId, false);
                    }
                    minionSet.Clear();
                    artifactHistoryMap.Remove(artifact);
                }
                else
                {
                    CustomLogger.Log($"[DEBUG] No minionSet found for artifact {artifact.name} when deactivating.");
                }
            }

            System.IO.File.AppendAllText(CustomLogger.LogPath, ""); // No-op, ensures file is up to date
        }

        // Helper to enumerate all minions in the scene
        private static IEnumerable<GameObject> GetAllMinions()
        {
            CustomLogger.Log("[DEBUG] GetAllMinions called.");
            int count = 0;
            foreach (var minion in UnityEngine.Object.FindObjectsOfType<KPrefabID>())
            {
                if (minion != null && minion.HasTag("Minion"))
                {
                    count++;
                    yield return minion.gameObject;
                }
            }
            CustomLogger.Log($"[DEBUG] GetAllMinions found {count} minions.");
        }

        /// <summary>
        /// Adds a status effect to all minions.
        /// </summary>
        public static void AddStatusToAllMinions(string statusId)
        {
            foreach (var minion in GetAllMinions())
            {
                var effects = minion.GetComponent<Effects>();
                if (effects != null && !effects.HasEffect(statusId))
                {
                    effects.Add(statusId, true);
                    CustomLogger.Log($"[HOTKEY] Added status '{statusId}' to minion '{minion.name}'");
                }
            }
        }

        /// <summary>
        /// Removes a status effect from all minions.
        /// </summary>
        public static void RemoveStatusFromAllMinions(string statusId)
        {
            foreach (var minion in GetAllMinions())
            {
                var effects = minion.GetComponent<Effects>();
                if (effects != null && effects.HasEffect(statusId))
                {
                    effects.Remove(statusId);
                    CustomLogger.Log($"[HOTKEY] Removed status '{statusId}' from minion '{minion.name}'");
                }
            }
        }

        /// <summary>
        /// Applies or removes artifact attribute modifiers to a minion.
        /// </summary>
        /// <param name="minion">The minion GameObject.</param>
        /// <param name="artifactId">The artifact ID.</param>
        /// <param name="apply">True to apply, false to remove.</param>
        public static void ApplyOrRemoveArtifactModifiersToMinion(GameObject minion, string artifactId, bool apply)
        {
            string action = apply ? "Applying" : "Removing";
            CustomLogger.Log($"[EffectTracker] {action} modifiers for minion '{minion?.name ?? "null"}' and artifact '{artifactId}'");

            if (minion == null)
                return;

            var minionModifiers = minion.GetComponent<MinionModifiers>();
            if (minionModifiers == null)
            {
                CustomLogger.Log($"[DEBUG] Minion {minion.name} does not have MinionModifiers component.");
                return;
            }

            if (TryGetArtifactModifiers(artifactId, out var modifierDict))
            {
                foreach (var kvp in modifierDict)
                {
                    string attrName = kvp.Key;
                    float modValue = kvp.Value;

                    Klei.AI.Attribute attribute = Db.Get().Attributes.resources
                        .FirstOrDefault(a => string.Equals(a.Id, attrName, StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(a.Name, attrName, StringComparison.OrdinalIgnoreCase));

                    if (attribute == null)
                    {
                        CustomLogger.Log($"[DEBUG] Attribute '{attrName}' not found by Id or Name in Db for {minion.name}.");
                        continue;
                    }

                    var attrInstance = minionModifiers.attributes?.Get(attribute);
                    if (attrInstance != null)
                    {
                        if (apply)
                        {
                            float before = attrInstance.GetTotalValue();
                            var modifier = new AttributeModifier(attribute.Id, modValue, $"Artifact Modifier: {artifactId}");
                            attrInstance.Add(modifier);
                            float after = attrInstance.GetTotalValue();
                            CustomLogger.Log($"[MODIFIER] Applied: {minion.GetProperName()} {attribute.Id} += {modValue} (Artifact: {artifactId}) [{before} -> {after}]");
                        }
                        else
                        {
                            var toRemove = new List<AttributeModifier>();
                            var modifiers = attrInstance.Modifiers;
                            for (int i = 0; i < modifiers.Count; i++)
                            {
                                var mod = modifiers[i];
                                if (mod.Description == $"Artifact Modifier: {artifactId}")
                                    toRemove.Add(mod);
                            }
                            if (toRemove.Count > 0)
                            {
                                float before = attrInstance.GetTotalValue();
                                foreach (var mod in toRemove)
                                {
                                    attrInstance.Remove(mod);
                                }
                                float after = attrInstance.GetTotalValue();
                                CustomLogger.Log($"[MODIFIER] Removed: {minion.GetProperName()} {attribute.Id} (Artifact: {artifactId}) [{before} -> {after}]");
                            }
                        }
                    }
                    else
                    {
                        CustomLogger.Log($"[DEBUG] {minion.name} does not have attribute '{attribute.Id}'.");
                    }
                }
            }
            else
            {
                CustomLogger.Log($"[DEBUG] No artifact modifiers found for artifactId={artifactId}");
            }
        }

        /// <summary>
        /// Applies or removes artifact status effects to a minion.
        /// </summary>
        /// <param name="minion">The minion GameObject.</param>
        /// <param name="artifactId">The artifact ID.</param>
        /// <param name="apply">True to apply, false to remove.</param>
        public static void ApplyOrRemoveArtifactStatusEffectsToMinion(GameObject minion, string artifactId, bool apply)
        {
            // Get the artifact config to retrieve status effects (called "Effects" in config)
            var config = ArtifactsPlus.ArtifactStateTracker.GetArtifactConfig(artifactId);
            if (config == null || config.Effects == null)
            {
                CustomLogger.Log($"[ArtifactEffectTracker][DEBUG] No config or effects found for artifact '{artifactId}' when {(apply ? "applying" : "removing")} status effects to minion '{minion?.name ?? "null"}'.");
                return;
            }

            foreach (var effectId in config.Effects)
            {
                if (string.IsNullOrEmpty(effectId))
                    continue;

                var effects = minion.GetComponent<Effects>();
                if (effects == null)
                {
                    CustomLogger.Log($"[ArtifactEffectTracker][DEBUG] Minion '{minion?.name ?? "null"}' does not have Effects component when processing '{effectId}' for artifact '{artifactId}'.");
                    continue;
                }

                if (apply)
                {
                    if (!effects.HasEffect(effectId))
                    {
                        effects.Add(effectId, true);
                        CustomLogger.Log($"[ArtifactEffectTracker] Added status effect '{effectId}' to minion '{minion.name}' from artifact '{artifactId}'.");
                    }
                    else
                    {
                        CustomLogger.Log($"[ArtifactEffectTracker][DEBUG] Minion '{minion.name}' already has status effect '{effectId}' from artifact '{artifactId}'.");
                    }
                }
                else
                {
                    if (effects.HasEffect(effectId))
                    {
                        effects.Remove(effectId);
                        CustomLogger.Log($"[ArtifactEffectTracker] Removed status effect '{effectId}' from minion '{minion.name}' from artifact '{artifactId}'.");
                    }
                    else
                    {
                        CustomLogger.Log($"[ArtifactEffectTracker][DEBUG] Minion '{minion.name}' did not have status effect '{effectId}' to remove for artifact '{artifactId}'.");
                    }
                }
            }
        }
    }
}