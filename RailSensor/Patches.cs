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
        public static readonly Tag AnythingTag = new Tag("Anything");

        public static void Postfix(Filterable __instance, ref Dictionary<Tag, HashSet<Tag>> __result)
        {
            RailSensor.Patches.Logger.Log($"[GetTagOptions] Called for Filterable={__instance?.GetHashCode() ?? -1}");
            var owner = FilterableOwnerTracker.GetOwner(__instance);
            RailSensor.Patches.Logger.Log($"[GetTagOptions] Owner={owner?.GetType().FullName ?? "null"} for Filterable={__instance?.GetHashCode() ?? -1}");
            if (owner is ConduitElementSensor /* || owner is ElementSensor, etc. */)
            {
                if (!__result.ContainsKey(AnythingTag))
                {
                    RailSensor.Patches.Logger.Log($"[GetTagOptions] Adding AnythingTag for Filterable={__instance?.GetHashCode() ?? -1}");
                    __result.Add(AnythingTag, new HashSet<Tag> { AnythingTag });
                }
                else
                {
                    RailSensor.Patches.Logger.Log($"[GetTagOptions] AnythingTag already present for Filterable={__instance?.GetHashCode() ?? -1}");
                }
            }
            else
            {
                RailSensor.Patches.Logger.Log($"[GetTagOptions] Owner is not a supported sensor for Filterable={__instance?.GetHashCode() ?? -1}");
            }
        }
    }

    [HarmonyPatch(typeof(Filterable), "OnPrefabInit")]
    public static class Filterable_OnPrefabInit_Patch
    {
        public static void Postfix(Filterable __instance)
        {
            RailSensor.Patches.Logger.Log($"[Filterable.OnPrefabInit] Called for Filterable={__instance.GetHashCode()}");
            // Try to find a ConduitElementSensor on the same GameObject
            var sensor = __instance.GetComponent<ConduitElementSensor>();
            if (sensor != null)
            {
                RailSensor.Patches.Logger.Log($"[Filterable.OnPrefabInit] Found ConduitElementSensor={sensor.GetHashCode()} for Filterable={__instance.GetHashCode()}");
                RailSensor.FilterableOwnerTracker.SetOwner(__instance, sensor);
            }
            else
            {
                RailSensor.Patches.Logger.Log($"[Filterable.OnPrefabInit] No ConduitElementSensor found for Filterable={__instance.GetHashCode()}");
            }
        }
    }

    // Add debug logging to the owner tracker
    public static class FilterableOwnerTracker
    {
        private static readonly ConditionalWeakTable<Filterable, object> Owners = new ConditionalWeakTable<Filterable, object>();

        public static void SetOwner(Filterable filterable, object owner)
        {
            RailSensor.Patches.Logger.Log($"[SetOwner] Called with Filterable={filterable?.GetHashCode() ?? -1}, Owner={owner?.GetType().FullName ?? "null"}");
            if (filterable != null && owner != null)
            {
                Owners.Remove(filterable);
                Owners.Add(filterable, owner);
                RailSensor.Patches.Logger.Log($"[SetOwner] Set owner for Filterable={filterable.GetHashCode()} to {owner.GetType().FullName}");
            }
            else
            {
                RailSensor.Patches.Logger.Log($"[SetOwner] Skipped: filterable or owner is null");
            }
        }

        public static object GetOwner(Filterable filterable)
        {
            if (filterable == null)
            {
                RailSensor.Patches.Logger.Log("[GetOwner] filterable is null");
                return null;
            }
            Owners.TryGetValue(filterable, out var owner);
            RailSensor.Patches.Logger.Log($"[GetOwner] For Filterable={filterable.GetHashCode()} got Owner={owner?.GetType().FullName ?? "null"}");
            return owner;
        }
    }

    // Add debug logging to each config patch
    [HarmonyPatch(typeof(GasConduitElementSensorConfig), "DoPostConfigureComplete")]
    public static class GasConduitElementSensorConfig_DoPostConfigureComplete_Patch
    {
        public static void Postfix(GameObject go)
        {
            RailSensor.Patches.Logger.Log("[GasConduitElementSensorConfig] DoPostConfigureComplete called");
            var filterable = go.GetComponent<Filterable>();
            var sensor = go.GetComponent<ConduitElementSensor>();
            RailSensor.Patches.Logger.Log($"[GasConduitElementSensorConfig] filterable={filterable?.GetHashCode() ?? -1}, sensor={sensor?.GetHashCode() ?? -1}");
            if (filterable != null && sensor != null)
                RailSensor.FilterableOwnerTracker.SetOwner(filterable, sensor);
        }
    }

    [HarmonyPatch(typeof(SolidConduitElementSensorConfig), "DoPostConfigureComplete")]
    public static class SolidConduitElementSensorConfig_DoPostConfigureComplete_Patch
    {
        public static void Postfix(GameObject go)
        {
            RailSensor.Patches.Logger.Log("[SolidConduitElementSensorConfig] DoPostConfigureComplete called");
            var filterable = go.GetComponent<Filterable>();
            var sensor = go.GetComponent<ConduitElementSensor>();
            RailSensor.Patches.Logger.Log($"[SolidConduitElementSensorConfig] filterable={filterable?.GetHashCode() ?? -1}, sensor={sensor?.GetHashCode() ?? -1}");
            if (filterable != null && sensor != null)
                RailSensor.FilterableOwnerTracker.SetOwner(filterable, sensor);
        }
    }

    [HarmonyPatch(typeof(LiquidConduitElementSensorConfig), "DoPostConfigureComplete")]
    public static class LiquidConduitElementSensorConfig_DoPostConfigureComplete_Patch
    {
        public static void Postfix(GameObject go)
        {
            RailSensor.Patches.Logger.Log("[LiquidConduitElementSensorConfig] DoPostConfigureComplete called");
            var filterable = go.GetComponent<Filterable>();
            var sensor = go.GetComponent<ConduitElementSensor>();
            RailSensor.Patches.Logger.Log($"[LiquidConduitElementSensorConfig] filterable={filterable?.GetHashCode() ?? -1}, sensor={sensor?.GetHashCode() ?? -1}");
            if (filterable != null && sensor != null)
                RailSensor.FilterableOwnerTracker.SetOwner(filterable, sensor);
        }
    }
}
