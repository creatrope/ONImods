using HarmonyLib;
using KSerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static STRINGS.UI.UISIDESCREENS.AUTOPLUMBERSIDESCREEN.BUTTONS;

namespace Mementos
{

    [HarmonyPatch(typeof(Health), "Damage")]
    public static class Health_DamageMedalPatch
    {
        private static void Postfix(Health __instance, float amount)
        {
            var minion = __instance.GetComponent<MinionIdentity>();
            if (minion != null && amount > 0)
            {
                // Print the current schedule block if available
                var schedAssignable = minion.GetComponent<Schedulable>();
                string blockName = null;
                string blockTypeId = null;
                bool inWorkBlock = false;
                if (schedAssignable != null && schedAssignable.GetSchedule() != null)
                {
                    var schedule = schedAssignable.GetSchedule();
                    var block = schedule.GetBlock(schedule.GetCurrentBlockIdx());
                    blockName = block?.name ?? "Unknown";
                    blockTypeId = block?.GroupId ?? "Unknown";
                    inWorkBlock = string.Equals(blockTypeId, "Worktime", StringComparison.OrdinalIgnoreCase);
                }
                if (!inWorkBlock)
                {
                    return;
                }

                var mementoInfo = MementoPrototypes.Mementos["Injury"];
                var medalInfo = minion.FindOrAddComponent<MedalInfo>();
                string mementoId = "Injury";

                if (mementoInfo.unique)
                {
                    if (MedalsSaveData.Instance.awardedUnique.ContainsKey(mementoId))
                    {
                        return;
                    }
                }
                if (!mementoInfo.repeatable)
                {
                    if (medalInfo != null && medalInfo.HasAwardedNonRepeatableMemento(mementoId))
                    {
                        return;
                    }
                }
                MementoConfig.CreateMemento(mementoInfo, minion);
                if (mementoInfo.unique)
                    MedalsSaveData.Instance.awardedUnique[mementoId] = minion.GetProperName();
                if (!mementoInfo.repeatable && medalInfo != null)
                    medalInfo.SetAwardedNonRepeatableMemento(mementoId);
            }
        }
    }

    [HarmonyPatch(typeof(AssignmentManager), "MinionMigration")]
    public static class AssignmentManager_MinionMigration_Patch
    {
        public static void Postfix(AssignmentManager __instance, object data)
        {
            Debug.Log("[Mementos] MinionMigration called.");
            if (data == null)
            {
                Debug.LogWarning("[Mementos] MinionMigration: data is null.");
                return;
            }
            var migrationEventArgs = data as MinionMigrationEventArgs;
            if (migrationEventArgs == null)
            {
                Debug.LogWarning($"[Mementos] MinionMigration: data is not MinionMigrationEventArgs, it is {data.GetType().FullName}");
                return;
            }

            Debug.Log($"[Mementos] MinionMigration: minionId={migrationEventArgs.minionId}, prevWorldId={migrationEventArgs.prevWorldId}, targetWorldId={migrationEventArgs.targetWorldId}");

            var minion = migrationEventArgs.minionId;
            var medalInfo = minion != null ? minion.FindOrAddComponent<MedalInfo>() : null;

            int oldWorldId = migrationEventArgs.prevWorldId;
            int newWorldId = migrationEventArgs.targetWorldId;

            var selectable = minion != null ? minion.GetComponent<KSelectable>() : null;
            string minionName = selectable != null ? selectable.GetProperName() : "Unknown Minion";
            Debug.Log($"[Mementos] MinionMigration: minionName={minionName}");

            if (oldWorldId == newWorldId) // first in space
            {
                Debug.Log("[Mementos] MinionMigration: Minion is first in space.");
                var mementoInfo = MementoPrototypes.Mementos["Space"];
                string mementoId = "Space";

                Debug.Log($"[Mementos] MinionMigration: mementoInfo.unique={mementoInfo.unique}, mementoInfo.repeatable={mementoInfo.repeatable}");

                if (mementoInfo.unique)
                {
                    if (MedalsSaveData.Instance.awardedUnique.ContainsKey(mementoId))
                    {
                        Debug.Log("[Mementos] MinionMigration: Unique memento already awarded.");
                        return;
                    }
                }
                if (!mementoInfo.repeatable)
                {
                    if (medalInfo != null && medalInfo.HasAwardedNonRepeatableMemento(mementoId))
                    {
                        Debug.Log("[Mementos] MinionMigration: Non-repeatable memento already awarded.");
                        return;
                    }
                }
                Debug.Log("[Mementos] MinionMigration: Awarding Space memento.");
                MementoConfig.CreateMemento(mementoInfo, minion);
                if (mementoInfo.unique)
                    MedalsSaveData.Instance.awardedUnique[mementoId] = minion.GetProperName();
                if (!mementoInfo.repeatable && medalInfo != null)
                    medalInfo.SetAwardedNonRepeatableMemento(mementoId);
            }
            else // first visit to a new world
            {
                Debug.Log("[Mementos] MinionMigration: Minion is visiting a new world.");
                if (!MementoPrototypes.Mementos.ContainsKey("FirstVisit"))
                {
                    Debug.LogWarning("[Mementos] MinionMigration: No FirstVisit memento defined.");
                    return;
                }

                var world = ClusterManager.Instance.GetWorld(newWorldId);
                Debug.Log($"[Mementos] MinionMigration: world={world}");

                string mementoId = "FirstVisit";
                var mementoInfo = MementoPrototypes.Mementos[mementoId];

                Debug.Log($"[Mementos] MinionMigration: mementoInfo.unique={mementoInfo.unique}, mementoInfo.repeatable={mementoInfo.repeatable}");

                if (mementoInfo.unique)
                {
                    if (MedalsSaveData.Instance.awardedUnique.ContainsKey(mementoId))
                    {
                        Debug.Log("[Mementos] MinionMigration: Unique memento already awarded.");
                        return;
                    }
                }
                if (!mementoInfo.repeatable)
                {
                    if (medalInfo != null && medalInfo.HasAwardedNonRepeatableMemento(mementoId))
                    {
                        Debug.Log("[Mementos] MinionMigration: Non-repeatable memento already awarded.");
                        return;
                    }
                }
                string name = mementoInfo.GetName();
                string desc = mementoInfo.GetDesc();

                string worldName = world != null
                    ? (world.GetComponent<ClusterGridEntity>()?.GetProperName() ?? world.name)
                    : $"World {newWorldId}";
                Debug.Log($"[Mementos] MinionMigration: worldName={worldName}");

                if (newWorldId == 0 || MedalsSaveData.Instance.awardedFirstVisitWorlds.Contains(newWorldId))
                {
                    Debug.Log("[Mementos] MinionMigration: First visit already awarded for this world or worldId is 0.");
                    return;
                }
                Debug.Log("[Mementos] MinionMigration: Awarding FirstVisit memento.");
                MementoConfig.CreateMemento(mementoInfo, minion, worldName);
                if (mementoInfo.unique)
                    MedalsSaveData.Instance.awardedUnique[mementoId] = minion.GetProperName();
                if (!mementoInfo.repeatable && medalInfo != null)
                    medalInfo.SetAwardedNonRepeatableMemento(mementoId);
                MedalsSaveData.Instance.awardedFirstVisitWorlds.Add(newWorldId);
            }
        }
    }


    [HarmonyPatch(typeof(RescueIncapacitatedChore), "DropIncapacitatedDuplicant")]
    public static class RescueIncapacitatedChore_RescuedDupeMedalPatch
    {
        private static HashSet<int> rescueAwardedChores = new HashSet<int>();

        public static void Postfix(RescueIncapacitatedChore __instance)
        {
            if (__instance == null) return;
            int choreId = __instance.GetHashCode();
            if (rescueAwardedChores.Contains(choreId))
            {
                return;
            }
            rescueAwardedChores.Add(choreId);

            var smi = __instance.smi;
            if (smi == null || smi.sm == null) return;
            var rescuerObj = smi.sm.rescuer.Get(smi);
            if (rescuerObj == null) return;

            var minion = rescuerObj.GetComponent<MinionIdentity>();
            if (minion == null) return;

            var deliverTarget = smi.sm.deliverTarget.Get(smi);
            string targetName = deliverTarget != null ? deliverTarget.name : "null";
            bool isMedicalCot = deliverTarget != null && deliverTarget.HasTag(new Tag("MedicalCot"));

            if (isMedicalCot)
            {
                string rescuedName = "Unknown";
                var rescueTargetObj = smi.sm.rescueTarget.Get(smi);
                if (rescueTargetObj != null)
                {
                    var rescuedMinion = rescueTargetObj.GetComponent<MinionIdentity>();
                    if (rescuedMinion != null)
                        rescuedName = rescuedMinion.GetProperName();

                    var mementoInfo = MementoPrototypes.Mementos["Rescue"];
                    var medalInfo = minion.FindOrAddComponent<MedalInfo>();
                    string mementoId = "Rescue";

                    if (mementoInfo.unique)
                    {
                        if (MedalsSaveData.Instance.awardedUnique.ContainsKey(mementoId))
                        {
                            return;
                        }
                    }
                    if (!mementoInfo.repeatable)
                    {
                        if (medalInfo != null && medalInfo.HasAwardedNonRepeatableMemento(mementoId))
                        {
                            return;
                        }
                    }
                    string medalName = mementoInfo.GetName() + $" {rescuedName}";
                    string medalDesc = mementoInfo.GetDesc() + $" {rescuedName}";

                    // Prevent duplicate medals
                    bool alreadyAwarded = medalInfo.Medals.Any(m => m.Name == medalName && m.Description == medalDesc);
                    if (!alreadyAwarded)
                    {
                        MementoConfig.CreateMemento(mementoInfo, minion, rescuedName);
                        if (mementoInfo.unique)
                            MedalsSaveData.Instance.awardedUnique[mementoId] = minion.GetProperName();
                        if (!mementoInfo.repeatable && medalInfo != null)
                            medalInfo.SetAwardedNonRepeatableMemento(mementoId);
                    }
                }
            }
        }
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public class MedalsSaveData : KMonoBehaviour, ISaveLoadable
    {
        [Serialize]
        public HashSet<int> awardedFirstVisitWorlds = new HashSet<int>();

        [Serialize]
        public Dictionary<string, string> awardedUnique = new Dictionary<string, string>();

        public static MedalsSaveData Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = GameObject.Find("MedalsSaveData") ?? new GameObject("MedalsSaveData");
                    _instance = go.GetComponent<MedalsSaveData>() ?? go.AddComponent<MedalsSaveData>();
                    UnityEngine.Object.DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        private static MedalsSaveData _instance;
    }

    public static class MementosEvents
    {
        public static void OnLandedStatic(object data)
        {
            Debug.Log("[Mementos] Landed event received!");

        }


        public static void OnRocketLandedStatic(object data)
        {
            Debug.Log("[Mementos] RocketLanded event received!");

            if (data is GameObject rocket)
            {
                // Print all available components on the rocket GameObject
                var components = rocket.GetComponents<Component>();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[Mementos] Rocket GameObject Components:");
                foreach (var comp in components)
                {
                    if (comp != null)
                        sb.AppendLine($"- {comp.GetType().FullName}");
                }
                Debug.Log(sb.ToString());

                var rocketModule = rocket.GetComponent<RocketModuleCluster>();

                var worldID = ClusterManager.Instance.GetWorld(rocketModule.GetMyWorldId());
                if (worldID != null)
                {
                    var clusterEntity = worldID.GetComponent<ClusterGridEntity>();
                    string properName = clusterEntity != null ? clusterEntity.GetProperName() : worldID.name;
                    Debug.Log($"[Mementos] Rocket landed on world: {properName} {worldID}");
                }
                else
                {
                    Debug.Log("[Mementos] worldLanded is null.");
                }

                var clustercraft = rocketModule.CraftInterface.GetComponent<Clustercraft>();

                if (clustercraft != null && clustercraft.ModuleInterface != null)
                {
                    var interiorWorld = clustercraft.ModuleInterface.GetInteriorWorld();
                    if (interiorWorld != null && interiorWorld.IsModuleInterior)
                    {
                        Debug.Log($"[Mementos] Found rocket interior: {interiorWorld.name} (id: {interiorWorld.id})");
                        var minionsInInterior = Components.MinionIdentities.GetWorldItems(interiorWorld.id);
                        foreach (var minion in minionsInInterior)
                            Debug.Log($"[Mementos] Minion onboard: {minion.GetProperName()}");
                    }
                    else
                    {
                        Debug.Log("[Mementos] No valid rocket interior found.");
                    }
                }
                else
                {
                    Debug.LogWarning("[Mementos] No Clustercraft or ModuleInterface found on rocket GameObject.");
                }
            }
        }
    }

}
