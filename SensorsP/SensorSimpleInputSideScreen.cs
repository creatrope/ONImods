using PeterHan.PLib.UI;
using UnityEngine;
using TMPro;
using System.Runtime.CompilerServices;
using HLib;

namespace SensorsP
{
    public class SensorSimpleInputSideScreen : SideScreenContent
    {
        private PTextField inputField;
        private TMP_InputField unityInputField;
        private TMP_Text derivativeText;
        private SensorInputValueComponent state;
        private LogicPressureSensor pressureSensor;
        private LogicTemperatureSensor temperatureSensor;

        public override bool IsValidForTarget(GameObject target)
        {
            // Accept both pressure and temperature sensors
            bool valid = target != null &&
                (target.GetComponent<LogicPressureSensor>() != null ||
                 target.GetComponent<LogicTemperatureSensor>() != null);
            HLib.CustomLogger.Log("[SensorSimpleInputSideScreen] IsValidForTarget called. Target: " + (target != null ? target.name : "null") + " => " + valid);

            return valid;
        }

        public override void SetTarget(GameObject target)
        {
            HLib.CustomLogger.Log("[SensorSimpleInputSideScreen] SetTarget called. Target: " + (target != null ? target.name : "null"));

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

            GameObject container = ContentContainer != null ? ContentContainer : gameObject;

            var row = new PPanel("ThresholdRow") {
                Direction = PanelDirection.Horizontal,
                Spacing = 5,
                Margin = new RectOffset(0, 0, 0, 10)
            };

            var thresholdLabel = new PLabel("ThresholdLabel") {
                Text = "Threshold +/-",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            };

            inputField = new PTextField() {
                MinWidth = 90 // Adjust as needed for your font/UI scale; 90 is a good starting point for 6 digits
            };
            inputField.OnTextChanged += (sender, text) =>
            {
                if (state != null)
                    state.inputValue = text;
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
                derivativeText = go.transform.Find("Text")?.GetComponent<LocText>();
                if (derivativeText != null)
                    derivativeText.alignment = TMPro.TextAlignmentOptions.Left; // Left-justify derivative label
                UpdateDerivativeLabel();
            });

            row.AddChild(thresholdLabel);
            row.AddChild(inputField);
            row.AddChild(derivativeLabel);

            var rowGO = row.Build();
            rowGO.transform.SetParent(container.transform, false);
            rowGO.transform.SetAsLastSibling();

            HLib.CustomLogger.Log("[SensorSimpleInputSideScreen] Added Threshold label, input field, derivative label, and output field below default UI (OnSpawn).");
        }

        private void UpdateDerivativeLabel()
        {
            float firstDerivative = 0.0f;
            if (pressureSensor != null)
            {
                if (SensorsP.LogicPressureSensor_Sim200ms_Patch.DerivativeStates.TryGetValue(pressureSensor, out var derivativeState))
                {
                    int count = derivativeState.Samples.Count;
                    if (count >= 2)
                    {
                        var samples = derivativeState.Samples.ToArray();
                        var last = samples[count - 1];
                        var prev = samples[count - 2];
                        float dt = last.time - prev.time;
                        float dv = last.value - prev.value;
                        if (dt != 0)
                            firstDerivative = dv / dt;
                    }
                }
            }
            else if (temperatureSensor != null)
            {
                if (SensorsP.LogicTemperatureSensor_Sim200ms_Patch.DerivativeStates.TryGetValue(temperatureSensor, out var derivativeState))
                {
                    int count = derivativeState.Samples.Count;
                    if (count >= 2)
                    {
                        var samples = derivativeState.Samples.ToArray();
                        var last = samples[count - 1];
                        var prev = samples[count - 2];
                        float dt = last.time - prev.time;
                        float dv = last.value - prev.value;
                        if (dt != 0)
                            firstDerivative = dv / dt;
                    }
                }
            }

            if (derivativeText != null)
            {
                if (derivativeText is LocText locText)
                    locText.text = firstDerivative.ToString("0.###");
                else
                    derivativeText.text = firstDerivative.ToString("0.###");
            }
        }

        // Optionally, update the derivative label every frame if it can change live
        private void Update()
        {
            UpdateDerivativeLabel();
        }
    }
}