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
                var germ = new MilkGerm();
                Db.Get().Diseases.Add(germ);
            }
            MilkGermIdx = Db.Get().Diseases.GetIndex(MilkGermId);

            if (!Db.Get().effects.Exists("EspressoPlus"))
            {
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
                int germIdx = CafePlusGermRegistrationPatch.MilkGermIdx;
                if (germIdx == -1)
                {
                    germIdx = Db.Get().Diseases.GetIndex(CafePlusGermRegistrationPatch.MilkGermId);
                }

                var removed = conduit_mgr.RemoveElement(cell, contents.mass);

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
            Storage storage = __instance.GetComponent<Storage>();
            if (storage != null)
            {
                float amount_consumed;
                SimUtil.DiseaseInfo disease_info1;
                float aggregate_temperature;
                storage.ConsumeAndGetDisease(GameTags.Water, EspressoMachine.WATER_MASS_PER_USE, out amount_consumed, out disease_info1, out aggregate_temperature);

                int milkGermIdx = CafePlusGermRegistrationPatch.MilkGermIdx;
                if (milkGermIdx == -1)
                {
                    milkGermIdx = Db.Get().Diseases.GetIndex(CafePlusGermRegistrationPatch.MilkGermId);
                }

                var effects = worker.GetComponent<Effects>();
                if (effects != null)
                {
                    if (disease_info1.idx == milkGermIdx && disease_info1.count > 0)
                    {
                        effects.Add("EspressoPlus", true);
                        Debug.Log("[CafePlus] Milk Present, EspressoPlus effect added.");
                    }
                    else
                    {
                        effects.Add("Espresso", true);
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

    // kinda weird, but this gives us a way to track the milk through the system
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
