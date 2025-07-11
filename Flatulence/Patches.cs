using HarmonyLib;
using HLib;
using Klei.AI;
using KMod;
using KSerialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static STRINGS.UI.UISIDESCREENS.AUTOPLUMBERSIDESCREEN.BUTTONS;
using Object = UnityEngine.Object;

namespace FlatulenceMod
{
    public class Patches
    {
        private static bool staticInitialized = false;
        public static readonly CustomLogger Logger = new CustomLogger("FlatulenceMod");

        static Patches()
        {
            if (staticInitialized)
                return;
            staticInitialized = true;
        }

        public static void OnLoad()
        {
            Logger.SetLoggingEnabled(true);
            Logger.Reset();
        }
    }

    public class Mod : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Patches.OnLoad();
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class RegisterPatch
    {
        private static bool effectRegistered = false;

        public static void Postfix()
        {
            FlatulenceMod.Patches.Logger.Log("[RegisterPatch] Db.Initialize postfix called.");

            // Register effect
            if (!effectRegistered)
            {
                var effectsDb = Db.Get().effects;
                if (effectsDb != null)
                {
                    var effect = EFFECTS.CreateFlatulenceEffect();
                    effectsDb.Add(effect);
                    FlatulenceMod.Patches.Logger.Log("[RegisterPatch] FlatulenceEffect registered.");
                }
                effectRegistered = true;
            }

            FlatulenceMod.Patches.Logger.Log("[RegisterPatch] Exiting.");
        }
    }

    [HarmonyPatch(typeof(MinionConfig), "OnSpawn")]
    public static class MinionEffectPatch
    {
        public static void Postfix(GameObject go)
        {
            FlatulenceMod.Patches.Logger.Log("[MinionConfig] Postfix called.");
            if (go == null)
            {
                FlatulenceMod.Patches.Logger.Log("[MinionEffectPatch] Game Object null.");
                return;
            }

            var effects = go.GetComponent<Effects>();

            if (Db.Get().effects.Get(EFFECTS.FLATULENCE_EFFECT_ID) != null && !effects.HasEffect(EFFECTS.FLATULENCE_EFFECT_ID))
            {
                FlatulenceMod.Patches.Logger.Log("[MinionEffectPatch] Adding EFFECTS.FLATULENCE_EFFECT_ID effect to minion: " + go.name);
                effects.Add(EFFECTS.FLATULENCE_EFFECT_ID, true);
            }
            else
            {
                FlatulenceMod.Patches.Logger.Log("[MinionEffectPatch] EFFECTS.FLATULENCE_EFFECT_ID effect already present or not found in Db for minion: " + go.name);
            }

            FlatulenceMod.Patches.Logger.Log("[MinionConfig] Postfix Exiting.");
        }
    }

    [HarmonyPatch(typeof(Assets), "OnPrefabInit")]
    public static class Assets_OnPrefabInit_Patch
    {
        private static bool pillRegistered = false;
        public static void Postfix()
        {
            FlatulenceMod.Patches.Logger.Log("[Assets_OnPrefabInit_Patch] OnPrefabInit postfix called.");
            if (!pillRegistered)
            {
                NoFlatulencePillConfig pillConfig = new NoFlatulencePillConfig();
                pillConfig.CreatePrefab();
                FlatulenceMod.Patches.Logger.Log("[Assets_OnPrefabInit_Patch] NoFlatulencePill prefab registered.");
                pillRegistered = true;
            }
            FlatulenceMod.Patches.Logger.Log("[Assets_OnPrefabInit_Patch] Exiting.");
        }
    }

    public class EFFECTS
    {
        public const string FLATULENCE_EFFECT_ID = "FlatulenceEffect";

        public static Effect CreateFlatulenceEffect()
        {
            // duration: -1 means permanent until cured
            var effect = new Effect(
                FLATULENCE_EFFECT_ID,
                "Flatulence Effect",
                "This duplicant suffers from excessive flatulence.",
                duration: -1f,
                show_in_ui: true,
                trigger_floating_text: false,
                is_bad: true
            );
            effect.Add(new AttributeModifier("Stress", 10f, "Flatulence Stress")); // Optional: add stress
            return effect;
        }
    }

    public class MEDICINE
    {
        public static readonly MedicineInfo NOFLATULENCEPILL = new MedicineInfo(
            "NoFlatulencePill",
            EFFECTS.FLATULENCE_EFFECT_ID,
            MedicineInfo.MedicineType.CureSpecific,
            null,
            null
        );
    }

    public class NoFlatulencePillConfig : IEntityConfig, IHasDlcRestrictions
    {
        public const string ID = "NoFlatulencePill";
        public static ComplexRecipe recipe;
        private static bool prefabCreated = false;

        public string[] GetRequiredDlcIds() => DlcManager.EXPANSION1;
        public string[] GetForbiddenDlcIds() => null;
        string[] IEntityConfig.GetDlcIds() => GetRequiredDlcIds();

        public GameObject CreatePrefab()
        {
            if (prefabCreated)
            {
                FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] CreatePrefab called but prefab already created. Skipping.");
                return null;
            }
            prefabCreated = true;

            FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] CreatePrefab called.");

            // Test for animation existence before using it
            var testAnimName = "pill_radiation_kanim";
            FlatulenceMod.Patches.Logger.Log($"[NoFlatulencePillConfig] Testing for animation: {testAnimName}");
            var anim = Assets.GetAnim((HashedString)testAnimName);
            if (anim == null)
            {
                FlatulenceMod.Patches.Logger.Log($"[NoFlatulencePillConfig] TEST FAILED: Animation '{testAnimName}' not found!");
            }
            else
            {
                FlatulenceMod.Patches.Logger.Log($"[NoFlatulencePillConfig] TEST PASSED: Animation '{testAnimName}' found.");
            }

            FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] Creating looseEntity5 entity...");

            GameObject looseEntity = EntityTemplates.CreateLooseEntity(
                ID,
                "NoFlatulencePill",
                "NoFlatulencePill Cure",
                1f,
                true,
                anim,
                "object",
                Grid.SceneLayer.Front,
                EntityTemplates.CollisionShape.RECTANGLE,
                0.8f,
                0.4f,
                true);

            FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] looseEntity5 complete!");

            if (looseEntity == null)
            {
                FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] ERROR: looseEntity5 is null after CreateLooseEntity!");
                throw new Exception("Failed to create looseEntity for NoFlatulencePill!");
            }
            else
            {
                FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] looseEntity created successfully.");
            }

            FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] Extending entity to medicine...");
            EntityTemplates.ExtendEntityToMedicine(looseEntity, MEDICINE.NOFLATULENCEPILL);

            FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] Adding MedicinalPill component...");
            var pillComponent = looseEntity.AddOrGet<MedicinalPill>();
            if (pillComponent == null)
            {
                FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] ERROR: MedicinalPill component is null!");
            }
            else
            {
                FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] MedicinalPill component added.");
                pillComponent.info = MEDICINE.NOFLATULENCEPILL;
            }

            FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] Creating recipe elements...");
            ComplexRecipe.RecipeElement[] ingredients = new ComplexRecipe.RecipeElement[]
            {
                new ComplexRecipe.RecipeElement((Tag)"Carbon", 1f)
            };
            ComplexRecipe.RecipeElement[] results = new ComplexRecipe.RecipeElement[]
            {
                new ComplexRecipe.RecipeElement(ID.ToTag(), 1f, ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature)
            };

            FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] Creating ComplexRecipe...");
            recipe = new ComplexRecipe(
                ComplexRecipeManager.MakeRecipeID("Apothecary", ingredients, results),
                ingredients,
                results)
            {
                time = 50f,
                description = "Craft a pill to help with flatulence.",
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag>() { (Tag)"Apothecary" },
                sortOrder = 10
            };

            if (recipe == null)
            {
                FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] ERROR: ComplexRecipe is null!");
            }
            else
            {
                FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] ComplexRecipe created successfully.");
            }

            FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] CreatePrefab finished.");
            return looseEntity;
        }

        public void OnPrefabInit(GameObject inst)
        {
            // Add any initialization logic for the prefab here if needed
        }

        public void OnSpawn(GameObject inst)
        {
            // Add any logic to execute when the prefab spawns here if needed
        }
    }
}