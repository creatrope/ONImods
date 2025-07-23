using Database;
using HarmonyLib;
using KMod;
using KSerialization;
using Newtonsoft.Json; 
using Klei; 
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic; // For List<> and Dictionary<>
using System.Threading;
using HLib;
using TUNING;
using UnityEngine;
using static GameClock; // Add this directive to access GameClock functionality

namespace OverheatControl
{
    public class Patches
    {
        // Add static variables to store the settings
        public static float ShutdownPercent { get; private set; }
        public static float RestorePercent { get; private set; }

        private static bool staticInitialized = false;

        public static HLib.Logger logger = new HLib.Logger("OverheatControl");
        static Patches()
        {
            if (staticInitialized)
                return;
            staticInitialized = true;
        }

        public static void OnLoad()
        {
            // Determine if we are in the "Local" mod directory
            string modDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            bool enableCustomLog = modDir != null && modDir.IndexOf("Local", StringComparison.OrdinalIgnoreCase) >= 0;

            ShutdownPercent = POptions.ReadSettings<ModOptions>()?.ShutdownPercent ?? 90f;
            RestorePercent = POptions.ReadSettings<ModOptions>()?.RestorePercent ?? 80f;

            // Set logger state based on directory
            Patches.logger.SetLoggingState(enableCustomLog);

            string optionsJson = JsonConvert.SerializeObject(new { ShutdownPercent, RestorePercent, enableCustomLog }, Formatting.Indented);
            Patches.logger.LogDebug($"Settings loaded:\n{optionsJson}");
        }

        public static void LogCycleChanges()
        {
            if (GameClock.Instance != null)
            {
                int currentCycle = GameClock.Instance.GetCycle();
                float timeSinceStartOfCycle = GameClock.Instance.GetTimeSinceStartOfCycle();
            }
            else
            {
                Patches.logger.LogDebug("GameClock Instance is null.");
            }
        }

        public static void PopUpMessage(string internalName, string message, GameObject gameObject)
        {
            if (PopFXManager.Instance != null)
                PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Plus, message, gameObject.transform, new Vector3(0.0f, 0.0f, 0.0f), 2f);
            else
                Patches.logger.LogDebug("PopFXManager.Instance is null.");
        }

        public static bool IsOverheatableAndPowered(BuildingDef def)
        {
            return def != null && def.Overheatable && def.BuildingComplete.GetComponent<IEnergyConsumer>() != null;
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        public class Db_Initialize_Patch
        {
            public static void Prefix()
            {
            }

            public static void Postfix()
            {
            }
        }

        [HarmonyPatch(typeof(BuildingDef), "PostProcess")]
        public class BuildingDef_PostProcess_Patch
        {
            public static void Postfix(BuildingDef __instance)
            {
                if (__instance.Overheatable)
                {
                }
            }
        }

        [HarmonyPatch(typeof(GameClock), "AddTime")]
        public class GameClock_AddTime_Patch
        {
            public static void Postfix(float dt)
            {
                LogCycleChanges();
            }
        }

        [HarmonyPatch(typeof(GameClock), "OnPrefabInit")]
        public class GameClock_OnPrefabInit_Patch
        {
            public static void Postfix()
            {
                Patches.logger.LogDebug("GameClock Initialized.");
            }
        }
    }

    public class ModOptions
    {
        [JsonProperty]
        [Option("Shutdown %", "Shutdown @ % of Overheat Temp")]
        [Limit(5.0, 100.0)]
        public float ShutdownPercent { get; set; } = 90f;

        [Option("Restore %", "Restore % of Overheat Temp")]
        [Limit(5.0, 100.0)]
        [JsonProperty]
        public float RestorePercent { get; set; } = 80f;
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Patches.OnLoad();
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(ModOptions));
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class SideScreenRegister
    {
        private static bool registered;

        public static void Postfix()
        {
            if (registered)
                return;
            registered = true;
            PUIUtils.AddSideScreenContent<SimpleSideScreen>();
        }
    }

    public enum BuildingState
    {
        Active,
        Cooling,
    }

    [HarmonyPatch(typeof(Building), "OnSpawn")]
    public class Building_OnSpawn_Patch
    {
        public static void Postfix(Building __instance)
        {
            if (!Patches.IsOverheatableAndPowered(__instance.Def) ||
                __instance.gameObject.GetComponent<BuildingTemperatureMonitor>() != null)
                return;
            __instance.gameObject.AddComponent<BuildingTemperatureMonitor>().Initialize(__instance);
        }
    }
}
