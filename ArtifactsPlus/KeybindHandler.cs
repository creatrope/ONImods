using PeterHan.PLib.Actions;
using PeterHan.PLib.PatchManager;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

namespace ArtifactsPlus
{
    internal sealed class KeybindHandler : IInputHandler
    {
        private class Keybind
        {
            public string Id;
            public string DisplayName;
            public PKeyBinding Binding;
            public System.Action Handler;
            public PAction Action;
            public Action Snapshot;

            public Keybind(string id, string displayName, PKeyBinding binding, System.Action handler)
            {
                Id = id;
                DisplayName = displayName;
                Binding = binding;
                Handler = handler;
            }
        }

        // Keybind intentions
        private static readonly List<Keybind> keybinds = new List<Keybind>
        {
            new Keybind("ArtifactsPlus.PrintActiveArtifactsAction", "Print Active Artifacts", new PKeyBinding(KKeyCode.F8, Modifier.Ctrl), () => PrintAllArtifacts(true)),
            new Keybind("ArtifactsPlus.PrintAllArtifactsAction", "Print All Artifacts", new PKeyBinding(KKeyCode.F9, Modifier.Ctrl), () => PrintAllArtifacts(null)),
            new Keybind("ArtifactsPlus.PrintAllAttributesAction", "Print All Attributes", new PKeyBinding(KKeyCode.F10, Modifier.Ctrl), PrintAllMinionAttributesInGame),
        };

        public string handlerName => "ArtifactsPlusKeybindHandler";
        public KInputHandler inputHandler { get; set; }

        private bool keyIsDown = false;

        public KeybindHandler()
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

            foreach (var kb in keybinds)
            {
                if (kb.Snapshot == null && kb.Action != null)
                    kb.Snapshot = kb.Action.GetKAction();

                if (e.TryConsume(kb.Snapshot))
                {
                    kb.Handler?.Invoke();
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
                    new KeybindHandler(), 512);
                handlerRegistered = true;
            }
        }

        public static void Register(PPatchManager manager)
        {
            manager.RegisterPatchClass(typeof(KeybindHandler));
            foreach (var kb in keybinds)
            {
                kb.Action = new PActionManager().CreateAction(kb.Id, kb.DisplayName, kb.Binding);
                kb.Snapshot = kb.Action.GetKAction();
            }
        }

        // --- Support functions copied from Patches.cs ---

        public static void PrintAllArtifacts(bool? isActive = null)
        {
            var allArtifacts = ArtifactStateTracker.GetAllArtifacts();
            if (allArtifacts == null || allArtifacts.Length == 0)
            {
                Patches.logger.LogDebug("[ArtifactsPlus] No artifacts found in the game.");
                return;
            }

            string header;
            if (isActive == null)
                header = "[ArtifactsPlus] All Artifacts:";
            else if (isActive.Value)
                header = "[ArtifactsPlus] Active Artifacts:";
            else
                header = "[ArtifactsPlus] Inactive Artifacts:";

            Patches.logger.LogDebug(header);

            foreach (var artifact in allArtifacts)
            {
                if (artifact == null)
                    continue;

                int id = artifact.GetInstanceID();
                string artifactName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Artifact";

                if (isActive != null)
                {
                    if (!ArtifactStateTracker.ArtifactStates.TryGetValue(id, out var state) || state.IsActive != isActive.Value)
                        continue;
                }

                Patches.logger.LogDebug($"- {artifactName}");
            }
        }

        public static void PrintAllMinionAttributesInGame()
        {
            Patches.logger.LogDebug("[ArtifactsPlus] Minion Attributes in the game:");
            var dbAttributes = Db.Get()?.Attributes;
            if (dbAttributes == null)
            {
                Patches.logger.LogDebug("[ArtifactsPlus] Could not find Db.Get().Attributes.");
                return;
            }

            var attributeFields = typeof(Database.Attributes).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var minionAttributes = attributeFields
                .Select(f => f.GetValue(dbAttributes) as Klei.AI.Attribute)
                .Where(a => a != null)
                .OrderBy(a => a.Id, StringComparer.OrdinalIgnoreCase);

            foreach (var attr in minionAttributes)
            {
                if (attr.Description != null && attr.Description.Contains("MISSING"))
                    Patches.logger.LogDebug($"- {attr.Id}: {attr.Name}");
                else
                    Patches.logger.LogDebug($"- {attr.Id}: {attr.Name} ({attr.Description})");
            }
            Patches.logger.LogDebug($"[ArtifactsPlus] Total minion attributes found: {minionAttributes.Count()}");
        }

        public static void PrintAllKeepsakes()
        {
            // Find all GameObjects with the "Keepsake" tag and print their names
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
    }
}