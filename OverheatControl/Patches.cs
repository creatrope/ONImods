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
                Debug.Log("Hotkey Pressed!");
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
        [JsonProperty] // Add JSON property for serialization
        public bool EnableCustomLog { get; set; } = false;

        [Option("Max %", "Shutdown @ % of Overheat Temp")]
        [Limit(5, 100)]
        [JsonProperty] // Add JSON property for serialization
        public float ShutdownPercent { get; set; } = 50.0f;
        [Option("Min %", "Restore % of Overheat Temp")]
        [Limit(5, 100)]
        [JsonProperty] // Add JSON property for serialization
        public float RestorePercent { get; set; } = 40.0f;
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            OverheatControl.Patches.OnLoad(); // <-- Ensure hotkey system is initialized
            base.OnLoad(harmony);

            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(ModOptions));
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class SideScreenRegister
    {
        private static bool registered = false;

        public static void Postfix()
        {
            if (registered) return;
            registered = true;
            PUIUtils.AddSideScreenContent<SimpleSideScreen>();
        }
    }

    // Custom MonoBehaviour to run logic continuously
    public class BuildingTemperatureMonitor : MonoBehaviour
    {
        private Building building;
        private bool isOverheatable;

        // Use a per-instance check time instead of a global one
        private float lastCheckTime = 0f;

        // Add a state variable to track the building's state
        private string state = "active"; // Possible values: "active", "cooling"

        public void Initialize(Building building)
        {
            this.building = building;
            this.isOverheatable = building.Def.Overheatable; // Check if the building is overheatable
        }

        void Update()
        {
            // Verify GameClock.Instance is not null and GetTime is updating correctly  
            if (GameClock.Instance != null)
            {
                float currentTime = GameClock.Instance.GetTime();

                // Use a per-instance check time instead of a global one
                if (currentTime - lastCheckTime < 5) // Check every 5 seconds  
                    return;

                lastCheckTime = currentTime; // Update the per-instance check time
            }
            else
            {
                Patches.Logger.Log("[BuildingTemperatureMonitor] GameClock.Instance is null.");
                return;
            }

            if (building != null && isOverheatable) // Only process logic for overheatable buildings  
            {
                var def = building.Def;
                var kSelectable = building.gameObject.GetComponent<KSelectable>();
                string name = kSelectable != null ? kSelectable.GetProperName() : def.Name; // Fallback to def.Name if KSelectable is unavailable
                name = name.Replace("<link=\"BATTERYMEDIUM\">", "").Replace("</link>", "");

                // Return early if the building is not a BatteryMedium prefab  
                if (building.PrefabID() != BatteryMediumConfig.ID)
                    return;

                int instanceId = building.GetInstanceID();

                var primaryElement = building.gameObject.GetComponent<PrimaryElement>();
                if (primaryElement != null)
                {
                    float overheat = def.OverheatTemperature - 273.15f; // Convert Kelvin to Celsius
                    float shutdownThreshold = overheat * (Patches.ShutdownPercent / 100f);
                    float restoreThreshold = overheat * (Patches.RestorePercent / 100f);

                    float currentTemperature = primaryElement.Temperature - 273.15f; // Convert Kelvin to Celsius

                    //Patches.Logger.Log($"[Building] Checking Instance: {name}({instanceId}), {currentTemperature} vs overheat@{overheat}");

                    if (state == "active" && currentTemperature >= shutdownThreshold)
                    {
                        building.Def.EnergyConsumptionWhenActive = 0f;
                        state = "cooling";
                        Patches.Logger.Log($"[Building] Shutdown triggered: {name}({instanceId}), {currentTemperature} > {shutdownThreshold}. State set to 'cooling'.");
                    }
                    else if (state == "cooling")
                    {
                        if (currentTemperature <= restoreThreshold)
                        {
                            building.Def.EnergyConsumptionWhenActive = def.GeneratorWattageRating;
                            state = "active";
                            Patches.Logger.Log($"[Building] Restored: {name}({instanceId}), {currentTemperature} <= {restoreThreshold}. State set to 'active'.");
                        }
                        else
                        {
                            Patches.Logger.Log($"[Building] Cooling continues: {name}({instanceId}), {currentTemperature} > {restoreThreshold}. State remains 'cooling'.");
                            return;
                        }
                    }
                }
            }
        }
    }

    // Patch to attach the custom MonoBehaviour to buildings
    [HarmonyPatch(typeof(Building), "OnSpawn")]
    public class Building_OnSpawn_Patch
    {
        public static void Postfix(Building __instance)
        {
            // Check if the building is a BatteryMedium before attaching and logging
            if (__instance.PrefabID() == BatteryMediumConfig.ID)
            {
                var monitor = __instance.gameObject.AddComponent<BuildingTemperatureMonitor>();
                monitor.Initialize(__instance);

                // Add logging to confirm attachment timing and print InstanceID
                Patches.Logger.Log($"[Building_OnSpawn_Patch] Attempting to attach BuildingTemperatureMonitor to: {__instance.name}, InstanceID: {monitor.GetInstanceID()}");

                if (__instance.gameObject.GetComponent<BuildingTemperatureMonitor>() != null)
                {
                    Patches.Logger.Log($"[Building_OnSpawn_Patch] BuildingTemperatureMonitor successfully attached to: {__instance.name}, InstanceID: {monitor.GetInstanceID()}");
                }
                else
                {
                    Patches.Logger.Log($"[Building_OnSpawn_Patch] Failed to attach BuildingTemperatureMonitor to: {__instance.name}");
                }
            }
        }
    }
}
