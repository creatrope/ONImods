using PeterHan.PLib.UI;
using UnityEngine;
using TMPro;

namespace SensorsP
{ 
    public class SensorSimpleInputSideScreen : SideScreenContent
    {
        private PTextField inputField;
        private TMP_InputField unityInputField;
        private SensorInputValueComponent state;

        public override bool IsValidForTarget(GameObject target)
        {
            bool valid = target != null && target.GetComponent<LogicPressureSensor>() != null;
            CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] IsValidForTarget called. Target: " + (target != null ? target.name : "null") + " => " + valid);
            return valid;
        }

        public override void SetTarget(GameObject target)
        {
            state = target?.GetComponent<SensorInputValueComponent>();
            string value = state?.inputValue ?? "1.0";
            if (unityInputField != null)
                unityInputField.text = value;
            else if (inputField != null)
                inputField.Text = value;
            CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] SetTarget called. Target: " + (target != null ? target.name : "null"));
        }

        public override string GetTitle() => "Sensor Simple Input";

        protected override void OnPrefabInit()
        {
            CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] OnPrefabInit called.");
            base.OnPrefabInit();
        }

        public override int GetSideScreenSortOrder() => -100;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            // Use ContentContainer if available, fallback to gameObject
            GameObject container = ContentContainer != null ? ContentContainer : gameObject;

            // Create a horizontal panel for label + input
            var row = new PPanel("ThresholdRow") {
                Direction = PanelDirection.Horizontal,
                Spacing = 5,
                Margin = new RectOffset(0, 0, 0, 10) // <-- Adds 10px space below the row
            };

            var thresholdLabel = new PLabel("ThresholdLabel") {
                Text = "Threshold",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            };

            inputField = new PTextField();
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

            row.AddChild(thresholdLabel);
            row.AddChild(inputField);

            // Build the row and add it as the last child of the container
            var rowGO = row.Build();
            rowGO.transform.SetParent(container.transform, false);
            rowGO.transform.SetAsLastSibling();

            CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] Added Threshold label and input field below default UI (OnSpawn).");
        }
    }
}