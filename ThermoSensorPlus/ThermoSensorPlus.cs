using HarmonyLib;
using KMod;
using PeterHan.PLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using KSerialization;
using UnityEngine;
using TMPro;

namespace ThermoSensorPlus
{
    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Harmony.DEBUG = true;
            PUtil.InitLibrary();
            Debug.Log("[ThermoSensorPlus] Mod loaded. Applying Harmony patches.");
            harmony.PatchAll();
        }
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public class ThermoSensorStateComponent : KMonoBehaviour
    {
        [Serialize] public int randomID;
        [Serialize] public string customText;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if (randomID == 0)
            {
                randomID = UnityEngine.Random.Range(100000, 999999);
                Debug.Log($"[ThermoSensorPlus] OnSpawn: Assigned new random ID {randomID} to {gameObject.name}");
            }
            else
            {
                Debug.Log($"[ThermoSensorPlus] OnSpawn: Restored existing ID {randomID} for {gameObject.name}");
            }
        }
    }

    [HarmonyPatch(typeof(LogicTemperatureSensorConfig), "DoPostConfigureComplete")]
    public static class ThermoSensorPatchNew
    {
        public static void Postfix(GameObject go)
        {
            Debug.Log("[ThermoSensorPlus] DoPostConfigureComplete PATCH RAN for: " + go.name);
            go.AddOrGet<ThermoSensorStateComponent>();
        }
    }

    [HarmonyPatch(typeof(BuildingComplete), "OnSpawn")]
    public static class ThermoSensorPatchExisting
    {
        public static void Postfix(BuildingComplete __instance)
        {
            var go = __instance.gameObject;
            if (go.GetComponent<LogicTemperatureSensor>() != null &&
                go.GetComponent<ThermoSensorStateComponent>() == null)
            {
                go.AddOrGet<ThermoSensorStateComponent>();
                Debug.Log($"[ThermoSensorPlus] OnSpawn: Attached missing state to legacy sensor: {go.name}");
            }
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
        private PTextField textField;
        private TMP_InputField inputField;
        private ThermoSensorStateComponent currentState;

        private void EnsureUIBuilt()
        {
            if (root != null)
                return;

            Debug.Log("[ThermoSensorPlus] Building side screen UI...");

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
            _ = idLabel.Build();

            textField = new PTextField("CustomTextField")
            {
                MinWidth = 200,
                Text = "",
                OnTextChanged = (go, value) =>
                {
                    if (currentState != null)
                    {
                        currentState.customText = value;
                        Debug.Log($"[ThermoSensorPlus] Saved custom text: {value}");
                    }
                }
            };
            textField.OnRealize += go =>
            {
                inputField = go.GetComponent<TMP_InputField>();
                Debug.Log($"[ThermoSensorPlus] OnRealize: inputField assigned? {inputField != null}");
            };
            panel.AddChild(textField);
            _ = textField.Build();

            root = panel.AddTo(gameObject, 0);
            ContentContainer = root;

            Debug.Log("[ThermoSensorPlus] Side screen UI initialized.");
        }

        public override void SetTarget(GameObject target)
        {
            EnsureUIBuilt();

            currentState = target?.GetComponent<ThermoSensorStateComponent>();

            if (idLocText != null && currentState != null)
                idLocText.text = $"[TS+] Sensor ID: {currentState.randomID}";

            if (inputField != null && currentState != null)
                inputField.text = currentState.customText ?? "";
        }

        public override void ClearTarget() { }
        public override string GetTitle() => "ThermoSensor+";
        public override float GetSortKey() => -100f;

        public override bool IsValidForTarget(GameObject target)
        {
            return target != null && target.GetComponent<ThermoSensorStateComponent>() != null;
        }

        protected override void OnPrefabInit()
        {
            EnsureUIBuilt(); // Optional: prewarm
        }
    }
}