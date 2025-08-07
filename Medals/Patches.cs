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
        private Action snapshotAction;

        public string handlerName => "MinimalKeybindHandler";
        public KInputHandler inputHandler { get; set; }

        // Track the selected minion globally so the handler can access it
        internal static MinionIdentity SelectedMinion;

        internal MinimalKeybindHandler()
        {
            snapshotAction = KeyTestAction != null ? KeyTestAction.GetKAction() : PAction.MaxAction;
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
            // Add medals here
            AddMedal(new MedalInfo("Spawned Medal", "SpawnedMedal", "Awarded for spawning in the colony."));
            AddMedal(new MedalInfo("Rescued Dupe", "RescuedDupe", "Awarded for rescuing an incapacitated dupe."));
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

    [HarmonyPatch(typeof(MinionIdentity), "OnSpawn")]
    public static class MinionIdentity_SpawnMedalPatch
    {
        public static void Postfix(MinionIdentity __instance)
        {
            MedalsUtility.AddMedalToMinion(__instance.GetProperName(), "SpawnedMedal");
            Debug.Log($"[Medals] Awarded SpawnedMedal to '{__instance.GetProperName()}' on spawn.");
        }
    }

    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class Db_Initialize_MedalsRegistryPatch
    {
        public static void Postfix()
        {
            MedalsRegistry.LoadAndRegisterMedals();
            Debug.Log("[Medals] Medals registered after Db initialization.");
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

}
