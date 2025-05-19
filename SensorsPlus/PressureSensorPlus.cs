using HarmonyLib;
using KMod;
using KSerialization;
using PeterHan.PLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using STRINGS;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using SensorsPlus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SensorsPlus
{
    public static class PressureSensorGlobals
    {
        public const string ModuleName = "PressureSensorPlus";
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public partial class PressureSensorStateComponent : ThresholdSwitchStateComponentBase, ISim1000ms
    {
        [Serialize]
        private int RandomID = 0;

        private float? lastValue = null;
        private float? lastFirstDerivative = null;

        public float LastValue => lastValue ?? 0f;
        public float FirstDerivative { get; private set; }
        public float SecondDerivative { get; private set; }
        public float SmoothedFirst { get; private set; }
        public float SmoothedSecond { get; private set; }

        private const float SmoothingAlpha = 0.2f;

        private Dictionary<int, bool> switchSignalStates = new Dictionary<int, bool>();

        public PressureSensorStateComponent()
        {
            // Constructor logic only
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

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if (RandomID == 0)
                RandomID = UnityEngine.Random.Range(1, int.MaxValue);
        }

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

        public void Sim1000ms(float dt)
        {
            int signal = 0;

            // Only one LogicPressureSensor component is used for both gas and liquid sensors.
            if (TryGetComponent<LogicPressureSensor>(out var sensor))
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
            base.SendRibbonSignal(signal);
        }

        protected override HashedString RibbonPortId
        {
            get
            {
                // Use the correct port for gas or liquid sensor by checking desiredState
                if (TryGetComponent<LogicPressureSensor>(out var sensor) && sensor.desiredState == Element.State.Liquid)
                    return PressureSensorPatchNewLiquid.RIBBON_OUTPUT_PORT_ID;
                return PressureSensorPatchNew.RIBBON_OUTPUT_PORT_ID;
            }
        }
    }

    [HarmonyPatch(typeof(LogicPressureSensorGasConfig), "DoPostConfigureComplete")]
    public static class PressureSensorPatchNew
    {
        public static readonly HashedString RIBBON_OUTPUT_PORT_ID = new HashedString("PressureSensorPlusRibbonOutput");

        [HarmonyPostfix]
        public static void Postfix(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (go.GetComponent<PressureSensorStateComponent>() == null)
                go.AddComponent<PressureSensorStateComponent>();

            SensorsPlus.SensorHelpers.ConfigureRibbonOutputPort(
                go,
                RIBBON_OUTPUT_PORT_ID,
                new CellOffset(0, 0),
                STRINGS.BUILDINGS.PREFABS.LOGICPRESSURESENSORGAS.LOGIC_PORT,
                STRINGS.BUILDINGS.PREFABS.LOGICPRESSURESENSORGAS.LOGIC_PORT_ACTIVE,
                STRINGS.BUILDINGS.PREFABS.LOGICPRESSURESENSORGAS.LOGIC_PORT_INACTIVE,
                true
            );
        }
    }

    [HarmonyPatch(typeof(LogicPressureSensorLiquidConfig), "DoPostConfigureComplete")]
    public static class PressureSensorPatchNewLiquid
    {
        public static readonly HashedString RIBBON_OUTPUT_PORT_ID = new HashedString("PressureSensorPlusRibbonOutputLiquid");

        [HarmonyPostfix]
        public static void Postfix(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (go.GetComponent<PressureSensorStateComponent>() == null)
                go.AddComponent<PressureSensorStateComponent>();

            SensorsPlus.SensorHelpers.ConfigureRibbonOutputPort(
                go,
                RIBBON_OUTPUT_PORT_ID,
                new CellOffset(0, 0),
                STRINGS.BUILDINGS.PREFABS.LOGICPRESSURESENSORLIQUID.LOGIC_PORT,
                STRINGS.BUILDINGS.PREFABS.LOGICPRESSURESENSORLIQUID.LOGIC_PORT_ACTIVE,
                STRINGS.BUILDINGS.PREFABS.LOGICPRESSURESENSORLIQUID.LOGIC_PORT_INACTIVE,
                true
            );
        }
    }

    [HarmonyPatch(typeof(BuildingComplete), "OnSpawn")]
    public static class PressureSensorPatchExisting
    {
        public static void Postfix(BuildingComplete __instance)
        {
            var go = __instance.gameObject;
            if (go.GetComponent<LogicPressureSensor>() != null &&
                go.GetComponent<PressureSensorStateComponent>() == null)
            {
                go.AddOrGet<PressureSensorStateComponent>();
            }
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class PressureSensorSideScreenRegister
    {
        public static void Postfix()
        {
            PUIUtils.AddSideScreenContent<PressureSensorClickMeScreen>();
        }
    }

    public class PressureSensorClickMeScreen : SideScreenContent
    {
        private bool isSideScreenInitialized = false;
        private GameObject root;
        private PressureSensorStateComponent currentState;
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
            return target != null && target.GetComponent<PressureSensorStateComponent>() != null;
        }

        public override void SetTarget(GameObject target)
        {
            if (!isSideScreenInitialized)
            {
                OnPrefabInit();
            }

            currentState = target?.GetComponent<PressureSensorStateComponent>();

            foreach (var field in fields)
            {
                field.SetTarget(currentState);
                if (currentState != null)
                    currentState.RegisterSwitch(field);
            }
        }

        public override void ClearTarget() { }
        public override string GetTitle() => "PressureSensor+";

        public override int GetSideScreenSortOrder() => -100;

        protected override void OnPrefabInit()
        {
            if (isSideScreenInitialized)
                return;

            var panel = new PPanel("ClickPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10,
                BackColor = new Color(0.9f, 0.9f, 1f, 1f),
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

        private void SaveState()
        {
            if (currentState is PressureSensorStateComponent pressureState)
            {
                SensorHelpers.SaveSwitchFieldsState(fields, pressureState);
            }
        }
    }
}