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
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TUNING;
using UnityEngine;
using Object = UnityEngine.Object;

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
    }

    public class Patches
    {
        private static bool staticInitialized = false;
        public static readonly CustomLogger Logger = new CustomLogger("FlatulenceMod");
        static Patches()
        {
            if (staticInitialized)
                return;
            staticInitialized = true;
        }

        public static void OnLoad()
        {
            Logger.SetLoggingEnabled(true);
            Logger.Reset();
            LocString.CreateLocStringKeys(typeof(STRINGS), null);

            KeyTestHandler.Register();

            // Ensure FlatulencePeriodic runs by attaching it to a GameObject
            var flatulenceGo = new GameObject("FlatulenceMod_FlatulencePeriodic");
            flatulenceGo.AddComponent<FlatulencePeriodic>();
            UnityEngine.Object.DontDestroyOnLoad(flatulenceGo);
        }

        /// <summary>
        /// Adds FlatulenceSickness to a single minion, with logging.
        /// </summary>
        public static void AddFlatulenceSicknessToMinion(GameObject minionGo)
        {
            var minionIdentity = minionGo.GetComponent<MinionIdentity>();
            if (minionIdentity == null)
                return;

            string dupeLabel = $"{minionIdentity.GetProperName()}({minionGo.GetInstanceID()})";

            var modifiers = minionGo.GetComponent<Modifiers>();
            if (modifiers == null)
            {
                Logger.Log($"[AddFlatulenceSicknessToMinion][TEST] Minion '{dupeLabel}' does NOT have Modifiers component.");
                return;
            }

            var sicknesses = modifiers.sicknesses;
            if (sicknesses == null)
            {
                Logger.Log($"[AddFlatulenceSicknessToMinion][TEST] Minion '{dupeLabel}' does NOT have Sicknesses instance.");
                return;
            }

            bool hasFlatulenceSickness = sicknesses.Get(ModConstants.FLATULENCE_SICKNESS_ID) != null;
            if (!hasFlatulenceSickness)
            {
                Logger.Log($"Added FlatulenceSickness to minion: {dupeLabel}");
                sicknesses.Infect(new SicknessExposureInfo(ModConstants.FLATULENCE_SICKNESS_ID, null));
            }
            else
            {
                Logger.Log($"[AddFlatulenceSicknessToMinion] Minion '{dupeLabel}' already has FlatulenceSickness.");
            }

            hasFlatulenceSickness = sicknesses.Get(ModConstants.FLATULENCE_SICKNESS_ID) != null;
            Logger.Log($"[AddFlatulenceSicknessToMinion] {dupeLabel}");
            Logger.Log($"[TEST][AddFlatulenceSicknessToMinion][TEST] Minion: {dupeLabel}, HasFlatulenceSickness: {hasFlatulenceSickness}");
        }
    }

    internal sealed class KeyTestHandler : IInputHandler
    {
        private static PAction KeyTestAction;
        private readonly Action snapshotAction;

        public string handlerName => "KeyTest Handler";
        public KInputHandler inputHandler { get; set; }

        internal KeyTestHandler()
        {
            var action = KeyTestAction;
            if (action != null)
                snapshotAction = action.GetKAction();
            else
                snapshotAction = PAction.MaxAction;
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (e.TryConsume(snapshotAction))
                FlatulenceMod.Patches.Logger.Log("[KeyTest] Hotkey pressed!");
        }

        // Register the action and handler
        internal static void Register()
        {
            KeyTestAction = new PActionManager().CreateAction(
                "FlatulenceMod.KeyTestAction", "Test Key Action", new PKeyBinding(KKeyCode.F7, Modifier.Ctrl));
            KInputHandler.Add(Global.GetInputManager().GetDefaultController(), new KeyTestHandler(), 512);
        }
    }

    public class Mod : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Patches.OnLoad();
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class RegisterPatch
    {
        private static bool effectRegistered = false;
        private static bool sicknessRegistered = false;

        public static void Postfix()
        {
            FlatulenceMod.Patches.Logger.Log("[RegisterPatch] Db.Initialize postfix called.");

            // Register effect
            if (!effectRegistered)
            {
                var effectsDb = Db.Get().effects;
                if (effectsDb != null)
                {
                    var flatulenceEffect = EFFECTS.CreateFlatulenceEffect();
                    effectsDb.Add(flatulenceEffect);
                    FlatulenceMod.Patches.Logger.Log("[RegisterPatch] FlatulenceEffect registered.");

                    var noFlatulenceEffect = EFFECTS.CreateNoFlatulenceEffect();
                    effectsDb.Add(noFlatulenceEffect);
                    FlatulenceMod.Patches.Logger.Log("[RegisterPatch] NoFlatulenceEffect registered.");
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
                        FlatulenceMod.Patches.Logger.Log("[RegisterPatch] FlatulenceSickness registered.");
                    }
                    else
                    {
                        FlatulenceMod.Patches.Logger.Log("[RegisterPatch] FlatulenceSickness already registered.");
                    }
                }
                sicknessRegistered = true;
            }

            FlatulenceMod.Patches.Logger.Log("[RegisterPatch] Exiting.");
        }
    }

    [HarmonyPatch(typeof(MinionConfig), "OnSpawn")]
    public static class MinionConfigPatch
    {
        public static void Postfix(GameObject go)
        {
            if (go == null)
            {
                FlatulenceMod.Patches.Logger.Log("[MinionConfigPatch] Game Object null.");
                return;
            }

            // Add Flatulence trait to minion if not already present
            var traits = go.GetComponent<Traits>();
            if (traits != null && !traits.HasTrait("Flatulence"))
            {
                var flatulenceTrait = Db.Get().traits.Get("Flatulence");
                if (flatulenceTrait != null)
                {
                    traits.Add(flatulenceTrait);
                    FlatulenceMod.Patches.Logger.Log("[MinionConfigPatch] Flatulent trait added to minion.");
                }
                else
                {
                    FlatulenceMod.Patches.Logger.Log("[MinionConfigPatch] ERROR: Flatulence trait not found in Db.");
                }
            }
            else if (traits == null)
            {
                FlatulenceMod.Patches.Logger.Log("[MinionConfigPatch] Traits component is null, cannot add Flatulent trait.");
            }
            else
            {
                FlatulenceMod.Patches.Logger.Log("[MinionConfigPatch] Minion already has Flatulent trait.");
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
                FlatulenceMod.Patches.Logger.Log($"[FlatulencePeriodic] Timer triggered. Scanning minions...");
                timer = 0f;
                var allMinions = UnityEngine.Object.FindObjectsOfType<GameObject>()
                    .Where(go => go.GetComponent<MinionIdentity>() != null)
                    .ToArray();
                foreach (var go in allMinions)
                {
                    var minionIdentity = go.GetComponent<MinionIdentity>();
                    if (minionIdentity == null)
                    {
                        FlatulenceMod.Patches.Logger.Log($"[FlatulencePeriodic] minionIdentity is null."); return;
                    }
                    var modifiers = go.GetComponent<Modifiers>();
                    var sicknesses = modifiers != null ? modifiers.sicknesses : null;
                    var effects = go.GetComponent<Effects>();
                    var traits = go.GetComponent<Traits>();

                    string dupeLabel = $"{minionIdentity.GetProperName()}({go.GetInstanceID()})";

                    if (traits != null)
                    {
                        bool hasFlatulenceTrait = traits.HasTrait("Flatulence");
                        FlatulenceMod.Patches.Logger.Log(
                            $"[FlatulencePeriodic] Minion: {dupeLabel}, hasFlatulenceTrait: {hasFlatulenceTrait}");

                        // Add FlatulenceSickness if not present
                        if (sicknesses != null && sicknesses.Get(ModConstants.FLATULENCE_SICKNESS_ID) == null)
                        {
                            sicknesses.Infect(new SicknessExposureInfo(ModConstants.FLATULENCE_SICKNESS_ID, null));
                            FlatulenceMod.Patches.Logger.Log($"[FlatulencePeriodic] Added FlatulenceSickness to minion: {dupeLabel}");
                        }
                    }
                    else
                    {
                        FlatulenceMod.Patches.Logger.Log(
                            $"[FlatulencePeriodic] Minion: {dupeLabel} traits is null.");
                    }
                }
                FlatulenceMod.Patches.Logger.Log($"[FlatulencePeriodic] Timer exiting");

            }
        }
    }
   
    [HarmonyPatch(typeof(Flatulence), "Emit")]
    public static class Flatulence_Emit_Patch_Test
    {
        public static void Prefix(Flatulence __instance)
        {
            var minion = __instance.gameObject;
            var minionIdentity = minion.GetComponent<MinionIdentity>();
            var traits = minion.GetComponent<Traits>();
            if (traits != null && traits.HasTrait("Flatulence"))
            {
                string dupeLabel = minionIdentity != null
                    ? $"{minionIdentity.GetProperName()}({minion.GetInstanceID()})"
                    : $"{minion.name}({minion.GetInstanceID()})";
                FlatulenceMod.Patches.Logger.Log($"[Flatulence_Emit_Patch_Test] Dupe '{dupeLabel}' emit pass test triggered.");
            }
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
                FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] CreatePrefab called but prefab already created. Skipping.");
                return null;
            }
            prefabCreated = true;

            var testAnimName = "pill_radiation_kanim";
            var anim = Assets.GetAnim((HashedString)testAnimName);
            if (anim == null)
            {
                FlatulenceMod.Patches.Logger.Log($"[NoFlatulencePillConfig] [Test]: Animation '{testAnimName}' not found!");
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
                FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] ERROR: looseEntity is null after CreateLooseEntity!");
                throw new Exception("Failed to create looseEntity for NoFlatulencePill!");
            }

            EntityTemplates.ExtendEntityToMedicine(looseEntity, MEDICINE.NOFLATULENCEPILL);
            var pillComponent = looseEntity.AddOrGet<MedicinalPill>();
            if (pillComponent == null)
            {
                FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] ERROR: MedicinalPill component is null!");
            }
            else
            {
                FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] MedicinalPill component added.");
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
                FlatulenceMod.Patches.Logger.Log("[NoFlatulencePillConfig] ERROR: ComplexRecipe is null!");
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
        public const float STRESS_PER_CYCLE = .1f;

        public FlatulenceSickness()
            : base(ID, SicknessType.Pathogen, Severity.Minor, 0.0001f, new List<InfectionVector>
            {
                InfectionVector.Inhalation
            }, 60f)
        {
            //this.AddSicknessComponent(new CommonSickEffectSickness());
            //this.AddSicknessComponent(new AnimatedSickness(new HashedString[]
            //{
            //    (HashedString)"anim_idle_allergies_kanim"
            //}, Db.Get().Expressions.Pollen));
            this.AddSicknessComponent(new AttributeModifierSickness(new AttributeModifier[]
            {
                new AttributeModifier(Db.Get().Amounts.Stress.deltaAttribute.Id, STRESS_PER_CYCLE, "Flatulence Sickness"),
            }));
        }
    }
}