using KSerialization;
using UnityEngine;
using System.Collections.Generic;

[SerializationConfig(MemberSerialization.OptIn)]
public class TrophyInfo : KMonoBehaviour, ISaveLoadable
{
    [Serialize]
    public string trophyName;

    [Serialize]
    public string trophyDesc;

    public static readonly Dictionary<string, (string Name, string Description)> TrophyTypes =
        new Dictionary<string, (string Name, string Description)>
        {
            { "Injury", ("Injured", "Injured in the Line of Duty") },
            { "Rescue", ("Rescued", "Rescued Incapacited Duplicant") },
            { "Space", ("First To Space", "First To Space") },
            { "FirstVisit", ("First Visitor", "First Duplicant Visitor To Planet") }
        };

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

        var selectable = gameObject.GetComponent<KSelectable>();
        if (selectable != null && !string.IsNullOrEmpty(trophyName))
            selectable.SetName(trophyName);

        var infoDesc = gameObject.GetComponent<InfoDescription>();
        if (infoDesc != null && !string.IsNullOrEmpty(trophyDesc))
            infoDesc.description = trophyDesc;
    }
}