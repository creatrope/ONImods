using Database;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic; // For List<>
using System.Runtime.CompilerServices;
using TUNING; // Or the correct namespace for LogicPressureSensorConfig
using UnityEngine;
using KSerialization;
using static SensorMathUtils;
using HLib; // <-- Add this using directive

namespace SensorsP
{
    public class Patches
    {
        // Change from private to public so HotkeyListenerUpdater can access it
        public static HLib.HotkeyListener hotkeyListener;

        // Add a static flag to control ribbon debug output
        public static bool ribbonDebugEnabled = false;

        static Patches()
        {
            Debug.Log("SensorsP: Patches class loaded");

            HLib.CustomLogger.Log("SensorsP: Patches class loaded.");


            // Initialize and register hotkey
            hotkeyListener = new HLib.HotkeyListener();
            hotkeyListener.RegisterHotkey("F11", () =>
            {
                ribbonDebugEnabled = !ribbonDebugEnabled;
                HLib.CustomLogger.Log($"[HotkeyListener] F11 pressed: ribbonDebugEnabled is now {(ribbonDebugEnabled ? "ON" : "OFF")}");
            });

            // Register for Unity update loop
            HotkeyListenerUpdater.Create();
        }

        public static void OnLoad()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(SensorsP.Patches).TypeHandle);
        }

        [HarmonyPatch(typeof(Db))]
        [HarmonyPatch("Initialize")]
        public class Db_Initialize_Patch
        {
            public static void Prefix()
            {
                HLib.CustomLogger.LogPath = System.IO.Path.Combine(
                  System.IO.Path.GetDirectoryName(HLib.CustomLogger.LogPath),
                "SensorsP.log"
                );
                Debug.Log($"[SensorsP] Using log path: {HLib.CustomLogger.LogPath}");
                HLib.CustomLogger.Log("SensorsP: Prefix.");
            }

            public static void Postfix()
            {
                HLib.CustomLogger.Log("SensorsP: Postfix.");
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
                var go = new GameObject("SensorsP_HotkeyListenerUpdater");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<HotkeyListenerUpdater>();
            }
            HLib.CustomLogger.Log("HotkeyListenerUpdater.Create called");
        }

        void Update()
        {
            // Gather pressed keys (example for F11, expand as needed)
            var pressed = new System.Collections.Generic.List<string>();
            bool f11Down = Input.GetKey(KeyCode.F11);
            bool f11JustPressed = Input.GetKeyDown(KeyCode.F11);

            if (f11Down)
                pressed.Add("F11");

            // Call the static hotkeyListener
            if (Patches.hotkeyListener != null)
            {
                Patches.hotkeyListener.Update(pressed);
            }
            else
            {
                HLib.CustomLogger.Log("[HotkeyListenerUpdater] Patches.hotkeyListener is null.");
            }

            // Only log when F11 is actually pressed
            if (f11JustPressed)
            {
                HLib.CustomLogger.Log("[HotkeyListenerUpdater] F11 pressed (Input.GetKeyDown).");
            }
        }
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Debug.Log("SensorsP: Mod.OnLoad called.");

            // Set log path before resetting log, so the correct file is overwritten
            HLib.CustomLogger.LogPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(HLib.CustomLogger.LogPath),
                "SensorsP.log"
            );
            HLib.CustomLogger.ResetLog(); // Now this will reset the correct log file

            SensorsP.Patches.OnLoad(); // <-- Ensure hotkey system is initialized
            base.OnLoad(harmony);

            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(ModOptions));
            harmony.PatchAll();
        }
    }

    [ConfigFile("modoptions.yaml", true, true)]
    public class ModOptions
    {
        // Define your options here, e.g.:
        public bool ExampleOption { get; set; } = true;
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
    public static class LogicPressureSensor_Sim200ms_Patch
    {
        public static readonly ConditionalWeakTable<LogicPressureSensor, SensorMathUtils.DerivativeState<LogicPressureSensor>> DerivativeStates =
            new ConditionalWeakTable<LogicPressureSensor, SensorMathUtils.DerivativeState<LogicPressureSensor>>();

        private static readonly HashedString RIBBON_PORT_ID = new HashedString("LogicPressureSensorRibbon");

        // Use the shared sampling interval constant
        private const float T = SensorMathUtils.SamplingIntervalSeconds;

        // Add this static field to the LogicPressureSensor_Sim200ms_Patch class
        private static System.Collections.Generic.Dictionary<LogicPressureSensor, float> _lastSampleTimes;

        static void Postfix(LogicPressureSensor __instance)
        {
            var ports = __instance.GetComponent<LogicPorts>();
            if (!SensorMathUtils.HasRibbonPort(ports, RIBBON_PORT_ID))
                return;

            // Use a static dictionary to track last sample time per sensor to avoid duplicate samples per frame
            if (_lastSampleTimes == null)
                _lastSampleTimes = new System.Collections.Generic.Dictionary<LogicPressureSensor, float>();
            float now = Time.time;
            float lastSampleTime = -1f;
            _lastSampleTimes.TryGetValue(__instance, out lastSampleTime);

            // Only add a new sample if time has advanced
            if (now > lastSampleTime)
            {
                float value = __instance.CurrentValue;
                //HLib.CustomLogger.Log($"[Sim200ms_Patch] Adding sample: time={now}, value={value} for {__instance.name}");
                float firstDerivative = SensorMathUtils.UpdateAndGetFirstDerivative(DerivativeStates, __instance, now, value, T);
                _lastSampleTimes[__instance] = now;
            }
            else
            {
                //HLib.CustomLogger.Log($"[Sim200ms_Patch] Skipped adding sample for {__instance.name} (duplicate time: {now})");
            }

            if (DerivativeStates.TryGetValue(__instance, out var state))
            {
                var samples = state.Samples.ToArray();
                //HLib.CustomLogger.Log($"[Sim200ms_Patch] DerivativeStates sample count for {__instance.name}: {samples.Length}");
                if (samples.Length > 0)
                {
                    var last = samples[samples.Length - 1];
                    //HLib.CustomLogger.Log($"[Sim200ms_Patch] Last sample: time={last.time}, value={last.value}");
                }
            }

            float smoothedDerivative = 0.0f;
            if (DerivativeStates.TryGetValue(__instance, out var derivativeState))
                smoothedDerivative = derivativeState.GetSmoothedDerivative(3);

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
        public static void Postfix()
        {
            HLib.CustomLogger.Log("[SensorSimpleInputSideScreenRegister] Registering SensorSimpleInputSideScreen.");
            PUIUtils.AddSideScreenContent<SensorSimpleInputSideScreen>();
            // No need to register a separate temperature side screen.
        }
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public class SensorInputValueComponent : KMonoBehaviour
    {
        [Serialize]
        public string inputValue = "1.0";

        [NonSerialized]
        public float parsedValue = 1.0f;
    }

    [HarmonyPatch(typeof(LogicTemperatureSensor), "Sim200ms")]
    public static class LogicTemperatureSensor_Sim200ms_Patch
    {
        public static readonly ConditionalWeakTable<LogicTemperatureSensor, SensorMathUtils.DerivativeState<LogicTemperatureSensor>> DerivativeStates =
            new ConditionalWeakTable<LogicTemperatureSensor, SensorMathUtils.DerivativeState<LogicTemperatureSensor>>();

        private static readonly HashedString RIBBON_PORT_ID = new HashedString("ThermoSensorPlusRibbonOutput");
        private const float T = 0.2f; // 0.2 second, matches Sim200ms

        static void Postfix(LogicTemperatureSensor __instance)
        {
            var ports = __instance.GetComponent<LogicPorts>();
            if (ports == null)
                return;

            float now = Time.time;
            float value = __instance.CurrentValue;
            float firstDerivative = SensorMathUtils.UpdateAndGetFirstDerivative(DerivativeStates, __instance, now, value, T);

            // Use smoothed derivative for ribbon logic
            float smoothedDerivative = 0.0f;
            if (DerivativeStates.TryGetValue(__instance, out var derivativeState))
            {
                var samples = derivativeState.Samples.ToArray();
                smoothedDerivative = derivativeState.GetSmoothedDerivative(3);
            }

            // Optionally, get threshold from a component (for symmetry with pressure)
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
