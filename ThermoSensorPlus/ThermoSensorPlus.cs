using HarmonyLib;
using KMod;
using PeterHan.PLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using KSerialization;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace ThermoSensorPlus
{
    public static class CustomLogger
    {
        private const string PREFIX = "[ThermoSensorPlus] ";
        public static void Log(string message) => Debug.Log(PREFIX + message);
    }

    public static class ThermoSensorGlobals
    {
        public static float deltaT = 10f;
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Harmony.DEBUG = true;
            PUtil.InitLibrary();
            CustomLogger.Log("Mod loaded. Applying Harmony patches.");
            harmony.PatchAll();
        }
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public class ThermoSensorStateComponent : KMonoBehaviour, ISim1000ms
    {
        [Serialize] public int randomID;
        [Serialize] public Dictionary<string, string> customFields = new Dictionary<string, string>();
        [Serialize] public Dictionary<string, bool> buttonStates = new Dictionary<string, bool>();

        private float? lastValue = null;
        private float? lastFirstDerivative = null;

        public float LastValue => lastValue ?? 0f;
        public float FirstDerivative { get; private set; }
        public float SecondDerivative { get; private set; }
        public float SmoothedFirst { get; private set; }
        public float SmoothedSecond { get; private set; }

        private const float SmoothingAlpha = 0.2f;

        public void UpdateDerivatives(float currentValue, float deltaT)
        {
            float first = 0f;
            float second = 0f;

            if (lastValue.HasValue)
            {
                first = (currentValue - lastValue.Value) / deltaT;
                if (lastFirstDerivative.HasValue)
                    second = (first - lastFirstDerivative.Value) / deltaT;
            }

            FirstDerivative = first;
            SecondDerivative = second;

            if (lastValue.HasValue)
                SmoothedFirst = SmoothingAlpha * first + (1 - SmoothingAlpha) * SmoothedFirst;
            else
                SmoothedFirst = first;

            if (lastFirstDerivative.HasValue)
                SmoothedSecond = SmoothingAlpha * second + (1 - SmoothingAlpha) * SmoothedSecond;
            else
                SmoothedSecond = second;

            lastValue = currentValue;
            lastFirstDerivative = first;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            CustomLogger.Log($"OnSpawn: randomID={randomID}, customFields.Count={customFields?.Count ?? 0}");

            if (randomID == 0)
            {
                randomID = UnityEngine.Random.Range(100000, 999999);
                CustomLogger.Log($"OnSpawn: Assigned new random ID {randomID} to {gameObject.name}");
            }
            else
            {
                CustomLogger.Log($"OnSpawn: Restored existing ID {randomID} for {gameObject.name}");
            }
        }

        public void Sim1000ms(float dt)
        {
            if (TryGetComponent<LogicTemperatureSensor>(out var sensor))
            {
                float currentValue = sensor.CurrentValue;
                UpdateDerivatives(currentValue, ThermoSensorGlobals.deltaT);
                //CustomLogger.Log($"[{gameObject.name}] dT: {FirstDerivative:0.###}, d²T: {SecondDerivative:0.###}");
            }
        }
    }

    [HarmonyPatch(typeof(LogicTemperatureSensorConfig), "DoPostConfigureComplete")]
    public static class ThermoSensorPatchNew
    {
        public static void Postfix(GameObject go)
        {
            CustomLogger.Log("DoPostConfigureComplete PATCH RAN for: " + go.name);
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
                CustomLogger.Log($"OnSpawn: Attached missing state to legacy sensor: {go.name}");
            }
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class ThermoSensorSideScreenRegister
    {
        public static void Postfix()
        {
            CustomLogger.Log("Registering simple label side screen");
            PUIUtils.AddSideScreenContent<ThermoSensorClickMeScreen>();
        }
    }

    public class ThermoSensorClickMeScreen : SideScreenContent
    {
        private bool isSideScreenInitialized = false;

        private void Update()
        {
            if (!gameObject.activeInHierarchy || currentState == null)
                return;

            foreach (var field in fields)
                field.UpdateOutput();
        }

        private GameObject root;
        private ThermoSensorStateComponent currentState;
        private List<MyThresholdSwitch> fields = new List<MyThresholdSwitch>();

        public override bool IsValidForTarget(GameObject target)
        {
            bool valid = target != null && target.GetComponent<ThermoSensorStateComponent>() != null;
            CustomLogger.Log($"IsValidForTarget: {target?.name}, valid={valid}");
            return valid;
        }

        public override void SetTarget(GameObject target)
        {
            if (!isSideScreenInitialized)
            {
                // UI not built yet, so build it now
                OnPrefabInit();
            }

            currentState = target?.GetComponent<ThermoSensorStateComponent>();
            CustomLogger.Log($"SetTarget called for: {currentState?.gameObject.name ?? "null"}");

            foreach (var field in fields)
                field.SetTarget(currentState);
        }

        public override void ClearTarget() { }
        public override string GetTitle() => "ThermoSensor+";

        protected override void OnPrefabInit()
        {
            if (isSideScreenInitialized)
                return; // Prevent double-building

            var panel = new PPanel("ClickPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10,
                BackColor = new Color(1f, 0.9f, 0.9f, 1f),
                Margin = new RectOffset(10, 10, 10, 10)
            };

            root = panel.AddTo(gameObject, 0);
            ContentContainer = root;

            var threshold1 = new MyThresholdSwitch("threshold1", "Vel.", "1.0");
            fields.Add(threshold1);
            threshold1.Build(root);

            var threshold2 = new MyThresholdSwitch("threshold2", "Acc.", "1.0");
            fields.Add(threshold2);
            threshold2.Build(root);

            isSideScreenInitialized = true;

            CustomLogger.Log("Side screen UI initialized.");
        }
    }

    public class MyThresholdSwitch
    {
        private readonly string fieldId;
        private readonly string labelText;
        private readonly string defaultValue;

        private PTextField inputField;
        private TMP_InputField unityInputField; // <-- Add this field
        private PLabel outputField;
        private LocText outputLocText;
        private ThermoSensorStateComponent stateComponent;

        private GameObject parentForBuild = null;
        private bool isSideScreenInitialized = false;

        public MyThresholdSwitch(string id, string label, string defaultValue = "1.0")
        {
            this.fieldId = id;
            this.labelText = label;
            this.defaultValue = defaultValue;
        }

        public void SetParentForBuild(GameObject parent)
        {
            parentForBuild = parent;
        }

        public GameObject Build(GameObject parent)
        {
            var row = new PPanel("RowPanel_" + fieldId)
            {
                Direction = PanelDirection.Horizontal,
                Spacing = 5
            };

            row.AddChild(new PLabel("Label_" + fieldId)
            {
                Text = labelText,
                TextStyle = PUITuning.Fonts.TextDarkStyle
            });

            inputField = new PTextField("InputField_" + fieldId)
            {
                Text = defaultValue,
                MinWidth = 60,
                OnTextChanged = (source, val) => {
                    if (stateComponent != null)
                        stateComponent.customFields[fieldId] = val;
                }
            }
            .AddOnRealize(realizedGo => {
                // Find and keep the TMP_InputField for direct updates
                unityInputField = realizedGo.GetComponentInChildren<TMP_InputField>();
            });
            row.AddChild(inputField);

            outputField = new PLabel("OutputField_" + fieldId)
            {
                Text = "00000.00",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            }.AddOnRealize(realizedGo => {
                outputLocText = realizedGo.transform.Find("Text")?.GetComponent<LocText>();
            });
            row.AddChild(outputField);

            var go = row.AddTo(parent);

            return go;
        }

        public void SetTarget(ThermoSensorStateComponent state)
        {
            stateComponent = state;

            // Debug: print the value about to be restored
            string val = defaultValue;
            if (stateComponent != null && stateComponent.customFields.TryGetValue(fieldId, out string savedVal))
                val = savedVal;
            CustomLogger.Log($"[MyThresholdSwitch:{fieldId}] Restoring inputField.Text='{val}' for sensor id={stateComponent?.randomID}");

            // Update both the PTextField and the TMP_InputField directly
            if (inputField != null)
                inputField.Text = val;
            if (unityInputField != null && unityInputField.text != val)
                unityInputField.text = val;

            UpdateOutput();
        }

        public void UpdateOutput()
        {
            float val = 0f;
            if (stateComponent != null)
            {
                if (fieldId == "threshold1")
                    val = stateComponent.SmoothedFirst;
                else if (fieldId == "threshold2")
                    val = stateComponent.SmoothedSecond;
                else
                    val = stateComponent.LastValue;
            }

            if (outputLocText != null)
                outputLocText.text = val.ToString("00000.00");
        }
    }
}
