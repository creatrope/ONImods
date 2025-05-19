using System.Collections.Generic;
using UnityEngine;
using PeterHan.PLib.UI;
using TMPro;

namespace SensorsPlus
{
    public abstract class ThresholdSensorSideScreen<TState> : SideScreenContent where TState : ThresholdSwitchStateComponentBase
    {
        protected bool isSideScreenInitialized = false;
        protected GameObject root;
        protected TState currentState;
        protected List<MyThresholdSwitch> fields = new List<MyThresholdSwitch>();
        protected LocText sensorIdLocText;

        protected abstract string Title { get; }
        protected abstract Color PanelColor { get; }

        private void Update()
        {
            if (!gameObject.activeInHierarchy || currentState == null)
                return;
            foreach (var field in fields)
                field.UpdateOutput();
        }

        public override bool IsValidForTarget(GameObject target)
        {
            return target != null && target.GetComponent<TState>() != null;
        }

        public override void SetTarget(GameObject target)
        {
            if (!isSideScreenInitialized)
                OnPrefabInit();

            currentState = target?.GetComponent<TState>();
            foreach (var field in fields)
            {
                field.SetTarget(currentState);
                currentState?.RegisterSwitch(field);
            }
        }

        public override void ClearTarget() { }
        public override string GetTitle() => Title;
        public override int GetSideScreenSortOrder() => -100;

        protected override void OnPrefabInit()
        {
            if (isSideScreenInitialized)
                return;

            var panel = new PPanel("ClickPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10,
                BackColor = PanelColor,
                Margin = new RectOffset(10, 10, 10, 10)
            };

            root = panel.AddTo(gameObject, 0);
            ContentContainer = root;

            var sensorIdLabel = new PLabel("SensorIdLabel")
            {
                Text = "Sensor ID: N/A",
                TextStyle = PUITuning.Fonts.TextDarkStyle,
                ToolTip = "Unique sensor identifier"
            };
            sensorIdLocText = sensorIdLabel.AddTo(root).GetComponent<LocText>();

            var threshold1 = new MyThresholdSwitch("threshold1", "Vel.", "1.0", 1);
            fields.Add(threshold1);
            threshold1.BuildUIRow(root);

            var threshold2 = new MyThresholdSwitch("threshold2", "Acc.", "1.0", 2);
            fields.Add(threshold2);
            threshold2.BuildUIRow(root);

            isSideScreenInitialized = true;
        }
    }
}