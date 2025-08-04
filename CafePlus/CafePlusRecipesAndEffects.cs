using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Klei;
using Klei.AI;
using Newtonsoft.Json;
using UnityEngine;

namespace CafePlus
{
    public class CafePlusRecipe
    {
        public string Name { get; set; }
        public string LiquidIngredient { get; set; } // Store as string for JSON, convert to Tag at runtime
        public List<string> Effects { get; set; }
        public RecipeUserType AllowedUsers { get; set; }

        [JsonIgnore]
        public Tag LiquidIngredientTag => ElementLoader.FindElementByHash((SimHashes)Enum.Parse(typeof(SimHashes), LiquidIngredient)).tag;
    }

    public class EffectModifier
    {
        public string AttributeId { get; set; }
        public float Value { get; set; }
        public bool IsMultiplier { get; set; }
    }

    public class CafePlusData
    {
        public List<CafePlusRecipe> Recipes { get; set; }
        public Dictionary<string, List<EffectModifier>> EffectModifiers { get; set; }
    }

    public static class CafePlusRecipes
    {
        public static readonly List<CafePlusRecipe> All;
        public static readonly Dictionary<string, CafePlusRecipe> ByName;

        static CafePlusRecipes()
        {
            var data = CafePlusDataLoader.LoadJsonResource();
            All = data.Recipes ?? new List<CafePlusRecipe>();
            ByName = All.ToDictionary(r => r.Name, r => r);
        }
    }

    public static class CafePlusEffectModifiers
    {
        public static readonly Dictionary<string, List<EffectModifier>> Modifiers;

        static CafePlusEffectModifiers()
        {
            var data = CafePlusDataLoader.LoadJsonResource();
            Modifiers = data.EffectModifiers ?? new Dictionary<string, List<EffectModifier>>();
        }
    }

    public static class CafePlusDataLoader
    {
        private const string ResourceName = "CafePlus.CafePlusData.json";

        public static CafePlusData LoadJsonResource()
        {
            var assembly = Assembly.GetExecutingAssembly();
            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(ResourceName))
                {
                    if (stream == null)
                    {
                        Debug.LogError($"[CafePlus] Embedded resource '{ResourceName}' not found.");
                        Debug.LogError("[CafePlus] Available resources:");
                        foreach (var res in assembly.GetManifestResourceNames())
                            Debug.LogError("[CafePlus] Resource: " + res);
                        return new CafePlusData
                        {
                            Recipes = new List<CafePlusRecipe>(),
                            EffectModifiers = new Dictionary<string, List<EffectModifier>>()
                        };
                    }
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        try
                        {
                            return JsonConvert.DeserializeObject<CafePlusData>(json);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[CafePlus] Failed to parse CafePlusData.json: {ex}");
                            return new CafePlusData
                            {
                                Recipes = new List<CafePlusRecipe>(),
                                EffectModifiers = new Dictionary<string, List<EffectModifier>>()
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CafePlus] Unexpected error loading CafePlusData.json: {ex}");
                return new CafePlusData
                {
                    Recipes = new List<CafePlusRecipe>(),
                    EffectModifiers = new Dictionary<string, List<EffectModifier>>()
                };
            }
        }
    }
}
