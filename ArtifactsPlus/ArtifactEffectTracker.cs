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
using PeterHan.PLib.Options; // Add this namespace to resolve 'POptions'

namespace ArtifactsPlus
{
    public class ArtifactEffectTracker : MonoBehaviour
    {
        private static HotkeyListener hotkeyListener;

        public static void OnLoad()
        {
            try
            {
                var options = POptions.ReadSettings<ArtifactsPlusOptions>();

                Patches.Logger.SetLoggingEnabled(options.EnableCustomLog);
                if (options.EnableCustomLog)
                {
                    Patches.Logger.Reset(); // Reset the log file at the start of the game
                }

                ArtifactStateTracker.LoadArtifactConfig();

                // Initialize and register hotkeys
                hotkeyListener = new HotkeyListener();

                hotkeyListener.RegisterHotkey("Ctrl+F12", () =>
                {
                    PrintActiveArtifactsWithWorlds();
                });

                Patches.Logger.Log($"[ArtifactsPlus] onLoad: update the state of the artifacts");

                // Update the state of all artifacts
                var allArtifacts = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                    .Where(kp => kp != null && kp.HasTag("Artifact"))
                    .Select(kp => kp.gameObject);

                foreach (var artifact in allArtifacts)
                {
                    ArtifactStateTracker.UpdateArtifactState(artifact);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArtifactsPlus] Failed to initialize OnLoad: {ex.Message}");
            }
        }

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

        public static List<GameObject> GetMinionsInSameRoom(GameObject artifact)
        {
            var minionsInRoom = new List<GameObject>();
            int artifactCell = Grid.PosToCell(artifact.transform.position);
            var artifactCavity = Game.Instance?.roomProber?.GetCavityForCell(artifactCell)?.room?.cavity;
            if (artifactCavity == null)
                return minionsInRoom;

            foreach (var kp in UnityEngine.Object.FindObjectsOfType<KPrefabID>())
            {
                if (kp != null && kp.HasTag("Minion"))
                {
                    int minionCell = Grid.PosToCell(kp.transform.position);
                    var minionCavity = Game.Instance.roomProber.GetCavityForCell(minionCell)?.room?.cavity;
                    if (minionCavity == artifactCavity)
                        minionsInRoom.Add(kp.gameObject);
                }
            }
            return minionsInRoom;
        }

        public static List<GameObject> GetMinionsInSameWorld(GameObject artifact)
        {
            var minionsInWorld = new List<GameObject>();
            int artifactWorldId = Grid.WorldIdx[Grid.PosToCell(artifact.transform.position)];
            foreach (var kp in UnityEngine.Object.FindObjectsOfType<KPrefabID>())
            {
                if (kp != null && kp.HasTag("Minion"))
                {
                    int minionWorldId = Grid.WorldIdx[Grid.PosToCell(kp.transform.position)];
                    if (minionWorldId == artifactWorldId)
                        minionsInWorld.Add(kp.gameObject);
                }
            }
            return minionsInWorld;
        }

        public static bool ActiveAndInScope(GameObject minion, GameObject artifact)
        {
            if (artifact == null || minion == null)
                return false;

            int artifactId = artifact.GetInstanceID();
            if (!ArtifactStateTracker.ArtifactStates.TryGetValue(artifactId, out var state) || !state.IsActive)
                return false;

            var config = ArtifactStateTracker.GetArtifactConfig(artifact.GetComponent<KPrefabID>()?.PrefabTag.Name);
            if (config == null)
                return false;

            if (config.Scope == "All")
                return true;

            if (config.Scope == "InRoom")
                return GetMinionsInSameRoom(artifact).Contains(minion);

            if (config.Scope == "InWorld")
                return GetMinionsInSameWorld(artifact).Contains(minion);

            return false;
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
                        // Create a unique identifier for the modifier
                        var modifier = new AttributeModifier(attribute.Id, modValue, "Skill Level");
                        modifier.DescriptionCB = () => artifactInstanceId.ToString(); // Convert int to string for callback
                      
                        attrInstance.Add(modifier);
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
                            //Patches.Logger.Log($"Removing Modifier: ID='{currentModifier.AttributeId}', Value='{currentModifier.Value}', Description='{currentModifier.Description}', DescriptionCB='{descriptionCB}'");
                            attrInstance.Remove(currentModifier); // Safely remove the modifier
                        }
                    }
                }
            }
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

                for (int i = 0; i < attrInstance.Modifiers.size; i++)
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
            var allArtifacts = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && kp.HasTag("Artifact"))
                .Select(kp => kp.gameObject);
            // Search for the artifact in allArtifacts  
            var artifact = allArtifacts.FirstOrDefault(a => a.GetInstanceID() == artifactInstanceId);
            return artifact?.GetProperName() ?? "Unknown Artifact";
        }

        public static void PrintAllArtifactIDsAndInstanceIDs()
        {
            var allArtifacts = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && kp.HasTag("Artifact"))
                .Select(kp => kp.gameObject);

            foreach (var artifact in allArtifacts)
            {
                int instanceId = artifact.GetInstanceID();
                string artifactId = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Artifact ID";
                Patches.Logger.Log($"Artifact ID: {artifactId}, Instance ID: {instanceId}");
            }
        }

        private static void PrintActiveArtifactsWithWorlds()
        {
            var allArtifacts = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && kp.HasTag("Artifact"))
                .Select(kp => kp.gameObject);

            foreach (var artifact in allArtifacts)
            {
                int worldId = artifact.GetComponent<KPrefabID>()?.GetMyWorldId() ?? -1;
                string artifactId = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Artifact ID";
                Patches.Logger.Log($"Artifact ID: {artifactId}, World ID: {worldId}");
            }
        }
    }
}