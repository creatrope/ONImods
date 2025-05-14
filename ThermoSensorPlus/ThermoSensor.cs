using HarmonyLib;
using KMod;
using PeterHan.PLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using System;
using UnityEngine;

namespace ThermoSensorPlus
{
    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary(); // Required for PLib
            Debug.Log("[ThermoSensorPlus] Mod loaded and Harmony patches applied.");
            harmony.PatchAll();
        }
    }

    // Dummy marker component to distinguish our sensors
    public sealed class ThermoSensorPlusTag : KMonoBehaviour { }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class ThermoSensorSideScreenRegister
    {
        private static bool registered = false;

        public static void Postfix()
        {
            if (registered) return;
            registered = true;

            Debug.Log("[ThermoSensorPlus] Registering simple label side screen");
            PUIUtils.AddSideScreenContent<ThermoSensorClickMeScreen>();
        }
    }

    public class ThermoSensorClickMeScreen : SideScreenContent
    {
        private GameObject root;

        public override bool IsValidForTarget(GameObject target)
        {
            bool valid = target.GetComponent<LogicTemperatureSensor>() != null &&
                         target.GetComponent<ThermoSensorPlusTag>() != null;
            Debug.Log($"[ThermoSensorPlus] IsValidForTarget = {valid}");
            return valid;
        }

        public override void SetTarget(GameObject target)
        {
            Debug.Log($"[ThermoSensorPlus] SetTarget on instance {GetInstanceID()} for target {target?.name}");
        }

        public override string GetTitle() => "ThermoSensor+";

        public override float GetSortKey() => -100f;

        protected override void OnPrefabInit()
        {
            Debug.Log($"[ThermoSensorPlus] OnPrefabInit for instance {GetInstanceID()}");

            if (root != null)
            {
                Debug.Log("[ThermoSensorPlus] UI already built, skipping.");
                return;
            }

            Debug.Log("[ThermoSensorPlus] Building UI");

            var panel = new PPanel("ClickPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10,
                BackColor = new Color(0.1f, 0.1f, 0.1f, 1f),
                Margin = new RectOffset(10, 10, 10, 10)
            };

            var label = new PLabel("ClickLabel")
            {
                Text = "[TS+] Static Label",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            };
            panel.AddChild(label);

            root = panel.AddTo(gameObject, 0);
            ContentContainer = root;

            Debug.Log("[ThermoSensorPlus] Simple label side screen UI initialized.");
        }

        public override void ClearTarget() { }
    }

    // Patch your building to add the marker
    [HarmonyPatch(typeof(LogicTemperatureSensorConfig), "DoPostConfigureComplete")]
    public static class TagLogicSensor
    {
        public static void Postfix(GameObject go)
        {
            go.AddOrGet<ThermoSensorPlusTag>();
            Debug.Log("[ThermoSensorPlus] Tag added to LogicTemperatureSensor");
        }
    }
}
