using HarmonyLib;

[HarmonyPatch(typeof(BionicSideScreen), "OnSpawn")]
public static class BionicSideScreenPrefabPatch
{
    public static void Postfix(BionicSideScreen __instance)
    {
        var prefab = __instance.ownableSecondSideScreenPrefab;
        if (prefab == null)
        {
            UnityEngine.Debug.LogError("[Mementos] ownableSecondSideScreenPrefab is not assigned on BionicSideScreen.");
        }
        else
        {
            // Store the prefab reference for use in MementoGallery
            Mementos.MementoGallery.CachedOwnablesSecondSideScreenPrefab = prefab;
            UnityEngine.Debug.Log("[Mementos] Cached ownableSecondSideScreenPrefab from BionicSideScreen: " + prefab.name);
        }
    }
}