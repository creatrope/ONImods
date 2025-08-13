using KSerialization;
using UnityEngine;
using System.Collections.Generic;

// Data-only ScriptableObject
[SerializationConfig(MemberSerialization.OptIn)]
public class TrophyData : ScriptableObject, ISaveLoadable
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
        Trophy,
        Citation,
        Oops
    }

    [Serialize]
    public string trophyName;

    [Serialize]
    public string trophyDesc;

    [Serialize]
    public Reward rewardType = Reward.Trophy;

    [Serialize]
    public bool repeatable; // <-- Add this field

    public GameObject prefab;

    public static readonly Dictionary<Reward, RewardTypeInfo> RewardTypeInfos =
        new Dictionary<Reward, RewardTypeInfo>
        {
            { Reward.Trophy,   new RewardTypeInfo(Reward.Trophy,   "keepsake_medal_kanim") },
            { Reward.Citation, new RewardTypeInfo(Reward.Citation, "keepsake_medal_kanim") },
            { Reward.Oops,     new RewardTypeInfo(Reward.Oops,     "keepsake_medal_kanim") }
        };

    public static readonly Dictionary<string, (string Name, string Description, Reward RewardType, bool Repeatable)> TrophyTypes =
        new Dictionary<string, (string Name, string Description, Reward RewardType, bool Repeatable)>
        {
            { "Injury", ("Injured", "Injured in the Line of Duty", Reward.Trophy, false) },
            { "Rescue", ("Rescued", "Rescued Incapacited Duplicant", Reward.Trophy, true) },
            { "Space", ("First To Space", "First To Space", Reward.Trophy, false) },
            { "FirstVisit", ("First Visitor", "First Duplicant Visitor To Planet", Reward.Trophy, true) }
        };

    public void SetInfo(string name, string desc)
    {
        trophyName = name;
        trophyDesc = desc;
    }

    public string GetName() => trophyName;
    public string GetDesc() => trophyDesc;
    public Reward GetRewardType() => rewardType;

    public static string GetAnimForReward(Reward reward)
    {
        return RewardTypeInfos.TryGetValue(reward, out var info) ? info.Anim : null;
    }
}

// Data registry for TrophyData
public static class TrophyDb
{
    public static readonly Dictionary<string, TrophyData> Trophies;

    static TrophyDb()
    {
        Trophies = new Dictionary<string, TrophyData>();
        foreach (var kvp in TrophyData.TrophyTypes)
        {
            var info = ScriptableObject.CreateInstance<TrophyData>();
            info.SetInfo(kvp.Value.Name, kvp.Value.Description);
            info.rewardType = kvp.Value.RewardType;
            info.repeatable = kvp.Value.Repeatable;
            Trophies.Add(kvp.Key, info);
        }
    }
}