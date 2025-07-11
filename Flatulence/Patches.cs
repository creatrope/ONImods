using HarmonyLib;
using HLib;
using Klei.AI;
using KMod;
using KSerialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Flatulence
{
    public class Patches
    {
        private static bool staticInitialized = false;
        public static readonly CustomLogger Logger = new CustomLogger("Flatulence");

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

    [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
    public static class SideScreenRegister
    {
        private static bool registered = false;

        public static void Postfix()
        {
            if (registered) return;
            registered = true;
        }
    }

    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class RegisterFlatulenceEffectPatch
    {
        private static bool effectRegistered = false;

        public static void Postfix()
        {
            Flatulence.Patches.Logger.Log("[RegisterFlatulenceEffectPatch] Db.Initialize postfix called.");
            if (effectRegistered)
            {
                Flatulence.Patches.Logger.Log("[RegisterFlatulenceEffectPatch] Effect already registered, skipping.");
                return;
            }
            var effectsDb = Db.Get().effects;
            if (effectsDb == null)
            {
                Flatulence.Patches.Logger.Log("[RegisterFlatulenceEffectPatch] ERROR: effectsDb is null!");
                return;
            }
            Flatulence.Patches.Logger.Log("[RegisterFlatulenceEffectPatch] Registering FlatulenceEffect...");

            // Always add the effect, only once per session
            var effect = EFFECTS.CreateFlatulenceEffect();
            effectsDb.Add(effect);
            Flatulence.Patches.Logger.Log("[RegisterFlatulenceEffectPatch] FlatulenceEffect registered.");

            effectRegistered = true;
            Flatulence.Patches.Logger.Log("[RegisterFlatulenceEffectPatch] Exiting.");
        }
    }

    [HarmonyPatch(typeof(MinionConfig), "OnSpawn")]
    public static class MinionEffectPatch
    {
        public static void Postfix(GameObject go)
        {
            Flatulence.Patches.Logger.Log("[MinionEffectPatch] Entering.");
            if (go == null)
            {
                Flatulence.Patches.Logger.Log("[MinionEffectPatch] Game Object null.");
                return;
            }

            var effects = go.GetComponent<Effects>();
            if (effects == null)
            {
                Flatulence.Patches.Logger.Log("[MinionEffectPatch] WARNING: Effects component is null for minion: " + (go != null ? go.name : "null"));
                return;
            }

            const string WETFEET_EFFECT_ID = "WetFeet";
            if (Db.Get().effects.Get(WETFEET_EFFECT_ID) != null && !effects.HasEffect(WETFEET_EFFECT_ID))
            {
                Flatulence.Patches.Logger.Log("[MinionEffectPatch] Adding WetFeet effect to minion: " + go.name);
                effects.Add(WETFEET_EFFECT_ID, true);
            }
            else
            {
                Flatulence.Patches.Logger.Log("[MinionEffectPatch] WetFeet effect already present or not found in Db for minion: " + go.name);
            }
        
            if (Db.Get().effects.Get(EFFECTS.FLATULENCE_EFFECT_ID) != null && !effects.HasEffect(EFFECTS.FLATULENCE_EFFECT_ID))
            {
                Flatulence.Patches.Logger.Log($"[MinionEffectPatch] Adding {EFFECTS.FLATULENCE_EFFECT_ID} effect to minion: " + go.name);
                effects.Add(EFFECTS.FLATULENCE_EFFECT_ID, true);
            }
            else
            {
                Flatulence.Patches.Logger.Log($"[MinionEffectPatch] {EFFECTS.FLATULENCE_EFFECT_ID} effect already present or not found in Db for minion: " + go.name);

            }
        }
    }

    public class EFFECTS
    {
        public const string FLATULENCE_EFFECT_ID = "FlatulenceEffect";

        public static Effect CreateFlatulenceEffect()
        {
            var effect = new Effect(
                FLATULENCE_EFFECT_ID,
                "Flatulence Effect",
                "This duplicant suffers from excessive flatulence.",
                duration: -1f,
                show_in_ui: true,
                trigger_floating_text: true, // Show floating text
                is_bad: true
            );
            effect.Add(new AttributeModifier("Stress", 10f, "Gassy")); // Adds stress
            return effect;
        }
    }
}