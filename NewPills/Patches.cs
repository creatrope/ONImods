using System.Collections.Generic;
using UnityEngine;
using Klei.AI; // Assuming 'Effect' is part of the Klei.AI namespace

namespace NewPills
{
    public class EFFECTS
    {
        public const string FLATULENCE_EFFECT_ID = "FlatulenceEffect";

        public static Effect CreateFlatulenceEffect()
        {
            // duration: -1 means permanent until cured
            return new Effect(
                FLATULENCE_EFFECT_ID,
            "Flatulence Effect", // Changed display name to further distinguish
                "This duplicant suffers from excessive flatulence.",
                duration: -1f,
                show_in_ui: true,
                trigger_floating_text: false,
                is_bad: true
            );
        }
    }

    public class MEDICINE
    {
        public static readonly MedicineInfo NOFLATULENCEPILL = new MedicineInfo(
            "NoFlatulencePill",
            EFFECTS.FLATULENCE_EFFECT_ID, // effect: cures FlatulenceEffect
            MedicineInfo.MedicineType.CureSpecific, // medicineType: cures a specific trait/disease
            null,                              // doctorStationId: no specific station required
            null                               // curedDiseases: not needed if using effect
        );
    }

    public class NoFlatulencePillConfig : IEntityConfig, IHasDlcRestrictions
    {
        public const string ID = "NoFlatulencePill";
        public static ComplexRecipe recipe;
        public string[] GetRequiredDlcIds() => DlcManager.EXPANSION1;
        public string[] GetForbiddenDlcIds() => null;
        string[] IEntityConfig.GetDlcIds() => GetRequiredDlcIds();

        public GameObject CreatePrefab()
        {
            GameObject looseEntity = EntityTemplates.CreateLooseEntity(
                "NoFlatulencePill",
                "NoFlatulencePill",
                "NoFlatulencePill Cure",
                1f,
                true,
                Assets.GetAnim((HashedString)"pill_radiation_kanim"),
                "object",
                Grid.SceneLayer.Front,
                EntityTemplates.CollisionShape.RECTANGLE,
                0.8f,
                0.4f,
                true);

            EntityTemplates.ExtendEntityToMedicine(looseEntity, MEDICINE.NOFLATULENCEPILL);

            var pillComponent = looseEntity.AddOrGet<MedicinalPill>();
            pillComponent.info = MEDICINE.NOFLATULENCEPILL;

            ComplexRecipe.RecipeElement[] recipeElementArray1 = new ComplexRecipe.RecipeElement[1]
            {
                new ComplexRecipe.RecipeElement((Tag)"Carbon", 1f)
            };
            ComplexRecipe.RecipeElement[] recipeElementArray2 = new ComplexRecipe.RecipeElement[1]
            {
                new ComplexRecipe.RecipeElement("NoFlatulencePill".ToTag(), 1f, ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature)
            };
            NoFlatulencePillConfig.recipe = new ComplexRecipe(
                ComplexRecipeManager.MakeRecipeID("Apothecary", (IList<ComplexRecipe.RecipeElement>)recipeElementArray1, (IList<ComplexRecipe.RecipeElement>)recipeElementArray2),
                recipeElementArray1,
                recipeElementArray2)
            {
                time = 50f,
                description = "Craft a pill to help with flatulence.",
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag>() { (Tag)"Apothecary" },
                sortOrder = 10
            };
            return looseEntity;
        }

        public void OnPrefabInit(GameObject inst) { }

        public void OnSpawn(GameObject inst)
        {
            // Assign FlatulenceEffect to the minion when spawned
            var effects = inst.GetComponent<Effects>();
            if (effects != null && !effects.HasEffect(EFFECTS.FLATULENCE_EFFECT_ID))
            {
                effects.Add(EFFECTS.FLATULENCE_EFFECT_ID, true);
            }
        }
    }

}