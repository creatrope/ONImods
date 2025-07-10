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

        private static bool staticInitialized = false;

        // Change Logger field to public static
        public static readonly CustomLogger Logger = new CustomLogger("RailSensor");

        static Patches()
        {
            Logger.SetLoggingEnabled(true); // Always enable logging at startup
            Logger.Reset();

            if (staticInitialized)
            {
                return;
            }
            staticInitialized = true;

            var uniqueId = Guid.NewGuid();
            var timestamp = System.DateTime.Now.ToString("O");
            var domain = AppDomain.CurrentDomain.FriendlyName;
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public static void OnLoad()
        {
            var options = POptions.ReadSettings<ModOptions>() ?? new ModOptions();
            Logger.SetLoggingEnabled(options.EnableCustomLog);
            Logger.Reset();
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
    }
      
    public class ModOptions
    {
        [Option("Enable Custom Output Log", "Enable or disable writing the custom output log file.")]
        [JsonProperty] // Add JSON property for serialization
        public bool EnableCustomLog { get; set; } = true;
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
