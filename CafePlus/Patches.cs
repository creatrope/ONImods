using Database;
using HarmonyLib;
using HLib;
using KMod;
using KSerialization;
using Newtonsoft.Json;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using TUNING;
using UnityEngine;
using Klei;
using Klei.AI;

namespace CafePlus
{
    // Track the last consumed Element per machine
    public class LastConsumedLiquidComponent : MonoBehaviour
    {
        public Element lastConsumedLiquid;
    }

    [HarmonyPatch(typeof(ConduitConsumer), "Consume")]
    public static class MilkToWater_ConduitConsumerPatch
    {
        static void Prefix(ConduitConsumer __instance, float dt, ConduitFlow conduit_mgr)
        {
            var go = __instance.gameObject;
            if (go == null || go.GetComponent<EspressoMachine>() == null)
                return;

            var building = __instance.GetComponent<Building>();
            var cell = building.GetUtilityInputCell();
            var contents = conduit_mgr.GetContents(cell);

            if (contents.mass <= 0)
                return;

            var milkElement = Mod.GetMilkElement();
            if (milkElement == null)
                return;

            var storage = __instance.storage;
            if (storage == null)
            {
                Debug.LogWarning("[CafePlus][MilkToWater_ConduitConsumerPatch] __instance.storage is null!");
                return;
            }
            float storageCapacity = storage.capacityKg;
            float storageMass = storage.MassStored();

            if (storageMass >= storageCapacity)
            {
                return;
            }

            // Save the actual Element reference
            var lastLiquidComp = go.GetComponent<LastConsumedLiquidComponent>();
            if (lastLiquidComp != null)
                lastLiquidComp.lastConsumedLiquid = ElementLoader.FindElementByHash(contents.element);

            if (contents.element == milkElement.id)
            {
                var removed = conduit_mgr.RemoveElement(cell, contents.mass);

                storage.AddLiquid(
                    SimHashes.Water,
                    removed.mass,
                    removed.temperature,
                    byte.MinValue, // No disease/germ
                    0,
                    __instance.keepZeroMassObject,
                    false
                );
            }
        }
    }

    [HarmonyPatch(typeof(EspressoMachineWorkable), "OnCompleteWork")]
    public static class EspressoMachineWorkable_OnCompleteWork_MilkBonusPatch
    {
        static void Postfix(EspressoMachineWorkable __instance, WorkerBase worker)
        {
            // Print the internal name of the last consumed element
            var go = __instance.gameObject;
            var lastLiquidComp = go.GetComponent<LastConsumedLiquidComponent>();
            if (lastLiquidComp != null && lastLiquidComp.lastConsumedLiquid != null)
                Debug.Log($"[CafePlus] lastConsumedLiquid: {lastLiquidComp.lastConsumedLiquid.id}");
            else
                Debug.LogWarning("[CafePlus] LastConsumedLiquidComponent is missing or lastConsumedLiquid is null on this EspressoMachine!");

            Storage storage = __instance.GetComponent<Storage>();
            if (storage != null)
            {
                float amount_consumed;
                float aggregate_temperature;
                storage.ConsumeAndGetDisease(GameTags.Water, EspressoMachine.WATER_MASS_PER_USE, out amount_consumed, out SimUtil.DiseaseInfo disease_info, out aggregate_temperature);

                var effects = worker.GetComponent<Effects>();
                if (effects != null)
                {
                    // Compare directly to the Milk element
                    if (lastLiquidComp != null && lastLiquidComp.lastConsumedLiquid == Mod.GetMilkElement())
                    {
                        //effects.Add("EspressoPlus", true);
                        Debug.Log("[CafePlus] Milk Present, EspressoPlus effect added.");
                    }
                    else
                    {
                        //effects.Add("Espresso", true);
                        Debug.Log("[CafePlus] Milk NOT Present, Espresso effect added.");
                    }
                }
                else
                {
                    Debug.LogWarning("[CafePlus] Effects component not found on worker.");
                }
            }
            else
            {
                Debug.LogWarning("[CafePlus][EspressoMachineWorkable_OnCompleteWork] storage is null!");
            }
        }
    }

    public static class Mod
    {
        public static Element MilkElement;

        public static Element GetMilkElement()
        {
            if (MilkElement == null)
            {
                MilkElement = ElementLoader.FindElementByName("Milk");
                if (MilkElement == null)
                    Debug.LogWarning("[CafePlus][Mod] MilkElement is null!");
            }
            return MilkElement;
        }

        public static void OnLoad(Harmony harmony)
        {
            Debug.Log("[CafePlus][Mod] OnLoad called.");
            harmony.PatchAll();
            PUtil.InitLibrary();
        }
    }

    [HarmonyPatch(typeof(EspressoMachine), "OnSpawn")]
    public static class EspressoMachine_OnSpawn_AddLastConsumedLiquidComponent
    {
        static void Postfix(EspressoMachine __instance)
        {
            var go = __instance.gameObject;
            // Ensure the LastConsumedLiquidComponent is present
            if (go.GetComponent<LastConsumedLiquidComponent>() == null)
            {
                go.AddComponent<LastConsumedLiquidComponent>();
                Debug.Log("[CafePlus] LastConsumedLiquidComponent added to EspressoMachine: " + go.name);
            }
            else
            {
                Debug.Log("[CafePlus] LastConsumedLiquidComponent already present on EspressoMachine: " + go.name);
            }
        }
    }

    [HarmonyPatch(typeof(EspressoMachineConfig), "ConfigureBuildingTemplate")]
    public static class EspressoMachineConfig_ConfigureBuildingTemplate_CombinedPatch
    {
        static void Postfix(GameObject go, Tag prefab_tag)
        {
            // Patch storage filter to accept any liquid
            var storage = go.GetComponent<Storage>();
            if (storage != null)
            {
                storage.storageFilters = new List<Tag> { GameTags.Liquid };
                Debug.Log("[CafePlus] Patched EspressoMachine storage filter to accept any liquid.");
            }

            // Patch ConduitConsumer to accept any liquid
            var consumer = go.GetComponent<ConduitConsumer>();
            if (consumer != null)
            {
                consumer.capacityTag = GameTags.Liquid;
                Debug.Log("[CafePlus] Patched EspressoMachine ConduitConsumer to accept and store any liquid.");
            }

            // Attach the FewOption side screen component
            go.AddOrGet<EspressoMachineFewOptions>();
            Debug.Log("[CafePlus] Added FewOptionSideScreen to EspressoMachine.");

            Debug.Log("[CafePlus] called AddOrGet<EspressoMachine>();");
            go.AddOrGet<EspressoMachine>();
        }
    }

    [HarmonyPatch(typeof(EspressoMachine), "OnSpawn")]
    public static class EspressoMachine_OnSpawn_AddFewOptionSideScreen
    {
        static void Postfix(global::EspressoMachine __instance)
        {
            var go = __instance.gameObject;
            if (go.GetComponent<EspressoMachineFewOptions>() == null)
            {
                go.AddComponent<EspressoMachineFewOptions>();
                Debug.Log("[CafePlus] FewOptionSideScreen added to EspressoMachine: " + go.name);
            }
        }
    }

    public class EspressoMachineFewOptions : KMonoBehaviour, FewOptionSideScreen.IFewOptionSideScreen
    {
        private static readonly FewOptionSideScreen.IFewOptionSideScreen.Option[] options = new[]
        {
            new FewOptionSideScreen.IFewOptionSideScreen.Option
            {
                tag = new Tag("Option1"),
                labelText = "Option 1",
                tooltipText = "First option",
                iconSpriteColorTuple = new Tuple<UnityEngine.Sprite, UnityEngine.Color>(null, UnityEngine.Color.white)
            },
            new FewOptionSideScreen.IFewOptionSideScreen.Option
            {
                tag = new Tag("Option2"),
                labelText = "Option 2",
                tooltipText = "Second option",
                iconSpriteColorTuple = new Tuple<UnityEngine.Sprite, UnityEngine.Color>(null, UnityEngine.Color.white)
            }
        };

        private Tag selectedOption = options[0].tag;

        public FewOptionSideScreen.IFewOptionSideScreen.Option[] GetOptions() => options;

        public void OnOptionSelected(FewOptionSideScreen.IFewOptionSideScreen.Option option)
        {
            selectedOption = option.tag;
            Debug.Log("[CafePlus] EspressoMachineFewOptions: Selected " + option.labelText);
        }

        public Tag GetSelectedOption() => selectedOption;
    }
}
