using HarmonyLib;
using KSerialization;
using PeterHan.PLib;
using PeterHan.PLib.UI;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SmartSweep5
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class AutoSweeperSettings : KMonoBehaviour
    {
        [Serialize]
        public float minTempC = 0f;

        [Serialize]
        public float maxTempC = 1000f;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Debug.Log($"[SmartSweep5] Spawned {gameObject?.name} (ID={gameObject?.GetInstanceID()}) with min={minTempC}, max={maxTempC}");
        }
    }

    public class AutoSweeperSideScreen : SideScreenContent
    {
        private AutoSweeperSettings target;

        private PTextField minTempField;
        private PTextField maxTempField;

        private TMP_InputField minInput;
        private TMP_InputField maxInput;

        public override string GetTitle() => "Custom Auto-Sweeper Settings";

        public override bool IsValidForTarget(GameObject target)
        {
            return target != null && target.GetComponent<AutoSweeperSettings>() != null;
        }

        public override void SetTarget(GameObject target)
        {
            Debug.Log("[SmartSweep5] SetTarget called");
            this.target = target?.GetComponent<AutoSweeperSettings>();
            if (this.target == null)
            {
                Debug.LogWarning("[SmartSweep5] No AutoSweeperSettings found");
                return;
            }

            if (minInput != null)
                minInput.text = this.target.minTempC.ToString("F1");

            if (maxInput != null)
                maxInput.text = this.target.maxTempC.ToString("F1");
        }

        protected override void OnPrefabInit()
        {
            Debug.Log("[SmartSweep5] Building UI manually");

            var panel = new PPanel("SmartSweep5Panel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10
            };

            minTempField = new PTextField("MinTempField")
            {
                Text = "0",
                ToolTip = "Minimum pickup temperature (°C)",
                OnTextChanged = (go, text) =>
                {
                    if (target != null && float.TryParse(text, out float value))
                    {
                        target.minTempC = value;
                        Debug.Log($"[SmartSweep5] Updated minTempC to {value:F1}");
                    }
                }
            }.AddOnRealize(go => minInput = go.GetComponentInChildren<TMP_InputField>());

            maxTempField = new PTextField("MaxTempField")
            {
                Text = "1000",
                ToolTip = "Maximum pickup temperature (°C)",
                OnTextChanged = (go, text) =>
                {
                    if (target != null && float.TryParse(text, out float value))
                    {
                        target.maxTempC = value;
                        Debug.Log($"[SmartSweep5] Updated maxTempC to {value:F1}");
                    }
                }
            }.AddOnRealize(go => maxInput = go.GetComponentInChildren<TMP_InputField>());

            panel.AddChild(new PLabel("MinLabel") { Text = "Min Temperature (°C)", TextStyle = PUITuning.Fonts.TextDarkStyle });
            panel.AddChild(minTempField);

            panel.AddChild(new PLabel("MaxLabel") { Text = "Max Temperature (°C)", TextStyle = PUITuning.Fonts.TextDarkStyle });
            panel.AddChild(maxTempField);

            ContentContainer = panel.AddTo(gameObject, 0);
        }
    }

    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    public static class SmartSweep5LoadBuildings
    {
        public static void Prefix()
        {
            Debug.Log("[SmartSweep5] Registering SmartSweep5 Auto-Sweeper");

            Strings.Add("STRINGS.BUILDINGS.PREFABS.SMARTSWEEP5.NAME", "SmartSweep5 Auto-Sweeper");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.SMARTSWEEP5.DESC", "An enhanced Auto-Sweeper that respects temperature bounds.");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.SMARTSWEEP5.EFFECT", "Sweeps items between minimum and maximum temperature settings.");

            ModUtil.AddBuildingToPlanScreen("Conveyance", SmartSweep5Config.ID);
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class SmartSweep5DetailsScreenPatch
    {
        public static void Postfix()
        {
            Debug.Log("[SmartSweep5] Registering SideScreen via PLib");
            PUIUtils.AddSideScreenContent<AutoSweeperSideScreen>();
        }
    }

    public class SmartSweep5Config : IBuildingConfig
    {
        public const string ID = "SmartSweep5";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 3,
                height: 3,
                anim: "conveyor_transferarm_kanim",
                hitpoints: 30,
                construction_time: 120f,
                construction_mass: TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
                construction_materials: TUNING.MATERIALS.REFINED_METALS,
                melting_point: 1600f,
                build_location_rule: BuildLocationRule.Anywhere,
                decor: TUNING.BUILDINGS.DECOR.BONUS.TIER1,
                noise: TUNING.NOISE_POLLUTION.NOISY.TIER0
            );

            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = 120f;
            def.SelfHeatKilowattsWhenActive = 0.5f;
            def.ViewMode = OverlayModes.SolidConveyor.ID;
            def.AudioCategory = "Metal";
            def.UtilityInputOffset = new CellOffset(0, 0);
            def.PermittedRotations = PermittedRotations.R360;
            def.AnimFiles = new KAnimFile[] { Assets.GetAnim("conveyor_transferarm_kanim") };

            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            go.AddOrGet<Storage>();
            go.AddOrGet<SolidTransferArm>();
            go.AddOrGet<Operational>();
            go.AddOrGet<LogicOperationalController>();
            go.AddOrGet<AutoSweeperSettings>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            var animController = go.AddOrGet<KBatchedAnimController>();
            animController.initialAnim = "off";
        }
    }
}

[HarmonyPatch(typeof(SolidTransferArm), nameof(SolidTransferArm.FindFetchTarget))]
public static class SmartSweep5PickupFilterPatch
{
    public static void Postfix(SolidTransferArm __instance, Storage destination, FetchChore chore, ref Pickupable __result)
    {
        if (__result == null)
            return;

        var settings = __instance.GetComponent<SmartSweep5.AutoSweeperSettings>();
        if (settings == null)
            return;

        var element = __result.GetComponent<PrimaryElement>();
        if (element == null)
            return;

        float tempC = element.Temperature - 273.15f;

        if (tempC < settings.minTempC || tempC > settings.maxTempC)
        {
            Debug.Log($"[SmartSweep5] Rejecting {__result.name} at {tempC:F1}°C (bounds {settings.minTempC:F1}-{settings.maxTempC:F1})");
            __result = null;
        }
    }
}

