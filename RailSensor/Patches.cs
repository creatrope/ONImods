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
using TUNING;
using UnityEngine;
using static Rendering.BlockTileRenderer;
using static STRINGS.ELEMENTS;

namespace RailSensor
{
    public class Patches
    {
        // Change from private to public so HotkeyListenerUpdater can access it
        public static readonly HLib.HotkeyListener hotkeyListener;

        // Add a guard to prevent double static initialization
        private static bool staticInitialized = false;

        // Change Logger field to public static
        public static readonly CustomLogger Logger = new CustomLogger("RailSensor");

        static Patches()
        {
            Logger.SetLoggingEnabled(true); // Always enable logging at startup
            Logger.Reset();
            Logger.Log("CustomLogger initialized and enabled for RailSensor.");

            if (staticInitialized)
            {
                Logger.Log("Patches static constructor: already initialized, skipping.");
                return;
            }
            staticInitialized = true;

            var uniqueId = Guid.NewGuid();
            var timestamp = System.DateTime.Now.ToString("O");
            var domain = AppDomain.CurrentDomain.FriendlyName;
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Logger.Log($"Patches static constructor: uniqueId={uniqueId}, timestamp={timestamp}, domain={domain}, threadId={threadId}");

            // Initialize and register hotkeys
            hotkeyListener = new HLib.HotkeyListener();
            Logger.Log("HotkeyListener created.");

            hotkeyListener.RegisterHotkey("Ctrl+F11", () =>
            {
                Logger.Log("Hotkey Ctrl+F11 Pressed!");
                Debug.Log("Hotkey Pressed!");
            });

            // Register for Unity update loop
            HotkeyListenerUpdater.Create();
            Logger.Log("HotkeyListenerUpdater.Create called from Patches static constructor.");
        }

        public static void OnLoad()
        {
            Logger.Log("OnLoad called.");
            var options = POptions.ReadSettings<ModOptions>() ?? new ModOptions();
            Logger.Log($"ModOptions loaded: EnableCustomLog={options.EnableCustomLog}, MaxPercent={options.MaxPercent}, MinPercent={options.MinPercent}");
            Logger.SetLoggingEnabled(options.EnableCustomLog);
            Logger.Log($"Logger.SetLoggingEnabled({options.EnableCustomLog}) called.");
            Logger.Reset();
            Logger.Log("Logger.Reset() called.");
        }


        [HarmonyPatch(typeof(Db), "Initialize")]
        public class Db_Initialize_Patch
        {
            public static void Prefix()
            {
                Logger.Log("Db.Initialize Prefix called.");
            }

            public static void Postfix()
            {
                Logger.Log("Db.Initialize Postfix called.");
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
                Patches.Logger.Log("HotkeyListenerUpdater instance created and attached to GameObject.");
            }
            else
            {
                Patches.Logger.Log("HotkeyListenerUpdater.Create called but instance already exists.");
            }
        }

        void Update()
        {
            if (Patches.hotkeyListener != null)
            {
                //Patches.Logger.Log("HotkeyListenerUpdater.Update: calling hotkeyListener.Update().");
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
        public bool EnableCustomLog { get; set; } = true;

        [Option("Max %", "Turn Off % of Overheat Temp")]
        [Limit(5, 100)]
        [JsonProperty] // Add JSON property for serialization
        public float MaxPercent { get; set; } = 90.0f;
        [Option("Min %", "Turn Back On % of Overheat Temp")]
        [Limit(5, 100)]
        [JsonProperty] // Add JSON property for serialization
        public float MinPercent { get; set; } = 80.0f;
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            RailSensor.Patches.OnLoad(); // <-- Ensure hotkey system is initialized
            base.OnLoad(harmony);

            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(ModOptions));
            harmony.PatchAll();
            Patches.Logger.Log("Mod.OnLoad finished: PUtil.InitLibrary, options registered, harmony patched.");
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class SideScreenRegister
    {
        private static bool registered = false; // Change from readonly to a regular static field

        public static void Postfix()
        {
            if (registered)
            {
                return;
            }
            registered = true; // This assignment is now valid
            PUIUtils.AddSideScreenContent<SimpleSideScreen>();
        }
    }

    [HarmonyPatch(typeof(ConduitElementSensor), "ConduitUpdate")]
    public static class ConduitElementSensor_ConduitUpdate_Patch
    {
        public static bool Prefix(ConduitElementSensor __instance, float dt)
        {
            bool trigger = false;

            var filterable = Traverse.Create(__instance).Field("filterable").GetValue<Filterable>();
            Tag selectedTag = filterable != null ? filterable.SelectedTag : Tag.Invalid;

            // Replicate the original detection logic
            Tag element = GameTags.Void;
            bool hasMass = false;

            // Try to get the cell and conduit type
            var traverse = Traverse.Create(__instance);
            object cellObj = traverse.Field("utilityCell").GetValue();
            if (cellObj == null || (cellObj is int && (int)cellObj == 0))
                cellObj = traverse.Field("conduitCell").GetValue();
            if (cellObj == null || (cellObj is int && (int)cellObj == 0))
                cellObj = traverse.Field("cell").GetValue();
            // Fallback: get cell from GameObject position if still null or zero
            if (cellObj == null || (cellObj is int && (int)cellObj == 0))
            {
                var go = Traverse.Create(__instance).Property("gameObject").GetValue() as GameObject;
                if (go != null)
                    cellObj = Grid.PosToCell(go);
            }
            object conduitTypeObj = traverse.Field("conduitType").GetValue();

            if (cellObj != null && conduitTypeObj != null)
            {
                int cell = (cellObj is int) ? (int)cellObj : -1;
                var conduitType = (ConduitType)conduitTypeObj;

                if (conduitType == ConduitType.Solid)
                {
                    var flowManager = SolidConduit.GetFlowManager();
                    var solidContents = flowManager.GetContents(cell);
                    var handle = solidContents.pickupableHandle;
                    var pickupable = flowManager.GetPickupable(handle);
                    if (pickupable)
                    {
                        element = pickupable.PrimaryElement != null ? pickupable.PrimaryElement.Element.tag : Tag.Invalid;
                        Tag at = Filterable_GetTagOptions_Patch.AnythingTag;

                        trigger = (element == selectedTag || selectedTag == at);
                        Patches.Logger.Log($"pickupable={element} {selectedTag} {trigger}");
                    }
                }
            }

            if (trigger)
            {
                Traverse.Create(__instance).Method("SetState", true).GetValue();
            }
            else
            {
                Traverse.Create(__instance).Method("SetState", false).GetValue();
            }

            //Return false to skip the original method if you want to fully override it,
            // or true to let the original run after this.
            return false;
        }

        public static void Postfix(ConduitElementSensor __instance) { }
    }

    [HarmonyPatch(typeof(Filterable), "GetTagOptions")]
    public static class Filterable_GetTagOptions_Patch
    {
        // Make the "Anything" tag static for reuse and testing
        public static readonly Tag AnythingTag = new Tag("Anything");

        public static void Postfix(Filterable __instance, ref Dictionary<Tag, HashSet<Tag>> __result)
        {
            if (!__result.ContainsKey(AnythingTag))
                __result.Add(AnythingTag, new HashSet<Tag> { AnythingTag });
        }
    }
}
