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
    [Serialize] public MedalType Type;
    [Serialize] public bool Repeatable;

    public Medal(string name, string description, MedalType type, bool repeatable)
    {
        Name = name;
        Description = description;
        Type = type;
        Repeatable = repeatable;
    }
}

public class MedalInfo : KMonoBehaviour, ISaveLoadable
{
    [Serialize]
    public List<Medal> Medals = new List<Medal>();

    public static Medal CreateMedal(string name, string description, MedalType type, bool repeatable)
    {
        return new Medal(name, description, type, repeatable);
    }
}