using HarmonyLib;
using STRINGS;
using System;
using UnityEngine;

namespace Heinermann.CritterRename.Patches
{
    [HarmonyPatch(typeof(DetailsScreen), "UpdateTitle")]
    class DetailsScreen_UpdateTitle
    {
        static void Postfix(DetailsScreen __instance)
        {
            GameObject target = __instance.target;
            KPrefabID prefab = target?.GetComponent<KPrefabID>();
            EditableTitleBar tabTitle = Traverse.Create(__instance).Field("TabTitle").GetValue<EditableTitleBar>();
            if (tabTitle != null && prefab != null && (prefab.HasTag(GameTags.Creature) || prefab.HasTag(GameTags.Egg)))
            {
                tabTitle.SetUserEditable(true);

                string properName = UI.StripLinkFormatting(target.GetProperName());

                tabTitle.SetTitle(properName);
                tabTitle.SetSubText("" );

                if (!prefab.HasTag(GameTags.Egg))
                {
                    string originalProperName = TagManager.GetProperName(prefab.PrefabTag, stripLink: true);
                    if (properName != originalProperName)
                    {
                        tabTitle.SetSubText(originalProperName);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(DetailsScreen), "OnNameChanged")]
    class DetailsScreen_OnNameChanged
    {
        static void Postfix(string newName, DetailsScreen __instance)
        {
            GameObject target = __instance.target;
            KPrefabID prefab = target?.GetComponent<KPrefabID>();
            if (prefab != null && prefab.HasTag(GameTags.Creature))
            {
                target.AddOrGet<CritterName>().SetName(newName);
                __instance.UpdateTitle();
            }
        }
    }
}
