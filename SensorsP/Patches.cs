using HarmonyLib;
using PeterHan.PLib.Options;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

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
        static void Prefix(LogicPressureSensor __instance, float dt)
        {
            CustomLogger.CustomLogger.Log(
                $"[PATCH DEBUG] Patched LogicPressureSensor.Sim200ms called. Pressure: {__instance.CurrentValue}, IsSwitchedOn: {__instance.IsSwitchedOn}");
            // Add your custom logic here
        }
    }

    // Repeat for other sensor types as needed...

    // Optionally, patch the UI to allow switching output type
    // This is more advanced and may require patching side screen or config methods
}
