using PeterHan.PLib.UI;
using UnityEngine;
using TMPro;
using System.Runtime.CompilerServices;

namespace SensorsP
{ 
    public class SensorSimpleInputSideScreen : SideScreenContent
    {
        private PTextField inputField;
        private TMP_InputField unityInputField;
        private LocText derivativeText;
        private SensorInputValueComponent state;
        private LogicPressureSensor sensor;

        public override bool IsValidForTarget(GameObject target)
        {
            bool valid = target != null && target.GetComponent<LogicPressureSensor>() != null;
            CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] IsValidForTarget called. Target: " + (target != null ? target.name : "null") + " => " + valid);

            return valid;
        }

        public override void SetTarget(GameObject target)
        {
                        CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] SetTarget called. Target: " + (target != null ? target.name : "null"));

            state = target?.GetComponent<SensorInputValueComponent>();
            sensor = target?.GetComponent<LogicPressureSensor>();
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
            CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] OnPrefabInit called.");
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
                {
                    state.inputValue = text;
                    if (!float.TryParse(text, out state.parsedValue))
                        state.parsedValue = 1.0f; // fallback or previous value
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
        }

        private void UpdateDerivativeLabel()
        {
            if (derivativeText != null && sensor != null)
            {
                float firstDerivative = 0.0f;
                if (SensorsP.LogicPressureSensor_Sim200ms_Patch.DerivativeStates.TryGetValue(sensor, out var derivativeState))
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
                    // Only show the numeric value, left-justified
                    if (derivativeText is LocText locText)
                        locText.text = firstDerivative.ToString("0.###");
                    else
                        derivativeText.text = firstDerivative.ToString("0.###");
            }
            else if (derivativeText != null)
            {
                if (derivativeText is LocText locText)
                    locText.text = "0.0";
                else
                    derivativeText.text = "0.0";
            }
        }

        // Optionally, update the derivative label every frame if it can change live
        private void Update()
        {
            UpdateDerivativeLabel();
        }
    }
}