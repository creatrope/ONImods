using HarmonyLib;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

namespace ArtifactsPlus
{
    [HarmonyPatch(typeof(MinionPersonalityPanel), "OnPrefabInit")]
    public static class MinionPersonalityPanel_OnPrefabInit_Patch
    {
        internal static CollapsibleDetailContentPanel artifactPanel; // Changed to internal

        private static void Postfix(MinionPersonalityPanel __instance)
        {
            var createCollapsableSectionMethod = typeof(DetailScreenTab).GetMethod("CreateCollapsableSection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (createCollapsableSectionMethod != null)
            {
                artifactPanel = (CollapsibleDetailContentPanel)createCollapsableSectionMethod.Invoke(__instance, new object[] { "Artifact Effects" });
            }
        }
    }

    [HarmonyPatch(typeof(MinionPersonalityPanel), "OnSelectTarget")]
    public static class MinionPersonalityPanel_OnSelectTarget_Patch
    {
        private static void Postfix(MinionPersonalityPanel __instance, GameObject target)
        {
            if (target == null || MinionPersonalityPanel_OnPrefabInit_Patch.artifactPanel == null)
            {
                return;
            }

            string summary = target != null
                ? ArtifactEffectTracker.GetMinionArtifactInfusions(target)
                : "No minion selected.";
            MinionPersonalityPanel_OnPrefabInit_Patch.artifactPanel.SetLabel("artifact_summary", summary, "Summary of artifact modifiers currently applied to this minion.");
            MinionPersonalityPanel_OnPrefabInit_Patch.artifactPanel.Commit();
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