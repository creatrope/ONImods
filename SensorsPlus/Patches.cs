using System;
using Database;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using System.Collections.Generic; // For List<>
using System.Runtime.CompilerServices;
using TUNING; // Or the correct namespace for LogicPressureSensorConfig
using UnityEngine;
using KSerialization;
using static SensorMathUtils;
using HLib; // <-- Add this using directive

namespace SensorsPlus // CHANGED FROM SensorsP
{
    public class Patches
    {
        // Change from private to public so HotkeyListenerUpdater can access it
        public static HLib.HotkeyListener hotkeyListener;

        // Add a static flag to control ribbon debug output
        public static bool ribbonDebugEnabled = false;

        // Add a guard to prevent double static initialization
        private static bool staticInitialized = false;

        public static string DesktopLogPath => HLib.CustomLogger.LogPath;

        static Patches()
        {
            if (staticInitialized)
                return;
            staticInitialized = true;

            var uniqueId = Guid.NewGuid();
            var timestamp = System.DateTime.Now.ToString("O");
            var domain = AppDomain.CurrentDomain.FriendlyName;
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            HLib.CustomLogger.Log($"SensorsPlus: Patches static ctor loaded | {timestamp} | {uniqueId} | Domain: {domain} | Thread: {threadId}");

            Debug.Log("SensorsPlus: Patches class loaded");
            HLib.CustomLogger.Log("SensorsPlus: Patches class loaded.");

            // Initialize and register hotkeys
            hotkeyListener = new HLib.HotkeyListener();

            hotkeyListener.RegisterHotkey("Ctrl+F11", () =>
            {
                ribbonDebugEnabled = !ribbonDebugEnabled;
                HLib.CustomLogger.Log($"[HotkeyListener] Ctrl+F11 pressed: ribbonDebugEnabled is now {(ribbonDebugEnabled ? "ON" : "OFF")}");
            });

            // Register for Unity update loop
            HotkeyListenerUpdater.Create();
        }

        public static void OnLoad()
        {
            // Initialize the custom log path only if EnableCustomLog is true  
            var options = POptions.ReadSettings<ModOptions>() ?? new ModOptions();


            if (options.EnableCustomLog)
            {
                HLib.CustomLogger.SetLogPath("SensorsPlus");
            }

            var uniqueId = Guid.NewGuid();
            var timestamp = System.DateTime.Now.ToString("O");
            var domain = AppDomain.CurrentDomain.FriendlyName;
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            // Initialize and register hotkeys  
            hotkeyListener = new HLib.HotkeyListener();

            hotkeyListener.RegisterHotkey("Ctrl+F11", () =>
            {
                ribbonDebugEnabled = !ribbonDebugEnabled;
                HLib.CustomLogger.Log($"[HotkeyListener] Ctrl+F11 pressed: ribbonDebugEnabled is now {(ribbonDebugEnabled ? "ON" : "OFF")}");
            });

            // Register for Unity update loop  
            HotkeyListenerUpdater.Create();
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        public class Db_Initialize_Patch
        {
            // Static counters for logging
            private static int prefixCount = 0;
            private static int postfixCount = 0;

            public static void Prefix()
            {
                prefixCount++;
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                HLib.CustomLogger.Log($"SensorsPlus: Prefix {prefixCount} | Assembly: {asm.Location}");
            }

            public static void Postfix()
            {
                postfixCount++;
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                HLib.CustomLogger.Log($"SensorsPlus: Postfix {postfixCount} | Assembly: {asm.Location}");
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
            HLib.CustomLogger.Log("HotkeyListenerUpdater.Create called");
        }

        void Update()
        {
            if (Patches.hotkeyListener != null)
            {
                Patches.hotkeyListener.Update();
            }
            else
            {
                HLib.CustomLogger.Log("[HotkeyListenerUpdater] Patches.hotkeyListener is null.");
            }
        }
    }

    [ConfigFile("modoptions.yaml", true, true)]
    public class ModOptions
    {
        [Option("Enable Custom Output Log", "Enable or disable writing the custom output log file.")]
        public bool EnableCustomLog { get; set; } = true;

        [Option("Moving Average Window", "Number of samples for the moving average of the derivative (minimum 1).")]
        [Limit(1, 32)]
        public int MovingAverageWindow { get; set; } = 3;

        [Option("Sampling Interval (seconds)", "How often sensors sample values (in seconds). Default is 1.0.")]
        [Limit(0.05, 10.0)]
        public float SamplingIntervalSeconds { get; set; } = 1.0f;
    }

    public class Mod : UserMod2
    {
        private static int onLoadCount = 0;

        public override void OnLoad(Harmony harmony)
        {
            // Load options and set logger enabled flag
            var options = POptions.ReadSettings<ModOptions>() ?? new ModOptions();

            // Set the moving average window globally
            SensorMathUtils.MovingAverageWindow = options.MovingAverageWindow > 0 ? options.MovingAverageWindow : 3;

            // Set the sampling interval globally
            SensorMathUtils.SamplingIntervalSeconds = options.SamplingIntervalSeconds > 0.01f ? options.SamplingIntervalSeconds : 1.0f;

            onLoadCount++;
            var uniqueId = Guid.NewGuid();
            var timestamp = System.DateTime.Now.ToString("O");
            var domain = AppDomain.CurrentDomain.FriendlyName;
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Debug.Log($"SensorsPlus: Mod.OnLoad called. Count={onLoadCount} | {timestamp} | {uniqueId} | Domain: {domain} | Thread: {threadId}");
            CustomLogger.Log($"SensorsPlus: Mod.OnLoad called. Count={onLoadCount} | {timestamp} | {uniqueId} | Domain: {domain} | Thread: {threadId}");

            Debug.Log("SensorsPlus: Mod.OnLoad called.");

            SensorsPlus.Patches.OnLoad(); // <-- Ensure hotkey system is initialized
            base.OnLoad(harmony);

            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(ModOptions));
            harmony.PatchAll();
        }
    }

    public class SensorOutputType
    {
        public static readonly SensorOutputType Single = new SensorOutputType("Single");
        public static readonly SensorOutputType RibbonCable = new SensorOutputType("RibbonCable");

        public string Name { get; }

        private SensorOutputType(string name) { Name = name; }

        public override string ToString() => Name;
    } 

    public static class SensorOutputManager
    {
        private static readonly ConditionalWeakTable<object, SensorOutputType> OutputTypes =
            new ConditionalWeakTable<object, SensorOutputType>();

        public static SensorOutputType GetOutputType(object sensor)
        {
            if (OutputTypes.TryGetValue(sensor, out var type))
                return type;
            return SensorOutputType.Single;
        }

        public static void SetOutputType(object sensor, SensorOutputType type)
        {
            OutputTypes.Remove(sensor);
            OutputTypes.Add(sensor, type);
        }
    }   

    [HarmonyPatch(typeof(LogicPressureSensor), "Sim200ms")]
    public static class LogicPressureSensor_Sim200ms_Patch // <-- Renamed from LogicPressureSensor_Sim200ms_Paatch
    {
        public static readonly ConditionalWeakTable<LogicPressureSensor, SensorMathUtils.DerivativeState<LogicPressureSensor>> DerivativeStates =
            new ConditionalWeakTable<LogicPressureSensor, SensorMathUtils.DerivativeState<LogicPressureSensor>>();

        private static readonly HashedString RIBBON_PORT_ID = new HashedString("LogicPressureSensorRibbon");

        private static System.Collections.Generic.Dictionary<LogicPressureSensor, float> _lastSampleTimes;

        static void Postfix(LogicPressureSensor __instance)
        {
            var ports = __instance.GetComponent<LogicPorts>();
            if (!SensorMathUtils.HasRibbonPort(ports, RIBBON_PORT_ID))
                return;

            if (_lastSampleTimes == null)
                _lastSampleTimes = new System.Collections.Generic.Dictionary<LogicPressureSensor, float>();
            float now = Time.time;
            float lastSampleTime = -1f;
            _lastSampleTimes.TryGetValue(__instance, out lastSampleTime);

            // Only add a new sample if time has advanced
            if (now > lastSampleTime)
            {
                float value = __instance.CurrentValue;
                // Use the dynamic interval
                SensorMathUtils.UpdateAndGetFirstDerivative(DerivativeStates, __instance, now, value, SensorMathUtils.SamplingIntervalSeconds);
                _lastSampleTimes[__instance] = now;
            }

            float smoothedDerivative = 0.0f;
            if (DerivativeStates.TryGetValue(__instance, out var state))
                smoothedDerivative = state.ComputeMovingAverageFirstDerivative(SensorMathUtils.MovingAverageWindow);

            var inputValueComponent = __instance.GetComponent<SensorInputValueComponent>();
            float threshold = inputValueComponent != null ? inputValueComponent.parsedValue : 1.0f;

            bool bit0 = __instance.IsSwitchedOn;
            bool bit1 = smoothedDerivative > threshold;
            bool bit2 = smoothedDerivative < -threshold;

            int ribbonSignal = (bit0 ? 1 : 0)
                             | (bit1 ? (1 << 1) : 0)
                             | (bit2 ? (1 << 2) : 0);

            if (Patches.ribbonDebugEnabled)
            {
                HLib.CustomLogger.Log(
                    $"[DEBUG] RibbonSignal calculation for {__instance.name}:\n" +
                    $"  bit0 (IsSwitchedOn): {bit0}\n" +
                    $"  bit1 (smoothed dP/dt > +threshold): {bit1} (smoothedDerivative={smoothedDerivative:0.###}, threshold={threshold})\n" +
                    $"  bit2 (smoothed dP/dt < -threshold): {bit2} (smoothedDerivative={smoothedDerivative:0.###}, -threshold={-threshold})\n" +
                    $"  ribbonSignal (binary): {Convert.ToString(ribbonSignal, 2).PadLeft(4, '0')} (decimal {ribbonSignal})"
                );
            }
            ports.SendSignal(RIBBON_PORT_ID, ribbonSignal);
        }
    }

    [HarmonyPatch(typeof(LogicPressureSensorGasConfig), "DoPostConfigureComplete")]
    public static class LogicPressureGasConfig_DoPostConfigureComplete_Patch
    {
        private static readonly HashedString RIBBON_PORT_ID = new HashedString("LogicPressureSensorRibbon");

        [HarmonyPostfix]
        public static void Postfix(GameObject go)
        {
            if (go == null) return;

            var logicPorts = go.AddOrGet<LogicPorts>();
            var newPort = new LogicPorts.Port(
                RIBBON_PORT_ID,
                new CellOffset(0, 0),
                "Ribbon Output (Test)",
                "Ribbon Output Active",
                "Ribbon Output Inactive",
                true,
                LogicPortSpriteType.RibbonOutput
            );

            if (logicPorts.outputPortInfo == null)
            {
                logicPorts.outputPortInfo = new[] { newPort };
            }
            else
            {
                var ports = new List<LogicPorts.Port>(logicPorts.outputPortInfo);
                if (!ports.Exists(p => p.id == RIBBON_PORT_ID))
                    ports.Add(newPort);
                logicPorts.outputPortInfo = ports.ToArray();
            }
        }
    }

    [HarmonyPatch(typeof(LogicPressureSensorLiquidConfig), "DoPostConfigureComplete")]
    public static class LogicPressureLiquidConfig_DoPostConfigureComplete_Patch
    {
        private static readonly HashedString RIBBON_PORT_ID = new HashedString("LogicPressureSensorRibbon");

        [HarmonyPostfix]
        public static void Postfix(GameObject go)
        {
            if (go == null) return;

            var logicPorts = go.AddOrGet<LogicPorts>();
            var newPort = new LogicPorts.Port(
                RIBBON_PORT_ID,
                new CellOffset(0, 0),
                "Ribbon Output (Test)",
                "Ribbon Output Active",
                "Ribbon Output Inactive",
                true,
                LogicPortSpriteType.RibbonOutput
            );

            if (logicPorts.outputPortInfo == null)
            {
                logicPorts.outputPortInfo = new[] { newPort };
            }
            else
            {
                var ports = new List<LogicPorts.Port>(logicPorts.outputPortInfo);
                if (!ports.Exists(p => p.id == RIBBON_PORT_ID))
                    ports.Add(newPort);
                logicPorts.outputPortInfo = ports.ToArray();
            }
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
            HLib.CustomLogger.Log("[SensorSimpleInputSideScreenRegister] Registering SensorSimpleInputSideScreen.");
            // Register the side screen for both pressure and temperature sensors
            PUIUtils.AddSideScreenContent<SensorSimpleInputSideScreen>();
        }
    }

    [HarmonyPatch(typeof(LogicTemperatureSensor), "Sim200ms")]
    public static class LogicTemperatureSensor_Sim200ms_Patch
    {
        public static readonly ConditionalWeakTable<LogicTemperatureSensor, SensorMathUtils.DerivativeState<LogicTemperatureSensor>> DerivativeStates =
            new ConditionalWeakTable<LogicTemperatureSensor, SensorMathUtils.DerivativeState<LogicTemperatureSensor>>();

        private static readonly HashedString RIBBON_PORT_ID = new HashedString("ThermoSensorPlusRibbonOutput");

        static void Postfix(LogicTemperatureSensor __instance)
        {
            var ports = __instance.GetComponent<LogicPorts>();
            if (ports == null)
                return;

            float now = Time.time;
            float value = __instance.CurrentValue;

            // Use the dynamic interval
            float firstDerivative = SensorMathUtils.UpdateAndGetFirstDerivative(DerivativeStates, __instance, now, value, SensorMathUtils.SamplingIntervalSeconds);

            float smoothedDerivative = 0.0f;
            if (DerivativeStates.TryGetValue(__instance, out var derivativeState))
            {
                var samples = derivativeState.Samples.ToArray();
                smoothedDerivative = derivativeState.ComputeMovingAverageFirstDerivative(SensorMathUtils.MovingAverageWindow);
            }

            float threshold = 0.1f;
            var inputValueComponent = __instance.GetComponent<SensorInputValueComponent>();
            if (inputValueComponent != null)
                threshold = inputValueComponent.parsedValue;

            bool bit0 = __instance.IsSwitchedOn;
            bool bit1 = smoothedDerivative > threshold;
            bool bit2 = smoothedDerivative < -threshold;

            int ribbonSignal = (bit0 ? 1 : 0)
                             | (bit1 ? (1 << 1) : 0)
                             | (bit2 ? (1 << 2) : 0);

            ports.SendSignal(RIBBON_PORT_ID, ribbonSignal);
        }
    }

    [HarmonyPatch(typeof(LogicTemperatureSensorConfig), "DoPostConfigureComplete")]
    public static class ThermoSensorPatchNew
    {
        [HarmonyPostfix]
        public static void Postfix(GameObject go)
        {
            // Add ribbon port if needed (implement as in your pressure sensor logic)
            var logicPorts = go.AddOrGet<LogicPorts>();
            var portId = new HashedString("ThermoSensorPlusRibbonOutput");
            var newPort = new LogicPorts.Port(
                portId,
                new CellOffset(0, 0),
                "Ribbon Output (Temp)",
                "Ribbon Output Active",
                "Ribbon Output Inactive",
                true,
                LogicPortSpriteType.RibbonOutput
            );
            if (logicPorts.outputPortInfo == null)
                logicPorts.outputPortInfo = new[] { newPort };
            else
            {
                var ports = new List<LogicPorts.Port>(logicPorts.outputPortInfo);
                if (!ports.Exists(p => p.id == portId))
                    ports.Add(newPort);
                logicPorts.outputPortInfo = ports.ToArray();
            }
        }
    }
}
