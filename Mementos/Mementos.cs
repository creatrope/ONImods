using HarmonyLib;
using KSerialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mementos
{
    using static STRINGS.MEMENTOS; 
    using static UnityEngine.GraphicsBuffer;

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
            Proclamation,
            Rocket,
            Planet,
            Oops
        }

        [Serialize]
        public string mementoName;

        [Serialize]
        public string mementoDesc;

        [Serialize]
        public Reward rewardType = Reward.Trophy;

        public GameObject prefab;

        public static readonly Dictionary<Reward, RewardTypeInfo> RewardTypeInfos =
            new Dictionary<Reward, RewardTypeInfo>
            {
                { Reward.Planet,   new RewardTypeInfo(Reward.Planet,   "keepsake_planet_kanim") },
                { Reward.Trophy,   new RewardTypeInfo(Reward.Trophy,   "keepsake_trophy_kanim") },
                { Reward.Proclamation, new RewardTypeInfo(Reward.Proclamation, "keepsake_proclamation_kanim") },
                { Reward.Oops,     new RewardTypeInfo(Reward.Oops,     "keepsake_medal_kanim") },
                   { Reward.Rocket,     new RewardTypeInfo(Reward.Rocket,     "keepsake_rocket_kanim") }
         };

        public static readonly Dictionary<string, (string Name, string Description, Reward RewardType)> MementoTypes =
            new Dictionary<string, (string Name, string Description, Reward RewardType)>
            {
                { "Injury", (INJURY_NAME, INJURY_DESC, Reward.Proclamation) },
                { "Rescue", (RESCUE_NAME, RESCUE_DESC, Reward.Trophy) },
                { "Space", (SPACE_NAME, SPACE_DESC, Reward.Rocket) },
                { "FirstVisit", (FIRSTVISIT_NAME, FIRSTVISIT_DESC, Reward.Planet) }
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

    // this is the per minion data
    [SerializationConfig(MemberSerialization.OptIn)]
    public class MedalInfo : KMonoBehaviour, ISaveLoadable
    {
        [Serialize]
        public List<Medal> Medals = new List<Medal>();

        public void PrintAllMementos()
        {
            var allMementos = UnityEngine.Object.FindObjectsOfType<MementoModifiable>();
            foreach (var memento in allMementos)
            {
                Debug.Log($"Memento: {memento.GetName()} - {memento.GetDesc()}");
            }
        }
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public class MementoConfig : IMultiEntityConfig
    {
        public List<GameObject> CreatePrefabs()
        {
            // Debug.LogWarning("[MementoConfig] CreatePrefabs.");

            var prefabs = new List<GameObject>();

            foreach (var kvp in MementoData.MementoTypes)
            {
                string id = kvp.Key.ToLowerInvariant() + "_memento";
                string name = kvp.Value.Name;
                string desc = kvp.Value.Description;
                var rewardType = kvp.Value.RewardType;
                string kanim = MementoData.GetAnimForReward(rewardType);

                // Debug.Log($"[MementoConfig] Creating keepsake with id='{id}', name='{name}', desc='{desc}', kanim='{kanim}', rewardType='{rewardType}'");

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

                // Debug.Log($"[MementoConfig] Created prefab id={id}, KPrefabID='{prefabid}'");

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
    }

    public static class MementoUtils
    {
        public static string GetWorldName(WorldContainer world)
        {
            string worldName = "Unknown World";
            if (world != null)
            {
                var clusterEntity = world.GetComponent<ClusterGridEntity>();
                worldName = clusterEntity != null ? clusterEntity.GetProperName() : world.name;
            }
            return worldName;
        }
        public static float GetMinionAge(MinionIdentity minion)
        {
            if (minion == null) return -1f;
            return GameClock.Instance != null ? GameClock.Instance.GetCycle() - minion.arrivalTime : -1f;
        }
        public static bool AwardMementosOnce(string mementoId, List<MinionIdentity> minions, object target = null)
        {
            if (minions == null || minions.Count == 0)
                return false;

            if (string.IsNullOrEmpty(mementoId) || !MementoPrototypes.Mementos.ContainsKey(mementoId))
            {
                Debug.LogWarning($"[Mementos] AwardMementosConditional: Invalid arguments. mementoId='{mementoId}', minions count={minions?.Count ?? 0}, mementoId found={MementoPrototypes.Mementos.ContainsKey(mementoId)}");
                return false;
            }

            var mementoInfo = MementoPrototypes.Mementos[mementoId];
            string targetName = null;

            if (target is WorldContainer world)
                targetName = MementoUtils.GetWorldName(world);
            else if (target is MinionIdentity minion)
                targetName = minion.GetProperName();
            else if (target != null)
                targetName = target.ToString();
            bool anyAwarded = false;

            string key = MementoUtils.makeKey(mementoId, targetName);

            if (!MementosGlobalData.Instance.Issued.ContainsKey(key))
            {
                Debug.Log($"[Mementos] AwardMementosOnce {key} doesn't exist, creating...");
                foreach (var minion in minions)
                    MementoUtils.CreateMemento(mementoInfo, minion, targetName);
                MementosGlobalData.Instance.Issued[key] = true;
                anyAwarded = true;
            }
            else
            {
                Debug.Log($"[Mementos] AwardMementosOnce {key} exists, skipping...");

            }

            return anyAwarded;
        }

        public static string makeKey(string mementoId, string targetName)
        {
            return (string.IsNullOrEmpty(targetName)) ? mementoId : $"{mementoId}_{targetName}";
        }
        public static void CreateMemento(MementoData mementoInfo, MinionIdentity minion, string target = null)
        {
            if (minion == null || mementoInfo == null || mementoInfo.prefab == null)
            {
                Debug.LogError($"[MementoConfig] CreateMemento called with null argument(s): minion={(minion == null ? "null" : "ok")}, mementoInfo={(mementoInfo == null ? "null" : "ok")}, prefab={(mementoInfo?.prefab == null ? "null" : "ok")}");
                return;
            }

            var prefab = mementoInfo.prefab;
            string minionName = minion.GetProperName();

            var medalInfo = minion.FindOrAddComponent<MedalInfo>();

            var worldTime = GameClock.Instance;
            int cycle = worldTime.GetCycle();

            string name = mementoInfo.GetName();
            string desc = mementoInfo.GetDesc();

            string medalInscription = MakeMedalInscription(minionName, name, desc, target, cycle);
            var medal = new Medal(name, medalInscription);
            medalInfo.Medals.Add(medal);

            string mementoInscription = MakeMementoInscription(minionName, name, desc, target, cycle);

            GameObject memento = Util.KInstantiate(prefab, Grid.CellToPosCCC(Grid.PosToCell(minion.transform.position + new Vector3(0, 2f, 0)), Grid.SceneLayer.Ore));
            if (memento == null)
            {
                Debug.LogError("[MementoConfig] Failed to instantiate memento prefab.");
                return;
            }

            memento.name = name;

            var newMementoInfo = memento.GetComponent<MementoModifiable>();
            if (newMementoInfo != null)
                newMementoInfo.SetInfo(name, mementoInscription, mementoInfo.rewardType);

            var selectable = memento.GetComponent<KSelectable>();
            if (selectable != null)
                selectable.SetName(name);

            var infoDesc = memento.GetComponent<InfoDescription>();
            if (infoDesc != null)
                infoDesc.description = mementoInscription;

            MementoUtils.PlaceMemento(memento, minion);
            memento.SetActive(true);
        }

        public static string MakeMedalInscription(string minionName, string name, string desc, string target, int cycle)
        {
            string inscription = null;
            if (target == minionName)
            {
                inscription = string.Format(MEDAL_INSCRIPTION, desc, cycle); // ready for translate
            }
            else
            {
                inscription = string.Format(MEDAL_INSCRIPTION_WITH_TARGET, desc, target, cycle); // ready for translate
            }
            return inscription;
        }
        public static string MakeMementoInscription(string minionName, string name, string desc, string target, int cycle)
        {
            string inscription = null;
            if (target == minionName)
            {
                inscription = string.Format(MEMENTO_INSCRIPTION, minionName, desc, cycle); // ready for translate
            } 
            else
            {
                inscription = string.Format(MEMENTO_INSCRIPTION_WITH_TARGET, minionName, desc, target, cycle); // ready for translate
            }
            return inscription;
        }

        public static string GetAnimForMementoComponent(MementoModifiable memento)
        {
            if (memento == null)
                return null;

            foreach (var kvp in MementoPrototypes.Mementos)
            {
                var data = kvp.Value;
                if (data != null && data.GetName() == memento.GetName())
                {
                    return MementoData.GetAnimForReward(data.GetRewardType());
                }
            }

            if (MementoData.MementoTypes.TryGetValue(memento.GetName(), out var tuple))
            {
                return MementoData.GetAnimForReward(tuple.RewardType);
            }

            return null;
        }

        public static bool PlaceMemento(GameObject memento, MinionIdentity minion)
        {
            if (memento == null || minion == null)
                return false;

            var navigator = minion.GetComponent<Navigator>();
            if (navigator == null)
                return false;

            Vector3 placePos = minion.transform.position;
            placePos.y += (float)0.5;

            memento.transform.position = placePos;
            //Debug.Log($"[Mementos] PlaceMemento: {memento.name} (targetPos={placePos}");
            return true;
        }
    }
}