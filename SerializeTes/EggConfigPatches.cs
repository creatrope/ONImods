using HarmonyLib;
using UnityEngine;
using System.Linq;

namespace SerializeTes
{
    // Patch: CreateEgg(string, string, string, Tag, string, float, int, float)
    [HarmonyPatch(typeof(EggConfig), "CreateEgg", new[] {
        typeof(string), typeof(string), typeof(string), typeof(Tag), typeof(string),
        typeof(float), typeof(int), typeof(float)
    })]
    public class EggConfig_CreateEgg_Obsolete1
    {
        static void Postfix(ref GameObject __result)
        {
            Debug.Log("[SerializeTest][EggConfig_CreateEgg_Obsolete1] Postfix called");
        }
    }

    // Patch: CreateEgg(string, string, string, Tag, string, float, int, float, string[])
    [HarmonyPatch(typeof(EggConfig), "CreateEgg", new[] {
        typeof(string), typeof(string), typeof(string), typeof(Tag), typeof(string),
        typeof(float), typeof(int), typeof(float), typeof(string[])
    })]
    public class EggConfig_CreateEgg_Obsolete2
    {
        static void Postfix(ref GameObject __result)
        {
            Debug.Log("[SerializeTest][EggConfig_CreateEgg_Obsolete2] Postfix called");
        }
    }

    // Patch: CreateEgg(string, string, string, Tag, string, float, int, float, string[], string[])
    [HarmonyPatch(typeof(EggConfig), "CreateEgg", new[] {
        typeof(string), typeof(string), typeof(string), typeof(Tag), typeof(string),
        typeof(float), typeof(int), typeof(float), typeof(string[]), typeof(string[])
    })]
    public class EggConfig_CreateEgg
    {
        static void Postfix(ref GameObject __result)
        {
            Debug.Log("[SerializeTest][EggConfig_CreateEgg] Postfix called");
        }
    }

    public static class EggConfigPatches
    {
        public static void PrintEggConfigPatches(HarmonyLib.Harmony harmony)
        {
            var methods = new[]
            {
                typeof(EggConfig).GetMethod("CreateEgg", new[] {
                    typeof(string), typeof(string), typeof(string), typeof(Tag), typeof(string),
                    typeof(float), typeof(int), typeof(float)
                }),
                typeof(EggConfig).GetMethod("CreateEgg", new[] {
                    typeof(string), typeof(string), typeof(string), typeof(Tag), typeof(string),
                    typeof(float), typeof(int), typeof(float), typeof(string[])
                }),
                typeof(EggConfig).GetMethod("CreateEgg", new[] {
                    typeof(string), typeof(string), typeof(string), typeof(Tag), typeof(string),
                    typeof(float), typeof(int), typeof(float), typeof(string[]), typeof(string[])
                })
            };

            foreach (var method in methods)
            {
                if (method == null) continue;
                var patchInfo = HarmonyLib.Harmony.GetPatchInfo(method);
                if (patchInfo != null)
                {
                    UnityEngine.Debug.Log($"Method: {method.Name}");
                    UnityEngine.Debug.Log($"  Prefixes: {string.Join(", ", patchInfo.Prefixes.Select(p => p.owner))}");
                    UnityEngine.Debug.Log($"  Postfixes: {string.Join(", ", patchInfo.Postfixes.Select(p => p.owner))}");
                    UnityEngine.Debug.Log($"  Transpilers: {string.Join(", ", patchInfo.Transpilers.Select(p => p.owner))}");
                }
            }
        }
    }
}