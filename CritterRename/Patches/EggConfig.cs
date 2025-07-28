using HarmonyLib;
using System.Reflection;
using UnityEngine;
using System.Linq;

namespace Heinermann.CritterRename.Patches
{
    [HarmonyPatch(typeof(EggConfig), "CreateEgg", new[] {
        typeof(string), typeof(string), typeof(string), typeof(Tag),
        typeof(string), typeof(float), typeof(int), typeof(float),
        typeof(string[]), typeof(string[]), typeof(bool)
    })]
    public static class CreateEggPatch
    {
        static void Postfix(ref GameObject __result)
        {
            __result.AddOrGet<CritterName>();
        }
    }
}
