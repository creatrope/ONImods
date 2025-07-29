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
    [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
    public static class CafePlusGermRegistrationPatch
    {
        public static int MilkGermIdx = -1;
        public static string MilkGermId = "CafePlus_MilkGerm";

        static void Postfix()
        {
            int idx = Db.Get().Diseases.GetIndex(MilkGermId);
            if (idx == -1)
            {
                Debug.Log("[CafePlus][GermRegistrationPatch] MilkGerm not found, adding new MilkGerm.");
                var germ = new MilkGerm();
                Db.Get().Diseases.Add(germ);
            }
            MilkGermIdx = Db.Get().Diseases.GetIndex(MilkGermId);
            Debug.Log($"[CafePlus][GermRegistrationPatch] MilkGermIdx set to: {MilkGermIdx}");

            if (!Db.Get().effects.Exists("EspressoPlus"))
            {
                //PrintEspressoEffect();

                // Change showInUI to true so the effect appears in the minion's status
                Effect espressoPlus = new Effect(
                    "EspressoPlus",
                    "Drank Espresso with Milk",
                    "This Duplicant had delicious drink!\n\nLeisure activities increase Duplicants' <style=\"KKeyword\">Morale</style>",
                    450f,      // Duration
                    true,      // showInUI (was false)
                    false,     // triggerFloatingText
                    false,     // isBad
                    null,      // emoteAnim
                    0f,        // emoteCooldown
                    null,      // stompGroup
                    null       // customIcon
                );
                espressoPlus.Add(new AttributeModifier("QualityOfLife", 5f, "Drank Espresso Plus"));
                espressoPlus.Add(new AttributeModifier("Athletics", 2f, "Drank Espresso Plus"));
                if (!Db.Get().effects.Exists("EspressoPlus"))
                {
                    Db.Get().effects.Add(espressoPlus);
                }
                var effect = Db.Get().effects.Get("EspressoPlus");
                Debug.Log("[CafePlus][GermRegistrationPatch] EspressoPlus effect created and added to Db.");
            }
        }

        public static void PrintEspressoEffect()
        {
            if (Db.Get().effects.Exists("Espresso"))
            {
                var espressoEffect = Db.Get().effects.Get("Espresso");
                Debug.Log("[CafePlus] --- Espresso Effect Dump ---");
                Debug.Log($"Id: {espressoEffect.Id}");
                Debug.Log($"Name: {espressoEffect.Name}");
                Debug.Log($"Description: {espressoEffect.description}");
                Debug.Log($"Duration: {espressoEffect.duration}");
                Debug.Log($"ShowInUI: {espressoEffect.showInUI}");
                Debug.Log($"IsBad: {espressoEffect.isBad}");
                Debug.Log($"CanStack: {espressoEffect.isBad}"); // No can_stack property, using isBad
                Debug.Log($"EmoteAnim: {espressoEffect.emoteAnim}");
                Debug.Log($"EmoteCooldown: {espressoEffect.emoteCooldown}");
                Debug.Log($"StompGroup: {espressoEffect.stompGroup}");
                Debug.Log($"CustomIcon: {espressoEffect.customIcon}");
                Debug.Log($"Tag: {espressoEffect.tag}");
                Debug.Log($"ImmunityEffectsNames: {(espressoEffect.immunityEffectsNames != null ? string.Join(",", espressoEffect.immunityEffectsNames) : "null")}");
                Debug.Log($"Modifiers:");
                foreach (var modifier in espressoEffect.SelfModifiers)
                {
                    Debug.Log($"  AttributeId: {modifier.AttributeId}, Value: {modifier.Value}, Description: {modifier.Description}");
                }
            }
            else
            {
                Debug.LogWarning("[CafePlus] Espresso effect not found in Db.Get().effects.");
            }
        }
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

            // Check if storage is full before adding more
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

            if (contents.element == milkElement.id)
            {
                Debug.Log($"[CafePlus] Converting Milk to Water in Espresso Machine. Mass: {contents.mass}, Temp: {contents.temperature}, Germs: {contents.diseaseCount}");

                int germIdx = CafePlusGermRegistrationPatch.MilkGermIdx;
                Debug.Log($"[CafePlus][MilkToWater_ConduitConsumerPatch] germIdx: {germIdx}");
                if (germIdx == -1)
                {
                    germIdx = Db.Get().Diseases.GetIndex(CafePlusGermRegistrationPatch.MilkGermId);
                    Debug.Log($"[CafePlus][MilkToWater_ConduitConsumerPatch] germIdx (fetched): {germIdx}");
                }

                var removed = conduit_mgr.RemoveElement(cell, contents.mass);
                Debug.Log($"[CafePlus][MilkToWater_ConduitConsumerPatch] removed.mass: {removed.mass}, removed.temperature: {removed.temperature}");

                Debug.Log("[CafePlus][MilkToWater_ConduitConsumerPatch] Adding liquid to storage...");
                storage.AddLiquid(
                    SimHashes.Water,
                    removed.mass,
                    removed.temperature,
                    (byte)germIdx,
                    100000,
                    __instance.keepZeroMassObject,
                    false
                );
            }
            else
            {
                //Debug.Log("[CafePlus][MilkToWater_ConduitConsumerPatch] contents.element is not Milk, skipping conversion.");
            }
        }
    }

    [HarmonyPatch(typeof(EspressoMachineWorkable), "OnCompleteWork")]
    public static class EspressoMachineWorkable_OnCompleteWork_MilkBonusPatch
    {
        static void Postfix(EspressoMachineWorkable __instance, WorkerBase worker)
        {
            Debug.Log("[CafePlus][EspressoMachineWorkable_OnCompleteWork] Postfix called __instance: {__instance} worker: {worker}.");

            Storage storage = __instance.GetComponent<Storage>();
            Debug.Log($"[CafePlus][EspressoMachineWorkable_OnCompleteWork] storage: {storage}");
            if (storage != null)
            {
                float amount_consumed;
                SimUtil.DiseaseInfo disease_info1;
                float aggregate_temperature;
                Debug.Log("[CafePlus][EspressoMachineWorkable_OnCompleteWork] Calling storage.ConsumeAndGetDisease...");
                storage.ConsumeAndGetDisease(GameTags.Water, EspressoMachine.WATER_MASS_PER_USE, out amount_consumed, out disease_info1, out aggregate_temperature);

                Debug.Log($"[CafePlus][EspressoMachineWorkable_OnCompleteWork] amount_consumed: {amount_consumed}, disease_info1.idx: {disease_info1.idx}, disease_info1.count: {disease_info1.count}");

                int milkGermIdx = CafePlusGermRegistrationPatch.MilkGermIdx;
                Debug.Log($"[CafePlus][EspressoMachineWorkable_OnCompleteWork] milkGermIdx: {milkGermIdx}");
                if (milkGermIdx == -1)
                {
                    milkGermIdx = Db.Get().Diseases.GetIndex(CafePlusGermRegistrationPatch.MilkGermId);
                    Debug.Log($"[CafePlus][EspressoMachineWorkable_OnCompleteWork] milkGermIdx (fetched): {milkGermIdx}");
                }

                var effects = worker.GetComponent<Effects>();
                if (effects != null)
                {
                    if (disease_info1.idx == milkGermIdx && disease_info1.count > 0)
                    {
                        effects.Add("EspressoPlus", true);
                        Debug.Log("[CafePlus] Milk germ was present in the water just consumed by EspressoMachineWorkable! EspressoPlus effect added.");
                    }
                    else
                    {
                        effects.Add("Espresso", true);
                        Debug.Log("[CafePlus] Default Espresso effect added to worker.");
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
                Debug.Log($"[CafePlus][Mod] MilkElement (lazy): {MilkElement}");
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
            Debug.Log("[CafePlus][Mod] PatchAll and PUtil.InitLibrary called.");
            // Do NOT call ElementLoader.FindElementByName here!
        }
    }

    public class MilkGerm : Disease
    {
        public MilkGerm()
            : base(
                "CafePlus_MilkGerm", // id
                1f, // strength
                new Disease.RangeInfo(0f, 273.15f, 373.15f, 373.15f), // temperatureRange
                new Disease.RangeInfo(600f, 600f, 600f, 600f), // temperatureHalfLives
                new Disease.RangeInfo(0f, 0f, 0f, 0f), // pressureRange
                new Disease.RangeInfo(0f, 0f, 0f, 0f), // pressureHalfLives
                0f, // radiationKillRate
                false // statsOnly
            )
        {
            Debug.Log("[CafePlus][MilkGerm] Constructor called.");
            // Optionally add growth/exposure rules here
        }
    }
}
