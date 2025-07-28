using HarmonyLib;
using UnityEngine;

namespace Heinermann.CritterRename.Patches
{
    [HarmonyPatch(typeof(EntityTemplates), "ExtendEntityToBasicCreature", new[] {
        typeof(GameObject), typeof(FactionManager.FactionID), typeof(string), typeof(string),
        typeof(NavType), typeof(int), typeof(float), typeof(string), typeof(float),
        typeof(bool), typeof(bool), typeof(float), typeof(float), typeof(float), typeof(float)
    })]
    class EntityTemplates_ExtendEntityToBasicCreature
    {
        static void Postfix(ref GameObject __result,
          GameObject template,
          FactionManager.FactionID faction,
          string initialTraitID,
          string NavGridName,
          NavType navType,
          int max_probing_radius,
          float moveSpeed,
          string onDeathDropID,
          float onDeathDropCount, // <-- changed from int to float
          bool drownVulnerable,
          bool entombVulnerable,
          float warningLowTemperature,
          float warningHighTemperature,
          float lethalLowTemperature,
          float lethalHighTemperature)
        {
            __result.AddOrGet<CritterName>();

        }
    }
}
