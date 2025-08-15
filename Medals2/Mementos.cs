namespace Medals2
{
    using HarmonyLib;
    using KSerialization;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using static STRINGS.UI;

    // 1. Enums
    public enum MedalType
    {
        Trophy,
        Demerit,
        Citation,
    }

    // 2. Static Prototypes
    public static class MementoPrototypes
    {
        public static readonly Dictionary<string, MementoData> Mementos;

        static MementoPrototypes()
        {
            Mementos = new Dictionary<string, MementoData>();
            foreach (var kvp in MementoData.MementoTypes)
            {
                var info = ScriptableObject.CreateInstance<MementoData>();
                info.mementoName = kvp.Value.Name;
                info.mementoDesc = kvp.Value.Description;
                info.rewardType = kvp.Value.RewardType;
                info.repeatable = kvp.Value.Repeatable;
                info.unique = kvp.Value.Unique;
                Mementos.Add(kvp.Key, info);
            }
        }
    }

    // 3. Data classes
    [SerializationConfig(MemberSerialization.OptIn)]
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
            Trophy,
            Citation,
            Oops
        }

        [Serialize]
        public string mementoName;

        [Serialize]
        public string mementoDesc;

        [Serialize]
        public Reward rewardType = Reward.Trophy;

        [Serialize]
        public bool repeatable;

        [Serialize]
        public bool unique;

        public GameObject prefab;

        public static readonly Dictionary<Reward, RewardTypeInfo> RewardTypeInfos =
            new Dictionary<Reward, RewardTypeInfo>
            {
                { Reward.Trophy,   new RewardTypeInfo(Reward.Trophy,   "keepsake_medal_kanim") },
                { Reward.Citation, new RewardTypeInfo(Reward.Citation, "keepsake_proclamation_kanim") },
                { Reward.Oops,     new RewardTypeInfo(Reward.Oops,     "keepsake_medal_kanim") }
            };

        public static readonly Dictionary<string, (string Name, string Description, Reward RewardType, bool Repeatable, bool Unique)> MementoTypes =
            new Dictionary<string, (string Name, string Description, Reward RewardType, bool Repeatable, bool Unique)>
            {
                { "Injury", ("Injured", "Injured in the Line of Duty", Reward.Citation, true, false) },
                { "Rescue", ("Rescued", "Rescued Incapacited Duplicant", Reward.Trophy, true, false) },
                { "Space", ("First To Space", "First To Space", Reward.Trophy, false, true) },
                { "FirstVisit", ("First Visitor", "First Duplicant Visitor To Planet", Reward.Trophy, true, true) }
            };
        public string GetName() => mementoName;
        public string GetDesc() => mementoDesc;
        public Reward GetRewardType() => rewardType;

        public static string GetAnimForReward(Reward reward)
        {
            return RewardTypeInfos.TryGetValue(reward, out var info) ? info.Anim : null;
        }
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public class MementoModifiable : KMonoBehaviour, ISaveLoadable
    {
        [Serialize]
        public string mementoName;

        [Serialize]
        public string mementoDesc;

        [Serialize]
        public MementoData.Reward rewardType;

        public void SetInfo(string name, string desc, MementoData.Reward rew)
        {
            mementoName = name;
            mementoDesc = desc;
            rewardType = rew;
        }

        public string GetName() => mementoName;
        public string GetDesc() => mementoDesc;

        public override void OnSpawn()
        {
            base.OnSpawn();
            if (!string.IsNullOrEmpty(mementoName))
                gameObject.name = mementoName;

            var selectable = gameObject.GetComponent<KSelectable>();
            if (selectable != null)
                selectable.SetName(mementoName);

            var infoDesc = gameObject.GetComponent<InfoDescription>();
            if (infoDesc != null)
                infoDesc.description = mementoDesc;
        }
    }

    // 4. Info/manager classes
    [SerializationConfig(MemberSerialization.OptIn)]
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

        public void PrintAllMementos()
        {
            var allMementos = UnityEngine.Object.FindObjectsOfType<MementoModifiable>();
            foreach (var memento in allMementos)
            {
                Debug.Log($"Memento: {memento.GetName()} - {memento.GetDesc()}");
            }
        }
    }

    // 5. Config/creation classes
    [SerializationConfig(MemberSerialization.OptIn)]
    public class MementoConfig : IMultiEntityConfig
    {
        public List<GameObject> CreatePrefabs()
        {
            Debug.LogWarning("[MementoConfig] CreatePrefabs.");

            var prefabs = new List<GameObject>();

            foreach (var kvp in MementoData.MementoTypes)
            {
                string id = kvp.Key.ToLowerInvariant() + "_memento";
                string name = kvp.Value.Name;
                string desc = kvp.Value.Description;
                var rewardType = kvp.Value.RewardType;
                string kanim = MementoData.GetAnimForReward(rewardType);

                Debug.Log($"[MementoConfig] Creating keepsake with id='{id}', name='{name}', desc='{desc}', kanim='{kanim}', rewardType='{rewardType}'");

                var mementopf = KeepsakeConfig.CreateKeepsake(
                    id,
                    name,
                    desc,
                    kanim, "idle", "ui", null,
                    null, (KeepsakeConfig.PostInitFn)null, SimHashes.Creature);

                mementopf.GetComponent<KPrefabID>().AddTag(GameTags.PedestalDisplayable);
                mementopf.GetComponent<KPrefabID>().AddTag("Memento");
                mementopf.AddComponent<MementoModifiable>();
                var prefabid = mementopf.GetComponent<KPrefabID>();

                Debug.Log($"[MementoConfig] Created prefab id={id}, KPrefabID='{prefabid}'");

                if (MementoPrototypes.Mementos.TryGetValue(kvp.Key, out var mementoData))
                {
                    mementoData.prefab = mementopf;
                }
                prefabs.Add(mementopf);
            }
            return prefabs;
        }

        public void OnPrefabInit(GameObject inst) { }
        public void OnSpawn(GameObject inst) { }

        public static void CreateMemento(MementoData mementoInfo, MinionIdentity minion, string target = null)
        {
            if (minion == null)
            {
                Debug.LogError("[MementoConfig] CreateMemento called with null minion!");
                return;
            }
            if (mementoInfo == null)
            {
                Debug.LogError("[MementoConfig] CreateMemento called with null mementoInfo!");
                return;
            }

            var prefab = mementoInfo.prefab;
            if (prefab == null)
            {
                Debug.LogError("[MementoConfig] mementoInfo.prefab is null!");
                return;
            }

            string minionName = minion.GetProperName();

            var medalInfo = minion.FindOrAddComponent<MedalInfo>();

            var worldTime = GameClock.Instance;
            int cycle = worldTime.GetCycle();

            string name = $"{mementoInfo.GetName()} {target}";
            string desc = $"{mementoInfo.GetDesc()} {target}";
            var medal = new Medal(name, $"{desc} at cycle {cycle}");
            medalInfo.Medals.Add(medal);

            name = $"{name} ({minionName})";
            desc = $"{desc} ({minionName}) at cycle {cycle}";

            GameObject memento = Util.KInstantiate(prefab, Grid.CellToPosCCC(Grid.PosToCell(minion.transform.position + new Vector3(0, 2f, 0)), Grid.SceneLayer.Ore));
            if (memento == null)
            {
                Debug.LogError("[MementoConfig] Failed to instantiate memento prefab.");
                return;
            }

            memento.name = name;

            var newMementoInfo = memento.GetComponent<MementoModifiable>();
            if (newMementoInfo != null)
                newMementoInfo.SetInfo(name, desc, mementoInfo.rewardType);

            var selectable = memento.GetComponent<KSelectable>();
            if (selectable != null)
                selectable.SetName(name);

            var infoDesc = memento.GetComponent<InfoDescription>();
            if (infoDesc != null)
                infoDesc.description = desc;

            memento.transform.position = minion.transform.position + new Vector3(0, 2f, 0); // above head
            memento.SetActive(true);
            Debug.Log($"[MementoConfig] Awarded memento '{name}' to minion '{minion.GetProperName()}'.");
        }

        // Utility method to get the anim file for a MementoModifiable by examining the reward of its parent
        public static string GetAnimForMementoComponent(MementoModifiable memento)
        {
            if (memento == null)
                return null;

            // Try to find the parent GameObject that has a MementoData reference
            // This assumes the parent is the prefab created in MementoConfig.CreatePrefabs
            // and that the prefab is registered in MementoPrototypes.Mementos

            // Find the MementoData prototype by matching the memento's name
            foreach (var kvp in MementoPrototypes.Mementos)
            {
                var data = kvp.Value;
                if (data != null && data.GetName() == memento.GetName())
                {
                    // Get the anim file for the reward type
                    return MementoData.GetAnimForReward(data.GetRewardType());
                }
            }

            // Fallback: try to infer from mementoName if it matches a key in MementoTypes
            if (MementoData.MementoTypes.TryGetValue(memento.GetName(), out var tuple))
            {
                return MementoData.GetAnimForReward(tuple.RewardType);
            }

            return null;
        }
    }
}