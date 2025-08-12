using HarmonyLib;
using KSerialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using static STRINGS.UI;

public class TrophyConfig : IMultiEntityConfig
{
    public List<GameObject> CreatePrefabs()
    {
        Debug.LogWarning("[TrophyConfig] CreatePrefabs.");

        var prefabs = new List<GameObject>();

        var trophypf = KeepsakeConfig.CreateKeepsake(
                "medal",
                 "Medal Keepsake",
                 "Medal Keepsake Description",
                 "keepsake_medal_kanim", "idle", "ui", DlcManager.DLC2,
                 null, (KeepsakeConfig.PostInitFn)null, SimHashes.Creature);
        trophypf.GetComponent<KPrefabID>().AddTag(GameTags.PedestalDisplayable);
        trophypf.GetComponent<KPrefabID>().AddTag("Trophy");

        prefabs.Add(trophypf);
        return prefabs;
    }

    public void OnPrefabInit(GameObject inst) { }
    public void OnSpawn(GameObject inst) { }

    public static void CreateAndAwardTrophy(string name, string desc, MinionIdentity minion)
    {
        if (minion == null)
        {
            Debug.LogError("[Trophy] CreateAndAwardTrophy called with null minion!");
            return;
        }

        // Dump all available prefabs
        Debug.Log("[Trophy] Dumping all registered prefabs in Assets:");
        bool foundKeepsakeMedal = false;
        foreach (var p in Assets.Prefabs)
        {
            string prefabId = p.PrefabTag.Name;
            Debug.Log($"[Trophy] Prefab ID: {prefabId}");
            if (prefabId == "keepsake_medal")
                foundKeepsakeMedal = true;
        }
        if (foundKeepsakeMedal)
            Debug.Log("[Trophy] Found keepsake_medal in Assets!");

        var prefab = Assets.GetPrefab("keepsake_medal");
        if (prefab == null)
        {
            Debug.LogError("[Trophy] Prefab 'keepsake_medal' not found in Assets!");
            return;
        }

        GameObject trophy = Util.KInstantiate(prefab, Grid.CellToPosCCC(Grid.PosToCell(minion.transform.position + new Vector3(0, 2f, 0)), Grid.SceneLayer.Ore));
        if (trophy == null)
        {
            Debug.LogError("[Trophy] Failed to instantiate trophy prefab.");
            return;
        }

        trophy.name = name;
        trophy.transform.position = minion.transform.position + new Vector3(0, 2f, 0); // above head
        trophy.SetActive(true);
        Debug.Log($"[Trophy] Awarded trophy '{name}' to minion '{minion.GetProperName()}'.");
    }
}