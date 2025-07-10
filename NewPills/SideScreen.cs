using HLib;
using NewPills;
using PeterHan.PLib.UI;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

namespace NewPills
{
    public class SimpleSideScreen : SideScreenContent
    {
        // Add a guard to prevent duplicate UI creation
        private bool uiInitialized = false;

        // Declare the panel variable
        private PPanel panel;

        public override bool IsValidForTarget(GameObject target)
        {
            return true;
        }

        public override void SetTarget(GameObject target)
        {
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
            var panelgo = panel.Build();
            panelgo.transform.SetParent(container.transform, false);
            panelgo.transform.SetAsFirstSibling();

            //Patches.Logger.Log("[SensorSimpleInputSideScreen] Added text fields, threshold label, input field, derivative label, and output field below default UI (OnSpawn).");
        }

        private void Update()
        {
        }
    }
}