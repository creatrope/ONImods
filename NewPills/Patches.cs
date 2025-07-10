using Database;
using Epic.OnlineServices.Platform;
using HarmonyLib;
using HLib;
using KMod;
using KSerialization;
using Newtonsoft.Json; // Ensure this using directive is present

using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic; // For List<> and Dictionary<>
using System.Runtime.CompilerServices; // For ConditionalWeakTable
using TUNING;
using UnityEngine;
using Klei.AI; // Ensure this using directive is present
using static Rendering.BlockTileRenderer;

namespace NewPills
{
    public class MyCustomPillWorkable : Workable
    {
        // Add your custom workable logic here
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            // Custom initialization logic
        }

        // Change the parameter type from Worker to WorkerBase
        protected override void OnCompleteWork(WorkerBase worker)
        {
            base.OnCompleteWork(worker);
            // Custom work completion logic
        }
    }

    public class Patches
    {
        // Change from private to public so HotkeyListenerUpdater can access it
        public static HLib.HotkeyListener hotkeyListener;

        // Add a guard to prevent double static initialization
        private static bool staticInitialized = false;

        // Change Logger field to public static
        public static readonly CustomLogger Logger = new CustomLogger("NewPills");

        static Patches()
        {
            if (staticInitialized)
                return;
            staticInitialized = true;

            var uniqueId = Guid.NewGuid();
            var timestamp = System.DateTime.Now.ToString("O");
            var domain = AppDomain.CurrentDomain.FriendlyName;
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            // Initialize and register hotkeys
            hotkeyListener = new HLib.HotkeyListener();

            hotkeyListener.RegisterHotkey("Ctrl+F11", () =>
            {
                Debug.Log("Hotkey Pressed!");
            });

            // Register for Unity update loop
            HotkeyListenerUpdater.Create();
        }

        public static void OnLoad()
        {
            var options = POptions.ReadSettings<ModOptions>() ?? new ModOptions();
            Patches.Logger.SetLoggingEnabled(true);
            Patches.Logger.Reset();
            Patches.Logger.Log("[DoNothingPillConfig] Logger working.");
        }


        [HarmonyPatch(typeof(Db), "Initialize")]
        public class Db_Initialize_Patch
        {
            public static void Prefix()
            {
            }

            public static void Postfix()
            {
            }
        }
    }

    public class BasicRadPillConfig : IEntityConfig, IHasDlcRestrictions
    {
        public const string ID = "BasicRadPill";
        public static ComplexRecipe recipe;
        public string[] GetRequiredDlcIds() => DlcManager.EXPANSION1;

        public string[] GetForbiddenDlcIds() => null;

        // Explicitly implement GetDlcIds to avoid relying on default interface implementation
        string[] IEntityConfig.GetDlcIds() => GetRequiredDlcIds();


        public GameObject CreatePrefab()
        {
            GameObject looseEntity = EntityTemplates.CreateLooseEntity("BasicRadPill", (string)STRINGS.ITEMS.PILLS.BASICRADPILL.NAME, (string)STRINGS.ITEMS.PILLS.BASICRADPILL.DESC, 1f, true, Assets.GetAnim((HashedString)"pill_radiation_kanim"), "object", Grid.SceneLayer.Front, EntityTemplates.CollisionShape.RECTANGLE, 0.8f, 0.4f, true);
            EntityTemplates.ExtendEntityToMedicine(looseEntity, TUNING.MEDICINE.BASICRADPILL);
            ComplexRecipe.RecipeElement[] recipeElementArray1 = new ComplexRecipe.RecipeElement[1]
            {
                new ComplexRecipe.RecipeElement((Tag)"Carbon", 1f)
            };
            ComplexRecipe.RecipeElement[] recipeElementArray2 = new ComplexRecipe.RecipeElement[1]
            {
                new ComplexRecipe.RecipeElement("BasicRadPill".ToTag(), 1f, ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature)
            };
            BasicRadPillConfig.recipe = new ComplexRecipe(ComplexRecipeManager.MakeRecipeID("Apothecary", (IList<ComplexRecipe.RecipeElement>)recipeElementArray1, (IList<ComplexRecipe.RecipeElement>)recipeElementArray2), recipeElementArray1, recipeElementArray2)
            {
                time = 50f,
                description = (string)STRINGS.ITEMS.PILLS.BASICRADPILL.RECIPEDESC,
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag>() { (Tag)"Apothecary" },
                sortOrder = 10
            };
            return looseEntity;
        }

        public void OnPrefabInit(GameObject inst)
        {
        }

        public void OnSpawn(GameObject inst)
        {
        }
    }

    public class DuplicateRadPillConfig : IEntityConfig, IHasDlcRestrictions
    {
        public const string ID = "DuplicateRadPill";
        public static ComplexRecipe recipe;

        public string[] GetRequiredDlcIds() => DlcManager.EXPANSION1;

        public string[] GetForbiddenDlcIds() => null;

        // Explicitly implement GetDlcIds to avoid relying on default interface implementation
        string[] IEntityConfig.GetDlcIds() => GetRequiredDlcIds();

        public GameObject CreatePrefab()
        {
            GameObject looseEntity = EntityTemplates.CreateLooseEntity(
                "DuplicateRadPill",
                "DuplicateRadPill", // Use unique name
                "My Duplicate Rad Pill", // Use unique desc
                1f,
                true,
                Assets.GetAnim((HashedString)"pill_radiation_kanim"),
                "object",
                Grid.SceneLayer.Front,
                EntityTemplates.CollisionShape.RECTANGLE,
                0.8f,
                0.4f,
                true);

            // Use a unique medicine info for DuplicateRadPill
            EntityTemplates.ExtendEntityToMedicine(looseEntity, MEDICINE.DUPLICATERADPILL);

            ComplexRecipe.RecipeElement[] recipeElementArray1 = new ComplexRecipe.RecipeElement[1]
            {
                new ComplexRecipe.RecipeElement((Tag)"Carbon", 1f)
            };
            ComplexRecipe.RecipeElement[] recipeElementArray2 = new ComplexRecipe.RecipeElement[1]
            {
                new ComplexRecipe.RecipeElement("DuplicateRadPill".ToTag(), 1f, ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature)
            };
            DuplicateRadPillConfig.recipe = new ComplexRecipe(
                ComplexRecipeManager.MakeRecipeID("Apothecary", (IList<ComplexRecipe.RecipeElement>)recipeElementArray1, (IList<ComplexRecipe.RecipeElement>)recipeElementArray2),
                recipeElementArray1,
                recipeElementArray2)
            {
                time = 50f,
                description = "a recipe", // Use unique recipe desc
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag>() { (Tag)"Apothecary" },
                sortOrder = 11
            };
            return looseEntity;
        }

        public void OnPrefabInit(GameObject inst)
        {
        }

        public void OnSpawn(GameObject inst)
        {
        }
    }

    // MonoBehaviour to call HotkeyListener.Update every frame
    public class HotkeyListenerUpdater : KMonoBehaviour
    {
        private static HotkeyListenerUpdater _instance;

        public static void Create()
        {
            if (_instance == null)
            {
                var go = new GameObject("HotKeyListenerUpdater");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<HotkeyListenerUpdater>();
            }

            Patches.Logger.Log("HotkeyListenerUpdater.Create called");
        }

        void Update()
        {
            if (Patches.hotkeyListener != null)
            {
                Patches.hotkeyListener.Update();
            }
            else
            {
                Patches.Logger.Log("[HotkeyListenerUpdater] Patches.hotkeyListener is null.");
            }
        }
    }

    public class ModOptions
    {
        [Option("Enable Custom Output Log", "Enable or disable writing the custom output log file.")]
        [JsonProperty] // Add JSON property for serialization
        public bool EnableCustomLog { get; set; } = true;

        [Option("Max %", "Turn Off % of Overheat Temp")]
        [Limit(5, 100)]
        [JsonProperty] // Add JSON property for serialization
        public float MaxPercent { get; set; } = 90.0f;
        [Option("Min %", "Turn Back On % of Overheat Temp")]
        [Limit(5, 100)]
        [JsonProperty] // Add JSON property for serialization
        public float MinPercent { get; set; } = 80.0f;
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            NewPills.Patches.OnLoad(); // <-- Ensure hotkey system is initialized
            base.OnLoad(harmony);

            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(ModOptions));
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class SideScreenRegister
    {
        private static bool registered = false;

        public static void Postfix()
        {
            if (registered) return;
            registered = true;
            PUIUtils.AddSideScreenContent<SimpleSideScreen>();
        }
    }

    public class ITEMS
    {
        public class PILLS
        {
            public class DUPLICATERADPILL
            {
                public static LocString NAME = "Duplicate Radiation Pill";
                public static LocString DESC = "A pill to reduce radiation exposure.";
                public static LocString RECIPEDESC = "Craft a pill to reduce radiation exposure.";
            }
        }
    }

    public class MEDICINE
    {
        public const float DEFAULT_MASS = 1;
        public const float RECUPERATION_DISEASE_MULTIPLIER = 1.1f;
        public const float RECUPERATION_DOCTORED_DISEASE_MULTIPLIER = 1.2f;
        public const float WORK_TIME = 10;

        // Add the missing definition for DUPLICATERADPILL
        public static readonly MedicineInfo DUPLICATERADPILL = new MedicineInfo(
            "DuplicateRadPill",                // id
            null,                              // effect
            MedicineInfo.MedicineType.CureSpecific, // medicineType
            null,                              // doctorStationId
            new string[] { "RadiationSickness" } // curedDiseases: must not be null or empty!
        );
    }
}
