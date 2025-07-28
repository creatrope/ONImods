using HarmonyLib;
using Klei.AI;
using System.Diagnostics;
using UnityEngine;

namespace Heinermann.CritterRename.Patches
{
    [HarmonyPatch(typeof(Amount), "Copy")]
    static class Amount_Copy
    {
        static void Prefix(GameObject to, GameObject from)
        {
            string callingMethod = new StackFrame(2).GetMethod().Name;
            if (callingMethod == "SpawnBaby")
            {
                var fromCN = from.GetComponent<CritterName>();
                var toCN = to.AddOrGet<CritterName>();
                fromCN?.TransferTo(toCN);
            }
        }
    }
}
