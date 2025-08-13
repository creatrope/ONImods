using System;
using System.Collections.Generic;
using UnityEngine;
using KSerialization;

// Enum for medal types
public enum MedalType
{
    Award,
    Demerit,
    Citation,
}

[SerializationConfig(MemberSerialization.OptIn)]
// Medal data structure
[Serializable]
public class Medal
{
    [Serialize] public string Name;
    [Serialize] public string Description;

    public Medal(string name, string description)
    {
        Name = name;
        Description = description;
    }
}

public class MedalInfo : KMonoBehaviour, ISaveLoadable
{
    [Serialize]
    public List<Medal> Medals = new List<Medal>();
    public static Medal CreateMedal(TrophyData trophyInfo, MinionIdentity minion, string target = null)
    {
        string name = trophyInfo.GetName() + $" {target}";
        string desc = trophyInfo.GetDesc() + $" {target}";
        var medalInfo = minion.FindOrAddComponent<MedalInfo>();
        var medal = new Medal(name, desc);
        medalInfo.Medals.Add(medal);
        return medal;
    }
}