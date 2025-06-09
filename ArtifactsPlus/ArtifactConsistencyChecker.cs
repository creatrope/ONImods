using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Klei.AI;

namespace ArtifactsPlus
{
    // Ensure this is the only definition of ArtifactConsistencyChecker in the namespace
    public static class ArtifactConsistencyChecker
    {
        /// <summary>
        /// Checks consistency between artifactHistoryMap and the actual state of minion effects/modifiers.
        /// Logs inconsistencies for review in both directions.
        /// </summary>
        public static void CheckArtifactMinionConsistency(
            Dictionary<GameObject, HashSet<GameObject>> artifactHistoryMap,
            Func<string, Dictionary<string, float>> getArtifactModifiers,
            IEnumerable<GameObject> artifactsOnPedestals)
        {
            bool isConsistent = true;

            // 1. Check: artifactHistoryMap -> minion state
            foreach (var kvp in artifactHistoryMap)
            {
                var artifact = kvp.Key;
                var minionSet = kvp.Value;
                string artifactId = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                var config = ArtifactStateTracker.GetArtifactConfig(artifactId);

                foreach (var minion in minionSet.ToList())
                {
                    bool hasAny = false;

                    // Check status effects
                    if (config?.Effects != null && config.Effects.Count > 0)
                    {
                        var effects = minion.GetComponent<Effects>();
                        if (effects != null)
                        {
                            foreach (var effectId in config.Effects.Keys)
                            {
                                if (effects.HasEffect(effectId))
                                {
                                    hasAny = true;
                                    break;
                                }
                            }
                        }
                    }

                    // Check attribute modifiers
                    if (!hasAny)
                    {
                        var modifierDict = getArtifactModifiers(artifactId);
                        if (modifierDict != null)
                        {
                            var minionModifiers = minion.GetComponent<MinionModifiers>();
                            if (minionModifiers != null)
                            {
                                foreach (var attr in modifierDict.Keys)
                                {
                                    var attribute = Db.Get().Attributes.resources
                                        .FirstOrDefault(a => string.Equals(a.Id, attr, StringComparison.OrdinalIgnoreCase) ||
                                                             string.Equals(a.Name, attr, StringComparison.OrdinalIgnoreCase));
                                    if (attribute != null)
                                    {
                                        var attrInstance = minionModifiers.attributes?.Get(attribute);
                                        if (attrInstance != null)
                                        {
                                            if (attrInstance.Modifiers.Count > 0 && attrInstance.Modifiers.FindIndex(m => m.Description == $"Artifact Modifier: {artifactId}") != -1)
                                            {
                                                hasAny = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!hasAny)
                    {
                        isConsistent = false;
                        CustomLogger.Log($"[CONSISTENCY] Minion '{minion.name}' is in artifactHistoryMap for artifact '{artifactId}' but has no matching effects/modifiers.");
                    }
                }
            }

            // 2. Check: minion state -> artifactHistoryMap
            var allArtifacts = artifactsOnPedestals;
            var allMinions = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && kp.HasTag("Minion"))
                .Select(kp => kp.gameObject);

            foreach (var minion in allMinions)
            {
                foreach (var artifact in allArtifacts)
                {
                    if (artifact == null) continue;
                    string artifactId = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                    var config = ArtifactStateTracker.GetArtifactConfig(artifactId);

                    bool hasEffect = false;

                    // Check status effects
                    if (config?.Effects != null && config.Effects.Count > 0)
                    {
                        var effects = minion.GetComponent<Effects>();
                        if (effects != null)
                        {
                            foreach (var effectId in config.Effects.Keys)
                            {
                                if (effects.HasEffect(effectId))
                                {
                                    hasEffect = true;
                                    break;
                                }
                            }
                        }
                    }

                    // Check attribute modifiers
                    if (!hasEffect)
                    {
                        var modifierDict = getArtifactModifiers(artifactId);
                        if (modifierDict != null)
                        {
                            var minionModifiers = minion.GetComponent<MinionModifiers>();
                            if (minionModifiers != null)
                            {
                                foreach (var attr in modifierDict.Keys)
                                {
                                    var attribute = Db.Get().Attributes.resources
                                        .FirstOrDefault(a => string.Equals(a.Id, attr, StringComparison.OrdinalIgnoreCase) ||
                                                             string.Equals(a.Name, attr, StringComparison.OrdinalIgnoreCase));
                                    if (attribute != null)
                                    {
                                        var attrInstance = minionModifiers.attributes?.Get(attribute);
                                        if (attrInstance != null)
                                        {
                                            if (attrInstance.Modifiers.Count > 0 && attrInstance.Modifiers.FindIndex(m => m.Description == $"Artifact Modifier: {artifactId}") != -1)
                                            {
                                                hasEffect = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // If minion has effect/modifier but is not in artifactHistoryMap for this artifact, log it
                    if (hasEffect)
                    {
                        if (!artifactHistoryMap.TryGetValue(artifact, out var minionSet) || !minionSet.Contains(minion))
                        {
                            isConsistent = false;
                            CustomLogger.Log($"[CONSISTENCY] Minion '{minion.name}' has artifact effect/modifier for '{artifactId}' but is NOT in artifactHistoryMap.");
                        }
                    }
                }
            }

            if (isConsistent)
            {
                CustomLogger.Log("[CONSISTENCY] All artifact/minion states are consistent.");
            }
            else
            {
                CustomLogger.Log("[CONSISTENCY] Inconsistencies detected in artifact/minion states.");
            }
        }
    }
}