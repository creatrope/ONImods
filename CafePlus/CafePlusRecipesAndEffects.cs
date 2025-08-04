using System;
using System.Collections.Generic;
using System.Linq;
using Klei;
using Klei.AI;
using UnityEngine;

namespace CafePlus
{
    public class CafePlusRecipe
    {
        public string Name { get; }
        public Tag LiquidIngredient { get; }
        public List<string> Effects { get; }
        public RecipeUserType AllowedUsers { get; } // Remove 'set;' to match the type signature

        public CafePlusRecipe(string name, Tag liquidIngredient, List<string> effects, RecipeUserType allowedUsers)
        {
            Name = name;
            LiquidIngredient = liquidIngredient;
            Effects = effects;
            AllowedUsers = allowedUsers;
        }
    }

    public class EffectModifier
    {
        public string AttributeId { get; }
        public float Value { get; }
        public bool IsMultiplier { get; }

        public EffectModifier(string attributeId, float value, bool isMultiplier = false)
        {
            AttributeId = attributeId;
            Value = value;
            IsMultiplier = isMultiplier;
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
            new List<string> { "Espresso" },
            RecipeUserType.Standard
        );

        public static readonly CafePlusRecipe MilkEspresso = new CafePlusRecipe(
            "Milk Espresso",
            MilkTag,
            new List<string> { "EspressoPlus" },
            RecipeUserType.Standard
        );

        public static readonly CafePlusRecipe PetroleumBuzz = new CafePlusRecipe(
            "Petroleum Buzz",
            PetroleumTag,
            new List<string> { "PetroleumBuzz" },
            RecipeUserType.Bionic
        );

        public static readonly CafePlusRecipe OilSlick = new CafePlusRecipe(
            "Oil Slick",
            CrudeOilTag,
            new List<string> { "OilSlick" },
            RecipeUserType.Bionic
        );

        public static readonly List<CafePlusRecipe> All = new List<CafePlusRecipe>
        {
            WaterEspresso,
            MilkEspresso,
            PetroleumBuzz,
            OilSlick
        };

        public static readonly Dictionary<string, CafePlusRecipe> ByName =
            All.ToDictionary(r => r.Name, r => r);
    }

    public static class CafePlusEffectModifiers
    {
        public static readonly Dictionary<string, List<EffectModifier>> Modifiers = new Dictionary<string, List<EffectModifier>>
        {
            { "Espresso", new List<EffectModifier>
                {
                    new EffectModifier("QualityOfLife", 4f),
                    new EffectModifier("Athletics", 1f)
                }
            },
            { "EspressoPlus", new List<EffectModifier>
                {
                    new EffectModifier("QualityOfLife", 4f),
                    new EffectModifier("Athletics", 1f)
                }
            },
            { "PetroleumBuzz", new List<EffectModifier>
                {
                    new EffectModifier("QualityOfLife", 4f),
                    new EffectModifier("Athletics", 1f)
                }
            },
            { "OilSlick", new List<EffectModifier>
                {
                    new EffectModifier("QualityOfLife", 4f),
                    new EffectModifier("Athletics", 1f)
                }
            }
        };
    }
}