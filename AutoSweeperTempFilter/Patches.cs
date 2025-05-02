using HarmonyLib;
using KSerialization;
using PeterHan.PLib;
using PeterHan.PLib.UI;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using static PeterHan.PLib.UI.PUIDelegates;

namespace AutoSweeperTempFilter
{
    public static class Logger
    {
        public static bool VerboseLogging = false;

        public static void Log(string message)
        {
            if (VerboseLogging) Debug.Log("[AutoSweeperTempFilter] " + message);
        }

        public static void LogWarning(string message)
        {
            if (VerboseLogging) Debug.LogWarning("[AutoSweeperTempFilter] " + message);
        }

        public static void LogError(string message)
        {
            if (VerboseLogging) Debug.LogError("[AutoSweeperTempFilter] " + message);
        }
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public class AutoSweeperSettings : KMonoBehaviour
    {
        [Serialize]
        public float minTempK = 0f;

        [Serialize]
        public float maxTempK = 1000f;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Logger.Log($"Spawned {gameObject?.name} (ID={gameObject?.GetInstanceID()}) with min={minTempK}, max={maxTempK}");
        }
    }

    public class AutoSweeperSideScreen : SideScreenContent
    {
        private AutoSweeperSettings target;
        private PTextField minTempField, maxTempField;
        private PLabel minLabel, maxLabel;
        private GameObject minLabelGO, maxLabelGO;
        private TMP_InputField minInput, maxInput;

        public override string GetTitle() => "Temperature Filtering AutoSweeper";

        public override bool IsValidForTarget(GameObject target) => target != null && target.GetComponent<AutoSweeperSettings>() != null;

        public override void SetTarget(GameObject target)
        {
            Logger.Log("SetTarget called");

            this.target = target?.GetComponent<AutoSweeperSettings>();
            if (this.target == null)
            {
                Logger.LogWarning("No AutoSweeperSettings found");
                return;
            }

            string suffix = GameUtil.GetTemperatureUnitSuffix();
            var minLabelLocText = minLabelGO?.transform.Find("Text")?.GetComponent<LocText>();
            var maxLabelLocText = maxLabelGO?.transform.Find("Text")?.GetComponent<LocText>();

            if (minLabelLocText != null) minLabelLocText.text = $"Min Temperature {suffix}";
            if (maxLabelLocText != null) maxLabelLocText.text = $"Max Temperature {suffix}";

            if (minInput != null) minInput.text = ConvertToPreferredUnit(this.target.minTempK).ToString("F1");
            if (maxInput != null) maxInput.text = ConvertToPreferredUnit(this.target.maxTempK).ToString("F1");
        }

        protected override void OnPrefabInit()
        {
            Logger.Log("Building UI manually");

            var panel = new PPanel("AutoSweeperTempFilterPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10
            };

            minLabel = CreateLabel("MinLabel", $"Min Temperature {GameUtil.GetTemperatureUnitSuffix()}", go => minLabelGO = go);
            maxLabel = CreateLabel("MaxLabel", $"Max Temperature {GameUtil.GetTemperatureUnitSuffix()}", go => maxLabelGO = go);

            float minTemp = target?.minTempK ?? 0f;
            float maxTemp = target?.maxTempK ?? 1000f;

            minTempField = CreateTextField("MinTempField", ConvertToPreferredUnit(minTemp).ToString("F1"), "Minimum pickup temperature", val => target.minTempK = ConvertInputToKelvin(val), go => minInput = go.GetComponentInChildren<TMP_InputField>());
            maxTempField = CreateTextField("MaxTempField", ConvertToPreferredUnit(maxTemp).ToString("F1"), "Maximum pickup temperature", val => target.maxTempK = ConvertInputToKelvin(val), go => maxInput = go.GetComponentInChildren<TMP_InputField>());

            panel.AddChild(minLabel);
            panel.AddChild(minTempField);
            panel.AddChild(maxLabel);
            panel.AddChild(maxTempField);

            ContentContainer = panel.AddTo(gameObject, 0);
        }

        private PLabel CreateLabel(string name, string text, OnRealize onRealize)
        {
            var label = new PLabel(name)
            {
                Text = text,
                TextStyle = PUITuning.Fonts.TextDarkStyle
            };
            label.AddOnRealize(go => {
                onRealize(go);
                var locText = go.transform.Find("Text")?.GetComponent<LocText>();
                if (locText != null && locText.text != text)
                    Logger.LogWarning($"{name} label mismatch: expected \"{text}\", got \"{locText.text}\"");
            });
            return label;
        }

        private PTextField CreateTextField(string name, string defaultText, string tooltip, Action<float> onValidInput, OnRealize onRealize)
        {
            var field = new PTextField(name)
            {
                Text = defaultText,
                FlexSize = new Vector2(2.0f, 1.0f),
                ToolTip = tooltip,
                OnTextChanged = (go, text) =>
                {
                    if (target != null && float.TryParse(text, out float value))
                    {
                        onValidInput(value);
                        Logger.Log($"Updated {name} to {value:F1}");
                    }
                }
            };
            field.AddOnRealize(onRealize);
            return field;
        }

        private static float ConvertToPreferredUnit(float kelvin)
        {
            switch (GameUtil.temperatureUnit)
            {
                case GameUtil.TemperatureUnit.Celsius: return kelvin - 273.15f;
                case GameUtil.TemperatureUnit.Fahrenheit: return (kelvin - 273.15f) * 9f / 5f + 32f;
                default: return kelvin;
            }
        }

        private static float ConvertInputToKelvin(float input)
        {
            switch (GameUtil.temperatureUnit)
            {
                case GameUtil.TemperatureUnit.Celsius: return input + 273.15f;
                case GameUtil.TemperatureUnit.Fahrenheit: return (input - 32f) * 5f / 9f + 273.15f;
                default: return input;
            }
        }
    }

    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    public static class AutoSweeperTempFilterLoadBuildings
    {
        public static void Prefix()
        {
            Logger.Log("Registering AutoSweeperTempFilter Auto-Sweeper");

            Strings.Add("STRINGS.BUILDINGS.PREFABS.AUTOSWEEPERTEMPFILTER.NAME", "Auto-Sweeper/Temp Filter");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.AUTOSWEEPERTEMPFILTER.DESC", "An enhanced Auto-Sweeper that respects temperature bounds.");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.AUTOSWEEPERTEMPFILTER.EFFECT", "Sweeps items between minimum and maximum temperature settings.");

            ModUtil.AddBuildingToPlanScreen("Conveyance", AutoSweeperTempFilterConfig.ID);
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class AutoSweeperTempFilterDetailsScreenPatch
    {
        public static void Postfix()
        {
            Logger.Log("Registering SideScreen via PLib");
            PUIUtils.AddSideScreenContent<AutoSweeperSideScreen>();
        }
    }

    public class AutoSweeperTempFilterConfig : IBuildingConfig
    {
        public const string ID = "AutoSweeperTempFilter";

        public override BuildingDef CreateBuildingDef()
        {
            var anim = "mysweeper_kanim";

            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 3,
                height: 1,
                anim: anim,
                hitpoints: 30,
                construction_time: 120f,
                //construction_mass: TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
                //construction_materials: TUNING.MATERIALS.REFINED_METALS,
                construction_mass: new float[] { 200f, 50f, 20f },
                construction_materials: new string[] { "RefinedMetal", "Plastic","Glass" },
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
            def.AnimFiles = new KAnimFile[] { Assets.GetAnim(anim) };

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
            go.AddOrGet<SolidTransferArm>().pickupRange = 8;
            animController.initialAnim = "off";
            animController.TintColour = new Color(1.0f, 0.85f, 0.85f);
        }
    }

    [HarmonyPatch(typeof(SolidTransferArm), nameof(SolidTransferArm.FindFetchTarget))]
    public static class AutoSweeperTempFilterPickupFilterPatch
    {
        // FieldRef to private field: List<Pickupable> pickupables;
        private static readonly AccessTools.FieldRef<SolidTransferArm, List<Pickupable>> PickupablesRef =
            AccessTools.FieldRefAccess<SolidTransferArm, List<Pickupable>>("pickupables");

        public static bool Prefix(SolidTransferArm __instance, Storage destination, FetchChore chore, ref Pickupable __result)
        {
            var settings = __instance.GetComponent<AutoSweeperSettings>();
            if (settings == null) return true;

            List<Pickupable> validItems = new List<Pickupable>();
            List<Pickupable> pickupables = PickupablesRef(__instance);

            foreach (var item in pickupables)
            {
                var pe = item.GetComponent<PrimaryElement>();
                if (pe == null) continue;
                float tempK = pe.Temperature;
                if (tempK >= settings.minTempK && tempK <= settings.maxTempK)
                    validItems.Add(item);
                else if (Logger.VerboseLogging)
                    Logger.Log($"Skipping {pe.Element.name} at {tempK:F1}K (out of bounds)");
            }

            __result = FetchManager.FindFetchTarget(validItems, destination, chore);
            return false;
        }
    }

}
