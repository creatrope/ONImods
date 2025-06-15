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

            var panel = new PPanel("Vertical")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10
            };

            var label = new PLabel("TestLabel")
            {
                Text = "This is a test label.",
                ToolTip = "If you see this, the side screen is being built.",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            };
            panel.AddChild(label);

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

            panel.AddChild(inputField);

            var root = panel.AddTo(gameObject, 0);
            ContentContainer = root;
            CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] Added panel to side screen.");
        }
    }
}