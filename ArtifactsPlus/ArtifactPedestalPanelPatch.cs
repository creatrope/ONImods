using HarmonyLib;
using Klei.AI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using HLib;

namespace ArtifactsPlus
{
    [HarmonyPatch(typeof(DetailScreenTab), "OnSelectTarget")]
    public static class ArtifactPedestalPanel_OnSelectTarget_Patch
    {
        private static CollapsibleDetailContentPanel currentPanel; // Track the current panel

        private static void Postfix(DetailScreenTab __instance, GameObject target)
        {
            // Always clean up previous panel
            if (currentPanel != null)
            {
                Object.Destroy(currentPanel.gameObject);
                currentPanel = null;
            }

            if (!IsValidForTarget(target))
            {
                //Patches.logger.LogDebug("[ArtifactPedestalPanel_OnSelectTarget_Patch] Target is not a valid pedestal.");
                return;
            }

            var pedestal = target.GetComponent<ItemPedestal>();
            if (pedestal != null)
            {
                Patches.logger.LogDebug($"[ArtifactPedestalPanel_OnSelectTarget_Patch] found a pedestal, instance id: {pedestal.GetInstanceID()}");
            }

            var createCollapsableSectionMethod = typeof(DetailScreenTab).GetMethod("CreateCollapsableSection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            currentPanel = (CollapsibleDetailContentPanel)createCollapsableSectionMethod.Invoke(__instance, new object[] { "Artifact Details" });
            if (currentPanel != null)
            {
                var receptacleField = typeof(ItemPedestal).GetField("receptacle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var receptacle = receptacleField?.GetValue(pedestal) as SingleEntityReceptacle;
                var artifact = receptacle?.Occupant;

                if (artifact != null)
                {
                    string artifactName = artifact.name;
                    int artifactInstanceId = artifact.GetInstanceID();
                    var state = ArtifactStateTracker.ArtifactStates.TryGetValue(artifactInstanceId, out var artifactState) ? artifactState : null;
                    bool active = state?.IsActive ?? false;

                    var lines = new System.Collections.Generic.List<string>();
                    if (ArtifactEffectTracker.TryGetArtifactModifiers(artifactName, out var modifiers) && modifiers.Count > 0)
                    {
                        lines.AddRange(modifiers.Select(kv =>
                        {
                            string sign = kv.Value > 0 ? "+" : "";
                            return $"{kv.Key}: {sign}{kv.Value}";
                        }));
                    }

                    currentPanel.SetLabel("artifact_active", $"Active: {active}", "Artifact Active Status");
                    if (lines.Count > 0)
                    {
                        currentPanel.SetLabel("artifact_modifiers", string.Join("\n", lines), "Artifact Modifiers");
                    }
                    currentPanel.SetLabel("artifact_id", $"Instance ID: {artifactInstanceId}", "Artifact Instance ID");

                }
                else
                {
                    currentPanel.SetLabel("artifact_name", "No artifact found.", "Artifact Name");
                }

                currentPanel.Commit();
            }
        }

        private static bool IsValidForTarget(GameObject target)
        {
            bool isPedestal = target != null && target.GetComponent<ItemPedestal>() != null;
            //Patches.logger.LogDebug($"[ArtifactPedestalPanel_OnSelectTarget_Patch] IsValidForTarget called for '{target?.name ?? "null"}' (Type: {target?.GetType().Name ?? "null"}), Has ItemPedestal: {isPedestal}");
            return isPedestal;
        }
    }
}