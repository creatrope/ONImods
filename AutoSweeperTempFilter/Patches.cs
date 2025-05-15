using HarmonyLib;
using KMod;
using PeterHan.PLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using KSerialization;
using System;
using UnityEngine;

namespace ThermoSensorPlus
{
    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            Debug.Log("[ThermoSensorPlus] Mod loaded and Harmony patches applied.");
            harmony.PatchAll();
        }
    }

    // Persistent state component storing a random ID
    [SerializationConfig(MemberSerialization.OptIn)]
    public class ThermoSensorStateComponent : KMonoBehaviour
    {
        [Serialize]
        public int randomID;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if (randomID == 0)
            {
                randomID = UnityEngine.Random.Range(100000, 999999);
                Debug.Log($"[ThermoSensorPlus] Assigned new random ID: {randomID} to {gameObject.name}");
            }
            else
            {
                Debug.Log($"[ThermoSensorPlus] Restored existing ID: {randomID} for {gameObject.name}");
            }
        }
    }

    // Add component to newly built LogicTemperatureSensors
    [HarmonyPatch(typeof(LogicTemperatureSensorConfig), "DoPostConfigureComplete")]
    public static class ThermoSensorAddState_New
    {
        public static void Postfix(GameObject go)
        {
            go.AddOrGet<ThermoSensorStateComponent>();
            Debug.Log($"[ThermoSensorPlus] Attached state component (new) to {go.name}");
        }
    }

    // Add component to existing (loaded-from-save) LogicTemperatureSensors
    [HarmonyPatch(typeof(LogicTemperatureSensor), "OnSpawn")]
    public static class ThermoSensorAddState_Existing
    {
        public static void Prefix(LogicTemperatureSensor __instance)
        {
            var go = __instance.gameObject;
            if (go.GetComponent<ThermoSensorStateComponent>() == null)
            {
                go.AddOrGet<ThermoSensorStateComponent>();
                Debug.Log($"[ThermoSensorPlus] Attached state component (existing) to {go.name}");
            }
        }
    }

    // Register side screen
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
            bool valid = target.GetComponent<LogicTemperatureSensor>() != null &&
                         target.GetComponent<ThermoSensorStateComponent>() != null;
            Debug.Log($"[ThermoSensorPlus] IsValidForTarget = {valid}");
            return valid;
        }

        public override void SetTarget(GameObject target)
        {
            Debug.Log($"[ThermoSensorPlus] SetTarget on instance {GetInstanceID()} for target {target?.name}");

            var state = target?.GetComponent<ThermoSensorStateComponent>();
            if (idLocText != null && state != null)
            {
                idLocText.text = $"[TS+] Sensor ID: {state.randomID}";
                Debug.Log($"[ThermoSensorPlus] Displaying persistent random ID: {state.randomID}");
            }
            else
            {
                Debug.LogWarning("[ThermoSensorPlus] Could not assign label — missing LocText or state.");
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
                BackColor = new Color(0, 0, 0, 0),
                Margin = new RectOffset(10, 10, 10, 10)
            };

            idLabel = new PLabel("ClickLabel")
            {
                Text = "[TS+] Sensor ID: ",
                TextStyle = PUITuning.Fonts.TextDarkStyle,
            }.AddOnRealize(go =>
            {
                idLocText = go.transform.Find("Text")?.GetComponent<LocText>();
                Debug.Log($"[ThermoSensorPlus] OnRealize: idLocText assigned? {idLocText != null}");
            });

            panel.AddChild(idLabel);

            root = panel.AddTo(gameObject, 0);
            ContentContainer = root;

            Debug.Log("[ThermoSensorPlus] Simple label side screen UI initialized.");
        }

        public override void ClearTarget() { }
    }
}
