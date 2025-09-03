using ArtifactsPlus;
using PeterHan.PLib.Actions;
using PeterHan.PLib.PatchManager;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HLib;

namespace ArtifactsPlus
{
    internal static class HotKeys
    {
        public static MinionIdentity SelectedMinion { get; set; }

        // Upgrade KeybindDef to the 5-parameter format: (string id, string displayName, KKeyCode key, Modifier modifiers, Action handler)
        internal static readonly List<Keybinder.KeybindDef> All = new List<Keybinder.KeybindDef>
        {
            new Keybinder.KeybindDef { Id = "ArtifactsPlus.PrintAllArtifactsAction", DisplayName = "Print All Artifacts", Key = KKeyCode.F3, Modifiers = Modifier.Ctrl | Modifier.Shift, Handler = PrintAllArtifacts },
            new Keybinder.KeybindDef { Id = "ArtifactsPlus.PrintAllKeepsakesAction", DisplayName = "Print All Keepsakes", Key = KKeyCode.F5, Modifiers = Modifier.Ctrl | Modifier.Shift, Handler = PrintAllKeepsakes },
            new Keybinder.KeybindDef { Id = "ArtifactsPlus.PrintAllMinionsBionicAction", DisplayName = "Print All Minions (Bionic)", Key = KKeyCode.F4, Modifiers = Modifier.Ctrl | Modifier.Shift, Handler = PrintAllMinionsBionic }
        };

        private static void PrintAllArtifacts()
        {
            Debug.Log("[ArtifactsPlus] PrintAllArtifacts called");
            ArtifactStateTracker.BuildGlobalAllArtifacts(); // Ensure up-to-date list
            var allArtifacts = ArtifactStateTracker.GetAllArtifacts();
            Debug.Log($"[ArtifactsPlus] ArtifactStateTracker.GetAllArtifacts() returned {(allArtifacts == null ? "null" : allArtifacts.Length.ToString())} artifacts");

            if (allArtifacts == null || allArtifacts.Length == 0)  
            {
                Debug.Log("[ArtifactsPlus] No artifacts found in the game.");
                return;
            }

            int nullCount = allArtifacts.Count(a => a == null);
            Debug.Log($"[ArtifactsPlus] Null artifact count: {nullCount}");

            // Build a list of artifact info grouped by world
            var artifactInfos = allArtifacts
                .Where(artifact => artifact != null)
                .Select(artifact =>
                {
                    string artifactName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Artifact";
                    bool isActive = false;
                    if (ArtifactStateTracker.ArtifactStates.TryGetValue(artifact.GetInstanceID(), out var state))
                        isActive = state.IsActive;

                    int cell = Grid.PosToCell(artifact.transform.position);
                    int worldId = Grid.WorldIdx[cell];
                    var world = ClusterManager.Instance.GetWorld(worldId);
                    string worldName = world != null ? world.GetProperName() : $"World_{worldId}";

                    Debug.Log($"[ArtifactsPlus] Artifact: {artifactName}, Active: {isActive}, World: {worldName}, InstanceID: {artifact.GetInstanceID()}");

                    return new
                    {
                        Artifact = artifact,
                        ArtifactName = artifactName,
                        IsActive = isActive,
                        WorldName = worldName,
                        InstanceId = artifact.GetInstanceID()
                    };
                })
                .GroupBy(info => info.WorldName)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var worldGroup in artifactInfos)
            {
                var artifactsInWorld = worldGroup
                    .OrderByDescending(info => info.IsActive)
                    .ThenBy(info => info.ArtifactName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (artifactsInWorld.Count == 0)
                    continue;

                // Print world header with underline
                string header = worldGroup.Key;
                string underline = new string('-', header.Length);
                Debug.Log(header);
                Debug.Log(underline);

                foreach (var info in artifactsInWorld)
                {
                    Debug.Log($"- {info.ArtifactName}{(info.IsActive ? ", Active" : "")} (InstanceID: {info.InstanceId})");
                }
            }
            Debug.Log("[ArtifactsPlus] PrintAllArtifacts() completed");
        }

        private static void PrintAllKeepsakes()
        {
            Debug.Log("[ArtifactsPlus] PrintAllKeepsakes() called");
            var keepsakes = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && kp.HasTag(GameTags.Keepsake))
                .Select(kp => kp.gameObject)
                .ToArray();

            Debug.Log($"[ArtifactsPlus] Found {keepsakes.Length} keepsakes");

            if (keepsakes.Length == 0)
            {
                Debug.Log("[ArtifactsPlus] No keepsakes found in the game.");
                return;
            }

            Debug.Log("[ArtifactsPlus] All Keepsakes:");
            foreach (var keepsake in keepsakes)
            {
                string keepsakeName = keepsake.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Keepsake";
                int instanceId = keepsake.GetInstanceID();
                Debug.Log($"- {keepsakeName} (InstanceID: {instanceId})");
            }
            Debug.Log("[ArtifactsPlus] PrintAllKeepsakes() completed");
        }

        private static void PrintAllMinionsBionic()
        {
            Debug.Log("[ArtifactsPlus] PrintAllMinionsBionic() called");
            var allMinions = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && (kp.HasTag("Minion") || kp.HasTag("BionicMinion")))
                .Select(kp => kp.gameObject)
                .ToArray();

            Debug.Log($"[ArtifactsPlus] Found {allMinions.Length} minions (Minion or BionicMinion)");

            if (allMinions.Length == 0)
            {
                Debug.Log("[ArtifactsPlus] No minions found in the game.");
                return;
            }

            Debug.Log("[ArtifactsPlus] All Minions (Bionic status):");
            foreach (var minion in allMinions)
            {
                string minionName = minion.GetComponent<KSelectable>()?.GetProperName() ?? "Unknown Minion";
                var prefabId = minion.GetComponent<KPrefabID>();
                bool isBionic = prefabId != null && prefabId.HasTag("BionicMinion");
                int instanceId = minion.GetInstanceID();
                Debug.Log($"- {minionName} (Bionic: {(isBionic ? "Yes" : "No")}, InstanceID: {instanceId})");
            }
            Debug.Log("[ArtifactsPlus] PrintAllMinionsBionic() completed");
        }
    }
}
