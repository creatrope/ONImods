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
        internal static PAction KeyTestAction;
        internal static PAction DamageMinionAction;
        private Action snapshotAction;
        private Action damageSnapshotAction;
        private float lastSnapshotTime = 0f;
        private float lastDamageTime = 0f;
        private readonly float debounceInterval = 1.0f; // seconds

        public string handlerName => "MinimalKeybindHandler";
        public KInputHandler inputHandler { get; set; }
        internal static MinionIdentity SelectedMinion;

        internal MinimalKeybindHandler()
        {
            snapshotAction = KeyTestAction != null ? KeyTestAction.GetKAction() : PAction.MaxAction;
            damageSnapshotAction = DamageMinionAction != null ? DamageMinionAction.GetKAction() : PAction.MaxAction;
        }

        public void OnKeyDown(KButtonEvent e)
        {
            float now = Time.time;

            // Only consume one action per key press, and check which action was triggered
            bool incapacitatePressed = e.TryConsume(snapshotAction);
            bool damagePressed = e.TryConsume(damageSnapshotAction);

            if (damagePressed || incapacitatePressed)
            {
                if (now - lastSnapshotTime < debounceInterval)
                    return;
                lastSnapshotTime = now;

                Debug.Log("[Medals] Hotkey detected.");
                if (SelectedMinion != null)
                {
                    Debug.Log($"[Medals] Selected minion: {SelectedMinion.GetProperName()}");
                    var health = SelectedMinion.GetComponent<Health>();
                    if (health != null)
                    {
                        if (incapacitatePressed)
                        {
                            Debug.Log($"[Medals] Health component found. Can be incapacitated: {health.canBeIncapacitated}, IsIncapacitated: {health.IsIncapacitated()}");
                            if (health.canBeIncapacitated && !health.IsIncapacitated())
                            {
                                health.Incapacitate(new Tag("ManualIncapacitate"));
                                Debug.Log($"[Medals] Incapacitated '{SelectedMinion.GetProperName()}' via hotkey.");
                                SelectedMinion = null;
                            }
                            else
                            {
                                Debug.Log("[Medals] Minion cannot be incapacitated or is already incapacitated.");
                            }
                        }
                        else if (damagePressed)
                        {
                            float damageAmount = 10f;
                            health.Damage(damageAmount);
                            Debug.Log($"[Medals] Damaged '{SelectedMinion.GetProperName()}' for {damageAmount} HP via hotkey.");
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
                "Medals.DamageMinionAction", "Damage Minion", new PKeyBinding(KKeyCode.F11, Modifier.Ctrl));
        }
    }

    public class MedalInfo
    {
        public string Name { get; }
        public string EffectId { get; }
        public string Description { get; }
        public bool IsRepeatable { get; }

        public MedalInfo(string name, string effectId, string description, bool isRepeatable)
        {
            Name = name;
            EffectId = effectId;
            Description = description;
            IsRepeatable = isRepeatable;
        }
    }

    public static class MedalsRegistry
    {
        public static readonly List<MedalInfo> AllMedals = new List<MedalInfo>();

        public static void LoadAndRegisterMedals()
        {
            AddMedal(new MedalInfo("Rescued Dupe", "RescuedDupe", "Awarded for rescuing an incapacitated dupe.", true));
            AddMedal(new MedalInfo("Injured Medal", "InjuredMedal", "Awarded for being injured (taking damage).", false));
            AddMedal(new MedalInfo("Space Launch Medal", "SpaceLaunchMedal", "Awarded for launching to space (migrating to a new world).", false));

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
                    AddMedal(new MedalInfo(name, effectId, desc, false));
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
                duration: -1,
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

            var medal = MedalsRegistry.AllMedals.FirstOrDefault(m => m.EffectId == effectId);
            if (medal == null)
            {
                Debug.Log($"[Medals] Medal '{effectId}' not found in registry.");
                return;
            }

            // Only award once if not repeatable
            if (!medal.IsRepeatable && effects.HasEffect(effectId))
            {
                Debug.Log($"[Medals] Minion '{minionName}' already has non-repeatable medal effect '{effectId}'.");
                return;
            }

            effects.Add(effectId, true);
            Debug.Log($"[Medals] Added medal effect '{effectId}' to minion '{minionName}'.");

            // Spawn a keepsake at the minion's position
            SpawnKeepsakeForMedal(minion, effectId);
        }

        /// <summary>
        /// Spawns a keepsake prefab at the minion's position.
        /// </summary>
        private static void SpawnKeepsakeForMedal(MinionIdentity minion, string effectId)
        {
            string keepsakeId = $"keepsake-{effectId}";
            var pos = minion.transform.position;
            pos.z = Grid.GetLayerZ(Grid.SceneLayer.Ore);
            pos.y += 2.0f;

            // If you have a custom keepsake prefab, instantiate it like LargeImpactorDestroyedSequence
            GameObject prefab = Assets.GetPrefab((Tag)"keepsake_megabrain"); // stereoscope
            if (prefab != null)
            {
                Debug.Log($"[Medals] Keepsake prefab info: name={prefab.name}, activeSelf={prefab.activeSelf}, has PedestalDisplayable={prefab.HasTag(GameTags.PedestalDisplayable)}");

                GameObject keepsakeObj = Util.KInstantiate(prefab, pos);
                if (!keepsakeObj.HasTag(GameTags.PedestalDisplayable))
                    keepsakeObj.AddTag(GameTags.PedestalDisplayable);
                keepsakeObj.SetActive(true);
                Debug.Log($"[Medals] Spawned keepsake '{keepsakeId}' for medal '{effectId}' at {pos}.");

                Debug.Log($"[Medals] Keepsake instance info: name={keepsakeObj.name}, activeSelf={keepsakeObj.activeSelf}, has PedestalDisplayable={keepsakeObj.HasTag(GameTags.PedestalDisplayable)}");

                // Optionally, play a visual effect for extra visibility
                // new UpgradeFX.Instance((IStateMachineTarget) keepsakeObj.GetComponent<KMonoBehaviour>(), new Vector3(0.0f, -0.5f, -0.1f)).StartSM();
            }
            else
            {
                Debug.Log($"[Medals] No keepsake prefab found for '{keepsakeId}'.");
            }
        }

        public static List<string> GetMinionMedals(MinionIdentity minion)
        {
            var medals = new List<string>();
            var effects = minion.GetComponent<Effects>();
            if (effects != null)
            {
                foreach (var medal in MedalsRegistry.AllMedals)
                {
                    if (effects.HasEffect(medal.EffectId))
                        medals.Add(medal.Name);
                }
            }
            return medals;
        }
    }

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
                MedalsUtility.AddMedalToMinion(minion.GetProperName(), "InjuredMedal");
                Debug.Log($"[Medals] Awarded InjuredMedal to '{minion.GetProperName()}' for taking damage: {amount}");
            }
        }
    }

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

            if (oldWorldId == newWorldId)
            {
                Debug.Log("[Medals] oldWorldId == newWorldId, awarding SpaceLaunchMedal.");
                MedalsUtility.AddMedalToMinion(minionName, "SpaceLaunchMedal");
                Debug.Log($"[ArtifactsPlus] Minion migrated and awarded SpaceLaunchMedal.");
                return;
            }

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

    [HarmonyPatch]
    public static class DisplayablePatch
    {
        [HarmonyPatch(typeof(KeepsakeConfig), "CreatePrefabs")]
        [HarmonyPostfix]
        public static void Postfix(List<GameObject> __result)
        {
            if (__result != null)
            {
                foreach (var prefab in __result)
                {
                    if (prefab != null)
                    {
                        prefab.AddTag(GameTags.PedestalDisplayable);
                        Debug.Log("[Medals] Added PedestalDisplayable tag to {prefab.name}.");
                    }
                }
            }
        }
    }
}
