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
                string allowed = recipe.AllowedUsers ?? "null";
                string modifiers = "";
                if (recipe.Effect?.Modifiers != null && recipe.Effect.Modifiers.Count > 0)
                {
                    modifiers = " Modifiers: " + string.Join(", ",
                        recipe.Effect.Modifiers.Select(m => $"{m.Key}={m.Value:+0.##;-0.##;0}"));
                }
                Debug.Log($"[CafePlus] Recipe: Name={recipeName}, Effect={effectName}, Liquid={liquid}, AllowedUsers={allowed}{modifiers}");
            }
        }

        public static void RegisterAllRecipes()
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
                    var effect = new Effect(
                        id: effectId,
                        name: effectId,
                        description: $"CafePlus effect: {effectId}",
                        duration: 15f,
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
                    string effectName = recipe.EffectName;
                    string tooltip = $"Brew {effectName}";
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
                        tag = new Tag(effectName),
                        labelText = effectName,
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
                if (storage != null && recipe != null)
                    storage.storageFilters = new List<Tag> { recipe.LiquidIngredient, EspressoMachine.INGREDIENT_TAG };
                var consumer = GetComponent<ConduitConsumer>();
                if (consumer != null && recipe != null)
                    consumer.capacityTag = recipe.LiquidIngredient;
            }

            Debug.Log($"[CafePlus] EspressoMachineFewOptions: Selected {option.labelText}");
            Debug.Log($"[CafePlus] Selected InputLiquid: {(recipe != null ? recipe.LiquidIngredient : "null")}");
            Debug.Log($"[CafePlus] Selected Recipe: {(recipe != null ? recipe.EffectName : "null")}");
        }

        public Tag GetSelectedOption() => selectedOption;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            var recipeComponent = GetComponent<RecipeComponent>();
            if (recipeComponent != null)
            {
                var recipe = CafePlusRecipes.ByName.TryGetValue(selectedOption.Name, out var found) ? found : null;
                recipeComponent.SetSelectedRecipe(recipe);

                var storage = GetComponent<Storage>();
                if (storage != null && recipe != null)
                    storage.storageFilters = new List<Tag> { recipe.LiquidIngredient, EspressoMachine.INGREDIENT_TAG };
                var consumer = GetComponent<ConduitConsumer>();
                if (consumer != null && recipe != null)
                    consumer.capacityTag = recipe.LiquidIngredient;
            }
        }
    }

    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class CafePlus_Db_Initialize_Patch
    {
        static void Postfix()
        {
            CafePlus.Mod.RegisterAllRecipes();
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
                if (recipeComponent == null)
                    Debug.LogError("[CafePlus] ERROR: RecipeComponent is missing in IsReadyPostfix!");
                __result = false;
                return;
            }

            Tag inputLiquid = recipeComponent.InputLiquid.IsValid ? recipeComponent.InputLiquid : GameTags.DirtyWater;
            PrimaryElement primaryElement = storage.FindPrimaryElement(ElementLoader.GetElement(inputLiquid).id);

            bool hasLiquid = primaryElement != null && primaryElement.Mass >= EspressoMachine.WATER_MASS_PER_USE;
            bool hasIngredient = storage.GetAmountAvailable(EspressoMachine.INGREDIENT_TAG) >= EspressoMachine.INGREDIENT_MASS_PER_USE;

            Debug.Log($"[CafePlus][IsReady] InputLiquid={inputLiquid}, Mass={(primaryElement != null ? primaryElement.Mass.ToString() : "null")}, HasLiquid={hasLiquid}, HasIngredient={hasIngredient}");

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

            Tag inputLiquid = GameTags.DirtyWater;
            var recipeComponent = __instance.GetComponent<RecipeComponent>();
            if (recipeComponent != null)
            {
                inputLiquid = recipeComponent.InputLiquid;
            }
            else
            {
                Debug.LogError("[CafePlus] ERROR: RecipeComponent is missing in GetDescriptors!");
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
                SelectedRecipe = CafePlusRecipes.ByName.TryGetValue(SelectedRecipeName, out var recipe) ? recipe : null;
        }

        public void SetSelectedRecipe(CafePlusRecipe recipe)
        {
            SelectedRecipe = recipe;
            InputLiquid = recipe != null ? recipe.LiquidIngredient: GameTags.DirtyWater;
            SelectedRecipeName = recipe?.EffectName;
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
            if (recipeComponent != null && recipeComponent.SelectedRecipe != null)
            {
                inputLiquid = recipeComponent.InputLiquid;
                recipeName = recipeComponent.SelectedRecipe.EffectName;
                effectIds = recipeComponent.SelectedRecipe.Effects;
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
            {
                Debug.Log("[CafePlus][Precondition] __result is null, skipping precondition.");
                return;
            }
            var recipeComponent = smi.master.GetComponent<RecipeComponent>();
            if (recipeComponent == null)
            {
                Debug.LogError("[CafePlus] ERROR: RecipeComponent is missing in CreateChore!");
                Debug.Log("[CafePlus][Precondition] RecipeComponent is null on master.");
                return;
            }
            if (recipeComponent.SelectedRecipe == null)
            {
                Debug.Log("[CafePlus][Precondition] SelectedRecipe is null.");
                return;
            }

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

                        var recipe = recipeComponent.SelectedRecipe;
                        var allowed = recipe != null && Enum.TryParse(recipe.AllowedUsers, out RecipeUserType parsedAllowedUsers)
? parsedAllowedUsers
: RecipeUserType.None;
                        bool allowedResult = RecipeUserTypeUtil.IsWorkerAllowed(worker, allowed);
                        return allowedResult;
                    },
                }
            );
            Debug.Log("[CafePlus] Added user type precondition to EspressoMachine chore.");
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
    }

    public class CafePlusData
    {
        public List<CafePlusRecipe> Recipes { get; set; }
    }

    public static class CafePlusRecipes
    {
        public static readonly List<CafePlusRecipe> All;
        public static readonly Dictionary<string, CafePlusRecipe> ByName;

        static CafePlusRecipes()
        {
            var data = CafePlusDataLoader.LoadJsonResource();
            All = data.Recipes ?? new List<CafePlusRecipe>();
            // Convert string to Tag using FindElement and add debug logging
            foreach (var recipe in All)
            {
                if (recipe.LiquidIngredient != null && recipe.LiquidIngredient.IsValid)
                {
                    Debug.Log($"[CafePlus][RecipeLoad] Recipe '{recipe.Recipe}': LiquidIngredient='{recipe.LiquidIngredient}' is valid.");
                }
                else
                {
                    recipe.LiquidIngredient = Tag.Invalid;
                    Debug.LogWarning($"[CafePlus][RecipeLoad] Recipe '{recipe.Recipe}': LiquidIngredient is null or invalid, using Tag.Invalid.");
                }
            }
            ByName = All.ToDictionary(r => r.EffectName, r => r);
        }
    }

    public static class CafePlusDataLoader
    {
        private const string EmbeddedResourceName = "CafePlus.CafePlusConfig.json";
        private const string UserConfigFileName = "User.CafePlusConfig.json";

        public static CafePlusData LoadJsonResource()
        {
            CafePlusData baseData = null;

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
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CafePlus] Error loading embedded config: {ex}");
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
                    Debug.Log($"[CafePlus] Loading user config: {userConfigPath}");
                    string userJson = File.ReadAllText(userConfigPath);
                    var userData = JsonConvert.DeserializeObject<CafePlusData>(userJson);

                    var recipeDict = baseData.Recipes
                        .Where(r => !string.IsNullOrEmpty(r.EffectName))
                        .ToDictionary(r => r.EffectName, r => r);
                    if (userData.Recipes != null)
                    {
                        foreach (var recipe in userData.Recipes.Where(r => !string.IsNullOrEmpty(r.EffectName)))
                        {
                            recipeDict[recipe.EffectName] = recipe;
                        }
                    }
                    baseData.Recipes = recipeDict.Values.ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CafePlus] Error loading/merging user config: {ex}");
            }

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
}

