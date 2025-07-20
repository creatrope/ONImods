using Database;
using HarmonyLib;
using HLib;
using KMod;
using KSerialization;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI; 

[HarmonyPatch(typeof(AssignableSideScreenRow), "SetContent")]
public static class AssignableSideScreenRow_AddWorldTooltip_Patch
{
    public static void Postfix(AssignableSideScreenRow __instance)
    {
        var refLabel = __instance.GetComponentInChildren<LocText>();
        if (refLabel != null)
        {
            var crewPortrait = __instance.GetComponentInChildren<CrewPortrait>(true);
            if (crewPortrait == null)
            {
                Debug.Log("CrewPortrait is null");
                return;
            }
            var identity = crewPortrait.identityObject;
            MinionIdentity minionIdentity = null;
    
            if (identity is MinionAssignablesProxy proxy && proxy.target is MinionIdentity proxyMinion)
            {
                minionIdentity = proxyMinion;
            }
            else
            {
                Debug.Log("Could not resolve MinionIdentity from CrewPortrait.identityObject. Type: " + (identity?.GetType().FullName ?? "null"));
                return;
            }

            string worldName = GetDupeWorldName(minionIdentity);

            ToolTip tooltip = refLabel.gameObject.GetComponent<ToolTip>();
            if (tooltip == null)
                tooltip = refLabel.gameObject.AddComponent<ToolTip>();
            tooltip.toolTip = worldName;
        }
    }

    private static string GetDupeWorldName(MinionIdentity minionIdentity)
    {
        if (minionIdentity == null)
        {
            Debug.Log("minionIdentity is null");
            return "Unknown World";
        }

        var world = minionIdentity.GetMyWorld();
        if (world != null)
        {
            var asteroidEntity = world.GetComponent<AsteroidGridEntity>();
            if (asteroidEntity != null)
                return asteroidEntity.Name;
        }

        Debug.Log("No world found for dupe.");
        return "Unknown World";
    }
}
