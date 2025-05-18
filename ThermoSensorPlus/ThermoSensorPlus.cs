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

namespace SensorsPlus
{
    public static class CustomLogger
    {
        private const string PREFIX = "[ThermoSensorPlus] ";
        private static readonly string LogFilePath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(Application.consoleLogPath), "ThermoSensorPlus.debug.log");

        // Only writes error/debug messages to the custom log file
        public static void Log(string message)
        {
            string fullMessage = PREFIX + message;

            // Always write to custom log file
            try
            {
                System.IO.File.AppendAllText(LogFilePath, fullMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(PREFIX + "Failed to write to custom log file: " + ex.Message);
            }

            // Optionally, only write errors to Unity log
            if (message.StartsWith("ERROR") || message.Contains("null") || message.Contains("Exception"))
                Debug.LogError(fullMessage);
        }
    }

    public static class ThermoSensorGlobals
    {
        public static float deltaT = 10f;
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Harmony.DEBUG = true;
            PUtil.InitLibrary();
            CustomLogger.Log("ThermoSensorPlus loaded.");
            harmony.PatchAll();
        }
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public partial class ThermoSensorStateComponent : ThresholdSwitchStateComponentBase, ISim1000ms
    {
        [Serialize]
        private int RandomID = 0; // Add this field to resolve the error

        public ThermoSensorStateComponent()
        {
            CustomLogger.Log("ThermoSensorStateComponent CONSTRUCTOR called (fresh instance)");
        }

        [OnSerializing]
        private void OnSerializing()
        {
            CustomLogger.Log("Saving ThermoSensorStateComponent:");
            foreach (var kvp in CustomFields)
                CustomLogger.Log($"  CustomField: {kvp.Key} = {kvp.Value}");
            foreach (var kvp in ButtonStates)
                CustomLogger.Log($"  ButtonState: {kvp.Key} = {kvp.Value}");
            CustomLogger.Log($"  RandomID: {RandomID}");
        }

        private void OnDeserialized()
        {
            CustomLogger.Log("ThermoSensorStateComponent OnDeserialized CALLED (loaded from save)");
            if (CustomFields == null)
                CustomLogger.Log("ERROR: CustomFields is null after deserialization!");
            if (ButtonStates == null)
                CustomLogger.Log("ERROR: ButtonStates is null after deserialization!");

            CustomLogger.Log("OnDeserialized called for ThermoSensorStateComponent:");
            if (CustomFields != null)
            {
                foreach (var kvp in CustomFields)
                    CustomLogger.Log($"  CustomField: {kvp.Key} = {kvp.Value}");
            }
            if (ButtonStates != null)
            {
                foreach (var kvp in ButtonStates)
                    CustomLogger.Log($"  ButtonState: {kvp.Key} = {kvp.Value}");
            }
            CustomLogger.Log($"  RandomID: {RandomID}");
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

        // Use the base class properties with correct casing
        private Dictionary<string, string> customFields => CustomFields;
        private Dictionary<string, bool> buttonStates => ButtonStates;

        public void UpdateDerivatives(float currentValue, float deltaT)
        {
            float first = 0f;
            float second = 0f;

            if (lastValue.HasValue)
            {
                first = (currentValue - lastValue.Value) / deltaT;
                if (lastFirstDerivative.HasValue)
                    second = (first - lastFirstDerivative.Value) / deltaT;
            }

            FirstDerivative = first;
            SecondDerivative = second;

            if (lastValue.HasValue)
                SmoothedFirst = SmoothingAlpha * first + (1 - SmoothingAlpha) * SmoothedFirst;
            else
                SmoothedFirst = first;

            if (lastFirstDerivative.HasValue)
                SmoothedSecond = SmoothingAlpha * second + (1 - SmoothingAlpha) * SmoothedSecond;
            else
                SmoothedSecond = second;

            lastValue = currentValue;
            lastFirstDerivative = first;
        }

        public void EnsureDefaults()
        {
            if (!customFields.ContainsKey("threshold1"))
                customFields["threshold1"] = "1.0";
            if (!customFields.ContainsKey("threshold2"))
                customFields["threshold2"] = "1.0";

            foreach (var prefix in new[] { "threshold1", "threshold2" })
            {
                bool a = buttonStates.ContainsKey($"{prefix}_A") ? buttonStates[$"{prefix}_A"] : false;
                bool b = buttonStates.ContainsKey($"{prefix}_B") ? buttonStates[$"{prefix}_B"] : false;
                bool aInteract = buttonStates.ContainsKey($"{prefix}_A_interactable") ? buttonStates[$"{prefix}_A_interactable"] : true;
                bool bInteract = buttonStates.ContainsKey($"{prefix}_B_interactable") ? buttonStates[$"{prefix}_B_interactable"] : true;

                bool validA = a && !aInteract && !b && bInteract;
                bool validB = b && !bInteract && !a && aInteract;

                if (!(validA || validB))
                {
                    buttonStates[$"{prefix}_A"] = true;
                    buttonStates[$"{prefix}_A_interactable"] = false;
                    buttonStates[$"{prefix}_B"] = false;
                    buttonStates[$"{prefix}_B_interactable"] = true;
                }
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            CustomLogger.Log("ThermoSensorStateComponent OnSpawn called");

            // Debugging: check for nulls after deserialization
            if (CustomFields == null)
                CustomLogger.Log("ERROR: CustomFields is null after deserialization!");
            if (ButtonStates == null)
                CustomLogger.Log("ERROR: ButtonStates is null after deserialization!");

            CustomLogger.Log("OnSpawn (post-deserialization) for ThermoSensorStateComponent:");
            if (CustomFields != null)
            {
                foreach (var kvp in CustomFields)
                    CustomLogger.Log($"  CustomField: {kvp.Key} = {kvp.Value}");
            }
            if (ButtonStates != null)
            {
                foreach (var kvp in ButtonStates)
                    CustomLogger.Log($"  ButtonState: {kvp.Key} = {kvp.Value}");
            }
            CustomLogger.Log($"  RandomID: {RandomID}");

            if (RandomID == 0)
                RandomID = UnityEngine.Random.Range(100000, 999999);
            EnsureDefaults();
        }
            
        public void Sim1000ms(float dt)
        {
            int signal = 0;

            if (TryGetComponent<LogicTemperatureSensor>(out var sensor))
            {
                float currentValue = sensor.CurrentValue;
                UpdateDerivatives(currentValue, ThermoSensorGlobals.deltaT);
                if (sensor.IsSwitchedOn)
                {
                    signal |= 1 << 0;
                }
            }

            switchSignalStates.Clear();
            foreach (var sw in RegisteredSwitches)
            {
                bool signalOn = sw.GetSignalOn();
                switchSignalStates[sw.OutputBit] = signalOn;
                if (signalOn)
                {
                    signal |= 1 << sw.OutputBit;
                }
            }

            if (TryGetComponent<LogicPorts>(out var ports))
            {
                ports.SendSignal(ThermoSensorPatchNew.RIBBON_OUTPUT_PORT_ID, signal);
            }
        }
    }

    [HarmonyPatch(typeof(LogicTemperatureSensorConfig), "DoPostConfigureComplete")]
    public static class ThermoSensorPatchNew
    {
        public static readonly HashedString RIBBON_OUTPUT_PORT_ID = new HashedString("ThermoSensorPlusRibbonOutput");

        public static void Postfix(GameObject go)
        {
            // Ensure the component is added to the prefab at config time, not just at runtime
            if (go.GetComponent<ThermoSensorStateComponent>() == null)
                go.AddComponent<ThermoSensorStateComponent>();

            var ports = go.AddOrGet<LogicPorts>();

            ports.inputPortInfo = new LogicPorts.Port[0];
            ports.outputPortInfo = new LogicPorts.Port[0];

            ports.outputPortInfo = new[]
            {
                LogicPorts.Port.RibbonOutputPort(
                    RIBBON_OUTPUT_PORT_ID,
                    new CellOffset(0, 0),
                    STRINGS.BUILDINGS.PREFABS.LOGICTEMPERATURESENSOR.LOGIC_PORT,
                    STRINGS.BUILDINGS.PREFABS.LOGICTEMPERATURESENSOR.LOGIC_PORT_ACTIVE,
                    STRINGS.BUILDINGS.PREFABS.LOGICTEMPERATURESENSOR.LOGIC_PORT_INACTIVE,
                    true
                )
            };
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

        private void Update()
        {
            if (!gameObject.activeInHierarchy || currentState == null)
                return;

            foreach (var field in fields)
                field.UpdateOutput();
        }

        private GameObject root;
        private ThermoSensorStateComponent currentState;
        private List<MyThresholdSwitch> fields = new List<MyThresholdSwitch>();

        private LocText sensorIdLocText;

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
                foreach (var field in fields)
                {
                    thermoState.ButtonStates[$"{field.FieldId}_A"] = field.IsAButtonPressed;
                    thermoState.ButtonStates[$"{field.FieldId}_B"] = field.IsBButtonPressed;
                    thermoState.ButtonStates[$"{field.FieldId}_A_interactable"] = field.IsAButtonInteractable;
                    thermoState.ButtonStates[$"{field.FieldId}_B_interactable"] = field.IsBButtonInteractable;
                    if (field.InputField != null)
                        thermoState.CustomFields[field.FieldId] = field.InputField.Text;
                }
            }
        }
    }

}
