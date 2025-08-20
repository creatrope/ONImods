using ArtifactsPlus;
using PeterHan.PLib.Actions;
using PeterHan.PLib.PatchManager;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArtifactsPlus
{
    internal sealed class KeybindsHandler : IInputHandler
    {
        private class Keybind
        {
            public string Id;
            public string DisplayName;
            public PKeyBinding Binding;
            public System.Action Handler;
            public PAction Action;
            public Action Snapshot;
            public bool LocalOnly;

            public Keybind(string id, string displayName, PKeyBinding binding, System.Action handler, bool localOnly = false)
            {
                Id = id;
                DisplayName = displayName;
                Binding = binding;
                Handler = handler;
                LocalOnly = localOnly;
            }
        }

        // Keybinds at the top
        private static readonly List<Keybind> keybinds = new List<Keybind>
        {
            new Keybind("ArtifactsPlus.PrintAllArtifactsAction", "Print All Artifacts", new PKeyBinding(KKeyCode.F9, Modifier.Ctrl), PrintAllArtifacts, false),
            new Keybind("ArtifactsPlus.PrintAllKeepsakesAction", "Print All Keepsakes", new PKeyBinding(KKeyCode.F3, Modifier.Ctrl), PrintAllKeepsakes, false),
            new Keybind("ArtifactsPlus.PrintAllMinionsBionicAction", "Print All Minions (Bionic)", new PKeyBinding(KKeyCode.F4, Modifier.Ctrl), PrintAllMinionsBionic, false),
        };

        private float lastSnapshotTime = 0f;
        private readonly float debounceInterval = 1.0f; // seconds

        public string handlerName => "KeybindsHandler";
        public KInputHandler inputHandler { get; set; }

        private bool keyIsDown = false;

        public KeybindsHandler()
        {
            foreach (var kb in keybinds)
            {
                kb.Snapshot = (kb.Action != null) ? kb.Action.GetKAction() : Action.Invalid;
            }
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (keyIsDown)
                return;

            keyIsDown = true;

            float now = Time.time;
            foreach (var kb in keybinds)
            {
                if (kb.Snapshot == null && kb.Action != null)
                    kb.Snapshot = kb.Action.GetKAction();

                if (e.TryConsume(kb.Snapshot))
                {
                    if (now - lastSnapshotTime >= debounceInterval)
                    {
                        lastSnapshotTime = now;
                        kb.Handler?.Invoke();
                    }
                    break;
                }
            }
        }

        public void OnKeyUp(KButtonEvent e)
        {
            keyIsDown = false;
        }

        private static bool handlerRegistered = false;

        [PLibMethod(RunAt.AfterLayerableLoad)]
        public static void AddKeycodeHandler()
        {
            if (!handlerRegistered)
            {
                KInputHandler.Add(Global.GetInputManager().GetDefaultController(),
                    new KeybindsHandler(), 512);
                handlerRegistered = true;
            }
        }

        public static void Register(PPatchManager manager)
        {
            manager.RegisterPatchClass(typeof(KeybindsHandler));
            foreach (var kb in keybinds)
            {
                kb.Action = new PActionManager().CreateAction(kb.Id, kb.DisplayName, kb.Binding);
                kb.Snapshot = kb.Action.GetKAction();
            }
        }

        // --- SUPPORT FUNCTIONS BELOW ---

        private static void PrintAllArtifacts()
        {
            ArtifactStateTracker.BuildGlobalAllArtifacts(); // Ensure up-to-date list
            var allArtifacts = ArtifactStateTracker.GetAllArtifacts();
            if (allArtifacts == null || allArtifacts.Length == 0)
            {
                Patches.logger.LogDebug("[ArtifactsPlus] No artifacts found in the game.");
                return;
            }

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

                    return new
                    {
                        Artifact = artifact,
                        ArtifactName = artifactName,
                        IsActive = isActive,
                        WorldName = worldName
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
                Patches.logger.LogDebug(header);
                Patches.logger.LogDebug(underline);

                foreach (var info in artifactsInWorld)
                {
                    Patches.logger.LogDebug($"- {info.ArtifactName}, {(info.IsActive ? "Active" : "")}");
                }
            }
        }

             private static void PrintAllKeepsakes()
        {
            var keepsakes = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && kp.HasTag(GameTags.Keepsake))
                .Select(kp => kp.gameObject)
                .ToArray();

            if (keepsakes.Length == 0)
            {
                Patches.logger.LogDebug("[ArtifactsPlus] No keepsakes found in the game.");
                return;
            }

            Patches.logger.LogDebug("[ArtifactsPlus] All Keepsakes:");
            foreach (var keepsake in keepsakes)
            {
                string keepsakeName = keepsake.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Keepsake";
                Patches.logger.LogDebug($"- {keepsakeName}");
            }
        }

        private static void PrintAllMinionsBionic()
        {
            var allMinions = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && (kp.HasTag("Minion") || kp.HasTag("BionicMinion")))
                .Select(kp => kp.gameObject)
                .ToArray();

            if (allMinions.Length == 0)
            {
                Patches.logger.LogDebug("[ArtifactsPlus] No minions found in the game.");
                return;
            }

            Patches.logger.LogDebug("[ArtifactsPlus] All Minions (Bionic status):");
            foreach (var minion in allMinions)
            {
                string minionName = minion.GetComponent<KSelectable>()?.GetProperName() ?? "Unknown Minion";
                var prefabId = minion.GetComponent<KPrefabID>();
                bool isBionic = prefabId != null && prefabId.HasTag("BionicMinion");
                Patches.logger.LogDebug($"- {minionName} (Bionic: {(isBionic ? "Yes" : "No")})");
            }
        }
    }
}
