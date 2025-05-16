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
                CustomLogger.Log($"[{gameObject.name}] dT: {FirstDerivative:0.###}, d²T: {SecondDerivative:0.###}");
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
            currentState = target?.GetComponent<ThermoSensorStateComponent>();
            CustomLogger.Log($"SetTarget called for: {currentState?.gameObject.name ?? "null"}");

            foreach (var field in fields)
            {
                field.SetTarget(currentState);
                field.UpdateOutput(); // Ensure output is refreshed with latest smoothed values
            }
        }

        public override void ClearTarget() { }
        public override string GetTitle() => "ThermoSensor+";

        public override void ScreenUpdate(bool topLevel)
        {
            base.ScreenUpdate(topLevel);
            foreach (var field in fields)
                field.UpdateOutput();
        }

        protected override void OnPrefabInit()
        {
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

            CustomLogger.Log("Side screen UI initialized.");
        }
    }

    public class MyThresholdSwitch
    {
        private GameObject root;
        private PLabel label;
        private PTextField textField;
        private TMP_InputField inputField;
        private PLabel outputField;
        private LocText outputLocText;

        private GameObject buttonAGameObject;
        private GameObject buttonBGameObject;

        private string fieldId;
        private string labelTextValue;
        private string defaultInputText;
        private ThermoSensorStateComponent stateComponent;

        private const string ToggleSuffix = "_toggle";
        private const string DefaultToggle = "A"; // "Above" is default

        public MyThresholdSwitch(string id, string labelText, string defaultInputText = "1.0")
        {
            this.fieldId = id;
            this.labelTextValue = labelText;
            this.defaultInputText = defaultInputText;
        }

        public GameObject Build(GameObject parent)
        {
            // Outer horizontal panel: [Label][Buttons][Input][Output]
            var container = new PPanel("Field_" + fieldId)
            {
                Direction = PanelDirection.Horizontal,
                Spacing = 5,
                Margin = new RectOffset(0, 0, 5, 5),
                FlexSize = new Vector2(1f, 0f)
            };

            // Label
            label = new PLabel("Label_" + fieldId)
            {
                Text = labelTextValue,
                TextStyle = PUITuning.Fonts.TextDarkStyle,
                FlexSize = new Vector2(0, 0)
            };
            container.AddChild(label);

            // Button "Above" (A)
            var buttonA = new PButton("ButtonA_" + fieldId)
            {
                Text = "A",
                ToolTip = "Above",
                FlexSize = new Vector2(0, 0),
                OnClick = (go) =>
                {
                    PButton.SetButtonEnabled(go, false);
                    if (buttonBGameObject != null)
                        PButton.SetButtonEnabled(buttonBGameObject, true);

                    if (stateComponent != null)
                        stateComponent.customFields[fieldId + ToggleSuffix] = "A";
                }
            };
            buttonA.OnRealize += go => buttonAGameObject = go;
            container.AddChild(buttonA);

            // Button "Below" (B)
            var buttonB = new PButton("ButtonB_" + fieldId)
            {
                Text = "B",
                ToolTip = "Below",
                FlexSize = new Vector2(0, 0),
                OnClick = (go) =>
                {
                    PButton.SetButtonEnabled(go, false);
                    if (buttonAGameObject != null)
                        PButton.SetButtonEnabled(buttonAGameObject, true);

                    if (stateComponent != null)
                        stateComponent.customFields[fieldId + ToggleSuffix] = "B";
                }
            };
            buttonB.OnRealize += go => buttonBGameObject = go;
            container.AddChild(buttonB);

            // Input field
            textField = new PTextField("Input_" + fieldId)
            {
                MinWidth = 48,
                Text = defaultInputText,
                OnTextChanged = (go, value) =>
                {
                    if (stateComponent != null)
                        stateComponent.customFields[fieldId] = value;
                }
            }.AddOnRealize(go =>
            {
                inputField = go.GetComponent<TMP_InputField>();
            });
            container.AddChild(textField);

            // Output field (to the right of input)
            outputField = new PLabel("OutputField_" + fieldId)
            {
                Text = "00000.00",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            }.AddOnRealize(go =>
            {
                outputLocText = go.transform.Find("Text")?.GetComponent<LocText>();
            });
            container.AddChild(outputField);

            root = container.AddTo(parent);
            return root;
        }

        public void SetTarget(ThermoSensorStateComponent target)
        {
            stateComponent = target;

            // Restore text field
            if (stateComponent != null && inputField != null)
            {
                if (stateComponent.customFields.TryGetValue(fieldId, out string savedValue))
                    inputField.text = savedValue;
                else
                    inputField.text = defaultInputText;
            }

            // Restore toggle state
            if (stateComponent != null && buttonAGameObject != null && buttonBGameObject != null)
            {
                string toggleKey = fieldId + ToggleSuffix;
                string toggleValue = DefaultToggle;
                if (stateComponent.customFields.TryGetValue(toggleKey, out string savedToggle))
                    toggleValue = savedToggle;

                if (toggleValue == "A")
                {
                    PButton.SetButtonEnabled(buttonAGameObject, false);
                    PButton.SetButtonEnabled(buttonBGameObject, true);
                }
                else
                {
                    PButton.SetButtonEnabled(buttonAGameObject, true);
                    PButton.SetButtonEnabled(buttonBGameObject, false);
                }
            }
            UpdateOutput();
        }

        public void UpdateOutput()
        {
            if (outputLocText != null && stateComponent != null)
            {
                float val = 0f;
                if (fieldId == "threshold1")
                    val = stateComponent.SmoothedFirst;
                else if (fieldId == "threshold2")
                    val = stateComponent.SmoothedSecond;
                else
                    val = stateComponent.LastValue;

                outputLocText.text = val.ToString("00000.00");
            }
        }
    }
}
