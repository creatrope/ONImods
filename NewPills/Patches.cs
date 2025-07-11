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

    public class FlatulencePillConfig : IEntityConfig, IHasDlcRestrictions
    {
        public const string ID = "FlatulencePill";
        public static ComplexRecipe recipe;

        public string[] GetRequiredDlcIds() => DlcManager.EXPANSION1;

        public string[] GetForbiddenDlcIds() => null;

        // Explicitly implement GetDlcIds to avoid relying on default interface implementation
        string[] IEntityConfig.GetDlcIds() => GetRequiredDlcIds();

        public GameObject CreatePrefab()
        {
            GameObject looseEntity = EntityTemplates.CreateLooseEntity(
                "FlatulencePill",
                "FlatulencePill",
                "A pill to help with flatulence.",
                1f,
                true,
                Assets.GetAnim((HashedString)"pill_radiation_kanim"),
                "object",
                Grid.SceneLayer.Front,
                EntityTemplates.CollisionShape.RECTANGLE,
                0.8f,
                0.4f,
                true);

            EntityTemplates.ExtendEntityToMedicine(looseEntity, MEDICINE.FLATULENCEPILL);

            ComplexRecipe.RecipeElement[] recipeElementArray1 = new ComplexRecipe.RecipeElement[1]
            {
                new ComplexRecipe.RecipeElement((Tag)"Carbon", 1f)
            };
            ComplexRecipe.RecipeElement[] recipeElementArray2 = new ComplexRecipe.RecipeElement[1]
            {
                new ComplexRecipe.RecipeElement("FlatulencePill".ToTag(), 1f, ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature)
            };
            FlatulencePillConfig.recipe = new ComplexRecipe(
                ComplexRecipeManager.MakeRecipeID("Apothecary", (IList<ComplexRecipe.RecipeElement>)recipeElementArray1, (IList<ComplexRecipe.RecipeElement>)recipeElementArray2),
                recipeElementArray1,
                recipeElementArray2)
            {
                time = 50f,
                description = "Craft a pill to help with flatulence.", // Use unique recipe desc
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
            public class FLATULENCEPILL
            {
                public static LocString NAME = "Flatulence Pill";
                public static LocString DESC = "A pill to help with flatulence.";
                public static LocString RECIPEDESC = "Craft a pill to help with flatulence.";
            }
        }
    }

    public class MEDICINE
    {
        public const float DEFAULT_MASS = 1;
        public const float RECUPERATION_DISEASE_MULTIPLIER = 1.1f;
        public const float RECUPERATION_DOCTORED_DISEASE_MULTIPLIER = 1.2f;
        public const float WORK_TIME = 10;

        // Add the missing definition for FLATULENCEPILL
        public static readonly MedicineInfo FLATULENCEPILL = new MedicineInfo(
            "FlatulencePill",                // id
            null,                            // effect
            MedicineInfo.MedicineType.CureSpecific, // medicineType
            null,                            // doctorStationId
            new string[] { "Flatulence" }    // curedDiseases: must not be null or empty!
        );
    }

    // Patch MinionIdentity.Sim1000ms to periodically assign a TakeMedicine chore for FlatulencePill
    [HarmonyPatch(typeof(MinionIdentity), "Sim1000ms")]
    public static class FlatulencePeriodicTreatmentChorePatch
    {
        private static float checkInterval = 10f; // seconds
        private static Dictionary<MinionIdentity, float> lastCheckTimes = new Dictionary<MinionIdentity, float>();

        public static void Postfix(MinionIdentity __instance)
        {
            //Patches.Logger.Log($"[FlatulencePeriodicTreatmentChorePatch] Postfix called for minion: {(__instance != null ? __instance.name : "null")}");

            try
            {
                float now = Time.time;
                if (!lastCheckTimes.TryGetValue(__instance, out float lastCheck))
                    lastCheck = 0f;

                if (now - lastCheck < checkInterval)
                    return;
                lastCheckTimes[__instance] = now;

                var traits = __instance.GetComponent<Traits>();
                var effects = __instance.GetComponent<Effects>();
                if (traits == null || effects == null)
                    return;

                // First debug log: trait/effect check
                if (!traits.HasTrait("Flatulence"))
                {
                    Patches.Logger.Log($"Minion {__instance.name} does not have the Flatulence trait, skipping pill chore.");
                    return;
                }
                if (effects.HasEffect("NoFlatulenceEffect"))
                {
                    Patches.Logger.Log($"Minion {__instance.name} has NoFlatulenceEffect, skipping pill chore.");
                    return;
                }

                var choreProvider = __instance.GetComponent<ChoreProvider>();
                if (choreProvider == null)
                    return;

                // Check for existing TakeMedicine chore
                foreach (var kvp in choreProvider.choreWorldMap)
                {
                    foreach (var chore in kvp.Value)
                    {
                        if (chore.choreType == Db.Get().ChoreTypes.TakeMedicine)
                        {
                            Patches.Logger.Log($"[FlatulencePeriodicTreatmentChorePatch] Found existing TakeMedicine chore for minion: {__instance.name}");
                            return;
                        }
                        if (chore.choreType == Db.Get().ChoreTypes.TakeMedicine)
                            return;
                    }
                }

                // Find a Flatulence Pill in the world
                GameObject pill = null;
                foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
                {
                    var prefabId = go != null ? go.GetComponent<KPrefabID>() : null;
                    if (prefabId != null && prefabId.PrefabTag.Name == "FlatulencePill")
                    {
                        pill = go;
                        break;
                    }
                }
                if (pill == null)
                    return;

                var medicineWorkable = pill.GetComponent<MedicinalPillWorkable>();
                if (medicineWorkable == null)
                    return;

                // With this line, assigning the proper type and variable declaration:
                TakeMedicineChore newChore = new TakeMedicineChore(medicineWorkable);

                // Print out the preconditions for newChore and whether the minion meets each one
                if (newChore != null)
                {
                    var preconditions = newChore.GetPreconditions();
                    if (preconditions != null)
                    {
                        foreach (var precondition in preconditions)
                        {
                            string id = precondition.condition.id ?? "(no id)";
                            string desc = precondition.condition.description ?? "(no description)";
                            bool met = false;
                            try
                            {
                                var consumerState = __instance.GetComponent<ChoreConsumerState>();
                                var context = new Chore.Precondition.Context();
                                context.Set(newChore, consumerState, false);
                                context.consumerState = consumerState;
                                context.chore = newChore;
                                context.data = precondition.data;
                                // If you have access to ChoreDriver, set context.driver as well
                                met = precondition.condition.fn != null && precondition.condition.fn(ref context, precondition.data);
                            }
                            catch (Exception ex)
                            {
                                Patches.Logger.Log($"TakeMedicineChore Precondition: id='{id}', description='{desc}', EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                                continue;
                            }
                            Patches.Logger.Log($"TakeMedicineChore Precondition: id='{id}', description='{desc}', met={met}");
                        }
                    }
                    else
                    {
                        Patches.Logger.Log("No preconditions found for TakeMedicineChore.");
                    }
                }

                Patches.Logger.Log($"TakeMedicineChore created for minion: {__instance.name}, pill: {pill.name}");
                // Log all chores in the minion's ChoreProvider after creation
                 choreProvider = __instance.GetComponent<ChoreProvider>();
                if (choreProvider != null)
                {
                    int totalChores = 0;
                    foreach (var kvp in choreProvider.choreWorldMap)
                    {
                        foreach (var c in kvp.Value)
                        {
                            totalChores++;
                            Patches.Logger.Log($"Chore: type={c.choreType?.Id ?? "null"}, target={c.target?.name ?? "null"}");
                        }
                    }
                    Patches.Logger.Log($"Total chores for {__instance.name}: {totalChores}");
                }
                else
                {
                    Patches.Logger.Log($"No ChoreProvider found for {__instance.name}.");
                }
            }
            catch (Exception ex)
            {
                Patches.Logger.Log($"[FlatulencePeriodicTreatmentChorePatch] Exception for minion {(__instance != null ? __instance.name : "null")}: {ex}");
            }
        }
    }
}
