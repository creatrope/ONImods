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
        public const string FLATULENCE_SICKNESS_ID = "FlatulenceSickness";
        public const float FLATULENCE_STRESS_PER_CYCLE = 0.1f;
        public const string FLATULENCE_EFFECT_ID = "FlatulenceEffect";
        public const string NOFLATULENCE_EFFECT_ID = "NoFlatulenceEffect";
        public const string NOFLATULENCE_PILL_ID = "NoFlatulencePill";
        public const float FLATULENCE_EFFECT_DURATION = 15f;
        public const float FLATULENCE_PERIODIC_INTERVAL = 30f;
        public const float NOFLATULENCE_PILL_RECIPE_TIME = 10f;
        public const int NOFLATULENCE_PILL_RECIPE_SORTORDER = 10;
        public const float FLATULENCE_REINFECT_INTERVAL = 30f;

        public const float FLATULENCE_EFFECT_STRESS_MODIFIER = 10f;
        public const float FLATULENCE_SICKNESS_STRESS_PER_CYCLE = 0.01f;

        public const float NOFLATULENCE_EFFECT_DURATION = 15f;

        // New constant for custom emit interval
        public const float FLATULENCE_CUSTOM_EMIT_INTERVAL = 10f;
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

        public bool EnableCustomLog { get; set; }

        private FlatulenceConfig()
        {
            // Initialize default values or load from a configuration file
            EnableCustomLog = false;
        }
    }

    public class Patches
    {
        public static HLib.Logger logger;

        private static bool staticInitialized = false;
        static Patches()
        {
            Patches.logger = new HLib.Logger("FlatulenceMod");
            // Do not enable here; enable from options/config in OnLoad
        }

        public static void OnLoad()
        {
            LocString.CreateLocStringKeys(typeof(STRINGS), null);
            var options = POptions.ReadSettings<ModOptions>() ?? new ModOptions();


            if (options != null)
            {
                Patches.logger.SetLoggingState(options.EnableCustomLog);
                Debug.Log($"[FlatulenceMod] Patches.logger {options.EnableCustomLog}");
            }

            // Ensure FlatulencePeriodic runs by attaching it to a GameObject
            var flatulenceGo = new GameObject("FlatulenceMod_FlatulencePeriodic");
            flatulenceGo.AddComponent<FlatulencePeriodic>();
            UnityEngine.Object.DontDestroyOnLoad(flatulenceGo);
        }

        public static void AddFlatulenceSicknessToMinion(GameObject minionGo)
        {
            var minionIdentity = minionGo.GetComponent<MinionIdentity>();
            if (minionIdentity == null)
                return;

            string dupeLabel = $"{minionIdentity.GetProperName()}({minionGo.GetInstanceID()})";

            var modifiers = minionGo.GetComponent<Modifiers>();
            if (modifiers == null)
            {
                Patches.logger.LogDebug($"[AddFlatulenceSicknessToMinion][TEST] Minion '{dupeLabel}' does NOT have Modifiers component.");
                return;
            }

            var sicknesses = modifiers.sicknesses;
            if (sicknesses == null)
            {
                Patches.logger.LogDebug($"[AddFlatulenceSicknessToMinion][TEST] Minion '{dupeLabel}' does NOT have Sicknesses instance.");
                return;
            }

            bool hasFlatulenceSickness = sicknesses.Get(ModConstants.FLATULENCE_SICKNESS_ID) != null;
            if (!hasFlatulenceSickness)
            {
                Patches.logger.LogDebug($"Added FlatulenceSickness to minion: {dupeLabel}");
                sicknesses.Infect(new SicknessExposureInfo(ModConstants.FLATULENCE_SICKNESS_ID, null));
            }
            else
            {
                Patches.logger.LogDebug($"[AddFlatulenceSicknessToMinion] Minion '{dupeLabel}' already has FlatulenceSickness.");
            }

            hasFlatulenceSickness = sicknesses.Get(ModConstants.FLATULENCE_SICKNESS_ID) != null;
            Patches.logger.LogDebug($"[AddFlatulenceSicknessToMinion] {dupeLabel}");
            Patches.logger.LogDebug($"[TEST][AddFlatulenceSicknessToMinion][TEST] Minion: {dupeLabel}, HasFlatulenceSickness: {hasFlatulenceSickness}");
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
                // Print selected minion info
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
                    var selectedGameObject = selectedObj.gameObject; // Convert KSelectable to GameObject
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
                    FlatulenceMod.Patches.logger.LogDebug("[Hotkey] No object selected for AddFlatulenceTrait.");
                }
            }
            else if (e.TryConsume(removeFlatulenceSnapshot))
            {
                var selectedObj = SelectTool.Instance?.selected;
                if (selectedObj != null)
                {
                    var selectedGameObject = selectedObj.gameObject; // Convert KSelectable to GameObject
                    if (FlatulenceMod.TraitHelpers.RemoveFlatulenceTrait(selectedGameObject))
                    {
                        FlatulenceMod.Patches.logger.LogDebug("[Hotkey] Flatulence trait removed via hotkey.");
                    }
                    else
                    {
                        FlatulenceMod.Patches.logger.LogDebug("[Hotkey] Could not remove Flatulence trait via hotkey.");
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
            if (!traits.HasTrait("Flatulence"))
            {
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
            }
            return false;
        }

        public static bool RemoveFlatulenceTrait(GameObject minionGo)
        {
            if (minionGo == null) return false;
            var traits = minionGo.GetComponent<Traits>();
            if (traits == null) return false;
            if (traits.HasTrait("Flatulence"))
            {
                var flatulenceTrait = Db.Get().traits.Get("Flatulence");
                if (flatulenceTrait != null)
                {
                    traits.Remove(flatulenceTrait);
                    FlatulenceMod.Patches.logger.LogDebug($"[TraitHelpers] Flatulence trait removed from minion: {minionGo.name}({minionGo.GetInstanceID()})");
                    return true;
                }
                else
                {
                    FlatulenceMod.Patches.logger.LogDebug("[TraitHelpers] ERROR: Flatulence trait not found in Db.");
                }
            }
            else
            {
                FlatulenceMod.Patches.logger.LogDebug("[TraitHelpers] Minion does not have Flatulence trait.");
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(MinionConfig), "OnSpawn")]
    public static class MinionConfigPatch
    {
        public static void Postfix(GameObject go)
        {
            if (go == null)
            {
                Patches.logger.LogDebug("[MinionConfigPatch] Game Object null.");
                return;
            }
            if (!TraitHelpers.AddFlatulenceTrait(go))
            {
                Patches.logger.LogDebug("[MinionConfigPatch] Could not add Flatulence trait (already present or missing component).");
            }
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
            string dupeLabel = minionIdentity != null
                ? $"{minionIdentity.GetProperName()}({minion.GetInstanceID()})"
                : $"{minion.name}({minion.GetInstanceID()})";

            if (effects != null && effects.HasEffect(FlatulenceMod.ModConstants.NOFLATULENCE_EFFECT_ID))
            {
                FlatulenceMod.Patches.logger.LogDebug($"[Flatulence_Emit_Patch_Test] EMIT SUPPRESSED for '{dupeLabel}' due to NoFlatulenceEffect.");
                return false; // Suppress emission
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
            __result = 1f;
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

            var testAnimName = "pill_radiation_kanim";
            var anim = Assets.GetAnim((HashedString)testAnimName);
            if (anim == null)
            {
                Patches.logger.LogDebug($"[NoFlatulencePillConfig] [Test]: Animation '{testAnimName}' not found!");
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
                new ComplexRecipe.RecipeElement((Tag)"Carbon", 1f)
            };
            ComplexRecipe.RecipeElement[] results = new ComplexRecipe.RecipeElement[]
            {
                new ComplexRecipe.RecipeElement(ID.ToTag(), 1f, ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature)
            };

            recipe = new ComplexRecipe(
                ComplexRecipeManager.MakeRecipeID("Apothecary", ingredients, results),
                ingredients,
                results)
            {
                time = ModConstants.NOFLATULENCE_PILL_RECIPE_TIME,
                description = STRINGS.ITEMS.MEDICINE.NOFLATULENCEPILL.DESC,
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag>() { (Tag)"Apothecary" },
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

    public class ModOptions
    {
        [Option("Enable Custom Output Log", "Enable or disable writing the custom output log file.")]
        [JsonProperty]
        public bool EnableCustomLog { get; set; }
    }
    [HarmonyPatch(typeof(Klei.AI.Effects), "Remove", new[] { typeof(Effect) })]
    public static class Effects_Remove_Effect_Patch
    {
        public static void Prefix(Effects __instance, Effect effect)
        {
            if (effect != null && effect.Id == FlatulenceMod.ModConstants.NOFLATULENCE_EFFECT_ID)
            {
                var minionIdentity = __instance.gameObject.GetComponent<MinionIdentity>();
                string dupeLabel = minionIdentity != null
                    ? $"{minionIdentity.GetProperName()}({__instance.gameObject.GetInstanceID()})"
                    : $"{__instance.gameObject.name}({__instance.gameObject.GetInstanceID()})";
                FlatulenceMod.Patches.logger.LogDebug($"[NoFlatulenceEffect] EXPIRED (auto) for '{dupeLabel}'.");
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
        /// </summary>
        public static void AddFlatulenceSickness(GameObject minionGo)
        {
            if (minionGo == null) return;
            var minionIdentity = minionGo.GetComponent<MinionIdentity>();
            if (minionIdentity == null) return;

            var modifiers = minionGo.GetComponent<Modifiers>();
            if (modifiers == null) return;

            var sicknesses = modifiers.sicknesses;
            if (sicknesses == null) return;

            if (sicknesses.Get(FlatulenceMod.ModConstants.FLATULENCE_SICKNESS_ID) == null)
            {
                sicknesses.Infect(new SicknessExposureInfo(FlatulenceMod.ModConstants.FLATULENCE_SICKNESS_ID, null));
                FlatulenceMod.Patches.logger.LogDebug($"[SicknessHelpers] Added FlatulenceSickness to minion: {minionGo.name}({minionGo.GetInstanceID()})");
            }
        }
    }
}