using HarmonyLib;
using KSerialization;
using PeterHan.PLib.UI;
using UnityEngine;
using TMPro; // Requires Unity.TextMeshPro.dll!

namespace SmartSweep4
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
            Debug.Log($"[SmartSweep4] Spawned {gameObject?.name} (ID={gameObject?.GetInstanceID()}) min={minTempC}, max={maxTempC}");
        }
    }

    public class AutoSweeperSideScreen : SideScreenContent
    {
        private AutoSweeperSettings target;

        private GameObject minFieldGO;
        private GameObject maxFieldGO;

        private PTextField minTempField;
        private PTextField maxTempField;

        public override string GetTitle() => "SmartSweep4 Settings";

        public override bool IsValidForTarget(GameObject target)
        {
            return target != null && target.GetComponent<AutoSweeperSettings>() != null;
        }

        public override void SetTarget(GameObject target)
        {
            this.target = target?.GetComponent<AutoSweeperSettings>();
            if (this.target == null)
            {
                Debug.LogWarning("[SmartSweep4] No AutoSweeperSettings found");
                return;
            }

            Debug.Log($"[SmartSweep4] SetTarget {this.target.gameObject?.name}");

            if (minFieldGO != null)
            {
                var input = minFieldGO.GetComponentInChildren<TMP_InputField>(true);
                if (input != null)
                    input.text = this.target.minTempC.ToString("F1");
            }
            if (maxFieldGO != null)
            {
                var input = maxFieldGO.GetComponentInChildren<TMP_InputField>(true);
                if (input != null)
                    input.text = this.target.maxTempC.ToString("F1");
            }
        }

        protected override void OnPrefabInit()
        {
            Debug.Log("[SmartSweep4] Building SideScreen UI");

            var panel = new PPanel("SweeperPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10
            };

            minTempField = new PTextField("MinTempField")
            {
                Text = "0",
                ToolTip = "Minimum pickup temperature (°C)",
                OnTextChanged = (go, text) => {
                    if (target != null && float.TryParse(text, out var value))
                    {
                        target.minTempC = value;
                        Debug.Log($"[SmartSweep4] minTempC changed to {value}");
                    }
                }
            }.AddOnRealize(go => minFieldGO = go);
            panel.AddChild(new PLabel("MinLabel") { Text = "Min Temperature (°C)", TextStyle = PUITuning.Fonts.TextDarkStyle });
            panel.AddChild(minTempField);

            maxTempField = new PTextField("MaxTempField")
            {
                Text = "1000",
                ToolTip = "Maximum pickup temperature (°C)",
                OnTextChanged = (go, text) => {
                    if (target != null && float.TryParse(text, out var value))
                    {
                        target.maxTempC = value;
                        Debug.Log($"[SmartSweep4] maxTempC changed to {value}");
                    }
                }
            }.AddOnRealize(go => maxFieldGO = go);
            panel.AddChild(new PLabel("MaxLabel") { Text = "Max Temperature (°C)", TextStyle = PUITuning.Fonts.TextDarkStyle });
            panel.AddChild(maxTempField);

            ContentContainer = panel.AddTo(gameObject, 0);
        }
    }

    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    public static class SmartSweep4LoadBuildings
    {
        public static void Prefix()
        {
            Debug.Log("[SmartSweep4] Registering Custom Auto-Sweeper");

            Strings.Add("STRINGS.BUILDINGS.PREFABS.SMARTSWEEP4.NAME", "SmartSweep4 Auto-Sweeper");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.SMARTSWEEP4.DESC", "An enhanced Auto-Sweeper that respects temperature settings.");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.SMARTSWEEP4.EFFECT", "Sweeps items between a minimum and maximum temperature.");

            ModUtil.AddBuildingToPlanScreen("Conveyance", SmartSweep4Config.ID);
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class SmartSweep4DetailsScreenPatch
    {
        public static void Postfix()
        {
            Debug.Log("[SmartSweep4] Registering SideScreen via PLib");

            PUIUtils.AddSideScreenContent<AutoSweeperSideScreen>();
        }
    }

    public class SmartSweep4Config : IBuildingConfig
    {
        public const string ID = "SmartSweep4";

        public override BuildingDef CreateBuildingDef()
        {
            Debug.Log("[SmartSweep4] CreateBuildingDef called");

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
            Debug.Log("[SmartSweep4] ConfigureBuildingTemplate called");

            go.AddOrGet<Storage>();
            go.AddOrGet<SolidTransferArm>();
            go.AddOrGet<Operational>();
            go.AddOrGet<LogicOperationalController>();
            go.AddOrGet<AutoSweeperSettings>(); // Attach the settings component
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            Debug.Log("[SmartSweep4] DoPostConfigureComplete called");

            var animController = go.AddOrGet<KBatchedAnimController>();
            animController.initialAnim = "off";
        }
    }
}
