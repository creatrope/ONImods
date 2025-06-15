using HarmonyLib;
using PeterHan.PLib.Options;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic; // For List<>
using TUNING; // Or the correct namespace for LogicPressureSensorConfig
using UnityEngine;
using Database;


namespace SensorsP
{
    public class Patches
    {
        static Patches()
        {
            CustomLogger.CustomLogger.Log("SensorsP: Patches class loaded.");
        }

        public static void OnLoad()
        {
            var _ = typeof(SensorsP.Patches); // This will trigger the static constructor

            // Demonstrate PLib integration: enable PLib logging
            CustomLogger.CustomLogger.Log("PLib options enabled.");
            CustomLogger.CustomLogger.ResetLog();
        }

        [HarmonyPatch(typeof(Db))]
        [HarmonyPatch("Initialize")]
        public class Db_Initialize_Patch
        {
            public static void Prefix()
            {
                Debug.Log("[SensorsP] execute before Db.Initialize!");
                CustomLogger.CustomLogger.LogPath = System.IO.Path.Combine(
                  System.IO.Path.GetDirectoryName(CustomLogger.CustomLogger.LogPath),
                "SensorsP.log"
                );
                Debug.Log($"[SensorsP] Using log path: {CustomLogger.CustomLogger.LogPath}");
                CustomLogger.CustomLogger.Log("SensorsP: Prefix.");
            }

            public static void Postfix()
            {
                Debug.Log("[SensorsP] I execute after Db.Initialize!");
                CustomLogger.CustomLogger.Log("SensorsP: Postfix.");
            }
        }
    }

    public class ModEntry : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
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
        private static readonly HashedString RIBBON_PORT_ID = new HashedString("LogicPressureSensorRibbon");
        private const float T = 1.0f; // seconds
        private const float H = 1.0f; // threshold

        private class DerivativeState
        {
            public Queue<(float time, float value)> Samples = new Queue<(float, float)>();
        }

        private static readonly ConditionalWeakTable<LogicPressureSensor, DerivativeState> DerivativeStates =
            new ConditionalWeakTable<LogicPressureSensor, DerivativeState>();

        static void Postfix(LogicPressureSensor __instance)
        {
            var ports = __instance.GetComponent<LogicPorts>();
            // Check if the ribbon port is present and connected
            if (ports == null || ports.outputPortInfo == null ||
                Array.FindIndex(ports.outputPortInfo, p => p.id == RIBBON_PORT_ID) < 0)
            {
                // No ribbon port, suppress calculation and debug output
                return;
            }

            var state = DerivativeStates.GetOrCreateValue(__instance);

            float now = Time.time;
            float value = __instance.CurrentValue;

            state.Samples.Enqueue((now, value));

            while (state.Samples.Count > 0 && now - state.Samples.Peek().time > T)
                state.Samples.Dequeue();

            float firstDerivative = 0f;
            if (state.Samples.Count >= 2)
            {
                var oldest = state.Samples.Peek();
                var newest = state.Samples.ToArray()[state.Samples.Count - 1];
                float dt = newest.time - oldest.time;
                if (dt > 0.0001f)
                    firstDerivative = (newest.value - oldest.value) / dt;
            }

            // Bit logic
            bool bit0 = __instance.IsSwitchedOn;
            bool bit1 = firstDerivative > H;
            bool bit2 = firstDerivative < -H;
            bool bit3 = false; // Always off

            int ribbonSignal = (bit0 ? 1 : 0)
                             | (bit1 ? (1 << 1) : 0)
                             | (bit2 ? (1 << 2) : 0);

            // Detailed debug output
            CustomLogger.CustomLogger.Log(
                $"[PATCH DEBUG] Ribbon output: {Convert.ToString(ribbonSignal, 2).PadLeft(4, '0')} (decimal {ribbonSignal})\n" +
                $"  Bit 0 (IsSwitchedOn): {bit0}\n" +
                $"  Bit 1 (dP/dt > +H):   {bit1} (FirstDerivative={firstDerivative:0.###}, H={H})\n" +
                $"  Bit 2 (dP/dt < -H):   {bit2} (FirstDerivative={firstDerivative:0.###}, -H={-H})\n" +
                $"  Bit 3 (always off):   {bit3}");

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
            CustomLogger.CustomLogger.Log("[DEBUG] LogicPressureSensorGasConfig.DoPostConfigureComplete patch executed.");
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
            CustomLogger.CustomLogger.Log("[DEBUG] LogicPressureSensorLiquidConfig.DoPostConfigureComplete patch executed.");
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

    // Repeat for other sensor types as needed...

    // Optionally, patch the UI to allow switching output type
    // This is more advanced and may require patching side screen or config methods
}
