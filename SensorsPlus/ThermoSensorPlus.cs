using HarmonyLib;
using KMod;
using KSerialization;
using PeterHan.PLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using SensorsPlus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SystemDateTime = System.DateTime;

namespace SensorsPlus
{
    public static class ThermoSensorGlobals
    {
        public const string ModuleName = "ThermoSensorPlus";
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public partial class ThermoSensorStateComponent : ThresholdSwitchStateComponentBase, ISim1000ms
    {
        [Serialize]
        private int RandomID = 0;

        public ThermoSensorStateComponent()
        {
            // Only keep module load or error messages. Remove constructor Debug.Log.
        }

        [OnSerializing]
        private void OnSerializing()
        {
            // Debugging code removed as requested
        }

        private void OnDeserialized()
        {
            // Debugging code removed as requested
        }

        private float? lastValue = null;
        private float? lastFirstDerivative = null;

        public float LastValue => lastValue ?? 0f;
        public float FirstDerivative { get; private set; }
        public float SecondDerivative { get; private set; }
        public float SmoothedFirst { get; private set; }
        public float SmoothedSecond { get; private set; }

        private const float SmoothingAlpha = 0.2f;

        private Dictionary<int, bool> switchSignalStates = new Dictionary<int, bool>();

        public void UpdateDerivatives(float currentValue, float deltaT)
        {
            float smoothedFirst = SmoothedFirst;
            float smoothedSecond = SmoothedSecond;

            SensorHelpers.UpdateDerivatives(
                ref lastValue,
                ref lastFirstDerivative,
                ref smoothedFirst,
                ref smoothedSecond,
                out float first,
                out float second,
                currentValue,
                deltaT,
                SmoothingAlpha
            );

            FirstDerivative = first;
            SecondDerivative = second;
            SmoothedFirst = smoothedFirst;
            SmoothedSecond = smoothedSecond;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if (RandomID == 0)
                RandomID = UnityEngine.Random.Range(100000, 999999);
        }

        public void Sim1000ms(float dt)
        {
            int signal = 0;

            if (TryGetComponent<LogicTemperatureSensor>(out var sensor))
            {
                float currentValue = sensor.CurrentValue;
                UpdateDerivatives(currentValue, dt);
                if (sensor.IsSwitchedOn)
                {
                    signal |= 1 << 0;
                }
            }

            switchSignalStates.Clear();
            signal |= base.GetRegisteredSwitchSignal();

            SendRibbonSignal(signal);
        }

        protected override void SendRibbonSignal(int signal)
        {
            SensorHelpers.SendRibbonSignal(
                RegisteredSwitches,
                switchSignalStates,
                gameObject,
                ThermoSensorPatchNew.RIBBON_OUTPUT_PORT_ID,
                signal
            );
        }

        public override float GetValue(string fieldId)
        {
            switch (fieldId)
            {
                case "threshold1": return SmoothedFirst;
                case "threshold2": return SmoothedSecond;
                default: return LastValue;
            }
        }
    }

    [HarmonyPatch(typeof(LogicTemperatureSensorConfig), "DoPostConfigureComplete")]
    public static class ThermoSensorPatchNew
    {
        public static readonly HashedString RIBBON_OUTPUT_PORT_ID = new HashedString("ThermoSensorPlusRibbonOutput");

        [HarmonyPostfix]
        public static void Postfix(GameObject go)
        {
            // Ensure the component is added to the prefab at config time, not just at runtime
            if (go.GetComponent<ThermoSensorStateComponent>() == null)
                go.AddComponent<ThermoSensorStateComponent>();

            SensorsPlus.SensorHelpers.ConfigureRibbonOutputPort(
                go,
                RIBBON_OUTPUT_PORT_ID,
                new CellOffset(0, 0),
                STRINGS.BUILDINGS.PREFABS.LOGICTEMPERATURESENSOR.LOGIC_PORT,
                STRINGS.BUILDINGS.PREFABS.LOGICTEMPERATURESENSOR.LOGIC_PORT_ACTIVE,
                STRINGS.BUILDINGS.PREFABS.LOGICTEMPERATURESENSOR.LOGIC_PORT_INACTIVE,
                true
            );
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
            }
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class ThermoSensorSideScreenRegister
    {
        public static void Postfix()
        {
            PUIUtils.AddSideScreenContent<ThermoSensorClickMeScreen>();
        }
    }

    public class ThermoSensorClickMeScreen : SideScreenContent
    {
        private bool isSideScreenInitialized = false;
        private GameObject root;
        private ThermoSensorStateComponent currentState;
        private List<MyThresholdSwitch> fields = new List<MyThresholdSwitch>();
        private LocText sensorIdLocText;
        private void Update()
        {
            if (!gameObject.activeInHierarchy || currentState == null)
                return;

            foreach (var field in fields)
                field.UpdateOutput();
        }
        public override bool IsValidForTarget(GameObject target)
        {
            return target != null && target.GetComponent<ThermoSensorStateComponent>() != null;
        }

        public override void SetTarget(GameObject target)
        {
            if (!isSideScreenInitialized)
            {
                OnPrefabInit();
            }

            currentState = target?.GetComponent<ThermoSensorStateComponent>();

            foreach (var field in fields)
            {
                field.SetTarget(currentState);
                if (currentState != null)
                    currentState.RegisterSwitch(field);
            }
        }

        public override void ClearTarget() { }
        public override string GetTitle() => "ThermoSensor+";

        public override int GetSideScreenSortOrder() => -100;

        protected override void OnPrefabInit()
        {
            if (isSideScreenInitialized)
                return;

            var panel = new PPanel("ClickPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10,
                BackColor = new Color(1f, 0.9f, 0.9f, 1f),
                Margin = new RectOffset(10, 10, 10, 10)
            };

            root = panel.AddTo(gameObject, 0);
            ContentContainer = root;

            // Use PLabel to create a LocText child properly attached to the UI hierarchy
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

        private void SaveState()
        {
            if (currentState is ThermoSensorStateComponent thermoState)
            {
                SensorHelpers.SaveSwitchFieldsState(fields, thermoState);
            }
        }
    }
}