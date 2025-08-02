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
using System.Linq;
using TUNING;
using UnityEngine;
using Klei;
using Klei.AI;

namespace CafePlus
{
    public class LastConsumedLiquidComponent : MonoBehaviour
    {
        public Element lastConsumedLiquid;
    }

    [HarmonyPatch(typeof(ConduitConsumer), "Consume")]
    public static class ConduitConsumerPatch
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

            var storage = __instance.storage;
            if (storage == null)
            {
                Debug.LogWarning("[CafePlus][ConduitConsumerPatch] __instance.storage is null!");
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

            // Only convert the mapped element to water if it's in the mapping
            if (LiquidEffectMap.LiquidToEffects.ContainsKey(contents.element))
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

    public static class LiquidEffectMap
    {
        // Map element id to a list of effect ids
        public static readonly Dictionary<SimHashes, List<string>> LiquidToEffects = new Dictionary<SimHashes, List<string>>
        {
            { SimHashes.Water, new List<string> { "Espresso" } },
            { SimHashes.Milk, new List<string> { "EspressoPlus" } }, 
            { SimHashes.Petroleum, new List<string> { "PetroleumBuzz" } },
            { SimHashes.CrudeOil, new List<string> { "OilSlick" } }
        };
    }

    public static class Mod
    {
 
        public static void OnLoad(Harmony harmony)
        {
            Debug.Log("[CafePlus][Mod] OnLoad called.");
            harmony.PatchAll();
            PUtil.InitLibrary();
        }

        public static void RegisterAllEffects()
        {
            Debug.Log("[CafePlus][RegisterAllEffects] Entered RegisterAllEffects()");
            var db = Db.Get();
            if (db == null)
            {
                Debug.LogError("[CafePlus][RegisterAllEffects] Db.Get() returned null!");
                return;
            }
            if (db.effects == null)
            {
                Debug.LogError("[CafePlus][RegisterAllEffects] db.effects is null!");
                return;
            }

            var effectIds = LiquidEffectMap.LiquidToEffects.SelectMany(pair => pair.Value).Distinct().ToList();
            Debug.Log($"[CafePlus][RegisterAllEffects] Effect IDs to register: {string.Join(", ", effectIds)}");

            foreach (var effectId in effectIds)
            {
                Debug.Log($"[CafePlus][RegisterAllEffects] Checking effect: {effectId}");
                if (!db.effects.Exists(effectId))
                {

                    var effect = new Effect(
                        id: effectId,
                        name: effectId,
                        description: $"Effect for {effectId}",
                        duration: 120f, // 2 minutes
                        show_in_ui: true,
                        is_bad: false,
                        trigger_floating_text: true
                    );
                    db.effects.Add(effect);
                    Debug.Log($"[CafePlus][RegisterAllEffects] Registered new effect: {effectId}");
                }
            }
            Debug.Log("[CafePlus][RegisterAllEffects] Finished RegisterAllEffects()");
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

    [HarmonyPatch(typeof(EspressoMachineWorkable), "OnCompleteWork")]
    public static class EspressoMachineWorkable_OnCompleteWork_CustomPrefix
    {
        static bool Prefix(EspressoMachineWorkable __instance, WorkerBase worker)
        {
            var go = __instance.gameObject;
            var storage = __instance.GetComponent<Storage>();
            if (storage != null)
            {
                float amount_consumed;
                float aggregate_temperature;
                SimUtil.DiseaseInfo disease_info1;
                SimUtil.DiseaseInfo disease_info2;

                // Find any available liquid in storage
                PrimaryElement foundLiquid = null;
                foreach (var item in storage.items)
                {
                    if (item is GameObject goItem)
                    {
                        var pe = goItem.GetComponent<PrimaryElement>();
                        if (pe != null && pe.Element.IsLiquid)
                        {
                            foundLiquid = pe;
                            break;
                        }
                    }
                }

                Tag liquidTag = GameTags.Water;
                string liquidName = "None";
                if (foundLiquid != null)
                {
                    liquidTag = foundLiquid.Element.tag;
                    liquidName = foundLiquid.Element.name;
                }

                storage.ConsumeAndGetDisease(liquidTag, EspressoMachine.WATER_MASS_PER_USE, out amount_consumed, out disease_info1, out aggregate_temperature);
                storage.ConsumeAndGetDisease(EspressoMachine.INGREDIENT_TAG, EspressoMachine.INGREDIENT_MASS_PER_USE, out amount_consumed, out disease_info2, out aggregate_temperature);

                Debug.Log($"[CafePlus] Consumed liquid: {liquidName} ({liquidTag})");

                GermExposureMonitor.Instance smi = worker.GetSMI<GermExposureMonitor.Instance>();
                if (smi != null)
                {
                    smi.TryInjectDisease(disease_info1.idx, disease_info1.count, liquidTag, Sickness.InfectionVector.Digestion);
                    smi.TryInjectDisease(disease_info2.idx, disease_info2.count, EspressoMachine.INGREDIENT_TAG, Sickness.InfectionVector.Digestion);
                }

                Effects effects = worker.GetComponent<Effects>();
                if (effects != null)
                {
                    if (foundLiquid != null)
                    {
                        var elementHash = foundLiquid.Element.id;
                        if (LiquidEffectMap.LiquidToEffects.TryGetValue(elementHash, out var effectList))
                        {
                            foreach (var effect in effectList)
                            {
                                var db = Db.Get();
                                if (db != null && db.effects != null && db.effects.Exists(effect) != null)
                                {
                                    effects.Add(effect, true);
                                    Debug.Log($"[CafePlus] Added effect '{effect}' for element {elementHash}.");
                                }
                                else
                                {
                                    Debug.LogWarning($"[CafePlus] Effect '{effect}' does not exist in db.effects (or db does not exist) and was not added.");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("[CafePlus][EspressoMachineWorkable_OnCompleteWork] storage is null!");
            }
            // Skip the original method
            return false;
        }
    }

    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class CafePlus_Db_Initialize_Patch
    {
        static void Postfix()
        {
            CafePlus.Mod.RegisterAllEffects();
        }
    }
}
