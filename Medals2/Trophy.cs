using HarmonyLib;
using KSerialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using static STRINGS.UI;

[SerializationConfig(MemberSerialization.OptIn)]
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
                 "keepsake_medal_kanim", "idle", "ui", null,
                 null, (KeepsakeConfig.PostInitFn)null, SimHashes.Creature);
        trophypf.GetComponent<KPrefabID>().AddTag(GameTags.PedestalDisplayable);
        trophypf.GetComponent<KPrefabID>().AddTag("Trophy");
        trophypf.AddComponent<TrophyInfo>();
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

        var prefab = Assets.GetPrefab("keepsake_medal");
        if (prefab == null)
        {
            Debug.LogError("[Trophy] Prefab 'keepsake_medal' not found in Assets!");
            return;
        }

        string minionName = minion.GetProperName();
        name = $"{name} ({minionName})";
        desc = $"{desc} ({minionName})";

        GameObject trophy = Util.KInstantiate(prefab, Grid.CellToPosCCC(Grid.PosToCell(minion.transform.position + new Vector3(0, 2f, 0)), Grid.SceneLayer.Ore));
        if (trophy == null)
        {
            Debug.LogError("[Trophy] Failed to instantiate trophy prefab.");
            return;
        }

        trophy.name = name;

        var trophyInfo = trophy.GetComponent<TrophyInfo>();
        trophyInfo.SetInfo(name, desc);

        var selectable = trophy.GetComponent<KSelectable>();
        if (selectable != null)
            selectable.SetName(name);

        var infoDesc = trophy.GetComponent<InfoDescription>();
        if (infoDesc != null)
            infoDesc.description = desc;

        trophy.transform.position = minion.transform.position + new Vector3(0, 2f, 0); // above head
        trophy.SetActive(true);
        Debug.Log($"[Trophy] Awarded trophy '{name}' to minion '{minion.GetProperName()}'.");
    }
}