using PeterHan.PLib.UI;
using UnityEngine;
using TMPro;
using System.Runtime.CompilerServices;
using HLib;
using SensorsPlus;

namespace SensorsPlus
{
    public class SensorSimpleInputSideScreen : SideScreenContent
    {
        private PTextField inputField;
        private TMP_InputField unityInputField;
        private TMP_Text derivativeText;
        private SensorInputValueComponent state;
        private LogicPressureSensor pressureSensor;
        private LogicTemperatureSensor temperatureSensor;

        // Add a guard to prevent duplicate UI creation
        private bool uiInitialized = false;

        public override bool IsValidForTarget(GameObject target)
        {
            // Add debug logging to see what is being checked
            bool hasPressure = target != null && target.GetComponent<LogicPressureSensor>() != null;
            bool hasTemperature = target != null && target.GetComponent<LogicTemperatureSensor>() != null;
            bool valid = hasPressure || hasTemperature;
            //HLib.CustomLogger.Log($"[SideScreen] IsValidForTarget: target={target?.name}, hasPressure={hasPressure}, hasTemperature={hasTemperature}, valid={valid}");
            return valid;
        }

        public override void SetTarget(GameObject target)
        {
            // Only update references and values, do not create UI here
            state = target?.GetComponent<SensorInputValueComponent>();
            pressureSensor = target?.GetComponent<LogicPressureSensor>();
            temperatureSensor = target?.GetComponent<LogicTemperatureSensor>();

            string value = state?.inputValue ?? "1.0";
            if (unityInputField != null)
                unityInputField.text = value;
            else if (inputField != null)
                inputField.Text = value;

            UpdateDerivativeLabel();
        }

        public override string GetTitle() => "Sensor Simple Input";

        public override int GetSideScreenSortOrder() => -100;

        protected override void OnPrefabInit()
        {
            HLib.CustomLogger.Log("[SensorSimpleInputSideScreen] OnPrefabInit called.");
            base.OnPrefabInit();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            // Prevent duplicate UI creation
            if (uiInitialized)
                return;
            uiInitialized = true;

            GameObject container = ContentContainer != null ? ContentContainer : gameObject;

            // Create a panel for the text fields
            var textFieldsPanel = new PPanel("TextFieldsPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 5,
                Margin = new RectOffset(0, 0, 10, 10)
            };

            // Add text fields for sensor output information
            var bit0Label = new PLabel("Bit0Label")
            {
                Text = "bit 0: default sensor output (use above)",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            };

            var bit1Label = new PLabel("Bit1Label")
            {
                Text = "bit 1: bit 0 exceeds +threshold",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            };

            var bit2Label = new PLabel("Bit2Label")
            {
                Text = "bit 2: bit 0 exceeds -threshold",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            };

            textFieldsPanel.AddChild(bit0Label);
            textFieldsPanel.AddChild(bit1Label);
            textFieldsPanel.AddChild(bit2Label);

            var textFieldsGO = textFieldsPanel.Build();
            textFieldsGO.transform.SetParent(container.transform, false);
            textFieldsGO.transform.SetAsFirstSibling();

            // Create the row for threshold label, input field, and derivative label
            var row = new PPanel("ThresholdRow")
            {
                Direction = PanelDirection.Horizontal,
                Spacing = 5,
                Margin = new RectOffset(0, 0, 0, 10)
            };

            var thresholdLabel = new PLabel("ThresholdLabel")
            {
                Text = "Threshold +/-",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            };

            inputField = new PTextField()
            {
                MinWidth = 90 // Adjust as needed for your font/UI scale; 90 is a good starting point for 6 digits
            };
            inputField.OnTextChanged += (sender, text) =>
            {
                if (state != null)
                {
                    state.inputValue = text;
                    if (float.TryParse(text, out float parsed))
                        state.parsedValue = parsed;
                    else
                        state.parsedValue = 1.0f; // fallback or handle as you wish
                }
            };
            inputField.AddOnRealize(go =>
            {
                unityInputField = go.GetComponent<TMP_InputField>();
                if (unityInputField != null && state != null)
                    unityInputField.text = state.inputValue ?? "1.0";
            });

            // Derivative label (right of input)
            var derivativeLabel = new PLabel("DerivativeLabel")
            {
                Text = "0.0",
                TextStyle = PUITuning.Fonts.TextDarkStyle,
                ToolTip = "Current first derivative"
            }.AddOnRealize(go =>
            {
                var textChild = go.transform.Find("Text");
                string sensorType = pressureSensor != null ? "Pressure" : (temperatureSensor != null ? "Temperature" : "Unknown");

                if (textChild == null)
                {
                    HLib.CustomLogger.Log($"[UI] Derivative label realized for {sensorType} sensor: 'Text' child NOT FOUND.");
                    derivativeText = null;
                }
                else
                {
                    derivativeText = textChild.GetComponent<TMP_Text>();
                    if (derivativeText != null)
                    {
                        HLib.CustomLogger.Log($"[UI] Derivative label realized for {sensorType} sensor: TMP_Text assigned.");
                        derivativeText.alignment = TMPro.TextAlignmentOptions.Left;
                    }
                    else
                    {
                        HLib.CustomLogger.Log($"[UI] Derivative label realized for {sensorType} sensor: 'Text' child found, but TMP_Text not present.");
                        derivativeText = null;
                    }
                }
                UpdateDerivativeLabel();
            });

            row.AddChild(thresholdLabel);
            row.AddChild(inputField);
            row.AddChild(derivativeLabel);

            var rowGO = row.Build();
            rowGO.transform.SetParent(container.transform, false);
            rowGO.transform.SetAsLastSibling();

            HLib.CustomLogger.Log("[SensorSimpleInputSideScreen] Added text fields, threshold label, input field, derivative label, and output field below default UI (OnSpawn).");
        }

        private void UpdateDerivativeLabel()
        {
            float firstDerivative = 0.0f;

            if (pressureSensor != null)
            {
                if (SensorsPlus.LogicPressureSensor_Sim200ms_Patch.DerivativeStates.TryGetValue(pressureSensor, out var derivativeState))
                {
                    // Show the moving average of the first derivative
                    firstDerivative = derivativeState.ComputeMovingAverageFirstDerivative(3);
                }
            }
            else if (temperatureSensor != null)
            {
                if (SensorsPlus.LogicTemperatureSensor_Sim200ms_Patch.DerivativeStates.TryGetValue(temperatureSensor, out var derivativeState))
                {
                    // Show the moving average of the first derivative
                    firstDerivative = derivativeState.ComputeMovingAverageFirstDerivative(3);
                }
            }

            if (derivativeText == null)
            {
                string sensorType = pressureSensor != null ? "Pressure" : (temperatureSensor != null ? "Temperature" : "Unknown");
                HLib.CustomLogger.Log($"[UI] UpdateDerivativeLabel: derivativeText is null for {sensorType} sensor!");
                return;
            }

            if (!(derivativeText is LocText locText))
            {
                string sensorType = pressureSensor != null ? "Pressure" : (temperatureSensor != null ? "Temperature" : "Unknown");
                HLib.CustomLogger.Log($"[UI] UpdateDerivativeLabel: derivativeText is not LocText (actual type: {derivativeText.GetType().Name}) for {sensorType} sensor!");
                return;
            }

            locText.text = firstDerivative.ToString("0.###");
        }

        // Optionally, update the derivative label every frame if it can change live
        private void Update()
        {
            UpdateDerivativeLabel();
        }
    }
}