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
    public class ThermoSensorStateComponent : KMonoBehaviour
    {
        [Serialize] public int randomID;
        [Serialize] public Dictionary<string, string> customFields = new Dictionary<string, string>();

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
    }

    public class MyThresholdSwitch
    {
        private GameObject root;
        private PLabel label;
        private LocText labelText;
        private PButton buttonA;
        private PButton buttonB;
        private PTextField textField;
        private TMP_InputField inputField;

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
            // Outer horizontal panel: [Label][RightPanel]
            var container = new PPanel("Field_" + fieldId)
            {
                Direction = PanelDirection.Horizontal,
                Spacing = 5,
                Margin = new RectOffset(0, 0, 5, 5),
                FlexSize = new Vector2(1f, 0f)
            };

            // Left label (takes all available space)
            label = new PLabel("Label_" + fieldId)
            {
                Text = labelTextValue,
                TextStyle = PUITuning.Fonts.TextDarkStyle,
                FlexSize = new Vector2(1f, 0f) // Take all available space
            }.AddOnRealize(go =>
            {
                this.labelText = go.transform.Find("Text")?.GetComponent<LocText>();
                CustomLogger.Log($"MyThresholdSwitch.OnRealize: labelText assigned? {this.labelText != null}");
            });
            container.AddChild(label);

            // Right panel for buttons and input field
            var rightPanel = new PPanel("RightPanel_" + fieldId)
            {
                Direction = PanelDirection.Horizontal,
                Spacing = 2
                // No flex, so it sizes to content and stays right
            };

            // Button "Above"
            buttonA = new PButton("ButtonA_" + fieldId)
            {
                Text = "Above",
                ToolTip = "Above",
                FlexSize = new Vector2(0, 0),
                OnClick = (go) =>
                {
                    PButton.SetButtonEnabled(go, false);
                    if (buttonBGameObject != null)
                        PButton.SetButtonEnabled(buttonBGameObject, true);

                    if (stateComponent != null)
                    {
                        stateComponent.customFields[fieldId + ToggleSuffix] = "A";
                        CustomLogger.Log($"Saved toggle {fieldId}: A");
                    }
                }
            };
            buttonA.OnRealize += go =>
            {
                go.SetMinUISize(new Vector2(24, 24));
                buttonAGameObject = go;
            };
            rightPanel.AddChild(buttonA);

            // Button "Below"
            buttonB = new PButton("ButtonB_" + fieldId)
            {
                Text = "Below",
                ToolTip = "Below",
                FlexSize = new Vector2(0, 0),
                OnClick = (go) =>
                {
                    PButton.SetButtonEnabled(go, false);
                    if (buttonAGameObject != null)
                        PButton.SetButtonEnabled(buttonAGameObject, true);

                    if (stateComponent != null)
                    {
                        stateComponent.customFields[fieldId + ToggleSuffix] = "B";
                        CustomLogger.Log($"Saved toggle {fieldId}: B");
                    }
                }
            };
            buttonB.OnRealize += go =>
            {
                go.SetMinUISize(new Vector2(24, 24));
                buttonBGameObject = go;
            };
            rightPanel.AddChild(buttonB);

            // Input field
            textField = new PTextField("Input_" + fieldId)
            {
                MinWidth = 48, // ~4 characters wide
                Text = defaultInputText,
                OnTextChanged = (go, value) =>
                {
                    if (stateComponent != null)
                    {
                        stateComponent.customFields[fieldId] = value;
                        CustomLogger.Log($"Saved custom field {fieldId}: {value}");
                    }
                }
            }.AddOnRealize(go =>
            {
                inputField = go.GetComponent<TMP_InputField>();
                CustomLogger.Log($"MyThresholdSwitch.OnRealize: inputField assigned? {inputField != null}");
            });
            rightPanel.AddChild(textField);

            container.AddChild(rightPanel);

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

                CustomLogger.Log($"Restoring toggle {fieldId}: {toggleValue}");

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
        private List<MyThresholdSwitch> fields = new List<MyThresholdSwitch>();
        private ThermoSensorStateComponent currentState;

        private void EnsureUIBuilt()
        {
            if (root != null)
                return;

            CustomLogger.Log("Building side screen UI...");

            var panel = new PPanel("ClickPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10,
                BackColor = new Color(0, 0, 0, 0),
                Margin = new RectOffset(10, 10, 10, 10)
            };

            root = panel.AddTo(gameObject, 0);
            ContentContainer = root;

            var threshold1 = new MyThresholdSwitch("threshold1", "Velocity", "1.0");
            fields.Add(threshold1);
            threshold1.Build(root);

            var threshold2 = new MyThresholdSwitch("threshold2", "Acceleration", "1.0");
            fields.Add(threshold2);
            threshold2.Build(root);

            CustomLogger.Log("Side screen UI initialized.");
        }

        public override void SetTarget(GameObject target)
        {
            EnsureUIBuilt();

            currentState = target?.GetComponent<ThermoSensorStateComponent>();

            foreach (var field in fields)
                field.SetTarget(currentState);
        }

        public override void ClearTarget() { }
        public override string GetTitle() => "ThermoSensor+";

        public override bool IsValidForTarget(GameObject target)
        {
            bool valid = target != null && target.GetComponent<ThermoSensorStateComponent>() != null;
            CustomLogger.Log($"IsValidForTarget: {target?.name}, valid={valid}");
            return valid;
        }

        public override int GetSideScreenSortOrder() => -300;

        protected override void OnPrefabInit()
        {
            EnsureUIBuilt();
        }
    }
}