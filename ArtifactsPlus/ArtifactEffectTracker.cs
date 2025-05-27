using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Klei.AI;

namespace ArtifactsPlus
{
    public static class ArtifactEffectTracker
    {
        // Map of minion to effect and the artifact that applied it
        private static readonly Dictionary<GameObject, Dictionary<string, string>> minionEffectArtifactMap = new Dictionary<GameObject, Dictionary<string, string>>();

        // Map of minion to modifier and the artifact that applied it
        private static readonly Dictionary<GameObject, Dictionary<(string attrName, float value), string>> minionModifierArtifactMap = new Dictionary<GameObject, Dictionary<(string attrName, float value), string>>();

        public static bool TryGetArtifactModifiers(string artifactId, out Dictionary<string, float> modifiers)
        {
            return ArtifactStateTracker.TryGetArtifactAttributes(artifactId, out modifiers);
        }

        public static void OnArtifactStateChanged(GameObject artifact, string artifactId, bool isActive, List<GameObject> minionList)
        {
            if (artifact == null)
                return;

            if (isActive)
            {
                foreach (var minion in minionList)
                {
                    ApplyOrRemoveArtifactModifiersToMinion(minion, artifactId, true);
                    ApplyOrRemoveArtifactStatusEffectsToMinion(minion, artifactId, true);
                }
            }
            else
            {
                foreach (var minion in GetAllMinions())
                {
                    ApplyOrRemoveArtifactModifiersToMinion(minion, artifactId, false);
                    ApplyOrRemoveArtifactStatusEffectsToMinion(minion, artifactId, false);
                }
            }

            System.IO.File.AppendAllText(CustomLogger.LogPath, "");
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
                    ApplyOrRemoveArtifactModifiersToMinion(minion, artifactId, false);
                    ApplyOrRemoveArtifactStatusEffectsToMinion(minion, artifactId, false);
                }
            }
        }

        public static void ApplyOrRemoveArtifactModifiersToMinion(GameObject minion, string artifactId, bool apply)
        {
            if (minion == null)
                return;

            var minionModifiers = minion.GetComponent<MinionModifiers>();
            if (minionModifiers == null)
                return;

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
                        continue;

                    var attrInstance = minionModifiers.attributes?.Get(attribute);
                    if (attrInstance != null)
                    {
                        var modifierKey = (attrName, modValue);
                        if (apply)
                        {
                            bool hasModifier = false;
                            for (int i = 0; i < attrInstance.Modifiers.size; i++)
                            {
                                var mod = attrInstance.Modifiers[i];
                                if (mod.Description != null && mod.Description == $"Artifact Modifier: {artifactId}")
                                {
                                    hasModifier = true;
                                    break;
                                }
                            }
                            if (!hasModifier)
                            {
                                var modifier = new AttributeModifier(attribute.Id, modValue, $"Artifact Modifier: {artifactId}");
                                attrInstance.Add(modifier);

                                // Track which artifact applied this modifier
                                if (!minionModifierArtifactMap.TryGetValue(minion, out var modMap))
                                {
                                    modMap = new Dictionary<(string attrName, float value), string>();
                                    minionModifierArtifactMap[minion] = modMap;
                                }
                                modMap[modifierKey] = artifactId;
                            }
                        }
                        else
                        {
                            var toRemove = new List<AttributeModifier>();
                            for (int i = 0; i < attrInstance.Modifiers.size; i++)
                            {
                                var mod = attrInstance.Modifiers[i];
                                if (mod.Description == $"Artifact Modifier: {artifactId}")
                                {
                                    toRemove.Add(mod);
                                }
                            }
                            for (int i = 0; i < toRemove.Count; i++)
                                attrInstance.Remove(toRemove[i]);

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
        }

        public static void ApplyOrRemoveArtifactStatusEffectsToMinion(GameObject minion, string artifactId, bool apply)
        {
            var config = ArtifactsPlus.ArtifactStateTracker.GetArtifactConfig(artifactId);
            if (config == null || config.Effects == null)
                return;

            foreach (var effectId in config.Effects)
            {
                if (string.IsNullOrEmpty(effectId))
                    continue;

                var effects = minion.GetComponent<Effects>();
                if (effects == null)
                    continue;

                if (apply)
                {
                    if (!effects.HasEffect(effectId))
                    {
                        effects.Add(effectId, true);
                        // Track which artifact applied this effect
                        if (!minionEffectArtifactMap.TryGetValue(minion, out var effectMap))
                        {
                            effectMap = new Dictionary<string, string>();
                            minionEffectArtifactMap[minion] = effectMap;
                        }
                        effectMap[effectId] = artifactId;
                    }
                }
                else
                {
                    if (effects.HasEffect(effectId))
                    {
                        effects.Remove(effectId);
                        // Remove tracking
                        if (minionEffectArtifactMap.TryGetValue(minion, out var effectMap))
                        {
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
                if (effectMap.Values.Contains(artifactId))
                    return true;
            }

            // Check attribute modifiers
            if (minionModifierArtifactMap.TryGetValue(minion, out var modMap))
            {
                if (modMap.Values.Contains(artifactId))
                    return true;
            }
            return false;
        }

        public static string GetMinionArtifactInfusions(GameObject minion)
        {
            var summary = new System.Text.StringBuilder();
            var listedEffects = new HashSet<string>();
            var listedModifiers = new HashSet<(string attrName, float value, string artifactId)>();

            // Effects
            if (minionEffectArtifactMap.TryGetValue(minion, out var effectMap))
            {
                foreach (var kvp in effectMap)
                {
                    if (listedEffects.Add(kvp.Key))
                    {
                        string artifactId = kvp.Value;
                        string properName = GetArtifactProperName(artifactId);
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
                    string artifactId = kvp.Value;
                    if (listedModifiers.Add((attrName, val, artifactId)))
                    {
                        string properName = GetArtifactProperName(artifactId);
                        summary.AppendLine($"{attrName} {(val >= 0 ? "+" : "")}{val} ({properName})");
                    }
                }
            }

            return summary.ToString();
        }

        private static string GetArtifactProperName(string artifactId)
        {
            // Try to find the artifact GameObject in the world
            var artifact = ArtifactsPlus.ArtifactStateTracker.ArtifactsOnPedestals
                .FirstOrDefault(a => a != null && a.GetComponent<KPrefabID>()?.PrefabTag.Name == artifactId);
            if (artifact != null)
            {
                var selectable = artifact.GetComponent<KSelectable>();
                if (selectable != null)
                    return selectable.GetProperName();
            }
            // Fallback to artifactId if not found
            return artifactId;
        }
    }
}