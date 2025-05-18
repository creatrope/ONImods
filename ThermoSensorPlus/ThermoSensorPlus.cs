using HarmonyLib;
using KMod;
using KSerialization;
using PeterHan.PLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using ThermoSensorPlus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoSensorPlus
{
    public static class CustomLogger
    {
        private const string PREFIX = "[ThermoSensorPlus] ";
        public static bool DebugMath = true;
        public static bool DebugUI = false;

        public static void LogMath(string message)
        {
            if (DebugMath)
                Debug.Log(PREFIX + message);
        }

        public static void LogUI(string message)
        {
            if (DebugUI)
                Debug.Log(PREFIX + message);
        }

        public static void Log(string message) => Debug.Log(PREFIX + message);
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
            CustomLogger.Log("Mod loaded. Applying Harmony patches.");
            harmony.PatchAll();
        }
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public partial class ThermoSensorStateComponent : KMonoBehaviour, ISim1000ms
    {
        [Serialize] public int randomID;
        [Serialize] public Dictionary<string, string> customFields = new Dictionary<string, string>();
        [Serialize] public Dictionary<string, bool> buttonStates = new Dictionary<string, bool>();

        private float? lastValue = null;
        private float? lastFirstDerivative = null;

        public float LastValue => lastValue ?? 0f;
        public float FirstDerivative { get; private set; }
        public float SecondDerivative { get; private set; }
        public float SmoothedFirst { get; private set; }
        public float SmoothedSecond { get; private set; }

        private const float SmoothingAlpha = 0.2f;

        private readonly List<MyThresholdSwitch> registeredSwitches = new List<MyThresholdSwitch>();

        // Add this: stores the current signalOn state for each OutputBit
        private Dictionary<int, bool> switchSignalStates = new Dictionary<int, bool>();

        public void RegisterSwitch(MyThresholdSwitch sw)
        {
            if (!registeredSwitches.Contains(sw))
                registeredSwitches.Add(sw);
        }

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
            // Initialize customFields
            if (!customFields.ContainsKey("threshold1"))
                customFields["threshold1"] = "1.0";
            if (!customFields.ContainsKey("threshold2"))
                customFields["threshold2"] = "1.0";

            // For each threshold, check if the button states are valid (opposite states)
            foreach (var prefix in new[] { "threshold1", "threshold2" })
            {
                bool a = buttonStates.ContainsKey($"{prefix}_A") ? buttonStates[$"{prefix}_A"] : false;
                bool b = buttonStates.ContainsKey($"{prefix}_B") ? buttonStates[$"{prefix}_B"] : false;
                bool aInteract = buttonStates.ContainsKey($"{prefix}_A_interactable") ? buttonStates[$"{prefix}_A_interactable"] : true;
                bool bInteract = buttonStates.ContainsKey($"{prefix}_B_interactable") ? buttonStates[$"{prefix}_B_interactable"] : true;

                // Valid if exactly one is pressed and not interactable, the other is not pressed and interactable
                bool validA = a && !aInteract && !b && bInteract;
                bool validB = b && !bInteract && !a && aInteract;

                if (!(validA || validB))
                {
                    // If not valid, set to default: A pressed, not interactable; B not pressed, interactable
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
            if (GetComponent<LogicTemperatureSensor>() == null)
                CustomLogger.Log("WARNING: ThermoSensorStateComponent added to object without LogicTemperatureSensor: " + gameObject.name);

            CustomLogger.Log($"OnSpawn: randomID={randomID}, customFields.Count={customFields?.Count ?? 0}");

            if (randomID == 0)
            {
                randomID = UnityEngine.Random.Range(100000, 999999);
                CustomLogger.Log($"OnSpawn: Assigned new random ID {randomID} to {gameObject.name}");
            }
            else
            {
                CustomLogger.Log($"OnSpawn: Restored existing ID {randomID} for {gameObject.name}");
            }

            EnsureDefaults();
        }
            
        public void Sim1000ms(float dt)
        {
            int signal = 0;

            // Bit 0: default sensor state
            if (TryGetComponent<LogicTemperatureSensor>(out var sensor))
            {
                float currentValue = sensor.CurrentValue;
                UpdateDerivatives(currentValue, ThermoSensorGlobals.deltaT);
                if (sensor.IsSwitchedOn)
                {
                    signal |= 1 << 0;
                    CustomLogger.LogMath($"DefaultSignal Bit 0 ON: {gameObject.name} (ID={randomID}) IsSwitchedOn={sensor.IsSwitchedOn}");
                }
                else
                {
                    CustomLogger.LogMath($"DefaultSignal Bit 0 OFF: {gameObject.name} (ID={randomID}) IsSwitchedOn={sensor.IsSwitchedOn}");
                }
            }
            else
            {
                CustomLogger.LogMath($"DefaultSignal Bit 0 SKIPPED: {gameObject.name} (ID={randomID}) LogicTemperatureSensor NOT FOUND");
            }

            CustomLogger.LogMath($"BitmaskAfterDefault {gameObject.name} (ID={randomID}) Bitmask after default: {Convert.ToString(signal, 2).PadLeft(8, '0')} (int: {signal})");

            // Test for registeredSwitches null or empty
            if (registeredSwitches == null)
            {
                CustomLogger.LogMath($"registeredSwitches is NULL for {gameObject.name} (ID={randomID})");
            }
            else if (registeredSwitches.Count == 0)
            {
                CustomLogger.LogMath($"registeredSwitches is EMPTY for {gameObject.name} (ID={randomID})");
            }
            else
            {
                foreach (var sw in registeredSwitches)
                {
                    bool signalOn = sw.GetSignalOn();
                    switchSignalStates[sw.OutputBit] = signalOn;
                    if (signalOn)
                    {
                        signal |= 1 << sw.OutputBit;
                    }
                    CustomLogger.LogMath(
                        $"CustomBit {sw.OutputBit}: {gameObject.name} (ID={randomID}) GetSignalOn={signalOn} [{sw.GetAutomationDebugString()}]"
                    );
                }
            }

            CustomLogger.LogMath($"BitmaskAfterCustom {gameObject.name} (ID={randomID}) Bitmask after custom: {Convert.ToString(signal, 2).PadLeft(8, '0')} (int: {signal})");

            // Send the combined signal
            if (TryGetComponent<LogicPorts>(out var ports))
            {
                CustomLogger.LogMath($"SendSignal PortID={ThermoSensorPatchNew.RIBBON_OUTPUT_PORT_ID}, Signal={signal}");
                ports.SendSignal(ThermoSensorPatchNew.RIBBON_OUTPUT_PORT_ID, signal);
            }
        }
    }

    [HarmonyPatch(typeof(LogicTemperatureSensorConfig), "DoPostConfigureComplete")]
    public static class ThermoSensorPatchNew
    {
        // Define a unique port ID for your ribbon output
        public static readonly HashedString RIBBON_OUTPUT_PORT_ID = new HashedString("ThermoSensorPlusRibbonOutput");

        public static void Postfix(GameObject go)
        {
            CustomLogger.Log("DoPostConfigureComplete PATCH RAN for: " + go.name);
            go.AddOrGet<ThermoSensorStateComponent>();

            var ports = go.AddOrGet<LogicPorts>();

            // Remove all input and output ports to ensure a clean slate
            ports.inputPortInfo = new LogicPorts.Port[0];
            ports.outputPortInfo = new LogicPorts.Port[0];

            // Add only the ribbon output port
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
                CustomLogger.Log($"OnSpawn: Attached missing state to legacy sensor: {go.name}");
            }
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class ThermoSensorSideScreenRegister
    {
        public static void Postfix()
        {
            CustomLogger.Log("Registering simple label side screen");
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

        // Cache for the sensor ID LocText
        private LocText sensorIdLocText;

        public override bool IsValidForTarget(GameObject target)
        {
            bool valid = target != null && target.GetComponent<ThermoSensorStateComponent>() != null;
            CustomLogger.Log($"IsValidForTarget: {target?.name}, valid={valid}");
            return valid;
        }

        public override void SetTarget(GameObject target)
        {
            if (!isSideScreenInitialized)
            {
                OnPrefabInit();
            }

            currentState = target?.GetComponent<ThermoSensorStateComponent>();
            CustomLogger.Log($"SetTarget called for: {currentState?.gameObject.name ?? "null"}");

            if (sensorIdLocText != null)
            {
                sensorIdLocText.text = currentState != null
                    ? $"Sensor ID: {currentState.randomID}"
                    : "Sensor ID: (none)";
            }

            // Register switches with the actual sensor GameObject
            foreach (var field in fields)
                field.SetTarget(currentState);
        }

        public override void ClearTarget() { }
        public override string GetTitle() => "ThermoSensor+";

        protected override void OnPrefabInit()
        {
            if (isSideScreenInitialized)
                return; // Prevent double-building

            var panel = new PPanel("ClickPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10,
                BackColor = new Color(1f, 0.9f, 0.9f, 1f),
                Margin = new RectOffset(10, 10, 10, 10)
            };

            // Add a label to show the sensor's randomID and cache its LocText
            var idLabel = new PLabel("SensorIdLabel")
            {
                Text = currentState != null ? $"Sensor ID: {currentState.randomID}" : "Sensor ID: (none)",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            }.AddOnRealize(realizedGo =>
            {
                sensorIdLocText = realizedGo.transform.Find("Text")?.GetComponent<LocText>();
            });
            panel.AddChild(idLabel);

            root = panel.AddTo(gameObject, 0);
            ContentContainer = root;

            // Build UI rows only (do not register with a GameObject here)
            var threshold1 = new MyThresholdSwitch("threshold1", "Vel.", "1.0", 1);
            fields.Add(threshold1);
            threshold1.BuildUIRow(root); // New method, see below

            var threshold2 = new MyThresholdSwitch("threshold2", "Acc.", "1.0", 2);
            fields.Add(threshold2);
            threshold2.BuildUIRow(root); // New method, see below

            isSideScreenInitialized = true;

            CustomLogger.Log("Side screen UI initialized.");
        }
    }

    public class MyThresholdSwitch
    {
        private readonly string fieldId;
        private readonly string labelText;
        private readonly string defaultValue;

        private PTextField inputField;
        private TMP_InputField unityInputField;
        private PLabel outputField;
        private LocText outputLocText;
        private ThermoSensorStateComponent stateComponent;

        private GameObject parentForBuild = null;

        // Button state and UI references are now per-instance, not shared
        private PButton aButton;
        private PButton bButton;
        private UnityEngine.UI.Button unityAButton;
        private UnityEngine.UI.Button unityBButton;
        private KButton kAButton;
        private KButton kBButton;
        private bool isAButtonPressed = false;
        private bool isBButtonPressed = false;
        private bool isAButtonInteractable = true;
        private bool isBButtonInteractable = true;

        private static readonly Color ButtonOnColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        private static readonly Color ButtonOffColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        public int OutputBit { get; } // Add this property

        public MyThresholdSwitch(string id, string label, string defaultValue = "1.0", int outputBit = 0)
        {
            this.fieldId = id;
            this.labelText = label;
            this.defaultValue = defaultValue;
            this.OutputBit = outputBit; // Set the output bit
        }

        public void SetParentForBuild(GameObject parent)
        {
            parentForBuild = parent;
        }

        private PButton CreateButton(string buttonId, string label, System.Action onToggle, out UnityEngine.UI.Button unityButtonRef, out KButton kButtonRef)
        {
            UnityEngine.UI.Button localButtonRef = null;
            KButton localKButtonRef = null;
            var pButton = new PButton($"{buttonId}Button_{fieldId}")
            {
                Text = label,
                TextStyle = PUITuning.Fonts.TextLightStyle,
                ToolTip = $"Toggle {label}",
                FlexSize = new Vector2(22, 22),
                OnClick = (buttonSource) =>
                {
                    onToggle?.Invoke();
                    if (localKButtonRef != null)
                        localKButtonRef.isInteractable = (buttonId == "A" ? isAButtonInteractable : isBButtonInteractable);
                    UpdateButtonVisual();
                }
            };
            pButton.AddOnRealize(realizedGo =>
            {
                localButtonRef = realizedGo.GetComponentInChildren<UnityEngine.UI.Button>();
                localKButtonRef = realizedGo.GetComponent<KButton>();
                // Store references
                if (buttonId == "A") { unityAButton = localButtonRef; kAButton = localKButtonRef; }
                else if (buttonId == "B") { unityBButton = localButtonRef; kBButton = localKButtonRef; }
                // Always update the visual and interactable state here
                UpdateButtonVisual();
            });
            unityButtonRef = localButtonRef;
            kButtonRef = localKButtonRef;
            return pButton;
        }

        public GameObject Build(GameObject parent)
        {
            // Ensure stateComponent exists on the parent GameObject (sensor)
            if (parent != null && stateComponent == null)
            {
                stateComponent = parent.GetComponent<ThermoSensorStateComponent>();
                if (stateComponent == null)
                {
                    stateComponent = parent.AddComponent<ThermoSensorStateComponent>();
                    CustomLogger.Log($"[MyThresholdSwitch:{fieldId}] Build: Created ThermoSensorStateComponent on {parent.name}");
                }
                else
                {
                    CustomLogger.Log($"[MyThresholdSwitch:{fieldId}] Build: Found existing ThermoSensorStateComponent on {parent.name}");
                }
            }
            // Always ensure defaults after creation or retrieval
            stateComponent?.EnsureDefaults();

            var row = new PPanel("RowPanel_" + fieldId)
            {
                Direction = PanelDirection.Horizontal,
                Spacing = 5
            };

            row.AddChild(new PLabel("Label_" + fieldId)
            {
                Text = labelText,
                TextStyle = PUITuning.Fonts.TextDarkStyle
            });

            // Add "A" button
            aButton = CreateButton("A", "A", OnAButtonClicked, out unityAButton, out kAButton);
            row.AddChild(aButton);

            // Add "B" button
            bButton = CreateButton("B", "B", OnBButtonClicked, out unityBButton, out kBButton);
            row.AddChild(bButton);

            inputField = new PTextField("InputField_" + fieldId)
            {
                Text = defaultValue,
                MinWidth = 60,
                OnTextChanged = (source, val) => {
                    if (stateComponent != null)
                        stateComponent.customFields[fieldId] = val;
                }
            }
            .AddOnRealize(realizedGo => {
                unityInputField = realizedGo.GetComponentInChildren<TMP_InputField>();
            });
            row.AddChild(inputField);

            outputField = new PLabel("OutputField_" + fieldId)
            {
                Text = "00000.00",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            }.AddOnRealize(realizedGo => {
                outputLocText = realizedGo.transform.Find("Text")?.GetComponent<LocText>();
            });
            row.AddChild(outputField);

            var go = row.AddTo(parent);

            if (stateComponent != null)
                stateComponent.RegisterSwitch(this);

            return go;
        }

        public GameObject BuildUIRow(GameObject parent)
        {
            var row = new PPanel("RowPanel_" + fieldId)
            {
                Direction = PanelDirection.Horizontal,
                Spacing = 5
            };

            row.AddChild(new PLabel("Label_" + fieldId)
            {
                Text = labelText,
                TextStyle = PUITuning.Fonts.TextDarkStyle
            });

            // Add "A" button with correct handler
            aButton = CreateButton("A", "A", OnAButtonClicked, out unityAButton, out kAButton);
            row.AddChild(aButton);

            // Add "B" button with correct handler
            bButton = CreateButton("B", "B", OnBButtonClicked, out unityBButton, out kBButton);
            row.AddChild(bButton);

            // Input field with OnTextChanged handler
            inputField = new PTextField("InputField_" + fieldId)
            {
                Text = defaultValue,
                MinWidth = 60,
                OnTextChanged = (source, val) => {
                    if (stateComponent != null)
                        stateComponent.customFields[fieldId] = val;
                }
            }
            .AddOnRealize(realizedGo => {
                unityInputField = realizedGo.GetComponentInChildren<TMP_InputField>();
            });
            row.AddChild(inputField);

            outputField = new PLabel("OutputField_" + fieldId)
            {
                Text = "00000.00",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            }.AddOnRealize(realizedGo => {
                outputLocText = realizedGo.transform.Find("Text")?.GetComponent<LocText>();
            });
            row.AddChild(outputField);

            return row.AddTo(parent);
        }

        public void SetTarget(ThermoSensorStateComponent state)
        {
            stateComponent = state;

            // Register with the state component if not already registered
            if (stateComponent != null)
                stateComponent.RegisterSwitch(this);

            LogButtonState("SetTarget (before applying state)");

            // Restore input field value
            string val = defaultValue;
            if (stateComponent != null && stateComponent.customFields.TryGetValue(fieldId, out string savedVal))
                val = savedVal;
            CustomLogger.LogUI($"[MyThresholdSwitch:{fieldId}] Restoring inputField.Text='{val}' for sensor id={stateComponent?.randomID}");

            if (inputField != null)
                inputField.Text = val;
            if (unityInputField != null && unityInputField.text != val)
                unityInputField.text = val;

            // Restore button states and interactable state from stateComponent if present
            bool prevAButtonPressed = isAButtonPressed, prevBButtonPressed = isBButtonPressed;
            bool prevAButtonInteractable = isAButtonInteractable, prevBButtonInteractable = isBButtonInteractable;

            isAButtonPressed = false;
            isBButtonPressed = false;
            isAButtonInteractable = true;
            isBButtonInteractable = true;
            if (stateComponent != null && stateComponent.buttonStates != null)
            {
                if (stateComponent.buttonStates.TryGetValue($"{fieldId}_A", out bool savedA))
                    isAButtonPressed = savedA;
                if (stateComponent.buttonStates.TryGetValue($"{fieldId}_B", out bool savedB))
                    isBButtonPressed = savedB;
                if (stateComponent.buttonStates.TryGetValue($"{fieldId}_A_interactable", out bool savedAInteract))
                    isAButtonInteractable = savedAInteract;
                if (stateComponent.buttonStates.TryGetValue($"{fieldId}_B_interactable", out bool savedBInteract))
                    isBButtonInteractable = savedBInteract;
            }

            UpdateButtonVisual();

            // Set KButton interactable state from saved state
            if (kAButton != null)
            {
                kAButton.isInteractable = isAButtonInteractable;
                CustomLogger.LogUI($"[MyThresholdSwitch:{fieldId}] Set kAButton.isInteractable = {isAButtonInteractable}");
            }
            if (kBButton != null)
            {
                kBButton.isInteractable = isBButtonInteractable;
                CustomLogger.LogUI($"[MyThresholdSwitch:{fieldId}] Set kBButton.isInteractable = {isBButtonInteractable}");
            }

            if (unityAButton != null)
                unityAButton.image.color = isAButtonPressed ? ButtonOnColor : ButtonOffColor;
            if (unityBButton != null)
                unityBButton.image.color = isBButtonPressed ? ButtonOnColor : ButtonOffColor;

            UpdateOutput();

            LogButtonState("SetTarget (after applying state)");
        }

        private void SaveState()
        {
            if (stateComponent != null)
            {
                stateComponent.buttonStates[$"{fieldId}_A"] = isAButtonPressed;
                stateComponent.buttonStates[$"{fieldId}_B"] = isBButtonPressed;
                stateComponent.buttonStates[$"{fieldId}_A_interactable"] = isAButtonInteractable;
                stateComponent.buttonStates[$"{fieldId}_B_interactable"] = isBButtonInteractable;
            }
        }

        private void UpdateButtonVisual()
        {
            if (aButton != null)
                aButton.Text = isAButtonPressed ? "A" : "A";
            if (bButton != null)
                bButton.Text = isBButtonPressed ? "B" : "B";

            if (unityAButton != null)
                unityAButton.image.color = isAButtonPressed ? ButtonOnColor : ButtonOffColor;
            if (unityBButton != null)
                unityBButton.image.color = isBButtonPressed ? ButtonOnColor : ButtonOffColor;

            if (kAButton != null)
                kAButton.isInteractable = isAButtonInteractable;
            if (kBButton != null)
                kBButton.isInteractable = isBButtonInteractable;
        }

        public void UpdateOutput()
        {
            float val = 0f;
            if (stateComponent != null)
            {
                if (fieldId == "threshold1")
                    val = stateComponent.SmoothedFirst;
                else if (fieldId == "threshold2")
                    val = stateComponent.SmoothedSecond;
                else
                    val = stateComponent.LastValue;
            }

            if (outputLocText != null)
                outputLocText.text = val.ToString("00000.00");
        }

        public void AutomationCheckAndDebug()
        {
            float outputVal = 0f;
            if (stateComponent != null)
            {
                if (fieldId == "threshold1")
                    outputVal = stateComponent.SmoothedFirst;
                else if (fieldId == "threshold2")
                    outputVal = stateComponent.SmoothedSecond;
                else
                    outputVal = stateComponent.LastValue;
            }

            float inputVal = 0f;
            if (inputField != null && float.TryParse(inputField.Text, out float parsed))
                inputVal = parsed;

            bool signalOn = GetSignalOn();

            if (signalOn)
            {
                CustomLogger.LogMath($"[AutomationDebug] {fieldId}: Automation Signal On (Output={outputVal}, Input={inputVal}, A_Pressed={isAButtonPressed}, B_Pressed={isBButtonPressed}, SensorID={stateComponent?.randomID})");
            }
        }

        public bool GetSignalOn()
        {
            float outputVal = 0f;
            if (stateComponent != null)
            {
                if (fieldId == "threshold1")
                    outputVal = stateComponent.SmoothedFirst;
                else if (fieldId == "threshold2")
                    outputVal = stateComponent.SmoothedSecond;
                else
                    outputVal = stateComponent.LastValue;
            }

            float inputVal = 0f;
            if (inputField != null && float.TryParse(inputField.Text, out float parsed))
                inputVal = parsed;

            return (isAButtonPressed && outputVal > inputVal) ||
                   (isBButtonPressed && outputVal < inputVal);
        }

        private void LogButtonState(string context)
        {
            string stateSummary =
                $"aBnState={isAButtonPressed}, bBnState={isBButtonPressed}, " +
                $"aInteract={isAButtonInteractable}, bInteract={isBButtonInteractable} | " +
                $"stateComponent.buttonStates: ";

            if (stateComponent == null || stateComponent.buttonStates == null || stateComponent.buttonStates.Count == 0)
            {
                stateSummary += "EMPTY";
            }
            else
            {
                foreach (var kvp in stateComponent.buttonStates)
                    stateSummary += $"{kvp.Key.Replace("Button", "Bn")}={kvp.Value}, ";
                stateSummary = stateSummary.TrimEnd(',', ' ');
            }

            CustomLogger.LogUI($"[MyThresholdSwitch:{fieldId}] {context}: {stateSummary}");
        }

        public string GetAutomationDebugString()
        {
            float outputVal = 0f;
            if (stateComponent != null)
            {
                if (fieldId == "threshold1")
                    outputVal = stateComponent.SmoothedFirst;
                else if (fieldId == "threshold2")
                    outputVal = stateComponent.SmoothedSecond;
                else
                    outputVal = stateComponent.LastValue;
            }

            float inputVal = 0f;
            if (inputField != null && float.TryParse(inputField.Text, out float parsed))
                inputVal = parsed;

            return $"[AutomationDebug] {fieldId}: Output={outputVal}, Input={inputVal}, A_Pressed={isAButtonPressed}, B_Pressed={isBButtonPressed}, SensorID={stateComponent?.randomID}";
        }

        private void OnAButtonClicked()
        {
            isAButtonPressed = !isAButtonPressed;
            isAButtonInteractable = false;
            isBButtonPressed = false;
            isBButtonInteractable = true;
            UpdateButtonVisual();
            SaveState();
        }

        private void OnBButtonClicked()
        {
            isBButtonPressed = !isBButtonPressed;
            isBButtonInteractable = false;
            isAButtonPressed = false;
            isAButtonInteractable = true;
            UpdateButtonVisual();
            SaveState();
        }
    }
}
