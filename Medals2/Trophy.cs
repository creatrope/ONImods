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

        foreach (var kvp in TrophyData.TrophyTypes)
        {
            string id = kvp.Key.ToLowerInvariant() + "_trophy";
            string name = kvp.Value.Name;
            string desc = kvp.Value.Description;
            var rewardType = kvp.Value.RewardType;
            string kanim = TrophyData.GetAnimForReward(rewardType);

            Debug.Log($"[TrophyConfig] Creating keepsake with id='{id}', name='{name}', desc='{desc}', kanim='{kanim}', rewardType='{rewardType}'");
            
            var trophypf = KeepsakeConfig.CreateKeepsake(
                id,
                name,
                desc,
                kanim, "idle", "ui", null,
                null, (KeepsakeConfig.PostInitFn)null, SimHashes.Creature);

            trophypf.GetComponent<KPrefabID>().AddTag(GameTags.PedestalDisplayable);
            trophypf.GetComponent<KPrefabID>().AddTag("Trophy");
            trophypf.AddComponent<TrophyModifiable>();
            var prefabid = trophypf.GetComponent<KPrefabID>();

            Debug.Log($"[TrophyConfig] Created prefab id={id}, KPrefabID='{prefabid}");

            if (TrophyDb.Trophies.TryGetValue(kvp.Key, out var trophyData))
            {
                trophyData.prefab = trophypf;
            }
            prefabs.Add(trophypf);
        }
        return prefabs;
    }

    public void OnPrefabInit(GameObject inst) { }
    public void OnSpawn(GameObject inst) { }

    public static void CreateTrophy(TrophyData trophyInfo, MinionIdentity minion, string target = null)
    {
        if (minion == null)
        {
            Debug.LogError("[CreateTrophy] CreateAndAwardTrophy called with null minion!");
            return;
        }
        if (trophyInfo == null)
        {
            Debug.LogError("[CreateTrophy] CreateAndAwardTrophy called with null trophyInfo!");
            return;
        }

        var prefab = trophyInfo.prefab;
        if (prefab == null)
        {
            Debug.LogError("[CreateTrophy] trophyInfo.prefab is null!");
            return;
        }
 
        string minionName = minion.GetProperName();
        string name = $"{trophyInfo.GetName()} {target} ({minionName})";
        string desc = $"{trophyInfo.GetDesc()} {target} ({minionName})";

        GameObject trophy = Util.KInstantiate(prefab, Grid.CellToPosCCC(Grid.PosToCell(minion.transform.position + new Vector3(0, 2f, 0)), Grid.SceneLayer.Ore));
        if (trophy == null)
        {
            Debug.LogError("[CreateTrophy] Failed to instantiate trophy prefab.");
            return;
        }

        trophy.name = name;

        var newTrophyInfo = trophy.GetComponent<TrophyModifiable>();
        if (newTrophyInfo != null)
            newTrophyInfo.SetInfo(name, desc);

        var selectable = trophy.GetComponent<KSelectable>();
        if (selectable != null)
            selectable.SetName(name);

        var infoDesc = trophy.GetComponent<InfoDescription>();
        if (infoDesc != null)
            infoDesc.description = desc;

        trophy.transform.position = minion.transform.position + new Vector3(0, 2f, 0); // above head
        trophy.SetActive(true);
        Debug.Log($"[CreateAndAwardTrophy] Awarded trophy '{name}' to minion '{minion.GetProperName()}'.");

    }
}

[SerializationConfig(MemberSerialization.OptIn)]
public class TrophyModifiable : KMonoBehaviour, ISaveLoadable
{
    [Serialize]
    public string trophyName;

    [Serialize]
    public string trophyDesc;

    public void SetInfo(string name, string desc)
    {
        trophyName = name;
        trophyDesc = desc;
    }

    public string GetName() => trophyName;
    public string GetDesc() => trophyDesc;

    public override void OnSpawn()
    {
        base.OnSpawn();
        if (!string.IsNullOrEmpty(trophyName))
            gameObject.name = trophyName;

        var selectable = gameObject.GetComponent<KSelectable>();
        if (selectable != null)
            selectable.SetName(trophyName);

        var infoDesc = gameObject.GetComponent<InfoDescription>();
        if (infoDesc != null)
            infoDesc.description = trophyDesc;
    }
}