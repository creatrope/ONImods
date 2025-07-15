using HarmonyLib;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using System.Linq; // Add for component listing
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

namespace ArtifactsPlus
{
    [HarmonyPatch(typeof(MinionPersonalityPanel), "OnPrefabInit")]
    public static class MinionPersonalityPanel_OnPrefabInit_Patch
    {
        internal static CollapsibleDetailContentPanel modifiers; // Changed to internal

        private static void Postfix(MinionPersonalityPanel __instance)
        {
            var createCollapsableSectionMethod = typeof(DetailScreenTab).GetMethod("CreateCollapsableSection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (createCollapsableSectionMethod != null)
            {
                modifiers = (CollapsibleDetailContentPanel)createCollapsableSectionMethod.Invoke(__instance, new object[] { "Artifact Effects" });
            }
        }
    }

    [HarmonyPatch(typeof(MinionPersonalityPanel), "OnSelectTarget")]
    public static class MinionPersonalityPanel_OnSelectTarget_Patch
    {
        private static void Postfix(MinionPersonalityPanel __instance, GameObject target)
        {
            // Call IsValidTarget from this file
            if (!IsValidTarget(target))
            {
                Patches.LogDebug("[MinionPersonalityPanel_OnSelectTarget_Patch] Target is not a valid minion.");
                return;
            }
            string minionName = target.GetComponent<MinionIdentity>()?.GetProperName() ?? target.name;
            Patches.LogDebug($"[MinionPersonalityPanel_OnSelectTarget_Patch] found a minion: {minionName}");

            string summary = ArtifactEffectTracker.GetMinionArtifactInfusions(target);
            MinionPersonalityPanel_OnPrefabInit_Patch.modifiers.SetLabel("artifact_summary", summary, "Summary of artifact modifiers currently applied to this minion.");
            MinionPersonalityPanel_OnPrefabInit_Patch.modifiers.Commit();
        }

        private static bool IsValidTarget(GameObject target)
        {
            bool isMinion = target != null && target.GetComponent<MinionIdentity>() != null;
            Patches.LogDebug($"[MinionPersonalityPanel_OnSelectTarget_Patch] IsValidForTarget called for '{target?.name ?? "null"}' (Type: {target?.GetType().Name ?? "null"}), isMinion: {isMinion}");
            return isMinion;
        }
    }

    [HarmonyPatch(typeof(MinionPersonalityPanel), "OnCleanUp")]
    public static class MinionPersonalityPanel_OnCleanUp_Patch
    {
        static void Postfix()
        {
        }
    }
}