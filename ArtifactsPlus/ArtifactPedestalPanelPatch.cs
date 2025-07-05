using HarmonyLib;
using PeterHan.PLib;
using PeterHan.PLib.UI;
using UnityEngine;
using System.Linq;

namespace ArtifactsPlus
{
    [HarmonyPatch(typeof(DetailsScreen), "Refresh", new[] { typeof(GameObject) })]
    public static class ArtifactPedestalPanelPatch
    {
        static void Postfix(DetailsScreen __instance)
        {
            Patches.Logger.Log("[ArtifactPedestalPanelPatch] Refresh called.");

            if (!__instance.gameObject.activeInHierarchy)
            {
                Patches.Logger.Log("[ArtifactPedestalPanelPatch] Panel is not active in hierarchy. Skipping update.");
                return; // Skip updates if disabled or panel is not visible
            }

            Patches.Logger.Log("[ArtifactPedestalPanelPatch] Panel is active. Refresh logic executed.");
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class ArtifactPedestalPanelRegister
    {
        private static bool registered = false;

        public static void Postfix()
        {
            Patches.Logger.Log("[ArtifactPedestalPanelRegister] OnPrefabInit called.");

            if (registered)
            {
                Patches.Logger.Log("[ArtifactPedestalPanelRegister] Already registered. Skipping.");
                return;
            }

            registered = true;
            Patches.Logger.Log("[ArtifactPedestalPanelRegister] Registering ArtifactPedestalSimpleLabelScreen.");
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
            Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] ArtifactInfo called.");

            var pedestal = target?.GetComponent<ItemPedestal>();
            Patches.Logger.Log($"[ArtifactPedestalSimpleLabelScreen] Pedestal found: {pedestal != null}");

            var receptacle = pedestal?.GetType()
                .GetField("receptacle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(pedestal) as SingleEntityReceptacle;
            Patches.Logger.Log($"[ArtifactPedestalSimpleLabelScreen] Receptacle found: {receptacle != null}");

            GameObject artifact = receptacle?.Occupant;
            int? artifactId = artifact?.GetInstanceID();
             
            Patches.Logger.Log($"[ArtifactPedestalSimpleLabelScreen] Artifact found: {artifact != null}, ID: {artifactId}");

            if (artifact != null)
            {
                Patches.Logger.Log($"[ArtifactPedestalSimpleLabelScreen] Pedestal displaying stats for artifact ID: {artifactId}");
            }
            else
            {
                Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] No artifact found in the pedestal.");
            }

            if (artifact == null || !artifactId.HasValue)
            {
                Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] No artifact found or artifact ID is null.");
                return "No artifact placed.";
            }

                var lines = new System.Collections.Generic.List<string>();

                if (ArtifactStateTracker.ArtifactStates.TryGetValue(artifactId.Value, out var state))
                {
                    string status = state.IsActive ? "Active" : "Inactive";
                    lines.Add($"Status: {status}");
                    Patches.Logger.Log($"[ArtifactPedestalSimpleLabelScreen] Artifact state retrieved: {status}");
                }
                else
                {
                    lines.Add("Status: Unknown");
                    Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] Artifact state unknown.");
                }

                lines.Add($"Instance ID: {artifactId.Value}");
                string artifactName = artifact?.name;

            if (ArtifactEffectTracker.TryGetArtifactModifiers(artifactName, out var modifiers) && modifiers.Count > 0)
                {
                    Patches.Logger.Log($"[ArtifactPedestalSimpleLabelScreen] Artifact modifiers found: {modifiers.Count}");
                    lines.AddRange(modifiers.Select(kv =>
                    {
                        string sign = kv.Value > 0 ? "+" : "";
                        return $"{kv.Key}: {sign}{kv.Value}";
                    }));
                }

                return string.Join("\n", lines);
             }

        public override bool IsValidForTarget(GameObject target)
        {
            Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] IsValidForTarget called.");
            bool valid = target != null && target.GetComponent<ItemPedestal>() != null;
            Patches.Logger.Log($"[ArtifactPedestalSimpleLabelScreen] Target valid: {valid}");
            return valid;
        }

        public override void SetTarget(GameObject target)
        {
            Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] SetTarget called.");

            if (root == null)
            {
                Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] Root is null. Ensure OnPrefabInit is called.");
                return;
            }

            if (!root.activeInHierarchy)
            {
                Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] Root is not active in hierarchy. Activating root.");
                root.SetActive(true); // Activate the root object
            }

            lastTarget = target;
            Patches.Logger.Log($"[ArtifactPedestalSimpleLabelScreen] Target set: {target?.name}");

            if (labelLocText != null)
            {
                labelLocText.text = ArtifactInfo(target);
                Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] Label text updated.");
            }
        }

        protected override void OnPrefabInit()
        {
            Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] OnPrefabInit called.");

            if (root == null)
            {
                Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] Initializing root layout.");

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
                    Text = ArtifactInfo(lastTarget),
                    TextStyle = PUITuning.Fonts.TextDarkStyle,
                    FlexSize = new Vector2(320, -1),
                    ToolTip = "Displays artifact effects and modifiers."
                }.AddOnRealize(go =>
                {
                    labelLocText = go.GetComponent<LocText>() ?? go.GetComponentInChildren<LocText>(true);
                    Patches.Logger.Log($"[ArtifactPedestalSimpleLabelScreen] LabelLocText initialized: {labelLocText != null}");

                    if (labelLocText != null)
                        labelLocText.text = ArtifactInfo(lastTarget);
                });

                layout.AddChild(label);

                if (base.ContentContainer != null)
                {
                    root = layout.AddTo(base.ContentContainer, -1);
                    Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] Root added to ContentContainer.");
                }
                else
                {
                    root = layout.AddTo(gameObject, -1);
                    Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] Root added to GameObject.");
                }
            }
        }

        public override string GetTitle()
        {
            Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] GetTitle called.");
            return "Artifact Effects";
        }

        public override float GetSortKey()
        {
            Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] GetSortKey called.");
            return 100f;
        }

        public override void ClearTarget()
        {
            Patches.Logger.Log("[ArtifactPedestalSimpleLabelScreen] ClearTarget called.");
        }
    }
}