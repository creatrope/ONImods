// Rename KeepsakeMedalInfo to MedalInfo (component version)
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public partial class MedalInfo : KMonoBehaviour, ISerializationCallbackReceiver
{
    [SerializeField]
    public List<MedalData> Medals = new List<MedalData>();

    // Use auto-properties with setters so they are assignable
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsRepeatable { get; set; }

    public MedalData SingleMedal { get; set; }

    // Static helper to create a MedalInfo instance with a single medal
    public static MedalInfo CreateMedalInfo(string name, string description, bool isRepeatable)
    {
        var info = new GameObject().AddComponent<MedalInfo>();
        info.Name = name;
        info.Description = description;
        info.IsRepeatable = isRepeatable;
        info.SingleMedal = new MedalData { Name = name, Description = description, IsRepeatable = isRepeatable };
        return info;
    }

    // Implement ISerializationCallbackReceiver methods
    public void OnBeforeSerialize()
    {
        // Add any pre-serialization logic here if needed
    }

    public void OnAfterDeserialize()
    {
        // Add any post-deserialization logic here if needed
    }
}

// Data class for each medal
[Serializable]
public class MedalData
{
    public string Name;
    public string Description;
    public bool IsRepeatable;
}