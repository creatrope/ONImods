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
using STRINGS; // Add this namespace to access UI constants

namespace CafePlus
{
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
            var db = Db.Get();
            if (db == null || db.effects == null)
            {
                Debug.LogWarning("[CafePlus] Db or db.effects is null, cannot register effects.");
                return;
            }

            foreach (var recipe in CafePlusRecipes.All)
            {
                foreach (var effectId in recipe.Effects)
                {
                    if (db.effects.Exists(effectId) == null)
                    {
                        var effect = new Effect(
                            id: effectId,
                            name: effectId,
                            description: $"CafePlus effect: {effectId}",
                            duration: 15f,
                            show_in_ui: true,
                            trigger_floating_text: true,
                            is_bad: false
                        );
                        db.effects.Add(effect);
                        Debug.Log($"[CafePlus] Registered new effect: {effectId}");
                    }
                }
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

            // Add RecipeComponent to the prefab
            go.AddOrGet<RecipeComponent>();
            Debug.Log("[CafePlus] Added RecipeComponent to EspressoMachine.");

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
        [Serialize]
        private Tag selectedOption = CafePlusRecipes.All[0].LiquidIngredient;

        private static readonly FewOptionSideScreen.IFewOptionSideScreen.Option[] options =
            CafePlusRecipes.All
                .Select(recipe => new FewOptionSideScreen.IFewOptionSideScreen.Option
                {
                    tag = recipe.LiquidIngredient,
                    labelText = recipe.Name,
                    tooltipText = $"Brew with {recipe.Name} ({string.Join(", ", recipe.Effects)})",
                    iconSpriteColorTuple = Def.GetUISprite(recipe.LiquidIngredient),
                })
                .ToArray();

        public FewOptionSideScreen.IFewOptionSideScreen.Option[] GetOptions() => options;

        public void OnOptionSelected(FewOptionSideScreen.IFewOptionSideScreen.Option option)
        {
            selectedOption = option.tag;

            // Set InputLiquid and SelectedRecipe in RecipeComponent
            var recipeComponent = GetComponent<RecipeComponent>();
            CafePlusRecipe recipe = null;
            if (recipeComponent != null)
            {
                recipe = CafePlusRecipes.All.FirstOrDefault(r => r.LiquidIngredient == option.tag);
                recipeComponent.SetSelectedRecipe(recipe);
            }

            Debug.Log($"[CafePlus] EspressoMachineFewOptions: Selected {option.labelText}");
            Debug.Log($"[CafePlus] Selected InputLiquid: {option.tag}");
            Debug.Log($"[CafePlus] Selected Recipe: {(recipe != null ? recipe.Name : "null")}");
        }

        public Tag GetSelectedOption() => selectedOption;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // Restore selection to RecipeComponent on load
            var recipeComponent = GetComponent<RecipeComponent>();
            if (recipeComponent != null)
            {
                var recipe = CafePlusRecipes.All.FirstOrDefault(r => r.LiquidIngredient == selectedOption);
                recipeComponent.SetSelectedRecipe(recipe);
            }
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

    public class CafePlusRecipe
    {
        public string Name { get; }
        public Tag LiquidIngredient { get; }
        public List<string> Effects { get; }

        public CafePlusRecipe(string name, Tag liquidIngredient, List<string> effects)
        {
            Name = name;
            LiquidIngredient = liquidIngredient;
            Effects = effects;
        }
    }

    public static class CafePlusRecipes
    {
        private static readonly Tag WaterTag = ElementLoader.FindElementByHash(SimHashes.Water).tag;
        private static readonly Tag MilkTag = ElementLoader.FindElementByHash(SimHashes.Milk).tag;
        private static readonly Tag PetroleumTag = ElementLoader.FindElementByHash(SimHashes.Petroleum).tag;
        private static readonly Tag CrudeOilTag = ElementLoader.FindElementByHash(SimHashes.CrudeOil).tag;

        public static readonly CafePlusRecipe WaterEspresso = new CafePlusRecipe(
            "Water Espresso",
             WaterTag,
            new List<string> { "Espresso" }
        );

        public static readonly CafePlusRecipe MilkEspresso = new CafePlusRecipe(
            "Milk Espresso",
            MilkTag,
            new List<string> { "EspressoPlus" }
        );

        public static readonly CafePlusRecipe PetroleumBuzz = new CafePlusRecipe(
            "Petroleum Buzz",
            PetroleumTag,
            new List<string> { "PetroleumBuzz" }
        );

        public static readonly CafePlusRecipe OilSlick = new CafePlusRecipe(
            "Oil Slick",
            CrudeOilTag,
            new List<string> { "OilSlick" }
        );

        public static readonly List<CafePlusRecipe> All = new List<CafePlusRecipe>
        {
            WaterEspresso,
            MilkEspresso,
            PetroleumBuzz,
            OilSlick
        };

        // Optional: Map by name for quick lookup
        public static readonly Dictionary<string, CafePlusRecipe> ByName =
            All.ToDictionary(r => r.Name, r => r);
    }

    [HarmonyPatch(typeof(EspressoMachine.States), nameof(EspressoMachine.States.IsReady))]
    public static class IsReadyPostfix
    {
        static void Postfix(EspressoMachine.StatesInstance smi, ref bool __result)
        {
            var storage = smi.GetComponent<Storage>();
            var recipeComponent = smi.GetComponent<RecipeComponent>();
            if (storage == null || recipeComponent == null)
            {
                __result = false;
                return;
            }

            // Use InputLiquid from RecipeComponent if valid, otherwise fallback to water
            Tag inputLiquid = recipeComponent.InputLiquid.IsValid ? recipeComponent.InputLiquid : GameTags.Water;

            PrimaryElement primaryElement = storage.FindPrimaryElement(ElementLoader.GetElement(inputLiquid).id);

            bool hasLiquid = primaryElement != null && primaryElement.Mass >= EspressoMachine.WATER_MASS_PER_USE;
            bool hasIngredient = storage.GetAmountAvailable(EspressoMachine.INGREDIENT_TAG) >= EspressoMachine.INGREDIENT_MASS_PER_USE;

            __result = hasLiquid && hasIngredient;
        }
    }

    [HarmonyPatch(typeof(EspressoMachine), "IGameObjectEffectDescriptor.GetDescriptors")]
    public static class EspressoMachine_GetDescriptors_Prefix
    {
        static bool Prefix(EspressoMachine __instance, GameObject go, ref List<Descriptor> __result)
        {
            var descs = new List<Descriptor>();
            Descriptor descriptor = new Descriptor();
            descriptor.SetupDescriptor(
                UI.BUILDINGEFFECTS.RECREATION,
                UI.BUILDINGEFFECTS.TOOLTIPS.RECREATION,
                Descriptor.DescriptorType.Effect
            );
            descs.Add(descriptor);

            Effect.AddModifierDescriptions(__instance.gameObject, descs, "Espresso", true);

            // Inline AddRequirementDesc logic for INGREDIENT_TAG
            string ingredientName = EspressoMachine.INGREDIENT_TAG.ProperName();
            Descriptor ingredientDesc = new Descriptor();
            ingredientDesc.SetupDescriptor(
                string.Format(
                    UI.BUILDINGEFFECTS.ELEMENTCONSUMEDPERUSE,
                    ingredientName,
                    GameUtil.GetFormattedMass(EspressoMachine.INGREDIENT_MASS_PER_USE, floatFormat: "{0:0.##}")
                ),
                string.Format(
                    UI.BUILDINGEFFECTS.TOOLTIPS.ELEMENTCONSUMEDPERUSE,
                    ingredientName,
                    GameUtil.GetFormattedMass(EspressoMachine.INGREDIENT_MASS_PER_USE, floatFormat: "{0:0.##}")
                ),
                Descriptor.DescriptorType.Requirement
            );
            descs.Add(ingredientDesc);

            // Use InputLiquid from RecipeComponent instead of FewOptions
            Tag inputLiquid = GameTags.Water; // fallback
            var recipeComponent = __instance.GetComponent<RecipeComponent>();
            if (recipeComponent != null)
            {
                inputLiquid = recipeComponent.InputLiquid;
            }

            // Inline AddRequirementDesc logic for inputLiquid
            string liquidName = inputLiquid.ProperName();
            Descriptor liquidDesc = new Descriptor();
            liquidDesc.SetupDescriptor(
                string.Format(
                    UI.BUILDINGEFFECTS.ELEMENTCONSUMEDPERUSE,
                    liquidName,
                    GameUtil.GetFormattedMass(EspressoMachine.WATER_MASS_PER_USE, floatFormat: "{0:0.##}")
                ),
                string.Format(
                    UI.BUILDINGEFFECTS.TOOLTIPS.ELEMENTCONSUMEDPERUSE,
                    liquidName,
                    GameUtil.GetFormattedMass(EspressoMachine.WATER_MASS_PER_USE, floatFormat: "{0:0.##}")
                ),
                Descriptor.DescriptorType.Requirement
            );
            descs.Add(liquidDesc);

            __result = descs;
            return false; // Skip original
        }
    }

    public class RecipeComponent : KMonoBehaviour
    {
        [Serialize]
        public Tag InputLiquid = GameTags.Water; // Default fallback

        [Serialize]
        public string SelectedRecipeName = null;

        public CafePlusRecipe SelectedRecipe = null;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (!string.IsNullOrEmpty(SelectedRecipeName))
                SelectedRecipe = CafePlusRecipes.ByName.TryGetValue(SelectedRecipeName, out var recipe) ? recipe : null;
        }

        public void SetSelectedRecipe(CafePlusRecipe recipe)
        {
            SelectedRecipe = recipe;
            InputLiquid = recipe != null ? recipe.LiquidIngredient : GameTags.Water;
            SelectedRecipeName = recipe?.Name;
        }
    }

    [HarmonyPatch(typeof(EspressoMachineWorkable), nameof(EspressoMachineWorkable.OnCompleteWork))]
    public static class EspressoMachine_OnCompleteWork_Prefix
    {
        public static bool Prefix(EspressoMachineWorkable __instance, WorkerBase worker)
        {
            var storage = __instance.GetComponent<Storage>();
            float amount_consumed;
            float aggregate_temperature;
            SimUtil.DiseaseInfo disease_info1;
            SimUtil.DiseaseInfo disease_info2;

            // Use selected recipe's input liquid
            var recipeComponent = __instance.GetComponent<RecipeComponent>();
            Tag inputLiquid = GameTags.Water;
            string recipeName = "Unknown";
            if (recipeComponent != null && recipeComponent.SelectedRecipe != null)
            {
                inputLiquid = recipeComponent.InputLiquid;
                recipeName = recipeComponent.SelectedRecipe.Name;
            }

            storage.ConsumeAndGetDisease(inputLiquid, EspressoMachine.WATER_MASS_PER_USE, out amount_consumed, out disease_info1, out aggregate_temperature);
            storage.ConsumeAndGetDisease(EspressoMachine.INGREDIENT_TAG, EspressoMachine.INGREDIENT_MASS_PER_USE, out amount_consumed, out disease_info2, out aggregate_temperature);

            GermExposureMonitor.Instance smi = worker.GetSMI<GermExposureMonitor.Instance>();
            if (smi != null)
            {
                smi.TryInjectDisease(disease_info1.idx, disease_info1.count, inputLiquid, Sickness.InfectionVector.Digestion);
                smi.TryInjectDisease(disease_info2.idx, disease_info2.count, EspressoMachine.INGREDIENT_TAG, Sickness.InfectionVector.Digestion);
            }

            Effects effects = worker.GetComponent<Effects>();
            Debug.Log($"[CafePlus] Giving {recipeName} effect");

            // Skip original
            return false;
        }
    }
}

