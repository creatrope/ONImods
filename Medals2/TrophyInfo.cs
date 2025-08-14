using KSerialization;
using UnityEngine;
using System.Collections.Generic;

// Data-only ScriptableObject
[SerializationConfig(MemberSerialization.OptIn)]
public class MementoData : ScriptableObject, ISaveLoadable
{
    public struct RewardTypeInfo
    {
        public Reward Reward;
        public string Anim;

        public RewardTypeInfo(Reward reward, string anim)
        {
            Reward = reward;
            Anim = anim;
        }
    }

    public enum Reward
    {
        Memento,
        Citation,
        Oops
    }

    [Serialize]
    public string mementoName;

    [Serialize]
    public string mementoDesc;

    [Serialize]
    public Reward rewardType = Reward.Memento;

    [Serialize]
    public bool repeatable;

    [Serialize]
    public bool unique;

    public GameObject prefab;

    public static readonly Dictionary<Reward, RewardTypeInfo> RewardTypeInfos =
        new Dictionary<Reward, RewardTypeInfo>
        {
            { Reward.Memento,   new RewardTypeInfo(Reward.Memento,   "keepsake_medal_kanim") },
            { Reward.Citation, new RewardTypeInfo(Reward.Citation, "keepsake_medal_kanim") },
            { Reward.Oops,     new RewardTypeInfo(Reward.Oops,     "keepsake_medal_kanim") }
        };

    // repeatable - dupe is eligible for this memento more than one
    // unique -  only one dupe can get this memento

    public static readonly Dictionary<string, (string Name, string Description, Reward RewardType, bool Repeatable, bool Unique)> MementoTypes =
        new Dictionary<string, (string Name, string Description, Reward RewardType, bool Repeatable, bool Unique)>
        {
            { "Injury", ("Injured", "Injured in the Line of Duty", Reward.Memento, true, false) },
            { "Rescue", ("Rescued", "Rescued Incapacited Duplicant", Reward.Memento, true, false) },
            { "Space", ("First To Space", "First To Space", Reward.Memento, false, true) },
            { "FirstVisit", ("First Visitor", "First Duplicant Visitor To Planet", Reward.Memento, true, true) }
        };

    public void SetInfo(string name, string desc)
    {
        mementoName = name;
        mementoDesc = desc;
    }

    public string GetName() => mementoName;
    public string GetDesc() => mementoDesc;
    public Reward GetRewardType() => rewardType;

    public static string GetAnimForReward(Reward reward)
    {
        return RewardTypeInfos.TryGetValue(reward, out var info) ? info.Anim : null;
    }
}

// Data registry for MementoData
public static class MementoDb
{
    public static readonly Dictionary<string, MementoData> Mementos;

    static MementoDb()
    {
        Mementos = new Dictionary<string, MementoData>();
        foreach (var kvp in MementoData.MementoTypes)
        {
            var info = ScriptableObject.CreateInstance<MementoData>();
            info.SetInfo(kvp.Value.Name, kvp.Value.Description);
            info.rewardType = kvp.Value.RewardType;
            info.repeatable = kvp.Value.Repeatable;
            info.unique = kvp.Value.Unique;

            Mementos.Add(kvp.Key, info);
        }
    }
}