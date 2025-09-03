using HarmonyLib;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Mementos
{
    public static class MementosGlobals
    {
        public static bool KeybindsEnabled = true;
    }

    public static class MinionSelectionManager // Renamed from KeybindHandler
    {
        public static MinionIdentity SelectedMinion { get; set; }
    }

    public class Mod : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary(false);
            base.OnLoad(harmony);
            if (MementosGlobals.KeybindsEnabled)
            {
                Keybinder.KeyInputHandler.Register(new PPatchManager(harmony), HotKeys.All);
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

                Mementos.MinionSelectionManager.SelectedMinion = target.GetComponent<MinionIdentity>();

                var minion = target.GetComponent<MinionIdentity>();
                if (minion != null && MinionPersonalityPanel_AddMedalsPanelPatch.medalsPanel != null)
                {
                    var medalInfo = minion.FindOrAddComponent<MedalInfo>();
                    string medalsText = "No mementos awarded.";
                    if (medalInfo != null && medalInfo.Medals.Count > 0)
                    {
                        medalsText = string.Join("\n", medalInfo.Medals.Select(m => $"{m.Description}"));
                    }
                    MinionPersonalityPanel_AddMedalsPanelPatch.medalsPanel.SetLabel("Mementos", medalsText, "Mementos awarded to this minion.");
                    MinionPersonalityPanel_AddMedalsPanelPatch.medalsPanel.Commit();
                }
            }
        }

        [HarmonyPatch(typeof(BaseMinionConfig), "BaseMinion")]
        public static class BaseMinionConfig_BaseMinion_MedalInfoPatch
        {
            public static void Postfix(GameObject __result)
            {
                __result.AddOrGet<MedalInfo>();
            }
        }

        [HarmonyPatch(typeof(Game), "OnSpawn")]
        public static class Game_OnSpawn_MementosPatch
        {
            public static void Postfix(Game __instance)
            {
                __instance.Subscribe((int)GameHashes.RocketLanded, Mementos.MementosEvents.OnRocketLanded);
                __instance.Subscribe((int)GameHashes.Landed, Mementos.MementosEvents.OnLanded);
                __instance.Subscribe((int)GameHashes.Landed, Mementos.MementosEvents.OnModuleLanderLanded);
            }
        }

        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.OnPrefabInit))]
        public static class SaveGamePatch
        {
            public static void Postfix(Game __instance)
            {
                if (__instance.GetComponent<MementosGlobalData>() == null)
                {
                    __instance.gameObject.AddComponent<MementosGlobalData>();
                    //Debug.Log("[Mementos] MementosGlobalData added to SaveGame object.");
                }
            }
        }
    }
}
