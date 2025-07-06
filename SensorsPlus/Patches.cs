using Database;
using Epic.OnlineServices.Platform;
using HarmonyLib;
using HLib;
using KMod;
using KSerialization;
using Newtonsoft.Json; // Ensure this using directive is present

using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic; // For List<> and Dictionary<>
using System.Runtime.CompilerServices; // For ConditionalWeakTable
using TUNING; // Or the correct namespace for LogicPressureSensorConfig
using UnityEngine;
using static Rendering.BlockTileRenderer;
using static SensorMathUtils;

namespace SensorsPlus
{
    public class Patches
    {
        // Define the missing RIBBON_PORT_ID constant
        public static readonly HashedString RIBBON_PORT_ID = new HashedString("RibbonPort");

        // Change from private to public so HotkeyListenerUpdater can access it
        public static HLib.HotkeyListener hotkeyListener;

        // Add a static flag to control ribbon debug output
        public static bool ribbonDebugEnabled = false;

        // Add a guard to prevent double static initialization
        private static bool staticInitialized = false;

        // Change Logger field to public static
        public static readonly CustomLogger Logger = new CustomLogger("SensorsPlus");

        static Patches()
        {
            if (staticInitialized)
                return;
            staticInitialized = true;

            var uniqueId = Guid.NewGuid();
            var timestamp = System.DateTime.Now.ToString("O");
            var domain = AppDomain.CurrentDomain.FriendlyName;
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            // Initialize and register hotkeys
            hotkeyListener = new HLib.HotkeyListener();

            hotkeyListener.RegisterHotkey("Ctrl+F11", () =>
            {
                ribbonDebugEnabled = !ribbonDebugEnabled;
                Patches.Logger.Log($"[SensorsPlus] Ctrl+F11 pressed: ribbonDebugEnabled is now {(ribbonDebugEnabled ? "ON" : "OFF")}");
            });

            // Register for Unity update loop
            HotkeyListenerUpdater.Create();
        }

        public static void OnLoad()
        {
            var options = POptions.ReadSettings<ModOptions>() ?? new ModOptions();
            Patches.Logger.SetLoggingEnabled(options.EnableCustomLog);
            Patches.Logger.Reset();


            // Set global variables for all options
            SensorMathUtils.MovingAverageWindow = options.MovingAverageWindow;
            SensorMathUtils.SamplingIntervalSeconds = options.SamplingIntervalSeconds;

            // Log all options to the custom logger
            Patches.Logger.Log($"[ModOptions] EnableCustomLog: {options.EnableCustomLog}");
            Patches.Logger.Log($"[ModOptions] MovingAverageWindow: {SensorMathUtils.MovingAverageWindow}");
            Patches.Logger.Log($"[ModOptions] SamplingIntervalSeconds: {SensorMathUtils.SamplingIntervalSeconds}");
        }

        // Utility method to add or update a ribbon port
        public static void AddOrUpdateRibbonPort(LogicPorts logicPorts, LogicPorts.Port newPort, string logContext)
        {
            if (logicPorts.outputPortInfo == null)
            {
                logicPorts.outputPortInfo = new[] { newPort };
                Patches.Logger.Log($"[{logContext}] Created ribbon port: {newPort.id} | InstanceID: {logicPorts.gameObject.GetInstanceID()}");
            }
            else
            {
                var ports = new List<LogicPorts.Port>(logicPorts.outputPortInfo);
                if (!ports.Exists(p => p.id == newPort.id))
                {
                    ports.Add(newPort);
                    Patches.Logger.Log($"[{logContext}] Added ribbon port: {newPort.id} | InstanceID: {logicPorts.gameObject.GetInstanceID()}");
                }
                else
                {
                    Patches.Logger.Log($"[{logContext}] Ribbon port already exists: {newPort.id} | InstanceID: {logicPorts.gameObject.GetInstanceID()}");
                }
                logicPorts.outputPortInfo = ports.ToArray();
            }
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        public class Db_Initialize_Patch
        {
            // note: there's evidence that initialize is being called multiple times in some cases, be careful!
            public static void Prefix()
            {
            }

            public static void Postfix()
            {
            }
        }
    }

    // MonoBehaviour to call HotkeyListener.Update every frame
    public class HotkeyListenerUpdater : KMonoBehaviour
    {
        private static HotkeyListenerUpdater _instance;

        public static void Create()
        {
            if (_instance == null)
            {
                var go = new GameObject("SensorsPlus_HotkeyListenerUpdater");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<HotkeyListenerUpdater>();
            }

            Patches.Logger.Log("HotkeyListenerUpdater.Create called");
        }

        void Update()
        {
            if (Patches.hotkeyListener != null)
            {
                Patches.hotkeyListener.Update();
            }
            else
            {
                Patches.Logger.Log("[HotkeyListenerUpdater] Patches.hotkeyListener is null.");
            }
        }
    }

    public class ModOptions
    {
        [Option("Enable Custom Output Log", "Enable or disable writing the custom output log file.")]
        [JsonProperty] // Add JSON property for serialization
        public bool EnableCustomLog { get; set; } = false;

        [Option("Moving Average Window", "Number of samples for the moving average of the derivative.")]
        [Limit(1, 32)]
        [JsonProperty] // Add JSON property for serialization
        public int MovingAverageWindow { get; set; } = 3;

        [Option("Sampling Interval (seconds)", "How often sensors sample values (in seconds).")]
        [Limit(0.5, 30.0)]
        [JsonProperty] // Add JSON property for serialization
        public float SamplingIntervalSeconds { get; set; } = 3.0f;
    }

    public class Mod : UserMod2
    {
        private static int onLoadCount = 0;

        public override void OnLoad(Harmony harmony)
        {
            onLoadCount++;
            Patches.Logger.Log($"SensorsPlus: Mod.OnLoad called. Count={onLoadCount}");
            SensorsPlus.Patches.OnLoad(); // <-- Ensure hotkey system is initialized
            base.OnLoad(harmony);

            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(ModOptions));
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(LogicPressureSensor), "Sim200ms")]
    public static class LogicPressureSensor_Sim200ms_Patch
    {
        public static readonly ConditionalWeakTable<LogicPressureSensor, SensorMathUtils.DerivativeState<LogicPressureSensor>> DerivativeStates =
            new ConditionalWeakTable<LogicPressureSensor, SensorMathUtils.DerivativeState<LogicPressureSensor>>();

        private static Dictionary<LogicPressureSensor, float> _lastSampleTimes = new Dictionary<LogicPressureSensor, float>();

        static void Postfix(LogicPressureSensor __instance)
        {
            var id = __instance.gameObject.GetInstanceID();
            //Patches.Logger.Log("$[Sim200ms] Processing {id}.");

            var counterComponent = __instance.GetComponent<SensorCounterComponent>();
            if (counterComponent == null)
            {
                //Patches.Logger.Log($"[Sim200ms] {id} SensorCounterComponent is null. Skipping processing.");
                return; // Exit early to avoid NullReferenceException
            }

            // Initialize if needed
            if (counterComponent.Init == 0)
            {
                counterComponent.Init = 1;
                counterComponent.Target = (int)(SensorMathUtils.SamplingIntervalSeconds / 0.2);
                Patches.Logger.Log($"[LogicPressureSensor] {id} target {counterComponent.Target}");

            }
            int incr = counterComponent.Increment();
            int target = counterComponent.Target;
            //Patches.Logger.Log($"[LogicPressureSensor] {id} count={incr}, target={target}");
            if (incr < target)
            {
                //Patches.Logger.Log($"[LogicPressureSensor] {incr}<{target}, Skipping processing.");
                return;
            }
            counterComponent.Reset();
            // proceeding

            var ports = __instance.GetComponent<LogicPorts>();
            if (ports == null)
            {
                Patches.Logger.Log("[LogicPressureSensor] LogicPorts is null. Skipping processing.");
                return; // Exit early to avoid NullReferenceException
            }

            int ribbonSignal = SensorMathUtils.ProcessSensorData(
                    __instance,
                    DerivativeStates,
                    ref _lastSampleTimes,
                    Patches.RIBBON_PORT_ID,
                    SensorMathUtils.SamplingIntervalSeconds,
                    SensorMathUtils.MovingAverageWindow,
                    sensor => sensor.CurrentValue,
                    sensor => sensor.IsSwitchedOn,
                    sensor => sensor.ActivateAboveThreshold,
                    sensor =>
                    {
                        var inputValueComponent = sensor.GetComponent<SensorInputValueComponent>();
                        return inputValueComponent != null ? inputValueComponent.parsedValue : 1.0f;
                    },
                    sensor => sensor.GetComponent<LogicPorts>()
                );

            ports.SendSignal(Patches.RIBBON_PORT_ID, ribbonSignal);
        }
    }

    [HarmonyPatch(typeof(LogicTemperatureSensor), "Sim200ms")]
    public static class LogicTemperatureSensor_Sim200ms_Patch
    {
        public static readonly ConditionalWeakTable<LogicTemperatureSensor, SensorMathUtils.DerivativeState<LogicTemperatureSensor>> DerivativeStates =
            new ConditionalWeakTable<LogicTemperatureSensor, SensorMathUtils.DerivativeState<LogicTemperatureSensor>>();

        private static Dictionary<LogicTemperatureSensor, float> _lastSampleTimes;

        static void Postfix(LogicTemperatureSensor __instance)
        {
            //var id = __instance.gameObject.GetInstanceID();

            // proceeding  
            var ports = __instance.GetComponent<LogicPorts>();

            int ribbonSignal = SensorMathUtils.ProcessSensorData(
                __instance,
                DerivativeStates,
                ref _lastSampleTimes,
                Patches.RIBBON_PORT_ID,
                SensorMathUtils.SamplingIntervalSeconds,
                SensorMathUtils.MovingAverageWindow,
                sensor => sensor.CurrentValue,
                sensor => sensor.IsSwitchedOn,
                sensor => sensor.ActivateAboveThreshold,
                sensor =>
                {
                    var inputValueComponent = sensor.GetComponent<SensorInputValueComponent>();
                    return inputValueComponent != null ? inputValueComponent.parsedValue : 0.1f;
                },
                sensor => sensor.GetComponent<LogicPorts>()
            );

            ports.SendSignal(Patches.RIBBON_PORT_ID, ribbonSignal);
        }
    }

    [HarmonyPatch(typeof(LogicPressureSensorGasConfig), "DoPostConfigureComplete")]
    public static class LogicPressureSensorGasConfig_DoPostConfigureComplete_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(GameObject go)
        {
            if (go == null)
            {
                Patches.Logger.Log("[LogicPressureSensorGasConfig] GameObject is null. Exiting.");
                return;
            }

            // Log GameObject InstanceID for debugging
            Patches.Logger.Log($"[LogicPressureSensorGasConfig] GameObject InstanceID: {go.GetInstanceID()}");

            var logicPorts = go.AddOrGet<LogicPorts>();
            var counterComponent = go.AddOrGet<SensorCounterComponent>();

            var newPort = new LogicPorts.Port(
                Patches.RIBBON_PORT_ID,
                new CellOffset(0, 0),
                $"Ribbon Output (Gas) - Sensor {go.GetInstanceID()}",
                "Ribbon Output Active",
                "Ribbon Output Inactive",
                true,
                LogicPortSpriteType.RibbonOutput
            );

            Patches.AddOrUpdateRibbonPort(logicPorts, newPort, "LogicPressureSensorGasConfig");
        }
    }

    [HarmonyPatch(typeof(LogicPressureSensorLiquidConfig), "DoPostConfigureComplete")]
    public static class LogicPressureSensorLiquidConfig_DoPostConfigureComplete_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(GameObject go)
        {
            if (go == null)
            {
                Patches.Logger.Log("[LogicPressureSensorLiquidConfig] GameObject is null. Exiting.");
                return;
            }

            var logicPorts = go.AddOrGet<LogicPorts>();
            var counterComponent = go.AddOrGet<SensorCounterComponent>();

            var newPort = new LogicPorts.Port(
                Patches.RIBBON_PORT_ID,
                new CellOffset(0, 0),
                $"Ribbon Output (Liquid) - Sensor {go.GetInstanceID()}",
                "Ribbon Output Active",
                "Ribbon Output Inactive",
                true,
                LogicPortSpriteType.RibbonOutput
            );

            Patches.AddOrUpdateRibbonPort(logicPorts, newPort, "LogicPressureSensorLiquidConfig");
        }
    }

    [HarmonyPatch(typeof(LogicTemperatureSensorConfig), "DoPostConfigureComplete")]
    public static class LogicTemperatureSensorConfig_DoPostConfigureComplete_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(GameObject go)
        {
            if (go == null)
            {
                Patches.Logger.Log("[LogicTemperatureSensorConfig] GameObject is null. Exiting.");
                return;
            }

            var logicPorts = go.AddOrGet<LogicPorts>();
            var counterComponent = go.AddOrGet<SensorCounterComponent>(); // Attach the counter component

            var newPort = new LogicPorts.Port(
                Patches.RIBBON_PORT_ID,
                new CellOffset(0, 0),
                $"Ribbon Output (Temperature) - Sensor {go.GetInstanceID()}",
                "Ribbon Output Active",
                "Ribbon Output Inactive",
                true,
                LogicPortSpriteType.RibbonOutput
            );

            Patches.AddOrUpdateRibbonPort(logicPorts, newPort, "LogicTemperatureSensorConfig");
        }
    }

    // Attach to new GAS pressure sensors
    [HarmonyPatch(typeof(LogicPressureSensorGasConfig), "DoPostConfigureComplete")]
    public static class AddSensorInputValueComponent_Gas
    {
        public static void Postfix(GameObject go)
        {
            go.AddOrGet<SensorInputValueComponent>();
        }
    }

    // Attach to new LIQUID pressure sensors
    [HarmonyPatch(typeof(LogicPressureSensorLiquidConfig), "DoPostConfigureComplete")]
    public static class AddSensorInputValueComponent_Liquid
    {
        public static void Postfix(GameObject go)
        {
            go.AddOrGet<SensorInputValueComponent>();
        }
    }

    // Attach to new TEMPERATURE sensors
    [HarmonyPatch(typeof(LogicTemperatureSensorConfig), "DoPostConfigureComplete")]
    public static class AddSensorInputValueComponent_Temperature
    {
        public static void Postfix(GameObject go)
        {
            go.AddOrGet<SensorInputValueComponent>();
        }
    }

    // Repeat for other sensor types as needed...

    // Optionally, patch the UI to allow switching output type
    // This is more advanced and may require patching side screen or config methods

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class SensorSimpleInputSideScreenRegister
    {
        private static bool registered = false;

        public static void Postfix()
        {
            if (registered) return;
            registered = true;
            //Patches.Logger.Log("[SensorSimpleInputSideScreenRegister] Registering SensorSimpleInputSideScreen.");
            // Register the side screen for both pressure and temperature sensors
            PUIUtils.AddSideScreenContent<SensorSimpleInputSideScreen>();
        }
    }
}

// Define a reusable component for the counter  
public class SensorCounterComponent : MonoBehaviour
{
    public int Init { get; set; } = 0;

    private int _counter;
    public int Target { get; set; } = 6;

    public int Counter
    {
        get => _counter;
        set => _counter = value;
    }

    public void Reset() => Counter = 0; // Use the property to reset

    public int Increment()
    {
        Counter++; // Increment the property instead of the field
        return Counter;
    }
}
