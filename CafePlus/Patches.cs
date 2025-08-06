using Database;
using HarmonyLib;
using HLib;
using Klei;
using Klei.AI;
using KMod;
using KSerialization;
using Newtonsoft.Json;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using STRINGS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TUNING;
using UnityEngine;

namespace CafePlus
{
    public static class Mod
    {
        public static void OnLoad(Harmony harmony)
        {
            harmony.PatchAll();
            PUtil.InitLibrary();
        }

        public static void PrintLoadedRecipes()
        {
            Debug.Log("[CafePlus] Loaded Recipes:");
            foreach (var recipe in CafePlusRecipes.All)
            {
                string recipeName = recipe.Recipe ?? "null";
                string effectName = recipe.EffectName ?? "null";
                string liquid = recipe.LiquidIngredient.IsValid ? recipe.LiquidIngredient.ToString() : "null";
                string solid = recipe.SolidIngredient.IsValid ? recipe.SolidIngredient.ToString() : "null";
                string allowed = recipe.AllowedUsers ?? "null";
                string duration = recipe.Effect != null ? $"{recipe.Effect.Duration:0.##}s" : "null";
                string modifiers = "";
                if (recipe.Effect?.Modifiers != null && recipe.Effect.Modifiers.Count > 0)
                {
                    modifiers = " Modifiers: " + string.Join(", ",
                        recipe.Effect.Modifiers.Select(m => $"{m.Key}={m.Value:+0.##;-0.##;0}"));
                }
                Debug.Log($"[CafePlus] Recipe: Name={recipeName}, Effect={effectName}, Duration={duration}, Liquid={liquid}, Solid={solid}, AllowedUsers={allowed}{modifiers}");
            }
        }

        public static void RegisterAllEffects()
        {
            var db = Db.Get();
            if (db == null || db.effects == null)
                return;

            foreach (var recipe in CafePlusRecipes.All)
            {
                var effectId = recipe.Effect?.Name;
                if (string.IsNullOrEmpty(effectId))
                    continue;

                if (!db.effects.Exists(effectId))
                {
                    float duration = recipe.Effect?.Duration > 0 ? recipe.Effect.Duration : 15f;
                    var effect = new Effect(
                        id: effectId,
                        name: effectId,
                        description: $"CafePlus effect: {effectId}",
                        duration: duration,
                        show_in_ui: true,
                        trigger_floating_text: true,
                        is_bad: false
                    );
                    if (recipe.Effect?.Modifiers != null)
                    {
                        foreach (var mod in recipe.Effect.Modifiers)
                            effect.Add(new AttributeModifier(mod.Key, mod.Value, effectId));
                    }
                    db.effects.Add(effect);
                }
            }
        }
    }

    [HarmonyPatch(typeof(EspressoMachineConfig), "ConfigureBuildingTemplate")]
    public static class EspressoMachineConfig_ManualDeliveryKG_Patch
    {
        static void Postfix(GameObject go, Tag prefab_tag)
        {
            var delivery = go.GetComponent<ManualDeliveryKG>();
            var storage = go.GetComponent<Storage>();
            var recipeComponent = go.GetComponent<RecipeComponent>();
            if (delivery != null && storage != null && recipeComponent != null && recipeComponent.SelectedRecipe != null)
            {
                Tag solid = recipeComponent.SelectedRecipe.SolidIngredient.IsValid
                    ? recipeComponent.SelectedRecipe.SolidIngredient
                    : EspressoMachine.INGREDIENT_TAG;
                Debug.Log($"[CafePlus][DEBUG] Setting storage filter to solid ingredient: {solid}");
                storage.storageFilters = new List<Tag> { solid };
                Debug.Log($"[CafePlus][DEBUG] Setting delivery.RequestedItemTag to: {solid}");
                delivery.RequestedItemTag = solid;
            }
        }
    }


    [HarmonyPatch(typeof(EspressoMachineConfig), "ConfigureBuildingTemplate")]
    public static class EspressoMachineConfig_ConfigureBuildingTemplate_CombinedPatch
    {
        static void Postfix(GameObject go, Tag prefab_tag)
        {
            var storage = go.GetComponent<Storage>();
            if (storage != null)
            {
                storage.storageFilters = new List<Tag> { GameTags.Liquid };
            }

            var consumer = go.GetComponent<ConduitConsumer>();
            if (consumer != null)
            {
                consumer.capacityTag = GameTags.Liquid;
            }

            go.AddOrGet<EspressoMachineFewOptions>();
            go.AddOrGet<RecipeComponent>();
            go.AddOrGet<EspressoMachine>();
            go.AddOrGet<EspressoMachineWorkable>();
        }
    }

    [HarmonyPatch(typeof(EspressoMachine), "OnSpawn")]
    public static class EspressoMachine_OnSpawn_AddFewOptionSideScreen
    {
        static void Postfix(global::EspressoMachine __instance)
        {
            var go = __instance.gameObject;
            go.AddOrGet<EspressoMachineFewOptions>();
        }
    }

    public class EspressoMachineFewOptions : KMonoBehaviour, FewOptionSideScreen.IFewOptionSideScreen
    {
        [Serialize]
        private Tag selectedOption = new Tag(CafePlusRecipes.All[0].EffectName);

        private static readonly FewOptionSideScreen.IFewOptionSideScreen.Option[] options =
            CafePlusRecipes.All
                .Select(recipe =>
                {
                    string recipeName = recipe.Recipe;
                    string effectName = recipe.EffectName;
                    string liquidName = recipe.LiquidIngredient.IsValid ? recipe.LiquidIngredient.ProperName() : "None";
                    string solidName = recipe.SolidIngredient.IsValid ? recipe.SolidIngredient.ProperName() : "None";
                    string tooltip = $"Brew {effectName}\nIngredients: {liquidName} (liquid), {solidName} (solid)";
                    if (recipe.Effect != null)
                    {
                        tooltip += $"\nDuration: {recipe.Effect.Duration:0.##}s";
                    }
                    if (recipe.Effect?.Modifiers != null && recipe.Effect.Modifiers.Count > 0)
                    {
                        tooltip += "\nModifiers:";
                        foreach (var mod in recipe.Effect.Modifiers)
                        {
                            tooltip += $"\n  {mod.Key}: {mod.Value:+0.##;-0.##;0}";
                        }
                    }

                    return new FewOptionSideScreen.IFewOptionSideScreen.Option
                    {
                        tag = new Tag(recipeName),
                        labelText = recipeName,
                        tooltipText = tooltip,
                        iconSpriteColorTuple = Def.GetUISprite(recipe.LiquidIngredient),
                    };
                })
                .ToArray();

        public FewOptionSideScreen.IFewOptionSideScreen.Option[] GetOptions() => options;

        public void OnOptionSelected(FewOptionSideScreen.IFewOptionSideScreen.Option option)
        {
            selectedOption = option.tag;
            var recipeComponent = GetComponent<RecipeComponent>();
            CafePlusRecipe recipe = null;
            if (recipeComponent != null)
            {
                recipe = CafePlusRecipes.ByName.TryGetValue(option.tag.Name, out var found) ? found : null;
                recipeComponent.SetSelectedRecipe(recipe);

                var storage = GetComponent<Storage>();
                var delivery = GetComponent<ManualDeliveryKG>();
                if (storage != null && recipe != null)
                {
                    var solid = recipe.SolidIngredient.IsValid ? recipe.SolidIngredient : EspressoMachine.INGREDIENT_TAG;
                    Debug.Log($"[CafePlus][DEBUG] OnOptionSelected: Setting storage filter to solid ingredient: {solid}");
                    storage.storageFilters = new List<Tag> { solid };
                    if (delivery != null)
                    {
                        Debug.Log($"[CafePlus][DEBUG] OnOptionSelected: Setting delivery.RequestedItemTag to: {solid}");
                        delivery.RequestedItemTag = solid;
                        ManualDeliveryKGExtensions.ForceManualDeliveryUpdate(delivery);
                    }
                }
                var consumer = GetComponent<ConduitConsumer>();
                if (consumer != null && recipe != null)
                    consumer.capacityTag = recipe.LiquidIngredient;
            }
        }

        public Tag GetSelectedOption() => selectedOption;

        private void UpdateManualDeliveryTag()
        {
            var delivery = GetComponent<ManualDeliveryKG>();
            var recipeComponent = GetComponent<RecipeComponent>();
            if (delivery != null && recipeComponent != null && recipeComponent.SelectedRecipe != null)
            {
                Tag solid = recipeComponent.SelectedRecipe.SolidIngredient.IsValid
                    ? recipeComponent.SelectedRecipe.SolidIngredient
                    : EspressoMachine.INGREDIENT_TAG;
                delivery.RequestedItemTag = solid;
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            var recipeComponent = GetComponent<RecipeComponent>();
            if (recipeComponent != null)
            {
                var recipe = CafePlusRecipes.ByName.TryGetValue(selectedOption.Name, out var found) ? found : null;
                recipeComponent.SetSelectedRecipe(recipe);

                var storage = GetComponent<Storage>();
                var delivery = GetComponent<ManualDeliveryKG>();
                if (storage != null && recipe != null)
                {
                    var solid = recipe.SolidIngredient.IsValid ? recipe.SolidIngredient : EspressoMachine.INGREDIENT_TAG;
                    Debug.Log($"[CafePlus][DEBUG] OnSpawn: Setting storage filter to solid ingredient: {solid}");
                    storage.storageFilters = new List<Tag> { solid };
                    if (delivery != null)
                    {
                        Debug.Log($"[CafePlus][DEBUG] OnSpawn: Setting delivery.RequestedItemTag to: {solid}");
                        delivery.RequestedItemTag = solid;
                    }
                }
                var consumer = GetComponent<ConduitConsumer>();
                if (consumer != null && recipe != null)
                    consumer.capacityTag = recipe.LiquidIngredient;
            }

            UpdateManualDeliveryTag();
        }
    }

    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class CafePlus_Db_Initialize_Patch
    {
        static void Postfix()
        {
            CafePlus.Mod.RegisterAllEffects();
            CafePlus.Mod.PrintLoadedRecipes();
        }
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

            Tag inputLiquid = recipeComponent.InputLiquid.IsValid ? recipeComponent.InputLiquid : GameTags.DirtyWater;
            PrimaryElement primaryElement = storage.FindPrimaryElement(ElementLoader.GetElement(inputLiquid).id);

            bool hasLiquid = primaryElement != null && primaryElement.Mass >= EspressoMachine.WATER_MASS_PER_USE;
            Tag solid = recipeComponent.SelectedRecipe?.SolidIngredient.IsValid == true
                ? recipeComponent.SelectedRecipe.SolidIngredient
                : EspressoMachine.INGREDIENT_TAG;
            bool hasIngredient = storage.GetAmountAvailable(solid) >= EspressoMachine.INGREDIENT_MASS_PER_USE;

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

            // Use the selected recipe's solid ingredient
            var recipeComponent = __instance.GetComponent<RecipeComponent>();
            Tag solidTag = EspressoMachine.INGREDIENT_TAG;
            if (recipeComponent != null && recipeComponent.SelectedRecipe != null && recipeComponent.SelectedRecipe.SolidIngredient.IsValid)
                solidTag = recipeComponent.SelectedRecipe.SolidIngredient;

            string ingredientName = solidTag.ProperName();
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

            Tag inputLiquid = GameTags.DirtyWater;
            if (recipeComponent != null)
            {
                inputLiquid = recipeComponent.InputLiquid;
            }

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
            return false;
        }
    }

    public class RecipeComponent : KMonoBehaviour
    {
        [Serialize]
        public Tag InputLiquid = GameTags.DirtyWater;

        [Serialize]
        public string SelectedRecipeName = null;

        public CafePlusRecipe SelectedRecipe = null;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (!string.IsNullOrEmpty(SelectedRecipeName))
            {
                // Use ByName with Recipe name, not EffectName
                SelectedRecipe = CafePlusRecipes.ByName.TryGetValue(SelectedRecipeName, out var recipe) ? recipe : null;
            }
            else
            {
                // Find the first water-based espresso recipe
                SelectedRecipe = CafePlusRecipes.All
                    .FirstOrDefault(r => r.Recipe == "Espresso" && r.LiquidIngredient == GameTags.Water);
                if (SelectedRecipe != null)
                {
                    InputLiquid = SelectedRecipe.LiquidIngredient;
                    SelectedRecipeName = SelectedRecipe.Recipe; // Use Recipe name
                }
            }
        }

        public void SetSelectedRecipe(CafePlusRecipe recipe)
        {
            SelectedRecipe = recipe;
            InputLiquid = recipe != null ? recipe.LiquidIngredient : GameTags.DirtyWater;
            SelectedRecipeName = recipe?.Recipe; // Use Recipe name
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

            var recipeComponent = __instance.GetComponent<RecipeComponent>();
            Tag inputLiquid = GameTags.DirtyWater;
            string recipeName = "Unknown";
            List<string> effectIds = null;
            Tag solidIngredient = EspressoMachine.INGREDIENT_TAG;
            if (recipeComponent != null && recipeComponent.SelectedRecipe != null)
            {
                inputLiquid = recipeComponent.InputLiquid;
                recipeName = recipeComponent.SelectedRecipe.EffectName;
                effectIds = recipeComponent.SelectedRecipe.Effects;
                if (recipeComponent.SelectedRecipe.SolidIngredient.IsValid)
                    solidIngredient = recipeComponent.SelectedRecipe.SolidIngredient;
            }

            storage.ConsumeAndGetDisease(inputLiquid, EspressoMachine.WATER_MASS_PER_USE, out amount_consumed, out disease_info1, out aggregate_temperature);
            storage.ConsumeAndGetDisease(solidIngredient, EspressoMachine.INGREDIENT_MASS_PER_USE, out amount_consumed, out disease_info2, out aggregate_temperature);

            GermExposureMonitor.Instance smi = worker.GetSMI<GermExposureMonitor.Instance>();
            if (smi != null)
            {
                smi.TryInjectDisease(disease_info1.idx, disease_info1.count, inputLiquid, Sickness.InfectionVector.Digestion);
                smi.TryInjectDisease(disease_info2.idx, disease_info2.count, solidIngredient, Sickness.InfectionVector.Digestion);
            }

            Effects effects = worker.GetComponent<Effects>();
            if (effects != null && effectIds != null)
            {
                foreach (var effectId in effectIds)
                {
                    effects.Add(effectId, true);
                }
            }

            return false;
        }
    }

    public static class RecipeUserTypeUtil
    {
        public static bool IsWorkerAllowed(WorkerBase worker, RecipeUserType allowed)
        {
            Tag minionModel = GameTags.Minions.Models.Standard;
            if (worker != null && worker.gameObject != null)
            {
                var tagComponent = worker.gameObject.GetComponent<KPrefabID>();
                if (tagComponent != null && tagComponent.HasTag(GameTags.Minions.Models.Bionic))
                    minionModel = GameTags.Minions.Models.Bionic;
            }

            if ((allowed & RecipeUserType.Bionic) != 0 && minionModel == GameTags.Minions.Models.Bionic)
                return true;
            if ((allowed & RecipeUserType.Standard) != 0 && minionModel == GameTags.Minions.Models.Standard)
                return true;
            return false;
        }
    }

    [HarmonyPatch(typeof(EspressoMachine.States), nameof(EspressoMachine.States.CreateChore))]
    public static class EspressoMachine_States_CreateChore_Patch
    {
        static void Postfix(EspressoMachine.StatesInstance smi, Chore __result)
        {
            if (__result == null)
                return;
            var recipeComponent = smi.master.GetComponent<RecipeComponent>();
            if (recipeComponent == null)
                return;
            if (recipeComponent.SelectedRecipe == null)
                return;

            // Prevent dupes from using the machine if they have ANY CafePlus effect
            __result.AddPrecondition(
                new Chore.Precondition
                {
                    id = "CafePlus:HasNoCafePlusEffect",
                    description = "Worker must not have any CafePlus drink effect",
                    fn = (ref Chore.Precondition.Context context, object data) =>
                    {
                        var worker = context.consumerState.worker;
                        if (worker == null)
                        {
                            Debug.Log("[CafePlus][DEBUG] Precondition: worker is null, returning false.");
                            return false;
                        }
                        var effects = worker.GetComponent<Effects>();
                        if (effects == null)
                        {
                            Debug.Log($"[CafePlus][DEBUG] Precondition: worker '{worker.name}' has no Effects component, returning true.");
                            return true;
                        }

                        Debug.Log($"[CafePlus][DEBUG] Precondition: checking CafePlus effects for worker '{worker.name}'.");

                        // Use EffectName, not Recipe name
                        foreach (var effectName in CafePlusRecipes.All.Select(r => r.EffectName).Where(n => !string.IsNullOrEmpty(n)))
                        {
                            if (effects.HasEffect(effectName))
                            {
                                Debug.Log($"[CafePlus][DEBUG] Chore blocked for worker '{worker.name}' because CafePlus effect '{effectName}' is active.");
                                return false;
                            }
                        }
                        Debug.Log($"[CafePlus][DEBUG] Precondition: worker '{worker.name}' has no active CafePlus effects, returning true.");
                        return true;
                    }
                }
            );

            __result.AddPrecondition(
                new Chore.Precondition
                {
                    id = "CafePlus:AllowedUserType",
                    description = "Worker must match recipe allowed user type",
                    fn = (ref Chore.Precondition.Context context, object data) =>
                    {
                        var worker = context.consumerState.worker;
                        var go = worker?.gameObject;
                        var tagComponent = go?.GetComponent<KPrefabID>();
                        var hasBionicTag = tagComponent != null && tagComponent.HasTag(GameTags.Minions.Models.Bionic);
                        var minionModel = hasBionicTag ? GameTags.Minions.Models.Bionic : GameTags.Minions.Models.Standard;

                        var recipe = smi.master.GetComponent<RecipeComponent>()?.SelectedRecipe;
                        string allowedUsersRaw = recipe?.AllowedUsers ?? "null";
                        RecipeUserType allowed = RecipeUserType.None;
                        bool parsed = Enum.TryParse(allowedUsersRaw, out allowed);

                        Debug.Log($"[CafePlus][DEBUG] AllowedUserType precondition for worker '{worker?.name ?? "null"}':");
                        Debug.Log($"[CafePlus][DEBUG]  - hasBionicTag: {hasBionicTag}");
                        Debug.Log($"[CafePlus][DEBUG]  - minionModel: {minionModel}");
                        Debug.Log($"[CafePlus][DEBUG]  - recipe.AllowedUsers: {allowedUsersRaw} (parsed: {parsed}, value: {allowed})");

                        bool allowedResult = RecipeUserTypeUtil.IsWorkerAllowed(worker, allowed);
                        Debug.Log($"[CafePlus][DEBUG]  - IsWorkerAllowed result: {allowedResult}");

                        if (!allowedResult)
                            Debug.Log($"[CafePlus][DEBUG] Chore blocked for worker '{worker?.name ?? "null"}' due to AllowedUserType mismatch.");

                        return allowedResult;
                    },
                }
            );
        }
    }

    public enum RecipeUserType
    {
        None = 0,
        Standard = 1 << 0,
        Bionic = 1 << 1,
        All = Standard | Bionic
    }

    public class CafePlusRecipe
    {
        public string Recipe { get; set; }
        public EffectData Effect { get; set; }
        public Tag LiquidIngredient { get; set; }
        public Tag SolidIngredient { get; set; } // <-- Add this line

        public string AllowedUsers { get; set; }

        [JsonIgnore]
        public List<string> Effects => Effect != null && !string.IsNullOrEmpty(Effect.Name) ? new List<string> { Effect.Name } : new List<string>();

        [JsonIgnore]
        public string EffectName => Effect?.Name;
    }

    public class EffectData
    {
        public string Name { get; set; }
        public Dictionary<string, float> Modifiers { get; set; }
        public float Duration { get; set; }
    }

    public class CafePlusData
    {
        public List<CafePlusRecipe> Recipes { get; set; }
    }

    public static class CafePlusRecipes
    {
        public static readonly List<CafePlusRecipe> All;
        public static readonly Dictionary<string, CafePlusRecipe> ByName;
        public static readonly HashSet<Tag> AllSolidIngredients;

        static CafePlusRecipes()
        {
            var data = CafePlusDataLoader.LoadJsonResource();
            All = data.Recipes ?? new List<CafePlusRecipe>();
            foreach (var recipe in All)
            {
                if (recipe.LiquidIngredient == null || !recipe.LiquidIngredient.IsValid)
                {
                    recipe.LiquidIngredient = Tag.Invalid;
                }
            }
            // Use Recipe (unique recipe name) as the key, not EffectName
            ByName = new Dictionary<string, CafePlusRecipe>();
            foreach (var recipe in All)
            {
                if (!string.IsNullOrEmpty(recipe.Recipe) && !ByName.ContainsKey(recipe.Recipe))
                    ByName.Add(recipe.Recipe, recipe);
            }

            // Collect all unique valid solid ingredients
            AllSolidIngredients = new HashSet<Tag>(
                All.Where(r => r.SolidIngredient.IsValid)
                   .Select(r => r.SolidIngredient)
            );
        }
    }

    public static class CafePlusDataLoader
    {
        private const string EmbeddedResourceName = "CafePlus.CafePlusConfig.json";
        private const string UserConfigFileName = "User.CafePlusConfig.json";

        public static CafePlusData LoadJsonResource()
        {
            CafePlusData baseData = null;
            int embeddedCount = 0;
            int userCount = 0;

            var assembly = Assembly.GetExecutingAssembly();
            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(EmbeddedResourceName))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string json = reader.ReadToEnd();
                            baseData = JsonConvert.DeserializeObject<CafePlusData>(json);
                            if (baseData != null && baseData.Recipes != null)
                                embeddedCount = baseData.Recipes.Count;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            if (baseData == null)
            {
                baseData = new CafePlusData
                {
                    Recipes = new List<CafePlusRecipe>(),
                };
            }

            try
            {
                string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string userConfigPath = Path.Combine(exeDir, UserConfigFileName);
                if (File.Exists(userConfigPath))
                {
                    string userJson = File.ReadAllText(userConfigPath);
                    var userData = JsonConvert.DeserializeObject<CafePlusData>(userJson);

                    var recipeDict = baseData.Recipes
                        .Where(r => !string.IsNullOrEmpty(r.Recipe))
                        .ToDictionary(r => r.Recipe, r => r);
                    if (userData.Recipes != null)
                    {
                        foreach (var userRecipe in userData.Recipes.Where(r => !string.IsNullOrEmpty(r.Recipe)))
                        {
                            recipeDict[userRecipe.Recipe] = userRecipe;
                        }
                        userCount = userData.Recipes.Count;
                    }
                    baseData.Recipes = recipeDict.Values.ToList();
                }
            }
            catch (Exception)
            {
            }

            Debug.Log($"[CafePlus] Embedded recipes loaded: {embeddedCount}, user recipes loaded: {userCount}, total recipes: {baseData.Recipes.Count}");

            return baseData;
        }
    }

    public static class CafePlusEffectModifiers
    {
        public static readonly Dictionary<string, List<EffectModifier>> Modifiers = new Dictionary<string, List<EffectModifier>>
        {
        };
    }

    public class EffectModifier
    {
        public string AttributeId { get; set; }
        public float Value { get; set; }
        public bool IsMultiplier { get; set; }
    }

    public static class ManualDeliveryKGExtensions
    {
        public static void ForceManualDeliveryUpdate(ManualDeliveryKG delivery)
        {
            delivery.UpdateDeliveryState();
        }
    }
}

