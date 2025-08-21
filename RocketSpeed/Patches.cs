using Database;
using Epic.OnlineServices.Platform;
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
using System.Runtime.CompilerServices;
using TUNING;
using UnityEngine;
using static Rendering.BlockTileRenderer;

namespace RocketSpeed
{
    internal sealed class MinimalKeybindHandler : IInputHandler
    {
        private static PAction KeyTestAction;
        private readonly Action snapshotAction;

        public string handlerName => "MinimalKeybindHandler";
        public KInputHandler inputHandler { get; set; }

        internal MinimalKeybindHandler()
        {
            snapshotAction = KeyTestAction != null ? KeyTestAction.GetKAction() : PAction.MaxAction;
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (e.TryConsume(snapshotAction))
            {
                Debug.Log("[RocketSpeed] Hotkey 1 pressed!");
                InstantTravelAllClusterTravelers(); // <-- This calls it directly on Ctrl+F11
            }
        }

        private static void InstantTravelAllClusterTravelers()
        {
            foreach (var traveler in UnityEngine.Object.FindObjectsOfType<ClusterTraveler>())
            {
                if (traveler.IsTraveling())
                {
                    while (traveler.IsTraveling())
                    {
                        traveler.AdvancePathOneStep();
                    }
                    Debug.Log("[RocketSpeed] Instantly moved rocket to destination.");
                }
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
                "RocketSpeed.KeyTestAction", "Test Key Action", new PKeyBinding(KKeyCode.F1, Modifier.Ctrl));
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

	[HarmonyPatch(typeof(ConditionDestinationReachable), "CanReachSpacecraftDestination")]
	public static class NoSpacecraftRangeRestrictionPatch
	{
		public static void Postfix(ref bool __result)
		{
			// Always allow destination to be reachable for spacecraft
			__result = true;
		}
	}
}
