using Database;
using HarmonyLib;
using HLib;
using KMod;
using KSerialization;
using Newtonsoft.Json; // Ensure this using directive is present
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using TUNING;
using UnityEngine;
using Klei.AI; // Add this for Amounts, Attribute, etc.


namespace LifeSpeed
{
    internal sealed class MinimalKeybindHandler : IInputHandler
    {
        private static PAction KeyTestAction;
        private static PAction KeyTestAction2; // Add second action
        private readonly Action snapshotAction;
        private readonly Action snapshotAction2; // Add second action field

        public string handlerName => "MinimalKeybindHandler";
        public KInputHandler inputHandler { get; set; }

        internal MinimalKeybindHandler()
        {
            snapshotAction = KeyTestAction != null ? KeyTestAction.GetKAction() : PAction.MaxAction;
            snapshotAction2 = KeyTestAction2 != null ? KeyTestAction2.GetKAction() : PAction.MaxAction;
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (e.TryConsume(snapshotAction))
            {
                Debug.Log("[MinimalKeybindHandler] Hotkey 1 pressed!");
            }
            else if (e.TryConsume(snapshotAction2))
            {
                Debug.Log("[MinimalKeybindHandler] Hotkey 2 pressed!");
            }
        }

        [PLibMethod(RunAt.AfterLayerableLoad)]
        internal static void AddKeycodeHandler()
        {
            KInputHandler.Add(Global.GetInputManager().GetDefaultController(),
                new MinimalKeybindHandler(), 512);
        }

        internal static void Register(PPatchManager manager)
        {
            manager.RegisterPatchClass(typeof(MinimalKeybindHandler));
            KeyTestAction = new PActionManager().CreateAction(
                "LifeSpeed.KeyTestAction", "Test Key Action", new PKeyBinding(KKeyCode.F11, Modifier.Ctrl));
            KeyTestAction2 = new PActionManager().CreateAction(
                "LifeSpeed.KeyTestAction2", "Test Key Action 2", new PKeyBinding(KKeyCode.F12, Modifier.Ctrl));
        }
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll();
            PUtil.InitLibrary();
            MinimalKeybindHandler.Register(new PPatchManager(harmony));
        }
    }

    [HarmonyPatch(typeof(HatchConfig), nameof(HatchConfig.CreatePrefab))]
    public static class FastReproPatch
    {
        // Adjustable speed constants
        public const float EggLayingSpeedMultiplier = 20f;
        public const float TamingSpeedMultiplier = 20f;
        public const float WildToTameSpeedMultiplier = 10.0f; // Set X here

        static void Postfix(GameObject __result)
        {
            // Find the FertilityMonitor.Def on the prefab and adjust reproduction interval
            var fertility = __result.GetDef<FertilityMonitor.Def>();
            if (fertility != null)
            {
                Debug.Log("[FastCritterLife][HatchConfig_FastReproPatch] FertilityMonitor.Def found: true");
                Debug.Log("[FastCritterLife][HatchConfig_FastReproPatch] baseFertileCycles before: " + fertility.baseFertileCycles);
                fertility.baseFertileCycles /= EggLayingSpeedMultiplier;
                Debug.Log("[FastCritterLife][HatchConfig_FastReproPatch] baseFertileCycles after: " + fertility.baseFertileCycles);
            }
            else
            {
                Debug.LogWarning("[FastCritterLife][HatchConfig_FastReproPatch] FertilityMonitor.Def not found!");
            }

            // Make wild-to-tame transition faster for hatches
            var amounts = __result.GetComponent<Klei.AI.Amounts>();
            if (amounts != null)
            {
                Debug.Log("[FastCritterLife][HatchConfig_FastReproPatch] Amounts found: true");
                var wildnessAmount = Db.Get().Amounts.Wildness;
                if (wildnessAmount != null)
                {
                    Debug.Log("[FastCritterLife][HatchConfig_FastReproPatch] WildnessAmount found: true");
                    var wildnessInstance = amounts.Get(wildnessAmount);
                    if (wildnessInstance != null)
                    {
                        Debug.Log("[FastCritterLife][HatchConfig_FastReproPatch] WildnessInstance found: true");
                        Debug.Log("[FastCritterLife][HatchConfig_FastReproPatch] wildnessInstance.value before: " + wildnessInstance.value);
                        wildnessInstance.SetValue(wildnessInstance.value * TamingSpeedMultiplier);
                        Debug.Log("[FastCritterLife][HatchConfig_FastReproPatch] wildnessInstance.value after: " + wildnessInstance.value);
                        Debug.Log("[FastCritterLife][HatchConfig_FastReproPatch] Wildness taming rate set to " + TamingSpeedMultiplier + "x faster for hatch.");
                    }
                    else
                    {
                        Debug.LogWarning("[FastCritterLife][HatchConfig_FastReproPatch] WildnessInstance not found!");
                    }
                }
                else
                {
                    Debug.LogWarning("[FastCritterLife][HatchConfig_FastReproPatch] WildnessAmount not found!");
                }
            }
            else
            {
                Debug.LogWarning("[FastCritterLife][HatchConfig_FastReproPatch] Amounts not found!");
            }
        }
    }

    public static class EggIncubationSpeedPatch
    {
        public const float IncubationSpeedMultiplier = 20f; // Change this value as needed

        // Shared routine to apply incubation speed multiplier
        public static void ApplyIncubationSpeed(GameObject eggObject)
        {
            var incubation = eggObject.GetDef<IncubationMonitor.Def>();
            if (incubation != null)
            {
                Debug.Log("[FastCritterLife][EggIncubationSpeedPatch] IncubationMonitor.Def found: true");
                Debug.Log("[FastCritterLife][EggIncubationSpeedPatch] baseIncubationRate before: " + incubation.baseIncubationRate);
                incubation.baseIncubationRate *= IncubationSpeedMultiplier;
                Debug.Log("[FastCritterLife][EggIncubationSpeedPatch] baseIncubationRate after: " + incubation.baseIncubationRate);
            }
            else
            {
                Debug.LogWarning("[FastCritterLife][EggIncubationSpeedPatch] IncubationMonitor.Def not found!");
            }
        }
    }

    [HarmonyPatch(typeof(EggConfig), "CreateEgg", new[] {
        typeof(string), typeof(string), typeof(string), typeof(Tag),
        typeof(string), typeof(float), typeof(int), typeof(float),
        typeof(string[]), typeof(string[]), typeof(bool)
    })]
    public static class CreateEggPatch
    {
        static void Postfix(ref GameObject __result)
        {
            Debug.LogWarning("[FastCritterLife][CreateEggPatch] Postfix called");
            EggIncubationSpeedPatch.ApplyIncubationSpeed(__result);
        }
    }

}
