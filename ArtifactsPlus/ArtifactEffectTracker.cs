using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using Klei.AI;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;

namespace ArtifactsPlus
{
    public class ArtifactEffectTracker : MonoBehaviour
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

            // Try to get durations from config.Effects (now a dictionary)
            Dictionary<string, float> effectDurations = null;
            if (config.Effects is IDictionary<string, float> dict)
                effectDurations = dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (effectDurations != null)
            {
                foreach (var kvp in effectDurations)
                {
                    string effectId = kvp.Key;
                    float duration = kvp.Value;

                    if (string.IsNullOrEmpty(effectId))
                        continue;

                    var effects = minion.GetComponent<Effects>();
                    if (effects == null)
                    {
                        CustomLogger.Log($"[DEBUG] Minion '{minion?.name}' has no Effects component when processing effect '{effectId}' (apply={apply}).");
                        continue;
                    }

                    string minionName = minion.GetComponent<KSelectable>()?.GetProperName() ?? minion.name;

                    if (apply)
                    {
                        if (!effects.HasEffect(effectId))
                        {
                            effects.Add(effectId, true);
                            // Set custom duration if specified and positive
                            var effectInstance = effects.Get(effectId);
                            if (effectInstance != null && duration > 0f)
                            {
                                effectInstance.timeRemaining = duration;
                                CustomLogger.Log($"[ArtifactsPlus] Applying effect '{effectId}' to minion '{minionName}' from artifact '{artifactId}' with custom duration {duration} seconds.");
                            }
                            else
                            {
                                CustomLogger.Log($"[ArtifactsPlus] Applying effect '{effectId}' to minion '{minionName}' from artifact '{artifactId}' with default or permanent duration.");
                            }
                            CustomLogger.Log($"[DEBUG] Added effect '{effectId}' to minion '{minionName}' from artifact '{artifactId}'.");
                            // Track which artifact applied this effect
                            if (!minionEffectArtifactMap.TryGetValue(minion, out var effectMap))
                            {
                                effectMap = new Dictionary<string, string>();
                                minionEffectArtifactMap[minion] = effectMap;
                            }
                            effectMap[effectId] = artifactId;
                        }
                        else
                        {
                            CustomLogger.Log($"[DEBUG] Minion '{minionName}' already has effect '{effectId}' (artifact '{artifactId}').");
                        }
                    }
                    else
                    {
                        if (minionEffectArtifactMap.TryGetValue(minion, out var effectMap) &&
                            effectMap.TryGetValue(effectId, out var sourceArtifact) &&
                            sourceArtifact == artifactId)
                        {
                            if (effects.HasEffect(effectId))
                                effects.Remove(effectId);
                            effectMap.Remove(effectId);
                            if (effectMap.Count == 0)
                                minionEffectArtifactMap.Remove(minion);
                            CustomLogger.Log($"[ArtifactsPlus] Removed effect '{effectId}' from minion '{minionName}' (was set by artifact '{artifactId}').");
                        }
                        else
                        {
                            CustomLogger.Log($"[ArtifactsPlus] Skipped removing effect '{effectId}' from minion '{minionName}' (not set by artifact '{artifactId}').");
                        }
                    }
                }
            }
            else
            {
                foreach (var effectEntry in config.Effects)
                {
                    string effectId = effectEntry.ToString();
                    if (string.IsNullOrEmpty(effectId))
                        continue;

                    var effects = minion.GetComponent<Effects>();
                    if (effects == null)
                    {
                        CustomLogger.Log($"[DEBUG] Minion '{minion?.name}' has no Effects component when processing effect '{effectId}' (apply={apply}).");
                        continue;
                    }

                    string minionName = minion.GetComponent<KSelectable>()?.GetProperName() ?? minion.name;

                    if (apply)
                    {
                        if (!effects.HasEffect(effectId))
                        {
                            effects.Add(effectId, true);
                            CustomLogger.Log($"[ArtifactsPlus] Applying effect '{effectId}' to minion '{minionName}' from artifact '{artifactId}' with default or permanent duration.");
                            CustomLogger.Log($"[DEBUG] Added effect '{effectId}' to minion '{minionName}' from artifact '{artifactId}'.");
                            // Track which artifact applied this effect
                            if (!minionEffectArtifactMap.TryGetValue(minion, out var effectMap))
                            {
                                effectMap = new Dictionary<string, string>();
                                minionEffectArtifactMap[minion] = effectMap;
                            }
                            effectMap[effectId] = artifactId;
                        }
                        else
                        {
                            CustomLogger.Log($"[DEBUG] Minion '{minionName}' already has effect '{effectId}' (artifact '{artifactId}').");
                        }
                    }
                    else
                    {
                        if (minionEffectArtifactMap.TryGetValue(minion, out var effectMap) &&
                            effectMap.TryGetValue(effectId, out var sourceArtifact) &&
                            sourceArtifact == artifactId)
                        {
                            if (effects.HasEffect(effectId))
                                effects.Remove(effectId);
                            effectMap.Remove(effectId);
                            if (effectMap.Count == 0)
                                minionEffectArtifactMap.Remove(minion);
                            CustomLogger.Log($"[ArtifactsPlus] Removed effect '{effectId}' from minion '{minionName}' (was set by artifact '{artifactId}').");
                        }
                        else
                        {
                            CustomLogger.Log($"[ArtifactsPlus] Skipped removing effect '{effectId}' from minion '{minionName}' (not set by artifact '{artifactId}').");
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

        // Assuming you have a class like this:
        public class EffectData
        {
            public string Id { get; set; }
            public float Duration { get; set; }
            // Other properties are ignored for export
        }

        // When exporting:
        public static void ExportEffectsJson(IEnumerable<EffectData> effects, string path)
        {
            // Only serialize Id and Duration for each effect
            var minimal = effects.Select(e => new { e.Id, e.Duration }).ToList();
            var json = JsonConvert.SerializeObject(minimal, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        [Serializable]
        public class MinionAttributeEffectSaveData
        {
            public string MinionId { get; set; }
            public List<AttributeModifierData> AttributeModifiers { get; set; }
            public List<EffectDurationData> Effects { get; set; }
        }

        [Serializable]
        public class AttributeModifierData
        {
            public string AttributeId { get; set; }
            public float Value { get; set; }
            public string Description { get; set; }
        }

        [Serializable]
        public class EffectDurationData
        {
            public string EffectId { get; set; }
            public float TimeRemaining { get; set; }
        }

        // Save all minion attribute effects and effect durations
        public static void SaveMinionAttributesAndEffects(string path)
        {
            var allData = new List<MinionAttributeEffectSaveData>();
            foreach (var minion in GetAllMinions())
            {
                var minionId = minion.GetComponent<KPrefabID>()?.InstanceID.ToString();
                if (string.IsNullOrEmpty(minionId)) continue;

                // Save attribute modifiers
                var minionModifiers = minion.GetComponent<MinionModifiers>();
                var attrMods = new List<AttributeModifierData>();
                if (minionModifiers != null && minionModifiers.attributes != null)
                {
                    foreach (var attrInstance in minionModifiers.attributes)
                    {
                        var attrId = attrInstance.Attribute.Id;
                        var modifiers = attrInstance.Modifiers;
                        for (int i = 0; i < modifiers.size; i++)
                        {
                            var mod = modifiers[i];
                            attrMods.Add(new AttributeModifierData
                            {
                                AttributeId = attrId,
                                Value = mod.Value,
                                Description = mod.Description
                            });
                        }
                    }
                }

                // Save effects and their durations
                var effects = minion.GetComponent<Effects>();
                var effectList = new List<EffectDurationData>();
                if (effects != null)
                {
                    foreach (var effectInfo in GetAllEffectInstances(effects))
                    {
                        effectList.Add(new EffectDurationData
                        {
                            EffectId = effectInfo.Id,
                            TimeRemaining = effectInfo.TimeRemaining
                        });
                    }
                }

                allData.Add(new MinionAttributeEffectSaveData
                {
                    MinionId = minionId,
                    AttributeModifiers = attrMods,
                    Effects = effectList
                });
            }
            var json = JsonConvert.SerializeObject(allData, Formatting.Indented);
            File.WriteAllText(path, json);

            // Debug print: Print a summary for each minion
            foreach (var minionData in allData)
            {
                CustomLogger.Log($"[ArtifactsPlus][SAVE] Minion {minionData.MinionId}:");
                foreach (var mod in minionData.AttributeModifiers)
                    CustomLogger.Log($"  Attribute: {mod.AttributeId} Value: {mod.Value} Desc: {mod.Description}");
                foreach (var eff in minionData.Effects)
                    CustomLogger.Log($"  Effect: {eff.EffectId} Remaining: {eff.TimeRemaining}");
            }
            CustomLogger.Log("[ArtifactsPlus][SAVE] Full JSON:");
            CustomLogger.Log(json);
        }

        // Load all minion attribute effects and effect durations
        public static void LoadMinionAttributesAndEffects(string path)
        {
            if (!File.Exists(path)) return;
            var json = File.ReadAllText(path);

            // Debug print: Print the loaded JSON
            CustomLogger.Log("[ArtifactsPlus][LOAD] Loaded JSON:");
            CustomLogger.Log(json);

            var allData = JsonConvert.DeserializeObject<List<MinionAttributeEffectSaveData>>(json);

            foreach (var minionData in allData)
            {
                var minion = FindMinionById(minionData.MinionId);
                if (minion == null)
                {
                    CustomLogger.Log($"[ArtifactsPlus][LOAD] Minion {minionData.MinionId} not found in scene.");
                    continue;
                }

                // Restore attribute modifiers
                var minionModifiers = minion.GetComponent<MinionModifiers>();
                if (minionModifiers != null && minionModifiers.attributes != null)
                {
                    foreach (var modData in minionData.AttributeModifiers)
                    {
                        var attr = Db.Get().Attributes.Get(modData.AttributeId);
                        if (attr != null)
                        {
                            var attrInstance = minionModifiers.attributes.Get(attr);
                            if (attrInstance != null)
                            {
                                bool hasMod = false;
                                var modifiers = attrInstance.Modifiers;
                                for (int i = 0; i < modifiers.size; i++)
                                {
                                    if (modifiers[i].Description == modData.Description)
                                    {
                                        hasMod = true;
                                        break;
                                    }
                                }
                                if (!hasMod)
                                {
                                    var mod = new AttributeModifier(attr.Id, modData.Value, modData.Description);
                                    attrInstance.Add(mod);
                                }
                            }
                        }
                    }
                }

                // Restore effects and their durations
                var effects = minion.GetComponent<Effects>();
                if (effects != null)
                {
                    foreach (var effectData in minionData.Effects)
                    {
                        var effectInstance = GetEffectInstanceById(effects, effectData.EffectId);
                        if (effectInstance != null)
                        {
                            SetTimeRemaining(effectInstance, effectData.TimeRemaining);
                        }
                    }
                }

                // Debug print: Print a summary for each minion after load
                CustomLogger.Log($"[ArtifactsPlus][LOAD] Minion {minionData.MinionId}:");
                foreach (var mod in minionData.AttributeModifiers)
                    CustomLogger.Log($"  Attribute: {mod.AttributeId} Value: {mod.Value} Desc: {mod.Description}");
                foreach (var eff in minionData.Effects)
                    CustomLogger.Log($"  Effect: {eff.EffectId} Remaining: {eff.TimeRemaining}");
            }
        }

        private static GameObject FindMinionById(string id)
        {
            return GetAllMinions().FirstOrDefault(m => m.GetComponent<KPrefabID>()?.InstanceID.ToString() == id);
        }

        // Helper to enumerate all EffectInstance objects and get Id and timeRemaining
        private static IEnumerable<(string Id, float TimeRemaining)> GetAllEffectInstances(Effects effects)
        {
            if (effects == null) yield break;
            var field = typeof(Effects).GetField("effects", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                var list = field.GetValue(effects) as IList;
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        var type = item.GetType();
                        var idProp = type.GetProperty("Id") ?? type.GetProperty("EffectId");
                        var timeProp = type.GetProperty("timeRemaining") ?? type.GetProperty("TimeRemaining");
                        if (idProp != null && timeProp != null)
                        {
                            yield return ((string)idProp.GetValue(item), (float)timeProp.GetValue(item));
                        }
                    }
                }
            }
        }

        // Helper to find an EffectInstance by Id
        private static object GetEffectInstanceById(Effects effects, string effectId)
        {
            if (effects == null || string.IsNullOrEmpty(effectId)) return null;
            var field = typeof(Effects).GetField("effects", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                var list = field.GetValue(effects) as IList;
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        var type = item.GetType();
                        var idProp = type.GetProperty("Id") ?? type.GetProperty("EffectId");
                        var timeProp = type.GetProperty("timeRemaining") ?? type.GetProperty("TimeRemaining");
                        if (idProp != null && (string)idProp.GetValue(item) == effectId)
                        {
                            return item;
                        }
                    }
                }
            }
            return null;
        }

        // Helper to set timeRemaining on an EffectInstance
        private static void SetTimeRemaining(object effectInstance, float time)
        {
            if (effectInstance == null) return;
            var type = effectInstance.GetType();
            var timeProp = type.GetProperty("timeRemaining") ?? type.GetProperty("TimeRemaining");
            if (timeProp != null && timeProp.CanWrite)
            {
                timeProp.SetValue(effectInstance, time);
            }
            else
            {
                var timeField = type.GetField("timeRemaining", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                              ?? type.GetField("TimeRemaining", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (timeField != null)
                    timeField.SetValue(effectInstance, time);
            }
        }
    }
}