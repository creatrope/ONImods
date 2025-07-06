using PeterHan.PLib.UI;
using UnityEngine;
using TMPro;
using System.Runtime.CompilerServices;
using HLib;
using OverheatControl;

namespace OverheatControl
{
    public class SimpleSideScreen : SideScreenContent
    {
        // Add a guard to prevent duplicate UI creation
        private bool uiInitialized = false;

        // Declare the panel variable
        private PPanel panel;

        // Declare the text field for displaying instance ID
        private TMP_Text idLocText;

        public override bool IsValidForTarget(GameObject target)
        {
            return true;
        }

        public override void SetTarget(GameObject target)
        {
            var building = target?.GetComponent<Building>();
            if (building != null)
            {
                int instanceID = building.GetInstanceID();
                Patches.Logger.Log($"[SideScreen] Displaying Instance ID: {instanceID} for {building.name}");

                var kSelectable = building.gameObject.GetComponent<KSelectable>();
                string name = kSelectable != null ? kSelectable.GetProperName() : building.Def.Name;

                // Update the UI with the instance ID
                if (idLocText != null)
                {
                    idLocText.text = $"Instance ID: {instanceID}";
                    Patches.Logger.Log($"[SideScreen] idLocText updated with Instance ID: {idLocText.text}");
                }
            }
            else
            {
                Patches.Logger.Log("[SideScreen] Target is not a valid building.");
            }
        }

        public override string GetTitle() => "SideScreen";

        public override int GetSideScreenSortOrder() => -100;

        protected override void OnPrefabInit()
        {
            Patches.Logger.Log("[SimpleSideScreen] OnPrefabInit called.");
            base.OnPrefabInit();

            // Initialize the panel
            panel = new PPanel("Panel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10
            };
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            // Prevent duplicate UI creation
            if (uiInitialized)
                return;
            uiInitialized = true;

            GameObject container = ContentContainer != null ? ContentContainer : gameObject;

            // Add text fields for sensor output information
            var testlabel = new PLabel("test")
            {
                Text = "testlabel",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            };

            panel.AddChild(testlabel);

            // Add a text field for displaying instance ID
            var idLabel = new PLabel("InstanceIDLabel")
            {
                Text = "Building: N/A, Instance ID: N/A",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            };

            // Correctly initialize idLocText using TMP_Text directly
            idLocText = idLabel.Build().GetComponentInChildren<TMP_Text>();
            panel.AddChild(idLabel);

            var panelgo = panel.Build();
            panelgo.transform.SetParent(container.transform, false);
            panelgo.transform.SetAsFirstSibling();
        }

        private void Update()
        {
        }
    }
}