using HarmonyLib;
using KMod;
// Ensure the correct namespace for PUtil is included
using PeterHan.PLib.Core;
using UnityEngine;

namespace SensorsPlus
{
    public class SensorsPlusMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Harmony.DEBUG = true;
            PUtil.InitLibrary();
            Debug.Log("[SensorsPlus] SensorsPlus loaded.");
            harmony.PatchAll();
        }
    }
}