using HarmonyLib;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

namespace ArtifactsPlus
{
    public static class MinionPersonalityPanel_Debug
    {
        // Global flag to enable or disable updates
        public static bool EnableUpdates = false; // Set to false to disable updates
    }

    [HarmonyPatch(typeof(MinionPersonalityPanel), "OnPrefabInit")]
    public static class MinionPersonalityPanel_OnPrefabInit_Patch
    {
        public static CollapsibleDetailContentPanel artifactPanel; // Changed to public

        static void Postfix(MinionPersonalityPanel __instance)
        {
            if (!MinionPersonalityPanel_Debug.EnableUpdates)
            {
                Patches.Logger.Log("[ArtifactsPlus] Minion Personality Panel updates are disabled.");
                return;
            }

            // Use reflection to access the protected method
            var createCollapsableSectionMethod = typeof(DetailScreenTab).GetMethod("CreateCollapsableSection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (createCollapsableSectionMethod != null)
            {
                artifactPanel = (CollapsibleDetailContentPanel)createCollapsableSectionMethod.Invoke(__instance, new object[] { "Artifact Effects" });
            }
        }
    }

    [HarmonyPatch(typeof(MinionPersonalityPanel), "Refresh")]
    public static class MinionPersonalityPanel_Refresh_Patch
    {
        static void Postfix(MinionPersonalityPanel __instance)
        {
            if (!MinionPersonalityPanel_Debug.EnableUpdates)
            {
                return;
            }

            if (MinionPersonalityPanel_OnPrefabInit_Patch.artifactPanel != null)
            {
                // Get the selected minion from the panel
                var selectedTargetField = typeof(TargetPanel).GetField("selectedTarget", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var selectedTarget = selectedTargetField?.GetValue(__instance) as UnityEngine.GameObject;

                // Use the new summary method
                string summary = selectedTarget != null
                    ? ArtifactEffectTracker.GetMinionArtifactInfusions(selectedTarget) // Updated method
                    : "No minion selected.";

                MinionPersonalityPanel_OnPrefabInit_Patch.artifactPanel.SetLabel("artifact_summary", summary, "Summary of artifact effects currently applied to this minion.");
                MinionPersonalityPanel_OnPrefabInit_Patch.artifactPanel.Commit();
            }
        }
    }
}