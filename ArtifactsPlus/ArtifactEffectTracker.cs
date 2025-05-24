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

        // Map of artifactId to its effects (what the artifact does)
        private static readonly Dictionary<string, Dictionary<string, float>> artifactEffectsMap = new Dictionary<string, Dictionary<string, float>>();

        // Use only this method for retrieving artifact effects
        public static bool TryGetArtifactEffects(string artifactId, out Dictionary<string, float> effects)
        {
            return ArtifactStateTracker.TryGetArtifactAttributes(artifactId, out effects);
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
                        ApplyOrRemoveArtifactEffectsToMinion(minion, artifactId, true);
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
                        ApplyOrRemoveArtifactEffectsToMinion(minion, artifactId, false);
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
        /// Applies or removes artifact attribute modifiers to a minion.
        /// </summary>
        /// <param name="minion">The minion GameObject.</param>
        /// <param name="artifactId">The artifact ID.</param>
        /// <param name="apply">True to apply, false to remove.</param>
        public static void ApplyOrRemoveArtifactEffectsToMinion(GameObject minion, string artifactId, bool apply)
        {
            string action = apply ? "Applying" : "Removing";
            CustomLogger.Log($"[EffectTracker] {action} effects for minion '{minion?.name ?? "null"}' and artifact '{artifactId}'");

            if (minion == null)
                return;

            var minionModifiers = minion.GetComponent<MinionModifiers>();
            if (minionModifiers == null)
            {
                CustomLogger.Log($"[DEBUG] Minion {minion.name} does not have MinionModifiers component.");
                return;
            }

            if (TryGetArtifactEffects(artifactId, out var effectDict))
            {
                foreach (var kvp in effectDict)
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
                            var modifier = new AttributeModifier(attribute.Id, modValue, $"Artifact Effect: {artifactId}");
                            attrInstance.Add(modifier);
                            float after = attrInstance.GetTotalValue();
                            CustomLogger.Log($"[EFFECT] Applied: {minion.GetProperName()} {attribute.Id} += {modValue} (Artifact: {artifactId}) [{before} -> {after}]");
                        }
                        else
                        {
                            var toRemove = new List<AttributeModifier>();
                            var modifiers = attrInstance.Modifiers;
                            for (int i = 0; i < modifiers.Count; i++)
                            {
                                var mod = modifiers[i];
                                if (mod.Description == $"Artifact Effect: {artifactId}")
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
                                CustomLogger.Log($"[EFFECT] Removed: {minion.GetProperName()} {attribute.Id} (Artifact: {artifactId}) [{before} -> {after}]");
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
                CustomLogger.Log($"[DEBUG] No artifact effects found for artifactId={artifactId}");
            }
        }
    }
}