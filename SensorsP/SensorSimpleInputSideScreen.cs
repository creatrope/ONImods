using PeterHan.PLib.UI;
using TMPro;
using UnityEngine;

public class SensorSimpleInputSideScreen : SideScreenContent
{
    private PTextField inputField;
    private TMP_InputField tmpInputField;

    public override void SetTarget(GameObject target)
    {
        CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] SetTarget called. Target: " + (target != null ? target.name : "null"));
    }

    public override bool IsValidForTarget(GameObject target)
    {
        bool valid = target != null;
        CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] IsValidForTarget called. Target: " + (target != null ? target.name : "null") + " => " + valid);
        return valid;
    }

    public override string GetTitle() => "Sensor Simple Input";

    protected override void OnPrefabInit()
    {
        CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] OnPrefabInit called.");
        base.OnPrefabInit();

        // Create a vertical panel
        var panel = new PPanel("Vertical")
        {
            Direction = PanelDirection.Vertical,
            Spacing = 10
        };
        CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] Created PPanel.");

        // Add a simple label before the text field
        var label = new PLabel("TestLabel")
        {
            Text = "This is a test label.",
            ToolTip = "If you see this, the side screen is being built.",
            TextStyle = PUITuning.Fonts.TextDarkStyle
        };
        panel.AddChild(label);
        CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] Added PLabel to panel.");

        // Add a PLib text field, set default to 1.0, and capture the TMP_InputField on realize
        inputField = new PTextField();
        inputField.Text = "1.0";
        inputField.AddOnRealize(go =>
        {
            CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] PTextField realized. GameObject: " + go.name);
            tmpInputField = go.GetComponent<TMP_InputField>();
            if (tmpInputField != null)
            {
                CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] TMP_InputField found.");
                if (tmpInputField.placeholder is TMP_Text placeholder)
                {
                    placeholder.text = "Enter value...";
                    CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] Placeholder set.");
                }
                else
                {
                    CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] TMP_InputField placeholder is not TMP_Text.");
                }
            }
            else
            {
                CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] TMP_InputField NOT found.");
            }
        });

        panel.AddChild(inputField);
        CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] Added PTextField to panel.");

        // Add the panel to the side screen
        var root = panel.AddTo(gameObject, 0);
        ContentContainer = root;
        CustomLogger.CustomLogger.Log("[SensorSimpleInputSideScreen] Added panel to side screen.");
    }
}