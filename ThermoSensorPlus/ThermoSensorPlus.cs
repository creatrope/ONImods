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
        private PLabel idLabel;
        private LocText idLocText;

        public override bool IsValidForTarget(GameObject target)
        {
            bool valid = target.GetComponent<LogicTemperatureSensor>() != null;
            Debug.Log($"[ThermoSensorPlus] IsValidForTarget = {valid}");
            return valid;
        }

        public override void SetTarget(GameObject target)
        {
            Debug.Log($"[ThermoSensorPlus] SetTarget on instance {GetInstanceID()} for target {target?.name}");

            if (idLocText != null && target != null)
            {
                idLocText.text = $"[TS+] Sensor ID: {target.GetInstanceID()}";
                Debug.Log($"[ThermoSensorPlus] Updated label with ID {target.GetInstanceID()}");
            }
        }

        public override string GetTitle() => "ThermoSensor+";

        public override float GetSortKey() => -100f;

        protected override void OnPrefabInit()
        {
            Debug.Log($"[ThermoSensorPlus] OnPrefabInit for instance {GetInstanceID()}");

            if (ContentContainer != null)
            {
                Debug.Log("[ThermoSensorPlus] UI already built, skipping.");
                return;
            }

            Debug.Log("[ThermoSensorPlus] Building UI");

            var panel = new PPanel("ClickPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10,
                BackColor = new Color(0, 0, 0, 0), // Transparent background
                Margin = new RectOffset(10, 10, 10, 10)
            };

            idLabel = new PLabel("ClickLabel")
            {
                Text = "[TS+] Sensor ID: ",
                TextStyle = PUITuning.Fonts.TextDarkStyle,
            };
            idLabel.OnRealize += go =>
            {
                idLocText = go.transform.Find("Text")?.GetComponent<LocText>();
                Debug.Log($"[ThermoSensorPlus] OnRealize: idLocText assigned? {idLocText != null}");
            };

            panel.AddChild(idLabel);

            root = panel.AddTo(gameObject, 0);
            ContentContainer = root;

            Debug.Log("[ThermoSensorPlus] Simple label side screen UI initialized.");
        }

        public override void ClearTarget() { }
    }
}
