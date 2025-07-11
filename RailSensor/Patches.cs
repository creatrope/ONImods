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
            Logger.SetLoggingEnabled(false); 
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
            Logger.SetLoggingEnabled(true);
            Logger.Reset();
        }
    }
    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            RailSensor.Patches.OnLoad(); // <-- Ensure hotkey system is initialized
            base.OnLoad(harmony);

            PUtil.InitLibrary();
            harmony.PatchAll();
            Patches.Logger.Log("Mod.OnLoad finished: PUtil.InitLibrary, options registered, harmony patched.");
        }
    }

    [HarmonyPatch(typeof(ConduitElementSensor), "ConduitUpdate")]
    public static class ConduitElementSensor_ConduitUpdate_Patch
    {
        public static void Postfix(ConduitElementSensor __instance, Filterable ___filterable, ConduitType ___conduitType, float dt)
        {
            bool trigger = false;

            Tag selectedTag = ___filterable != null ? ___filterable.SelectedTag : Tag.Invalid;
            Tag anythingTag = Filterable_GetTagOptions_Patch.AnythingTag;

            int cell = Grid.PosToCell(__instance.transform.position);

            if (cell != Grid.InvalidCell)
            {
                if (___conduitType == ConduitType.Solid)
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
                }
                else if (___conduitType == ConduitType.Liquid || ___conduitType == ConduitType.Gas)
                {
                    var flowManager = Conduit.GetFlowManager(___conduitType);
                    if (flowManager != null)
                    {
                        var contents = flowManager.GetContents(cell);
                        Tag element = contents.element != SimHashes.Vacuum && contents.element != SimHashes.Void
                            ? new Tag(contents.element.ToString())
                            : Tag.Invalid;
                        bool hasMass = contents.mass > 0.0f;

                        trigger = (hasMass && (selectedTag == anythingTag || element == selectedTag));
                    }
                }
            }

            // Only call SetState if the state actually changed
            bool currentState = __instance.IsSwitchedOn;
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
            if (__instance.GetComponent<ConduitElementSensor>() != null)
            {
                if (!__result.ContainsKey(AnythingTag))
                    __result.Add(AnythingTag, new HashSet<Tag> { AnythingTag });
            }
        }
    }
}
