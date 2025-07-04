using HarmonyLib;
using PeterHan.PLib;
using PeterHan.PLib.UI;
using UnityEngine;
using System.Linq;

namespace ArtifactsPlus
{
    public static class ArtifactPedestalPanel_Debug
    {
        // Global flag to enable or disable updates
        public static bool EnableUpdates = true; // Set to false to disable updates
    }

    [HarmonyPatch(typeof(DetailsScreen), "Refresh", new[] { typeof(GameObject) })]
    public static class ArtifactPedestalPanelPatch
    {
        static void Postfix(DetailsScreen __instance)
        {
            if (!ArtifactPedestalPanel_Debug.EnableUpdates || !__instance.gameObject.activeInHierarchy)
            {
                return; // Skip updates if disabled or panel is not visible
            }

            // No logic needed here for the sidescreen, handled by SideScreenContent
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class ArtifactPedestalPanelRegister
    {
        private static bool registered = false;

        public static void Postfix()
        {
            if (!ArtifactPedestalPanel_Debug.EnableUpdates)
            {
                return; // Skip registration if updates are disabled
            }

            if (registered) return;
            registered = true;
            PUIUtils.AddSideScreenContent<ArtifactPedestalSimpleLabelScreen>();
        }
    }

    public class ArtifactPedestalSimpleLabelScreen : SideScreenContent
    {
        private GameObject root;
        private PLabel label;
        private LocText labelLocText;
        private GameObject lastTarget;

        private string ArtifactInfo(GameObject target)
        {
            if (!ArtifactPedestalPanel_Debug.EnableUpdates || (root != null && !root.activeInHierarchy))
            {
                return string.Empty; // Return empty string if updates are disabled or panel is not visible
            }

            var pedestal = target?.GetComponent<ItemPedestal>();
            var receptacle = pedestal?.GetType()
                .GetField("receptacle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(pedestal) as SingleEntityReceptacle;

            string artifactId = receptacle?.Occupant?.PrefabID().ToString() ?? null;

            if (!string.IsNullOrEmpty(artifactId))
            {
                var lines = new System.Collections.Generic.List<string>();

                // Check if the artifact is active
                if (ArtifactStateTracker.ArtifactStates.TryGetValue(receptacle.Occupant.GetInstanceID(), out var state))
                {
                    string status = state.IsActive ? "Active" : "Inactive";
                    lines.Add($"Status: {status}");
                }
                else
                {
                    lines.Add("Status: Unknown");
                }
                // Add artifact instance ID
                lines.Add($"Instance ID: {receptacle.Occupant.GetInstanceID()}");
                // Add modifiers
                if (ArtifactEffectTracker.TryGetArtifactModifiers(artifactId, out var modifiers) && modifiers.Count > 0)
                {
                    lines.AddRange(modifiers.Select(kv =>
                    {
                        string sign = kv.Value > 0 ? "+" : "";
                        return $"{kv.Key}: {sign}{kv.Value}";
                    }));
                }

                // Add effects
                var config = ArtifactStateTracker.GetArtifactConfig(artifactId);
                if (config != null && config.Effects != null && config.Effects.Count > 0)
                {
                    lines.AddRange(config.Effects.Keys);
                }

                if (lines.Count == 0)
                    return "No artifact effects available.";
                return string.Join("\n", lines);
            }
            else
            {
                return "No artifact placed.";
            }
        }

        public override bool IsValidForTarget(GameObject target)
        {
            if (!ArtifactPedestalPanel_Debug.EnableUpdates)
            {
                return false; // Return false if updates are disabled
            }

            bool valid = target != null && target.GetComponent<ItemPedestal>() != null;
            return valid;
        }

        public override void SetTarget(GameObject target)
        {
            if (!ArtifactPedestalPanel_Debug.EnableUpdates || (root != null && !root.activeInHierarchy))
            {
                return; // Skip setting the target if updates are disabled or panel is not visible
            }

            lastTarget = target;
            // Update label text when the target changes
            if (labelLocText != null)
                labelLocText.text = ArtifactInfo(target);
        }

        protected override void OnPrefabInit()
        {
            if (!ArtifactPedestalPanel_Debug.EnableUpdates)
            {
                return; // Skip initialization if updates are disabled
            }

            if (root == null)
            {
                var layout = new PPanel("ArtifactPanel")
                {
                    Direction = PanelDirection.Vertical,
                    Spacing = 10,
                    BackColor = new Color(0, 0, 0, 0),
                    Margin = new RectOffset(10, 10, 10, 10),
                    DynamicSize = true
                };

                label = new PLabel("ArtifactLabel")
                {
                    Text = ArtifactInfo(lastTarget), // Use the new function here
                    TextStyle = PUITuning.Fonts.TextDarkStyle,
                    FlexSize = new Vector2(320, -1),
                    ToolTip = "Displays artifact effects and modifiers."
                }.AddOnRealize(go =>
                {
                    labelLocText = go.GetComponent<LocText>() ?? go.GetComponentInChildren<LocText>(true);
                    if (labelLocText != null)
                        labelLocText.text = ArtifactInfo(lastTarget);
                });

                layout.AddChild(label);

                if (base.ContentContainer != null)
                {
                    root = layout.AddTo(base.ContentContainer, -1);
                }
                else
                {
                    root = layout.AddTo(gameObject, -1);
                }
            }
        }

        public override string GetTitle() => "Artifact Effects";
        public override float GetSortKey() => 100f;

        public override void ClearTarget()
        {
            if (!ArtifactPedestalPanel_Debug.EnableUpdates)
            {
                return; // Skip clearing the target if updates are disabled
            }
        }
    }
}