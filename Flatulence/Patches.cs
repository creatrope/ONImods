using HarmonyLib;
using HLib;
using Klei.AI;
using KMod;
using KSerialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using TUNING;
using UnityEngine;



// ======= MOD CONSTANTS =======
namespace FlatulenceMod
{
    // Sickness and Effect IDs
    public static class ModConstants
    {
        // pill making constants
        public const string NOFLATULENCE_PILL_ID = "NoFlatulencePill";
        public const string FLATULENCE_SICKNESS_ID = "FlatulenceSickness";
        public const string NOFLATULENCE_EFFECT_ID = "NoFlatulenceEffect";
        public const int NOFLATULENCE_PILL_RECIPE_SORTORDER = 10;

        public const float NOFLATULENCE_PILL_RECIPE_TIME = 5f;
        public const float FLATULENCE_REINFECT_INTERVAL = 240f;
        public const float FLATULENCE_SICKNESS_STRESS_PER_CYCLE = 0f;
        public const float NOFLATULENCE_EFFECT_DURATION = 600f;

        // New constant for custom emit interval
        public const float FLATULENCE_CUSTOM_EMIT_INTERVAL = 240f;

        // not adding flatulence effect, so these shouldn't matter.
        public const string FLATULENCE_EFFECT_ID = "FlatulenceEffect";
        public const float FLATULENCE_EFFECT_DURATION = 15f;
        public const float FLATULENCE_EFFECT_STRESS_MODIFIER = 10f;

        // Add these if you want to centralize trait names and animation names
        public const string FLATULENCE_TRAIT_NAME = "Flatulence";
        //public const string TEST_ANIM_NAME = "pill_radiation_kanim";
        public const string ANIM_NAME = "flatulencepill_kanim";

        public const string APOTHECARY_FABRICATOR = "Apothecary";
        public const string PILL_INGREDIENT_TAG = "Carbon";

        public const float PILL_EFFECT_GRACE_PERIOD = 60f; // seconds
    }

    // Assuming FlatulenceConfig is a class that should be defined somewhere in your codebase
    public class FlatulenceConfig
    {
        private static FlatulenceConfig _instance;
        public static FlatulenceConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FlatulenceConfig();
                }
                return _instance;
            }
        }
    }

    public class Patches
    {
        public static HLib.Logger logger = new HLib.Logger("FlatulenceMod");

        private static bool staticInitialized = false;
        public static void OnLoad()
        {
            LocString.CreateLocStringKeys(typeof(STRINGS), null);
            var options = POptions.ReadSettings<ModOptions>() ?? new ModOptions();

            var flatulenceGo = new GameObject("FlatulenceMod_FlatulencePeriodic");
            flatulenceGo.AddComponent<FlatulencePeriodic>();
            UnityEngine.Object.DontDestroyOnLoad(flatulenceGo);
        }

        public static void AddFlatulenceSicknessToMinion(GameObject minionGo)
        {
            FlatulenceMod.Patches.logger.LogDebug("[Patches] AddFlatulenceSicknessToMinion called.");
            var minionIdentity = minionGo.GetComponent<MinionIdentity>();
            if (minionIdentity == null)
            {
                FlatulenceMod.Patches.logger.LogDebug("[Patches] minionIdentity is null.");
                return;
            }

            string dupeLabel = $"{minionIdentity.GetProperName()}({minionGo.GetInstanceID()})";
            FlatulenceMod.Patches.logger.LogDebug($"[Patches] dupeLabel: {dupeLabel}");

            var modifiers = minionGo.GetComponent<Modifiers>();
            if (modifiers == null)
            {
                FlatulenceMod.Patches.logger.LogDebug($"[Patches] Modifiers is null for '{dupeLabel}'.");
                return;
            }

            var sicknesses = modifiers.sicknesses;
            if (sicknesses == null)
            {
                FlatulenceMod.Patches.logger.LogDebug($"[Patches] Sicknesses is null for '{dupeLabel}'.");
                return;
            }

            bool hasFlatulenceSickness = sicknesses.Get(ModConstants.FLATULENCE_SICKNESS_ID) != null;
            FlatulenceMod.Patches.logger.LogDebug($"[Patches] HasFlatulenceSickness: {hasFlatulenceSickness}");

            if (!hasFlatulenceSickness)
            {
                FlatulenceMod.Patches.logger.LogDebug($"[Patches] Infecting '{dupeLabel}' with FlatulenceSickness...");
                sicknesses.Infect(new SicknessExposureInfo(ModConstants.FLATULENCE_SICKNESS_ID, null));
                FlatulenceMod.Patches.logger.LogDebug($"[Patches] Added FlatulenceSickness to minion: {dupeLabel}");
            }
            else
            {
                FlatulenceMod.Patches.logger.LogDebug($"[Patches] Minion '{dupeLabel}' already has FlatulenceSickness.");
            }

            hasFlatulenceSickness = sicknesses.Get(ModConstants.FLATULENCE_SICKNESS_ID) != null;
            FlatulenceMod.Patches.logger.LogDebug($"[Patches] Final HasFlatulenceSickness: {hasFlatulenceSickness}");
        }

        public static void RemoveFlatulenceSicknessFromMinion(GameObject minionGo)
        {
            FlatulenceMod.Patches.logger.LogDebug("[Patches] RemoveFlatulenceSicknessFromMinion called.");
            var minionIdentity = minionGo.GetComponent<MinionIdentity>();
            if (minionIdentity == null)
            {
                FlatulenceMod.Patches.logger.LogDebug("[Patches] minionIdentity is null.");
                return;
            }

            string dupeLabel = $"{minionIdentity.GetProperName()}({minionGo.GetInstanceID()})";
            FlatulenceMod.Patches.logger.LogDebug($"[Patches] dupeLabel: {dupeLabel}");

            var modifiers = minionGo.GetComponent<Modifiers>();
            if (modifiers == null)
            {
                FlatulenceMod.Patches.logger.LogDebug($"[Patches] Modifiers is null for '{dupeLabel}'.");
                return;
            }

            var sicknesses = modifiers.sicknesses;
            if (sicknesses == null)
            {
                FlatulenceMod.Patches.logger.LogDebug($"[Patches] Sicknesses is null for '{dupeLabel}'.");
                return;
            }

            var sickness = sicknesses.Get(ModConstants.FLATULENCE_SICKNESS_ID);
            FlatulenceMod.Patches.logger.LogDebug($"[Patches] Sickness found: {sickness != null}");

            if (sickness != null)
            {
                FlatulenceMod.Patches.logger.LogDebug($"[Patches] Removing FlatulenceSickness from '{dupeLabel}'...");
                sicknesses.Remove(sickness);
                FlatulenceMod.Patches.logger.LogDebug($"[Patches] Removed FlatulenceSickness from minion: {dupeLabel}");
            }
            else
            {
                FlatulenceMod.Patches.logger.LogDebug($"[Patches] Minion '{dupeLabel}' does not have FlatulenceSickness.");
            }
        }
    }

    internal sealed class KeyTestHandler : IInputHandler
    {
        private static PAction KeyTestAction;
        private static PAction KeyTestAction2;
        private static PAction AddFlatulenceAction;
        private static PAction RemoveFlatulenceAction;
        private readonly Action snapshotAction;
        private readonly Action snapshotAction2;
        private readonly Action addFlatulenceSnapshot;
        private readonly Action removeFlatulenceSnapshot;

        public string handlerName => "KeyTest Handler";
        public KInputHandler inputHandler { get; set; }

        internal KeyTestHandler()
        {
            snapshotAction = KeyTestAction != null ? KeyTestAction.GetKAction() : PAction.MaxAction;
            snapshotAction2 = KeyTestAction2 != null ? KeyTestAction2.GetKAction() : PAction.MaxAction;
            addFlatulenceSnapshot = AddFlatulenceAction != null ? AddFlatulenceAction.GetKAction() : PAction.MaxAction;
            removeFlatulenceSnapshot = RemoveFlatulenceAction != null ? RemoveFlatulenceAction.GetKAction() : PAction.MaxAction;
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (e.TryConsume(snapshotAction))
            {
                var selectedObj = SelectTool.Instance?.selected;
                if (selectedObj != null)
                {
                    var minionIdentity = selectedObj.GetComponent<MinionIdentity>();
                    if (minionIdentity != null)
                    {
                        string dupeLabel = $"{minionIdentity.GetProperName()}({selectedObj.GetInstanceID()})";
                        FlatulenceMod.Patches.logger.LogDebug($"[Hotkey] Selected minion: {dupeLabel}");
                    }
                    else
                    {
                        FlatulenceMod.Patches.logger.LogDebug("[Hotkey] Selected object is not a minion.");
                    }
                }
                else
                {
                    FlatulenceMod.Patches.logger.LogDebug("[Hotkey] No object selected.");
                }
            }
            else if (e.TryConsume(snapshotAction2))
            {
                Debug.Log("[Flatulence] CTRL-F8 pressed!");
            }
            else if (e.TryConsume(addFlatulenceSnapshot))
            {
                var selectedObj = SelectTool.Instance?.selected;
                if (selectedObj != null)
                {
                    var minionIdentity = selectedObj.GetComponent<MinionIdentity>();
                    if (minionIdentity != null)
                    {
                        var selectedGameObject = selectedObj.gameObject;
                        if (FlatulenceMod.TraitHelpers.AddFlatulenceTrait(selectedGameObject))
                        {
                            FlatulenceMod.Patches.logger.LogDebug("[Hotkey] Flatulence trait added via hotkey.");
                        }
                        else
                        {
                            FlatulenceMod.Patches.logger.LogDebug("[Hotkey] Could not add Flatulence trait via hotkey.");
                        }
                    }
                    else
                    {
                        FlatulenceMod.Patches.logger.LogDebug("[Hotkey] Selected object is not a minion.");
                    }
                }
                else
                {
                    FlatulenceMod.Patches.logger.LogDebug("[Hotkey] No object selected for AddFlatulenceTrait.");
                }
            }
            else if (e.TryConsume(removeFlatulenceSnapshot))
            {
                var selectedObj = SelectTool.Instance?.selected;
                if (selectedObj != null)
                {
                    var minionIdentity = selectedObj.GetComponent<MinionIdentity>();
                    if (minionIdentity != null)
                    {
                        var selectedGameObject = selectedObj.gameObject;
                        bool traitRemoved = FlatulenceMod.TraitHelpers.RemoveFlatulenceTrait(selectedGameObject);
                        bool sicknessRemoved = SicknessHelpers.RemoveFlatulenceSickness(selectedGameObject);

                        if (traitRemoved)
                        {
                            FlatulenceMod.Patches.logger.LogDebug("[Hotkey] Flatulence trait removed via hotkey.");
                        }
                        else
                        {
                            FlatulenceMod.Patches.logger.LogDebug("[Hotkey] Could not remove Flatulence trait via hotkey.");
                        }

                        if (sicknessRemoved)
                        {
                            FlatulenceMod.Patches.logger.LogDebug("[Hotkey] Flatulence sickness removed via hotkey.");
                        }
                        else
                        {
                            FlatulenceMod.Patches.logger.LogDebug("[Hotkey] No Flatulence sickness to remove via hotkey.");
                        }
                    }
                    else
                    {
                        FlatulenceMod.Patches.logger.LogDebug("[Hotkey] Selected object is not a minion.");
                    }
                }
                else
                {
                    FlatulenceMod.Patches.logger.LogDebug("[Hotkey] No object selected for RemoveFlatulenceTrait.");
                }
            }
        }

        [PLibMethod(RunAt.AfterLayerableLoad)]
        internal static void AddKeycodeHandler()
        {
            KInputHandler.Add(Global.GetInputManager().GetDefaultController(),
                new KeyTestHandler(), 512);
        }

        internal static void Register(PPatchManager manager)
        {
            manager.RegisterPatchClass(typeof(KeyTestHandler));
            KeyTestAction = new PActionManager().CreateAction(
                "FlatulenceMod.KeyTestAction", "Test Key Action", new PKeyBinding(KKeyCode.F7, Modifier.Ctrl));
            KeyTestAction2 = new PActionManager().CreateAction(
                "FlatulenceMod.KeyTestAction2", "Test Key Action 2", new PKeyBinding(KKeyCode.F8, Modifier.Ctrl));
            AddFlatulenceAction = new PActionManager().CreateAction(
                "FlatulenceMod.AddFlatulenceTrait", "Add Flatulence Trait", new PKeyBinding(KKeyCode.F9, Modifier.Ctrl));
            RemoveFlatulenceAction = new PActionManager().CreateAction(
                "FlatulenceMod.RemoveFlatulenceTrait", "Remove Flatulence Trait", new PKeyBinding(KKeyCode.F10, Modifier.Ctrl));
        }
    }

    public class Mod : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            new POptions().RegisterOptions(this, typeof(ModOptions)); // Register the options

            Patches.OnLoad();
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            harmony.PatchAll();
            KeyTestHandler.Register(new PPatchManager(harmony));

        }
    }

    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class RegisterPatch
    {
        private static bool effectRegistered = false;
        private static bool sicknessRegistered = false;

        public static void Postfix()
        {
            Patches.logger.LogDebug("[RegisterPatch] Db.Initialize postfix called.");

            // Register effect
            if (!effectRegistered)
            {
                var effectsDb = Db.Get().effects;
                if (effectsDb != null)
                {
                    var flatulenceEffect = EFFECTS.CreateFlatulenceEffect();
                    effectsDb.Add(flatulenceEffect);
                    Patches.logger.LogDebug("[RegisterPatch] FlatulenceEffect registered.");

                    var noFlatulenceEffect = EFFECTS.CreateNoFlatulenceEffect();
                    effectsDb.Add(noFlatulenceEffect);
                    Patches.logger.LogDebug("[RegisterPatch] NoFlatulenceEffect registered.");
                }

                effectRegistered = true;
            }

            // Register sickness
            if (!sicknessRegistered)
            {
                var sicknessesDb = Db.Get().Sicknesses;
                if (sicknessesDb != null)
                {
                    // Only add if not already present
                    if (sicknessesDb.resources.Find(s => s.id == ModConstants.FLATULENCE_SICKNESS_ID) == null)
                    {
                        sicknessesDb.Add(new FlatulenceSickness());
                        Patches.logger.LogDebug("[RegisterPatch] FlatulenceSickness registered.");
                    }
                    else
                    {
                        Patches.logger.LogDebug("[RegisterPatch] FlatulenceSickness already registered.");
                    }
                }
                sicknessRegistered = true;
            }

            Patches.logger.LogDebug("[RegisterPatch] Exiting.");
        }
    }

    public static class TraitHelpers
    {
        public static bool AddFlatulenceTrait(GameObject minionGo)
        {
            if (minionGo == null) return false;
            var traits = minionGo.GetComponent<Traits>();
            if (traits == null) return false;
            if (traits.HasTrait("Flatulence"))
            {
                FlatulenceMod.Patches.logger.LogDebug($"[TraitHelpers] Minion already has Flatulence trait: {minionGo.name}({minionGo.GetInstanceID()})");
                return true;
            }
            var flatulenceTrait = Db.Get().traits.Get("Flatulence");
            if (flatulenceTrait != null)
            {
                traits.Add(flatulenceTrait);
                FlatulenceMod.Patches.logger.LogDebug($"[TraitHelpers] Flatulence trait added to minion: {minionGo.name}({minionGo.GetInstanceID()})");
                return true;
            }
            else
            {
                FlatulenceMod.Patches.logger.LogDebug("[TraitHelpers] ERROR: Flatulence trait not found in Db.");
            }
            return false;
        }

        public static bool RemoveFlatulenceTrait(GameObject minionGo)
        {
            if (minionGo == null) return false;
            var traits = minionGo.GetComponent<Traits>();
            if (traits == null) return false;
            if (!traits.HasTrait("Flatulence"))
            {
                FlatulenceMod.Patches.logger.LogDebug("[TraitHelpers] Minion does not have Flatulence trait.");
                return true;
            }
            var flatulenceTrait = Db.Get().traits.Get("Flatulence");
            if (flatulenceTrait != null)
            {
                traits.Remove(flatulenceTrait);
                FlatulenceMod.Patches.logger.LogDebug($"[TraitHelpers] Flatulence trait removed from minion: {minionGo.name}({minionGo.GetInstanceID()})");
                return false;
            }
            else
            {
                FlatulenceMod.Patches.logger.LogDebug("[TraitHelpers] ERROR: Flatulence trait not found in Db.");
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(MinionConfig), "OnSpawn")]
    public static class MinionConfigPatch
    {
        public static void Postfix(GameObject go)
        {
        }
    }

    public class FlatulencePeriodic : MonoBehaviour
    {
        private float timer = 0f;
        private const float interval = ModConstants.FLATULENCE_REINFECT_INTERVAL; // Every 60 seconds

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer >= interval)
            {
                Patches.logger.LogDebug($"[FlatulencePeriodic] Timer triggered. Scanning minions...");
                timer = 0f;
                var allMinions = UnityEngine.Object.FindObjectsOfType<GameObject>()
                    .Where(go => go.GetComponent<MinionIdentity>() != null)
                    .ToArray();
                foreach (var go in allMinions)
                {
                    var minionIdentity = go.GetComponent<MinionIdentity>();
                    if (minionIdentity == null)
                    {
                        Patches.logger.LogDebug($"[FlatulencePeriodic] minionIdentity is null."); return;
                    }
                    var traits = go.GetComponent<Traits>();

                    if (traits != null && traits.HasTrait("Flatulence"))
                    {
                        SicknessHelpers.AddFlatulenceSickness(go);
                    }
                }
                Patches.logger.LogDebug($"[FlatulencePeriodic] Timer exiting");
            }
        }
    }

    [HarmonyPatch(typeof(Flatulence), "Emit")]
    public static class Flatulence_Emit_Patch_Test
    {
        public static bool Prefix(Flatulence __instance)
        {
            var minion = __instance.gameObject;
            var minionIdentity = minion.GetComponent<MinionIdentity>();
            var effects = minion.GetComponent<Effects>();
            var modifiers = minion.GetComponent<Modifiers>();
            string dupeLabel = minionIdentity != null
                ? $"{minionIdentity.GetProperName()}({minion.GetInstanceID()})"
                : $"{minion.name}({minion.GetInstanceID()})";

            // Check for FlatulenceSickness
            bool hasFlatulenceSickness = false;
            if (modifiers != null && modifiers.sicknesses != null)
            {
                hasFlatulenceSickness = modifiers.sicknesses.Get(FlatulenceMod.ModConstants.FLATULENCE_SICKNESS_ID) != null;
            }
            if (!hasFlatulenceSickness)
            {
                FlatulenceMod.Patches.logger.LogDebug($"[Flatulence_Emit_Patch_Test] EMIT SKIPPED for '{dupeLabel}' (no FlatulenceSickness).");
                return false;
            }

            if (effects != null && effects.HasEffect(FlatulenceMod.ModConstants.NOFLATULENCE_EFFECT_ID))
            {
                FlatulenceMod.Patches.logger.LogDebug($"[Flatulence_Emit_Patch_Test] EMIT SUPPRESSED for '{dupeLabel}' due to NoFlatulenceEffect.");
                return false; // Suppress emission
            }

            if (PillGracePeriodManager.IsInGracePeriod(minion))
            {
                FlatulenceMod.Patches.logger.LogDebug($"[Flatulence_Emit_Patch_Test] EMIT SUPPRESSED for '{dupeLabel}' due to Pill Effect Grace Period.");
                return false;
            }

            FlatulenceMod.Patches.logger.LogDebug($"[Flatulence_Emit_Patch_Test] EMIT ALLOWED for '{dupeLabel}'.");
            return true; // Allow emission
        }
    }

    [HarmonyPatch(typeof(Flatulence.States), "GetNewInterval")]
    public static class Flatulence_EmitInterval_Patch
    {
        public static bool Prefix(ref float __result)
        {
            // Set a custom interval for testing, e.g., 1 second
            __result = FlatulenceMod.ModConstants.FLATULENCE_CUSTOM_EMIT_INTERVAL;
            return false; // Skip original method
        }
    }

    public class EFFECTS
    {
        public const string FLATULENCE_EFFECT_ID = "FlatulenceEffect";
        public const string NOFLATULENCE_EFFECT_ID = "NoFlatulenceEffect";

        public static Effect CreateFlatulenceEffect()
        {
            var effect = new Effect(
                FLATULENCE_EFFECT_ID,
                STRINGS.EFFECTS.FLATULENCEEFFECT.NAME,
                STRINGS.EFFECTS.FLATULENCEEFFECT.DESC,
                duration: -1f,
                show_in_ui: true,
                trigger_floating_text: false,
                is_bad: true
            );
            effect.Add(new AttributeModifier("Stress", 1f, "Flatulence Stress"));
            return effect;
        }

        public static Effect CreateNoFlatulenceEffect()
        {
            var effect = new Effect(
                NOFLATULENCE_EFFECT_ID,
                STRINGS.EFFECTS.NOFLATULENCEEFFECT.NAME,
                STRINGS.EFFECTS.NOFLATULENCEEFFECT.DESC,
                duration: ModConstants.NOFLATULENCE_EFFECT_DURATION,
                show_in_ui: true,
                trigger_floating_text: false,
                is_bad: false
            );
            return effect;
        }
    }

    public class MEDICINE
    {
        public static readonly MedicineInfo NOFLATULENCEPILL = new MedicineInfo(
            "NoFlatulencePill",
            EFFECTS.NOFLATULENCE_EFFECT_ID,
            MedicineInfo.MedicineType.CureSpecific,
            null,
            new string[] { FlatulenceSickness.ID }
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
                Patches.logger.LogDebug("[NoFlatulencePillConfig] CreatePrefab called but prefab already created. Skipping.");
                return null;
            }
            prefabCreated = true;

            var anim = Assets.GetAnim((HashedString)ModConstants.ANIM_NAME);
            if (anim == null)
            {
                Patches.logger.LogDebug($"[NoFlatulencePillConfig] [Test]: Animation '{ModConstants.ANIM_NAME}' not found!");
            }

            GameObject looseEntity = EntityTemplates.CreateLooseEntity(
                ID,
                STRINGS.ITEMS.MEDICINE.NOFLATULENCEPILL.NAME,
                STRINGS.ITEMS.MEDICINE.NOFLATULENCEPILL.DESC,
                1f,
                true,
                anim,
                "object",
                Grid.SceneLayer.Front,
                EntityTemplates.CollisionShape.RECTANGLE,
                0.8f,
                0.4f,
                true);

            if (looseEntity == null)
            {
                Patches.logger.LogDebug("[NoFlatulencePillConfig] ERROR: looseEntity is null after CreateLooseEntity!");
                throw new Exception("Failed to create looseEntity for NoFlatulencePill!");
            }

            EntityTemplates.ExtendEntityToMedicine(looseEntity, MEDICINE.NOFLATULENCEPILL);
            var pillComponent = looseEntity.AddOrGet<MedicinalPill>();
            if (pillComponent == null)
            {
                Patches.logger.LogDebug("[NoFlatulencePillConfig] ERROR: MedicinalPill component is null!");
            }
            else
            {
                Patches.logger.LogDebug("[NoFlatulencePillConfig] MedicinalPill component added.");
                pillComponent.info = MEDICINE.NOFLATULENCEPILL;
            }

            ComplexRecipe.RecipeElement[] ingredients = new ComplexRecipe.RecipeElement[]
            {
                new ComplexRecipe.RecipeElement((Tag)ModConstants.PILL_INGREDIENT_TAG, 1f)
            };
            ComplexRecipe.RecipeElement[] results = new ComplexRecipe.RecipeElement[]
            {
                new ComplexRecipe.RecipeElement(ID.ToTag(), 1f, ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature)
            };

            recipe = new ComplexRecipe(
                ComplexRecipeManager.MakeRecipeID(ModConstants.APOTHECARY_FABRICATOR, ingredients, results),
                ingredients,
                results)
            {
                time = ModConstants.NOFLATULENCE_PILL_RECIPE_TIME,
                description = STRINGS.ITEMS.MEDICINE.NOFLATULENCEPILL.DESC,
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag>() { (Tag)ModConstants.APOTHECARY_FABRICATOR },
                sortOrder = ModConstants.NOFLATULENCE_PILL_RECIPE_SORTORDER
            };

            if (recipe == null)
            {
                Patches.logger.LogDebug("[NoFlatulencePillConfig] ERROR: ComplexRecipe is null!");
            }
            return looseEntity;
        }

        public void OnPrefabInit(GameObject inst)
        {
        }

        public void OnSpawn(GameObject inst)
        {
        }
    }

    public class FlatulenceSickness : Sickness
    {
        public const string ID = "FlatulenceSickness";

        public FlatulenceSickness()
            : base(ID, SicknessType.Pathogen, Severity.Minor, 0.0001f, new List<InfectionVector>
            {
                    InfectionVector.Inhalation
            }, -1f) // Permanent
        {
            //this.AddSicknessComponent(new CommonSickEffectSickness());
            //this.AddSicknessComponent(new AnimatedSickness(new HashedString[]
            //{
            //    (HashedString)"anim_idle_allergies_kanim"
            //}, Db.Get().Expressions.Pollen));
            this.AddSicknessComponent(new AttributeModifierSickness(new AttributeModifier[]
            {
                    new AttributeModifier(Db.Get().Amounts.Stress.deltaAttribute.Id, ModConstants.FLATULENCE_SICKNESS_STRESS_PER_CYCLE, "Flatulence Sickness"),
            }));
        }
    }

    [ConfigFile(SharedConfigLocation: true)]

    public class ModOptions
    {
        [Option(STRINGS.OPTIONS.NO_FLATULENCE_PILL_RECIPE_TIME, STRINGS.OPTIONS.NO_FLATULENCE_PILL_RECIPE_TIME_DESC)]
        [JsonProperty]
        public float NoFlatulencePillRecipeTime { get; set; } = 5f;

        [Option(STRINGS.OPTIONS.FLATULENCE_REINFECT_INTERVAL, STRINGS.OPTIONS.FLATULENCE_REINFECT_INTERVAL_DESC)]
        [JsonProperty]
        public float FlatulenceReinfectInterval { get; set; } = 240f;

        [Option(STRINGS.OPTIONS.FLATULENCE_SICKNESS_STRESS_PER_CYCLE, STRINGS.OPTIONS.FLATULENCE_SICKNESS_STRESS_PER_CYCLE_DESC)]
        [JsonProperty]
        public float FlatulenceSicknessStressPerCycle { get; set; } = 0f;

        [Option(STRINGS.OPTIONS.NO_FLATULENCE_EFFECT_DURATION, STRINGS.OPTIONS.NO_FLATULENCE_EFFECT_DURATION_DESC)]
        [JsonProperty]
        public float NoFlatulenceEffectDuration { get; set; } = 600f;

        [Option(STRINGS.OPTIONS.FLATULENCE_CUSTOM_EMIT_INTERVAL, STRINGS.OPTIONS.FLATULENCE_CUSTOM_EMIT_INTERVAL_DESC)]
        [JsonProperty]
        public float FlatulenceCustomEmitInterval { get; set; } = 240f;

        [Option("Pill Ingredient", "Tag of the ingredient used to craft the No Flatulence Pill.")]
        [JsonProperty]
        public string PillIngredientTag { get; set; } = "Carbon";

        [Option("Pill Effect Grace Period", "Seconds after NoFlatulenceEffect expires before emission resumes.")]
        [JsonProperty]
        public float PillEffectGracePeriod { get; set; } = FlatulenceMod.ModConstants.PILL_EFFECT_GRACE_PERIOD;
    }
    [HarmonyPatch(typeof(Klei.AI.Effects), "Remove", new[] { typeof(Effect) })]
    public static class Effects_Remove_Effect_Patch
    {
        public static void Prefix(Effects __instance, Effect effect)
        {
            if (effect != null && effect.Id == FlatulenceMod.ModConstants.NOFLATULENCE_EFFECT_ID)
            {
                var minion = __instance.gameObject;
                var options = POptions.ReadSettings<ModOptions>() ?? new ModOptions();
                PillGracePeriodManager.SetGracePeriod(minion, options.PillEffectGracePeriod);
                var minionIdentity = minion.GetComponent<MinionIdentity>();
                string dupeLabel = minionIdentity != null
                    ? $"{minionIdentity.GetProperName()}({minion.GetInstanceID()})"
                    : $"{minion.name}({minion.GetInstanceID()})";
                FlatulenceMod.Patches.logger.LogDebug($"[NoFlatulenceEffect] EXPIRED (auto) for '{dupeLabel}'. Grace period started for {options.PillEffectGracePeriod} seconds.");
            }
        }
    }

    [HarmonyPatch(typeof(Klei.AI.Effects), "Add", new Type[] { typeof(Effect), typeof(bool) })]
    public static class Effects_Add_Effect_Patch
    {
        public static void Prefix(Effects __instance, Effect newEffect, bool should_save)
        {
            if (newEffect != null && newEffect.Id == FlatulenceMod.ModConstants.NOFLATULENCE_EFFECT_ID)
            {
                var minionIdentity = __instance.gameObject.GetComponent<MinionIdentity>();
                string dupeLabel = minionIdentity != null
                    ? $"{minionIdentity.GetProperName()}({__instance.gameObject.GetInstanceID()})"
                    : $"{__instance.gameObject.name}({__instance.gameObject.GetInstanceID()})";
                FlatulenceMod.Patches.logger.LogDebug($"[NoFlatulenceEffect] ACTIVATED for '{dupeLabel}'.");
            }
        }
    }

    public static class SicknessHelpers
    {
        /// <summary>
        /// Adds FlatulenceSickness to the specified minion if not already present.
        /// Returns true if added, false otherwise.
        /// </summary>
        public static bool AddFlatulenceSickness(GameObject minionGo)
        {
            FlatulenceMod.Patches.logger.LogDebug("[SicknessHelpers] AddFlatulenceSickness called.");
            if (minionGo == null)
            {
                FlatulenceMod.Patches.logger.LogDebug("[SicknessHelpers] minionGo is null.");
                return false;
            }
            var minionIdentity = minionGo.GetComponent<MinionIdentity>();
            if (minionIdentity == null)
            {
                FlatulenceMod.Patches.logger.LogDebug("[SicknessHelpers] minionIdentity is null.");
                return false;
            }
            var modifiers = minionGo.GetComponent<Modifiers>();
            if (modifiers == null)
            {
                FlatulenceMod.Patches.logger.LogDebug("[SicknessHelpers] Modifiers is null.");
                return false;
            }
            var sicknesses = modifiers.sicknesses;
            if (sicknesses == null)
            {
                FlatulenceMod.Patches.logger.LogDebug("[SicknessHelpers] Sicknesses is null.");
                return false;
            }
            if (sicknesses.Get(FlatulenceMod.ModConstants.FLATULENCE_SICKNESS_ID) == null)
            {
                sicknesses.Infect(new SicknessExposureInfo(FlatulenceMod.ModConstants.FLATULENCE_SICKNESS_ID, null));
                FlatulenceMod.Patches.logger.LogDebug($"[SicknessHelpers] Added FlatulenceSickness to minion: {minionGo.name}({minionGo.GetInstanceID()})");
                return true;
            }
            FlatulenceMod.Patches.logger.LogDebug("[SicknessHelpers] Minion already has FlatulenceSickness.");
            return false;
        }

        /// <summary>
        /// Removes FlatulenceSickness from the specified minion if present.
        /// Returns true if removed, false otherwise.
        /// </summary>
        public static bool RemoveFlatulenceSickness(GameObject minionGo)
        {
            FlatulenceMod.Patches.logger.LogDebug("[SicknessHelpers] RemoveFlatulenceSickness called.");
            if (minionGo == null)
            {
                FlatulenceMod.Patches.logger.LogDebug("[SicknessHelpers] minionGo is null.");
                return false;
            }
            var minionIdentity = minionGo.GetComponent<MinionIdentity>();
            if (minionIdentity == null)
            {
                FlatulenceMod.Patches.logger.LogDebug("[SicknessHelpers] minionIdentity is null.");
                return false;
            }
            var modifiers = minionGo.GetComponent<Modifiers>();
            if (modifiers == null)
            {
                FlatulenceMod.Patches.logger.LogDebug("[SicknessHelpers] Modifiers is null.");
                return false;
            }
            var sicknesses = modifiers.sicknesses;
            if (sicknesses == null)
            {
                FlatulenceMod.Patches.logger.LogDebug("[SicknessHelpers] Sicknesses is null.");
                return false;
            }
            var sickness = sicknesses.Get(FlatulenceMod.ModConstants.FLATULENCE_SICKNESS_ID);
            if (sickness != null)
            {
                sicknesses.Remove(sickness);
                FlatulenceMod.Patches.logger.LogDebug($"[SicknessHelpers] Removed FlatulenceSickness from minion: {minionGo.name}({minionGo.GetInstanceID()})");
                return true;
            }
            FlatulenceMod.Patches.logger.LogDebug("[SicknessHelpers] Minion does not have FlatulenceSickness.");
            return false;
        }
    }


    public static class PillGracePeriodManager
    {
        // Maps minion instance IDs to the time when grace period ends
        private static readonly ConcurrentDictionary<int, float> gracePeriodEndTimes = new ConcurrentDictionary<int, float>();

        public static void SetGracePeriod(GameObject minion, float duration)
        {
            gracePeriodEndTimes[minion.GetInstanceID()] = Time.unscaledTime + duration;
        }

        public static bool IsInGracePeriod(GameObject minion)
        {
            float endTime;
            if (gracePeriodEndTimes.TryGetValue(minion.GetInstanceID(), out endTime))
            {
                return Time.unscaledTime < endTime;
            }
            return false;
        }

        public static void ClearGracePeriod(GameObject minion)
        {
            gracePeriodEndTimes.TryRemove(minion.GetInstanceID(), out _);
        }
    }
}