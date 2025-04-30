using HarmonyLib;
using UnityEngine;
using TUNING;
using PeterHan.PLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using KMod;
using System.Collections.Generic;

namespace SmartSweep2
{
    public sealed class SideScreenPatchMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Debug.Log("[SmartSweep2] OnLoad called");
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            // Note: Not depending on LocString.CreateLocStringKeys anymore for menu names
        }
    }

    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    public static class SmartSweep2LoadGeneratedBuildings
    {
        public static void Prefix()
        {
            Debug.Log("[SmartSweep2] Registering Custom Auto-Sweeper");

            // ✅ Hard-register strings manually
            Strings.Add("STRINGS.BUILDINGS.PREFABS.SMARTSWEEP2.NAME", "SmartSweep2 Auto-Sweeper");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.SMARTSWEEP2.DESC", "A modified Auto-Sweeper with custom settings.");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.SMARTSWEEP2.EFFECT", "Sweeps items based on temperature settings.");

            ModUtil.AddBuildingToPlanScreen("Conveyance", SmartSweep2Config.ID);
        }
    }

    [HarmonyPatch(typeof(DetailsScreen))]
    public static class DetailsScreen_OnPrefabInit_Patch
    {
        static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(DetailsScreen), "OnPrefabInit");
        }

        public static void Postfix()
        {
            Debug.Log("[SmartSweep2] Registering AutoSweeper SideScreen");
            PUIUtils.AddSideScreenContent<AutoSweeperSideScreen>();
        }
    }

    public class SmartSweep2Config : IBuildingConfig
    {
        public const string ID = "SmartSweep2";

        public override BuildingDef CreateBuildingDef()
        {
            Debug.Log("[SmartSweep2] CreateBuildingDef called");
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 3,
                height: 3,
                anim: "conveyor_transferarm_kanim",
                hitpoints: 30,
                construction_time: 120f,
                construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
                construction_materials: MATERIALS.REFINED_METALS,
                melting_point: 1600f,
                build_location_rule: BuildLocationRule.Anywhere,
                decor: BUILDINGS.DECOR.BONUS.TIER1,
                noise: NOISE_POLLUTION.NOISY.TIER0);

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

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            Debug.Log("[SmartSweep2] ConfigureBuildingTemplate called");
            go.AddOrGet<Storage>();
            go.AddOrGet<SolidTransferArm>();
            go.AddOrGet<Operational>();
            go.AddOrGet<LogicOperationalController>();
            go.AddOrGet<AutoSweeperSettings>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            Debug.Log("[SmartSweep2] DoPostConfigureComplete called");
            var animController = go.AddOrGet<KBatchedAnimController>();
            animController.initialAnim = "off";
        }
    }

    public class AutoSweeperSettings : KMonoBehaviour
    {
        public float minTempC = 0f;
        public float maxTempC = 1000f;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Debug.Log("[SmartSweep2] Spawned with default minTempC=0 and maxTempC=1000");
        }
    }

    public class AutoSweeperSideScreen : SideScreenContent
    {
        private AutoSweeperSettings target;
        private PTextField minTempField;
        private PTextField maxTempField;

        public override string GetTitle()
        {
            return "Custom Auto-Sweeper Settings";
        }

        public override bool IsValidForTarget(GameObject target)
        {
            Debug.Log("[SmartSweep2] Checking if target is valid for side screen");
            return target != null && target.GetComponent<AutoSweeperSettings>() != null;
        }

        public override void SetTarget(GameObject target)
        {
            Debug.Log("[SmartSweep2] Setting side screen target");
            this.target = target?.GetComponent<AutoSweeperSettings>();
            if (this.target == null)
                return;

            if (minTempField != null)
                minTempField.Text = this.target.minTempC.ToString("F1");
            if (maxTempField != null)
                maxTempField.Text = this.target.maxTempC.ToString("F1");
        }

        protected override void OnPrefabInit()
        {
            Debug.Log("[SmartSweep2] Initializing side screen UI");
            var panel = new PPanel("SweeperPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10
            };

            minTempField = new PTextField("MinTempField")
            {
                Text = "0",
                ToolTip = "Minimum pickup temperature (°C)",
                OnTextChanged = (sender, text) => {
                    if (target != null && float.TryParse(text, out float value))
                        target.minTempC = value;
                }
            };
            panel.AddChild(new PLabel("MinTempLabel") { Text = "Min Temperature (°C)" });
            panel.AddChild(minTempField);

            maxTempField = new PTextField("MaxTempField")
            {
                Text = "1000",
                ToolTip = "Maximum pickup temperature (°C)",
                OnTextChanged = (sender, text) => {
                    if (target != null && float.TryParse(text, out float value))
                        target.maxTempC = value;
                }
            };
            panel.AddChild(new PLabel("MaxTempLabel") { Text = "Max Temperature (°C)" });
            panel.AddChild(maxTempField);

            ContentContainer = panel.AddTo(gameObject, 0);
        }
    }
}
