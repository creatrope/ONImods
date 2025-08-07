using Database;
using HarmonyLib;
using HLib;
using Klei.AI;
using KMod;
using KSerialization;
using Newtonsoft.Json;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TUNING;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Medals
{
    internal sealed class MinimalKeybindHandler : IInputHandler
    {
        // Change from private to internal so other classes in the same assembly can access it
        internal static PAction KeyTestAction;
        internal static PAction DamageMinionAction; // New action for damage hotkey
        private Action snapshotAction;
        private Action damageSnapshotAction; // New snapshot for damage

        public string handlerName => "MinimalKeybindHandler";
        public KInputHandler inputHandler { get; set; }

        // Track the selected minion globally so the handler can access it
        internal static MinionIdentity SelectedMinion;

        internal MinimalKeybindHandler()
        {
            snapshotAction = KeyTestAction != null ? KeyTestAction.GetKAction() : PAction.MaxAction;
            damageSnapshotAction = DamageMinionAction != null ? DamageMinionAction.GetKAction() : PAction.MaxAction;
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (e.TryConsume(snapshotAction))
            {
                Debug.Log("[Medals] Hotkey CTRL+F12 detected.");
                if (SelectedMinion != null)
                {
                    Debug.Log($"[Medals] Selected minion: {SelectedMinion.GetProperName()}");
                    var health = SelectedMinion.GetComponent<Health>();
                    if (health != null)
                    {
                        Debug.Log($"[Medals] Health component found. Can be incapacitated: {health.canBeIncapacitated}, IsIncapacitated: {health.IsIncapacitated()}");
                        if (health.canBeIncapacitated && !health.IsIncapacitated())
                        {
                            health.Incapacitate(new Tag("ManualIncapacitate"));
                            Debug.Log($"[Medals] Incapacitated '{SelectedMinion.GetProperName()}' via hotkey.");
                            SelectedMinion = null; // Clear the selected minion after incapacitation
                        }
                        else
                        {
                            Debug.Log("[Medals] Minion cannot be incapacitated or is already incapacitated.");
                        }
                    }
                    else
                    {
                        Debug.Log("[Medals] Health component not found on selected minion.");
                    }
                }
                else
                {
                    Debug.Log("[Medals] No minion selected.");
                }
            }
            if (e.TryConsume(damageSnapshotAction))
            {
                Debug.Log("[Medals] Hotkey CTRL+F11 detected for damage.");
                if (SelectedMinion != null)
                {
                    var health = SelectedMinion.GetComponent<Health>();
                    if (health != null)
                    {
                        float damageAmount = 10f; // Amount of damage to apply
                        health.Damage(damageAmount);
                        Debug.Log($"[Medals] Damaged '{SelectedMinion.GetProperName()}' for {damageAmount} HP via hotkey.");
                    }
                    else
                    {
                        Debug.Log("[Medals] Health component not found on selected minion.");
                    }
                }
                else
                {
                    Debug.Log("[Medals] No minion selected.");
                }
            }
        }

        [PLibMethod(RunAt.AfterLayerableLoad)]
        internal static void AddKeycodeHandler()
        {
            KInputHandler.Add(Global.GetInputManager().GetDefaultController(),
                new MinimalKeybindHandler(), 512);
        }

        internal static void Register(PPatchManager manager)
        {
            manager.RegisterPatchClass(typeof(MinimalKeybindHandler));
            KeyTestAction = new PActionManager().CreateAction(
                "Medals.KeyTestAction", "Test Key Action", new PKeyBinding(KKeyCode.F12, Modifier.Ctrl));
            DamageMinionAction = new PActionManager().CreateAction(
                "Medals.DamageMinionAction", "Damage Minion", new PKeyBinding(KKeyCode.F11, Modifier.Ctrl)); // New hotkey: CTRL+F11
        }
    }

    public class MedalInfo
    {
        public string Name { get; }
        public string EffectId { get; }
        public string Description { get; }

        public MedalInfo(string name, string effectId, string description)
        {
            Name = name;
            EffectId = effectId;
            Description = description;
        }
    }

    public static class MedalsRegistry
    {
        public static readonly List<MedalInfo> AllMedals = new List<MedalInfo>();

        /// <summary>
        /// Loads all medals into the registry and registers their effects.
        /// </summary>
        public static void LoadAndRegisterMedals()
        {
            // Add static medals
            AddMedal(new MedalInfo("Rescued Dupe", "RescuedDupe", "Awarded for rescuing an incapacitated dupe."));
            AddMedal(new MedalInfo("Injured Medal", "InjuredMedal", "Awarded for being injured (taking damage)."));
            AddMedal(new MedalInfo("Space Launch Medal", "SpaceLaunchMedal", "Awarded for launching to space (migrating to a new world)."));

            Debug.Log("[Medals] Creating First World Visitor medals.");

            if (ClusterManager.Instance != null && ClusterManager.Instance.WorldContainers != null)
            {
                int worldCount = ClusterManager.Instance.WorldContainers.Count;
                Debug.Log($"[Medals] Found {worldCount} worlds in ClusterManager.Instance.WorldContainers.");

                foreach (var world in ClusterManager.Instance.WorldContainers)
                {
                    string effectId = $"FirstVisitor_{world.id}";
                    string worldDisplayName = world.GetComponent<ClusterGridEntity>()?.GetProperName();
                    string name = $"First Visitor to {worldDisplayName}";
                    string desc = $"Awarded to the first visitor to {worldDisplayName}.";
                    AddMedal(new MedalInfo(name, effectId, desc));
                }
                Debug.Log("[Medals] First Visitor medals registered for all worlds.");
            }
            int medalsCount = AllMedals.Count;
            Debug.Log($"[Medals] created {medalsCount} medals.");
        }

        private static void AddMedal(MedalInfo medal)
        {
            if (!AllMedals.Any(m => m.EffectId == medal.EffectId))
            {
                AllMedals.Add(medal);
                RegisterEffect(medal);
            }
        }

        private static void RegisterEffect(MedalInfo medal)
        {
            if (Db.Get().effects.Exists(medal.EffectId))
                return;

            var effect = new Effect(
                id: medal.EffectId,
                name: medal.Name,
                description: medal.Description,
                duration: -1, // Permanent
                show_in_ui: true,
                trigger_floating_text: false,
                is_bad: false
            );
            Db.Get().effects.Add(effect);
            Debug.Log($"[Medals] Registered effect '{medal.EffectId}' ({medal.Name})");
        }
    }

    public static class MedalsUtility
    {
        /// <summary>
        /// Awards a medal (effect) to the specified minion.
        /// </summary>
        public static void AddMedalToMinion(string minionName, string effectId)
        {
            var minion = Components.MinionIdentities?.Items?.FirstOrDefault(m => m.GetProperName() == minionName);
            if (minion == null)
            {
                Debug.Log($"[Medals] Minion '{minionName}' not found.");
                return;
            }

            var effects = minion.GetComponent<Effects>();
            if (effects == null)
            {
                Debug.Log($"[Medals] Minion '{minionName}' has no Effects component.");
                return;
            }

            // Check if the medal is already awarded
            if (effects.HasEffect(effectId))
            {
                Debug.Log($"[Medals] Minion '{minionName}' already has medal effect '{effectId}'.");
                return;
            }

            effects.Add(effectId, true);
            Debug.Log($"[Medals] Added medal effect '{effectId}' to minion '{minionName}'.");
        }

        /// <summary>
        /// Returns a list of medal effect names currently applied to the minion.
        /// </summary>
        public static List<string> GetMinionMedals(MinionIdentity minion)
        {
            var medals = new List<string>();
            var effects = minion.GetComponent<Effects>();
            if (effects != null)
            {
                // Use the same method as CafePlus: check HasEffect for each registered effect
                foreach (var medal in MedalsRegistry.AllMedals)
                {
                    if (effects.HasEffect(medal.EffectId))
                        medals.Add(medal.Name);
                }
            }
            return medals;
        }
    }

    // Patch RescueIncapacitatedChore.HoldingIncapacitated.deposit state's completion
    [HarmonyPatch(typeof(RescueIncapacitatedChore), "DropIncapacitatedDuplicant")]
    public static class RescueIncapacitatedChore_RescuedDupeMedalPatch
    {
        public static void Postfix(RescueIncapacitatedChore __instance)
        {
            var smi = __instance.smi;
            if (smi == null || smi.sm == null) return;
            var rescuerObj = smi.sm.rescuer.Get(smi);
            if (rescuerObj == null) return;

            var minionIdentity = rescuerObj.GetComponent<MinionIdentity>();
            if (minionIdentity == null) return;

            var deliverTarget = smi.sm.deliverTarget.Get(smi);
            string targetName = deliverTarget != null ? deliverTarget.name : "null";
            bool isMedicalCot = deliverTarget != null && deliverTarget.HasTag(new Tag("MedicalCot"));

            Debug.Log($"[Medals] DropIncapacitatedDuplicant called. deliverTarget: {targetName}, isMedicalCot: {isMedicalCot}");

            if (isMedicalCot)
            {
                MedalsUtility.AddMedalToMinion(minionIdentity.GetProperName(), "RescuedDupe");
            }
        }
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll();
            PUtil.InitLibrary();
            MinimalKeybindHandler.Register(new PPatchManager(harmony));
        }
    }

    [HarmonyPatch(typeof(MinionPersonalityPanel), "OnPrefabInit")]
    public static class MinionPersonalityPanel_AddMedalsPanelPatch
    {
        internal static CollapsibleDetailContentPanel medalsPanel;

        private static void Postfix(MinionPersonalityPanel __instance)
        {
            var method = typeof(DetailScreenTab).GetMethod("CreateCollapsableSection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (method != null)
            {
                medalsPanel = (CollapsibleDetailContentPanel)method.Invoke(__instance, new object[] { "Medals" });
                __instance.GetType().GetField("medalsPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                    ?.SetValue(__instance, medalsPanel);
            }
        }
    }

    [HarmonyPatch(typeof(MinionPersonalityPanel), "OnSelectTarget")]
    public static class MinionPersonalityPanel_OnSelectTargetMedalsPatch
    {
        private static void Postfix(MinionPersonalityPanel __instance, GameObject target)
        {
            if (target == null)
                return;

            MinimalKeybindHandler.SelectedMinion = target.GetComponent<MinionIdentity>();

            var minion = target.GetComponent<MinionIdentity>();
            if (minion != null && MinionPersonalityPanel_AddMedalsPanelPatch.medalsPanel != null)
            {
                var medals = MedalsUtility.GetMinionMedals(minion);
                string medalsText = medals.Count > 0
                    ? string.Join("\n", medals)
                    : "No medals awarded.";
                MinionPersonalityPanel_AddMedalsPanelPatch.medalsPanel.SetLabel("medals", medalsText, "Permanent medals awarded to this minion.");
                MinionPersonalityPanel_AddMedalsPanelPatch.medalsPanel.Commit();
            }
        }
    }

    [HarmonyPatch(typeof(Health), "Damage")]
    public static class Health_DamageMedalPatch
    {
        private static void Postfix(Health __instance, float amount)
        {
            var minion = __instance.GetComponent<MinionIdentity>();
            if (minion != null && amount > 0)
            {
                // Award a medal for being injured
                MedalsUtility.AddMedalToMinion(minion.GetProperName(), "InjuredMedal");
                Debug.Log($"[Medals] Awarded InjuredMedal to '{minion.GetProperName()}' for taking damage: {amount}");
            }
        }
    }

    // Add this tracker class to serialize first visitor awards
    [Serializable]
    public class FirstVisitorMedalTracker : KMonoBehaviour, ISaveLoadable
    {
        [Serialize]
        private Dictionary<int, string> worldFirstVisitors = new Dictionary<int, string>();

        public static FirstVisitorMedalTracker Instance;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Instance = this;
        }
        a
        public bool TryAwardFirstVisitor(int worldId, string minionName)
        {
            if (!worldFirstVisitors.ContainsKey(worldId))
            {
                worldFirstVisitors[worldId] = minionName;
                string effectId = $"FirstVisitor_{worldId}";
                MedalsUtility.AddMedalToMinion(minionName, effectId);
                Debug.Log($"[Medals] Awarded {effectId} to '{minionName}'.");
                return true;
            }
            return false;
        }

        public string GetFirstVisitor(int worldId)
        {
            return worldFirstVisitors.TryGetValue(worldId, out var minionName) ? minionName : null;
        }
    }

    [HarmonyPatch(typeof(AssignmentManager), "MinionMigration")]
    public static class AssignmentManager_MinionMigration_Patch
    {
        public static void Postfix(object data)
        {
            Debug.Log("[Medals] AssignmentManager_MinionMigration_Patch.Postfix called.");
            if (data == null)
            {
                Debug.Log("[Medals] MinionMigration data is null.");
                return;
            }

            Debug.Log($"[Medals] MinionMigration data type: {data.GetType().FullName}");

            var migrationEventArgs = data as MinionMigrationEventArgs;
            if (migrationEventArgs == null)
            {
                Debug.Log("[Medals] MinionMigrationEventArgs cast failed.");
                return;
            }

            Debug.Log($"[Medals] migrationEventArgs: prevWorldId={migrationEventArgs.prevWorldId}, targetWorldId={migrationEventArgs.targetWorldId}, minionId={migrationEventArgs.minionId}");

            var minionGo = migrationEventArgs.minionId?.gameObject;
            if (minionGo == null)
            {
                Debug.Log("[Medals] minionGo is null.");
                return;
            }

            Debug.Log($"[Medals] minionGo name: {minionGo.name}, minionGo type: {minionGo.GetType().FullName}");

            int oldWorldId = migrationEventArgs.prevWorldId;
            int newWorldId = migrationEventArgs.targetWorldId;

            var selectable = minionGo.GetComponent<KSelectable>();
            string minionName = selectable != null ? selectable.GetProperName() : "Unknown Minion";
            Debug.Log($"[Medals] minionName: {minionName}");

            Debug.Log($"[Medals] oldWorldId: {oldWorldId}, newWorldId: {newWorldId}");

            // Award the Space Launch Medal only if the world IDs are equal
            if (oldWorldId == newWorldId)
            {
                Debug.Log("[Medals] oldWorldId == newWorldId, awarding SpaceLaunchMedal.");
                MedalsUtility.AddMedalToMinion(minionName, "SpaceLaunchMedal");
                Debug.Log($"[ArtifactsPlus] Minion migrated and awarded SpaceLaunchMedal.");
                return;
            }

            // Award First Visitor medal if this is the first visit to the world
            var world = ClusterManager.Instance.GetWorld(newWorldId);
            if (world == null)
            {
                Debug.Log($"[Medals] ClusterManager.Instance.GetWorld({newWorldId}) returned null.");
            }
            else
            {
                Debug.Log($"[Medals] world id: {world.id}, world name: {world.name}, world type: {world.GetType().FullName}");
            }

            string worldName = world != null ? world.name : $"World {newWorldId}";
            Debug.Log($"[Medals] worldName: {worldName}");

            if (FirstVisitorMedalTracker.Instance != null)
            {
                Debug.Log("[Medals] FirstVisitorMedalTracker.Instance is not null, trying to award FirstVisitor medal.");
                bool awarded = FirstVisitorMedalTracker.Instance.TryAwardFirstVisitor(newWorldId, minionName);
                Debug.Log($"[Medals] TryAwardFirstVisitor returned: {awarded}");
            }
            else
            {
                Debug.Log("[Medals] FirstVisitorMedalTracker.Instance is null.");
            }
        }
    }


    [HarmonyPatch(typeof(ClusterManager), "OnSpawn")]
    public static class ClusterManager_OnSpawn_MedalsRegistryPatch
    {
        public static void DumpWorldMeta(string header)
        {
            var worlds = ClusterManager.Instance?.WorldContainers;
            if (worlds != null && worlds.Count > 0)
            {
                var world = worlds[0];
                var gridEntity = world.GetComponent<ClusterGridEntity>();
                Debug.Log($"[Medals] {header}:");
                Debug.Log($"  id: {world.id}");
                Debug.Log($"  name: {world.name}");
                Debug.Log($"  DisplayName: {world.GetProperName()}");
                Debug.Log($"  type: {world.GetType().FullName}");
                Debug.Log($"  IsStartWorld: {world.IsStartWorld}");
                Debug.Log($"  IsDiscovered: {world.IsDiscovered}");
                Debug.Log($"  IsDupeVisited: {world.IsDupeVisited}");
                Debug.Log($"  ClusterGridEntity name: {gridEntity?.name}");
                Debug.Log($"  ClusterGridEntity proper name: {gridEntity?.GetProperName()}");
                Debug.Log($"  ClusterGridEntity location: {gridEntity?.Location}");
            }
            else
            {
                Debug.Log("[Medals] No worlds found in ClusterManager.Instance.WorldContainers.");
            }
        }
    }

    [HarmonyPatch(typeof(MinionIdentity), "OnSpawn")]
    public static class MinionIdentity_OnSpawn_WorldMetaDumpPatch
    {
        private static bool loaded = false;

        public static void Prefix()
        {
            if (!loaded)
            {
                Debug.Log("[Medals] MinionIdentity_OnSpawn Prefix.");
                MedalsRegistry.LoadAndRegisterMedals();
                loaded = true;

                // Create and attach the singleton FirstVisitorMedalTracker if not present
                if (FirstVisitorMedalTracker.Instance == null)
                {
                    var trackerGo = new GameObject("FirstVisitorMedalTracker");
                    UnityEngine.Object.DontDestroyOnLoad(trackerGo);
                    FirstVisitorMedalTracker.Instance = trackerGo.AddComponent<FirstVisitorMedalTracker>();
                    Debug.Log("[Medals] FirstVisitorMedalTracker singleton created and component added.");
                }
            }
        }
    }
}
