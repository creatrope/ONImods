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
            // No logic needed here for the sidescreen, handled by SideScreenContent
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class ArtifactPedestalPanelRegister
    {
        private static bool registered = false;

        public static void Postfix()
        {
            if (registered) return;
            registered = true;
            CustomLogger.Log("[ArtifactPedestalSimpleLabelScreen] Registering sidescreen via PUIUtils.AddSideScreenContent.");
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
            var pedestal = target?.GetComponent<ItemPedestal>();
            var receptacle = pedestal?.GetType()
                .GetField("receptacle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(pedestal) as SingleEntityReceptacle;

            string artifactId = receptacle?.Occupant?.PrefabID().ToString() ?? null;

            if (!string.IsNullOrEmpty(artifactId))
            {
                var lines = new System.Collections.Generic.List<string>();

                if (ArtifactEffectTracker.TryGetArtifactModifiers(artifactId, out var modifiers) && modifiers.Count > 0)
                {
                    lines.Add("Modifiers:");
                    lines.AddRange(modifiers.Select(kv =>
                    {
                        string sign = kv.Value > 0 ? "+" : (kv.Value < 0 ? "-" : "");
                        return $"{kv.Key}: {sign}{kv.Value}";
                    }));
                }

                var config = ArtifactStateTracker.GetArtifactConfig(artifactId);
                if (config != null && config.Effects != null && config.Effects.Count > 0)
                {
                    lines.Add("Status Effects:");
                    lines.AddRange(config.Effects);
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
            bool valid = target != null && target.GetComponent<ItemPedestal>() != null;
            string componentList = target != null
                ? string.Join(", ", target.GetComponents<Component>().Select(c => c.GetType().Name))
                : "null";
            CustomLogger.Log($"[ArtifactPedestalSimpleLabelScreen] IsValidForTarget called. Target: {target?.name ?? "null"}, HasItemPedestal: {target?.GetComponent<ItemPedestal>() != null}, Result: {valid}, Components: [{componentList}]");
            return valid;
        }

        public override void SetTarget(GameObject target)
        {
            CustomLogger.Log($"[ArtifactPedestalSimpleLabelScreen] SetTarget ENTRY. Target: {target?.name ?? "null"}");
            lastTarget = target;
            // Update label text when the target changes
            if (labelLocText != null)
                labelLocText.text = ArtifactInfo(target);
        }

        protected override void OnPrefabInit()
        {
            CustomLogger.Log("[ArtifactPedestalSimpleLabelScreen] OnPrefabInit ENTRY");
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
                    CustomLogger.Log($"[ArtifactPedestalSimpleLabelScreen] OnRealize: labelLocText assigned? {labelLocText != null}");
                    if (labelLocText != null)
                        labelLocText.text = ArtifactInfo(lastTarget);
                });

                layout.AddChild(label);

                if (base.ContentContainer != null)
                {
                    root = layout.AddTo(base.ContentContainer, -1);
                    CustomLogger.Log("[ArtifactPedestalSimpleLabelScreen] Added layout as child to ContentContainer");
                }
                else
                {
                    root = layout.AddTo(gameObject, -1);
                    CustomLogger.Log("[ArtifactPedestalSimpleLabelScreen] Added layout as child to gameObject");
                }
            }
            CustomLogger.Log("[ArtifactPedestalSimpleLabelScreen] OnPrefabInit EXIT");
        }

        private static string GetRandomTestMessage()
        {
            string[] lines = new string[10];
            var rnd = new System.Random();
            for (int i = 0; i < 10; i++)
                lines[i] = $"Test Line {i + 1}: {rnd.Next(100000, 999999)}";
            return string.Join("\n", lines);
        }

        public override string GetTitle() => "Artifact Effects";
        public override float GetSortKey() => 100f;

        public override void ClearTarget() { }
    }
}