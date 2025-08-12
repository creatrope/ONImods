using KSerialization;
using UnityEngine;

[SerializationConfig(MemberSerialization.OptIn)]
public class TrophyInfo : KMonoBehaviour, ISaveLoadable
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

        var selectable = gameObject.GetComponent<KSelectable>();
        if (selectable != null && !string.IsNullOrEmpty(trophyName))
            selectable.SetName(trophyName);

        var infoDesc = gameObject.GetComponent<InfoDescription>();
        if (infoDesc != null && !string.IsNullOrEmpty(trophyDesc))
            infoDesc.description = trophyDesc;
    }
}