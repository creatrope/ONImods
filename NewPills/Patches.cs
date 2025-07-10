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
                // Register the pill prefab
                Assets.AddPrefab(DoNothingPillConfig.CreatePrefab().GetComponent<KPrefabID>());
                // Register the recipe
                DoNothingPillRecipe.Register();
            }
        }
    }

    public static class DoNothingPillConfig
    {
        public const string ID = "DoNothingPill";

        public static GameObject CreatePrefab()
        {
            // Use the radiation pill animation asset and log if missing
            var anim = Assets.GetAnim("pill_radiation_kanim");
            if (anim == null)
            {
                Patches.Logger.Log("[DoNothingPillConfig] Animation 'pill_radiation_kanim' not found! Prefab will not be created.");
                return null; // Prevents passing null to CreateLooseEntity
            }
            Patches.Logger.Log("[DoNothingPillConfig] Animation 'pill_radiation_kanim' found!");

            var pill = EntityTemplates.CreateLooseEntity(
                ID,
                "Do Nothing Pill",
                "A pill that does absolutely nothing.",
                1f,
                true,
                anim,
                "object",
                Grid.SceneLayer.Front,
                EntityTemplates.CollisionShape.RECTANGLE,
                0.4f, 0.2f, true, 0, SimHashes.Creature, null
            );
            pill.AddOrGet<MyCustomPillWorkable>();
            EntityTemplates.ExtendEntityToMedicine(pill, TUNING.MEDICINE.BASICRADPILL);
            return pill;
        }
    }

    public static class DoNothingPillRecipe
    {
        public static void Register()
        {
            ComplexRecipe.RecipeElement[] input = {
                new ComplexRecipe.RecipeElement(SimHashes.Water.CreateTag(), 1f)
            };
            ComplexRecipe.RecipeElement[] output = {
                new ComplexRecipe.RecipeElement(DoNothingPillConfig.ID, 1f)
            };

            string recipeID = ComplexRecipeManager.MakeRecipeID("Apothecary", input, output);

            new ComplexRecipe(recipeID, input, output)
            {
                time = 10f,
                description = "Crafts a pill that does nothing.",
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag> { "Apothecary".ToTag() }
            };
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
}
