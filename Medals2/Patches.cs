using Database;
using Epic.OnlineServices;
using HarmonyLib;
using Klei.AI;
using KMod;
using KSerialization;
using Newtonsoft.Json;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using STRINGS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TemplateClasses;
using TUNING;
using UnityEngine;
using static STRINGS.UI.UISIDESCREENS.AUTOPLUMBERSIDESCREEN.BUTTONS;

// --- MOD ENTRY POINT ---
public class Mod : UserMod2
{
    public override void OnLoad(Harmony harmony)
    {
        PUtil.InitLibrary(false);
        new POptions().RegisterOptions(this, typeof(Config));
        base.OnLoad(harmony);
        Medals.KeybindHandler.Register(new PPatchManager(harmony));
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

        Medals.KeybindHandler.SelectedMinion = target.GetComponent<MinionIdentity>();

        var minion = target.GetComponent<MinionIdentity>();
        if (minion != null && MinionPersonalityPanel_AddMedalsPanelPatch.medalsPanel != null)
        {
            var medalInfo = minion.FindOrAddComponent<MedalInfo>();
            string medalsText = "No medals awarded.";
            if (medalInfo != null && medalInfo.Medals.Count > 0)
            {
                medalsText = string.Join("\n", medalInfo.Medals.Select(m => $"{m.Name}: {m.Description}"));
            }
            MinionPersonalityPanel_AddMedalsPanelPatch.medalsPanel.SetLabel("medals", medalsText, "Permanent medals awarded to this minion.");
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

