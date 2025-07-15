using Database;
using Epic.OnlineServices.Platform;
using HarmonyLib;
using HLib;
using KMod;
using KSerialization;
using Newtonsoft.Json; // Ensure this using directive is present
using Klei; // Add this using directive for access to PrimaryElement
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic; // For List<> and Dictionary<>
using System.Runtime.CompilerServices; // For ConditionalWeakTable
using System.Threading;
using TUNING;
using UnityEngine;
using static Rendering.BlockTileRenderer;
using static GameClock; // Add this directive to access GameClock functionality

namespace OverheatControl
{
    public class Patches
    {
        // Change from private to public so HotkeyListenerUpdater can access it
        public static HLib.HotkeyListener hotkeyListener;

        // Add a guard to prevent double static initialization
        private static bool staticInitialized = false;

        // Change Logger field to public static
        public static readonly CustomLogger Logger = new CustomLogger("OverheatControl");

        // Add static variables to store the settings
        public static float ShutdownPercent { get; private set; }
        public static float RestorePercent { get; private set; }

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
                KSelectable selected = SelectTool.Instance?.selected;
                if (selected != null)
                    PopUpMessage("TestMessage", "Hotkey triggered on selected object!", selected.gameObject);
                else
                    Debug.Log("[Hotkey] No object selected.");
            });

            hotkeyListener.RegisterHotkey("Ctrl+F2", () =>
            {
                List<string> values = new List<string>();
                foreach (BuildingDef buildingDef in Assets.BuildingDefs)
                {
                    if (IsOverheatableAndPowered(buildingDef))
                        values.Add(buildingDef.PrefabID);
                }
                Logger.Log("[Hotkey] Overheatable and powered building IDs captured: " + string.Join(", ", values));
            });

            hotkeyListener.RegisterHotkey("Ctrl+F3", () =>
            {
                List<string> values = new List<string>();
                foreach (BuildingDef buildingDef in Assets.BuildingDefs)
                {
                    if (!buildingDef.Overheatable && buildingDef.BuildingComplete.GetComponent<IEnergyConsumer>() != null)
                        values.Add(buildingDef.PrefabID);
                }
                Logger.Log("[Hotkey] Non-overheatable powered building IDs captured: " + string.Join(", ", values));
            });

            // Register for Unity update loop
            HotkeyListenerUpdater.Create();
        }

        public static void OnLoad()
        {
            var options = POptions.ReadSettings<ModOptions>() ?? new ModOptions();
            Patches.Logger.SetLoggingEnabled(options.EnableCustomLog);
            Patches.Logger.Reset();

            // Read settings and store them in static variables for global access
            ShutdownPercent = options.ShutdownPercent;
            RestorePercent = options.RestorePercent;

            // Serialize options to JSON and log them
            string optionsJson = JsonConvert.SerializeObject(options, Formatting.Indented);
            Patches.Logger.Log($"[OverheatControl] Settings loaded:\n{optionsJson}");
        }

        // Add a method to log cycle changes
        public static void LogCycleChanges()
        {
            if (GameClock.Instance != null)
            {
                int currentCycle = GameClock.Instance.GetCycle();
                float timeSinceStartOfCycle = GameClock.Instance.GetTimeSinceStartOfCycle();
                //Logger.Log($"[GameClock] Current Cycle: {currentCycle}, Time Since Start of Cycle: {timeSinceStartOfCycle}");
            }
            else
            {
                Logger.Log("[GameClock] Instance is null.");
            }
        }

        public static void PopUpMessage(string internalName, string message, GameObject gameObject)
        {
            if (PopFXManager.Instance != null)
                PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Plus, message, gameObject.transform, new Vector3(0.0f, 0.0f, 0.0f), 2f);
            else
                Logger.Log("[PopUpMessage] PopFXManager.Instance is null.");
        }

        // Add this utility method to the Patches class
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
                    //Patches.Logger.Log($"[BuildingDef] Overheatable building detected: {__instance.Name}, OverheatTemperature: {__instance.OverheatTemperature}");
                }
            }
        }

        [HarmonyPatch(typeof(GameClock), "AddTime")]
        public class GameClock_AddTime_Patch
        {
            public static void Postfix(float dt)
            {
                // Log cycle changes whenever time is added
                LogCycleChanges();
            }
        }

        [HarmonyPatch(typeof(GameClock), "OnPrefabInit")]
        public class GameClock_OnPrefabInit_Patch
        {
            public static void Postfix()
            {
                Logger.Log("[GameClock] Initialized.");
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
                var go = new GameObject("HotKeyListenerUpdater");
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
        [JsonProperty]
        public bool EnableCustomLog { get; set; }

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

    // Add this enum to represent building states
    public enum BuildingState
    {
        Active,
        Cooling,
    }

    // Patch to attach the custom MonoBehaviour to buildings
    [HarmonyPatch(typeof(Building), "OnSpawn")]
    public class Building_OnSpawn_Patch
    {
        public static void Postfix(Building __instance)
        {
            // Only attach if building is overheatable and powered, and monitor is not already present
            if (!Patches.IsOverheatableAndPowered(__instance.Def) ||
                __instance.gameObject.GetComponent<BuildingTemperatureMonitor>() != null)
                return;
            __instance.gameObject.AddComponent<BuildingTemperatureMonitor>().Initialize(__instance);
        }
    }
}
