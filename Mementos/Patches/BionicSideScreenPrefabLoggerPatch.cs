using HarmonyLib;
using UnityEngine;

namespace Mementos
{
    [HarmonyPatch(typeof(BionicSideScreen), "OnSpawn")]
    public static class BionicSideScreenPrefabLoggerPatch
    {
        public static void Postfix(BionicSideScreen __instance)
        {
            var prefab = __instance.ownableSecondSideScreenPrefab;
            if (prefab == null)
            {
                Debug.LogError("[Mementos] ownableSecondSideScreenPrefab is not assigned on BionicSideScreen.");
            }
            else
            {
                Debug.Log($"[Mementos] BionicSideScreen.ownableSecondSideScreenPrefab: name='{prefab.name}', type='{prefab.GetType().Name}'");
                var kpid = prefab.GetComponent<KPrefabID>();
                if (kpid != null)
                    Debug.Log($"[Mementos] Prefab KPrefabID tag: {kpid.PrefabTag}");
            }
        }
    }
}