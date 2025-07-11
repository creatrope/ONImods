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
using static STRINGS.BUILDINGS.PREFABS;
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
        public bool EnableCustomLog { get; set; } = false;
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            RailSensor.Patches.OnLoad(); // <-- Ensure hotkey system is initialized
            base.OnLoad(harmony);

            PUtil.InitLibrary();
            //new POptions().RegisterOptions(this, typeof(ModOptions));
            harmony.PatchAll();
            Patches.Logger.Log("Mod.OnLoad finished: PUtil.InitLibrary, options registered, harmony patched.");
        }
    }

    [HarmonyPatch(typeof(ConduitElementSensor), "ConduitUpdate")]
    public static class ConduitElementSensor_ConduitUpdate_Patch
    {
        //public static void Postfix(ConduitElementSensor __instance, float dt)
        public static void Postfix(ConduitElementSensor __instance, Filterable ___filterable, float dt)
        {
            bool trigger = false;

            //var filterable = Traverse.Create(__instance).Field("filterable").GetValue<Filterable>();
            var filterable = ___filterable;

            Tag selectedTag = filterable != null ? filterable.SelectedTag : Tag.Invalid;
            Tag anythingTag = Filterable_GetTagOptions_Patch.AnythingTag;

            // Try to get the cell and conduit type
            var traverse = Traverse.Create(__instance);
            object cellObj = traverse.Field("utilityCell").GetValue();
            if (cellObj == null || (cellObj is int && (int)cellObj == 0))
                cellObj = traverse.Field("conduitCell").GetValue();
            if (cellObj == null || (cellObj is int && (int)cellObj == 0))
                cellObj = traverse.Field("cell").GetValue();
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
                    Tag element = pickupable != null && pickupable.PrimaryElement != null
                        ? pickupable.PrimaryElement.Element.tag
                        : Tag.Invalid;
                    bool hasMass = pickupable != null && pickupable.PrimaryElement != null && pickupable.PrimaryElement.Mass > 0.0f;

                    trigger = (hasMass && (selectedTag == anythingTag || element == selectedTag));
                    //Patches.Logger.Log($"[Solid] cell={cell}, hasMass={hasMass}, element={element}, selectedTag={selectedTag}, anythingTag={anythingTag}, trigger={trigger}");
                }
                else if (conduitType == ConduitType.Liquid || conduitType == ConduitType.Gas)
                {
                    var flowManager = Conduit.GetFlowManager(conduitType);
                    if (flowManager != null)
                    {
                        var contents = flowManager.GetContents(cell);
                        Tag element = contents.element != SimHashes.Vacuum && contents.element != SimHashes.Void
                            ? new Tag(contents.element.ToString())
                            : Tag.Invalid;
                        bool hasMass = contents.mass > 0.0f;

                        trigger = (hasMass && (selectedTag == anythingTag || element == selectedTag));
                        //Patches.Logger.Log($"[{conduitType}] cell={cell}, hasMass={hasMass}, element={element}, selectedTag={selectedTag}, anythingTag={anythingTag}, trigger={trigger}");
                    }
                }
            }

            // Only call SetState if the state actually changed
            bool currentState = Traverse.Create(__instance).Field("isOn").GetValue<bool>();
            if (currentState != trigger)
            {
                Patches.Logger.Log($"State Change {{trigger={trigger}, instanceID={__instance.GetInstanceID()}}}");
                Traverse.Create(__instance).Method("SetState", trigger).GetValue();
            }
        }
    }

    [HarmonyPatch(typeof(Filterable), "GetTagOptions")]
    public static class Filterable_GetTagOptions_Patch
    {
        public static readonly Tag AnythingTag = new Tag("Anything");

        public static void Postfix(Filterable __instance, ref Dictionary<Tag, HashSet<Tag>> __result)
        {
            var owner = FilterableOwnerTracker.GetOwner(__instance);
            if (owner is ConduitElementSensor /* || owner is ElementSensor, etc. */)
            {
                if (!__result.ContainsKey(AnythingTag))
                    __result.Add(AnythingTag, new HashSet<Tag> { AnythingTag });
            }
        }
    }

    [HarmonyPatch(typeof(Filterable), "OnPrefabInit")]
    public static class Filterable_OnPrefabInit_Patch
    {
        public static void Postfix(Filterable __instance)
        {
            var sensor = __instance.GetComponent<ConduitElementSensor>();
            if (sensor != null)
                RailSensor.FilterableOwnerTracker.SetOwner(__instance, sensor);
        }
    }

    public static class FilterableOwnerTracker
    {
        private static readonly ConditionalWeakTable<Filterable, object> Owners = new ConditionalWeakTable<Filterable, object>();

        public static void SetOwner(Filterable filterable, object owner)
        {
            if (filterable != null && owner != null)
            {
                Owners.Remove(filterable);
                Owners.Add(filterable, owner);
            }
        }

        public static object GetOwner(Filterable filterable)
        {
            if (filterable == null)
                return null;
            Owners.TryGetValue(filterable, out var owner);
            return owner;
        }
    }

    [HarmonyPatch(typeof(GasConduitElementSensorConfig), "DoPostConfigureComplete")]
    public static class GasConduitElementSensorConfig_DoPostConfigureComplete_Patch
    {
        public static void Postfix(GameObject go)
        {
            var filterable = go.GetComponent<Filterable>();
            var sensor = go.GetComponent<ConduitElementSensor>();
            if (filterable != null && sensor != null)
                RailSensor.FilterableOwnerTracker.SetOwner(filterable, sensor);
        }
    }

    [HarmonyPatch(typeof(SolidConduitElementSensorConfig), "DoPostConfigureComplete")]
    public static class SolidConduitElementSensorConfig_DoPostConfigureComplete_Patch
    {
        public static void Postfix(GameObject go)
        {
            var filterable = go.GetComponent<Filterable>();
            var sensor = go.GetComponent<ConduitElementSensor>();
            if (filterable != null && sensor != null)
                RailSensor.FilterableOwnerTracker.SetOwner(filterable, sensor);
        }
    }

    [HarmonyPatch(typeof(LiquidConduitElementSensorConfig), "DoPostConfigureComplete")]
    public static class LiquidConduitElementSensorConfig_DoPostConfigureComplete_Patch
    {
        public static void Postfix(GameObject go)
        {
            var filterable = go.GetComponent<Filterable>();
            var sensor = go.GetComponent<ConduitElementSensor>();
            if (filterable != null && sensor != null)
                RailSensor.FilterableOwnerTracker.SetOwner(filterable, sensor);
        }
    }
}
