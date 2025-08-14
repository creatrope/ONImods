using System;
using System.Collections.Generic;
using UnityEngine;
using KSerialization;

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
    [Serialize]
    private HashSet<string> awardedNonRepeatableMementos = new HashSet<string>();

    public bool HasAwardedNonRepeatableMemento(string mementoId)
    {
        return awardedNonRepeatableMementos.Contains(mementoId);
    }

    public void SetAwardedNonRepeatableMemento(string mementoId)
    {
        awardedNonRepeatableMementos.Add(mementoId);
    }
}