using HarmonyLib;
using Newtonsoft.Json; 
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using KMod;
using HLib;

namespace RocketSpeed
{
    public static class Patches 
    {
        public class Mod : UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                base.OnLoad(harmony);
                harmony.PatchAll();
                PUtil.InitLibrary();
                Keybinder.KeyInputHandler.Register(new PPatchManager(harmony), HotKeys.All);
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
}
