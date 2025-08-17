using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSerialization;
using UnityEngine;

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
                    Debug.Log($"[Health_DamageMedalPatch] {minion.GetProperName()} is in schedule block: {blockName} (type: {blockTypeId})");
                }
                else
                {
                    Debug.Log($"[Health_DamageMedalPatch] {minion.GetProperName()} has no schedule or schedule block.");
                }

                if (!inWorkBlock)
                {
                    Debug.Log($"[Health_DamageMedalPatch] Skipping Injury medal: {minion.GetProperName()} is not in a Work block (current: {blockName ?? "none"}, type: {blockTypeId ?? "none"}).");
                    return;
                }
                else
                {
                    Debug.Log($"[Health_DamageMedalPatch] {minion.GetProperName()} is in a Work block, proceeding to award Injury medal.");
                }

                var mementoInfo = MementoPrototypes.Mementos["Injury"];
                var medalInfo = minion.FindOrAddComponent<MedalInfo>();
                string mementoId = "Injury";

                if (mementoInfo.unique)
                {
                    if (MedalsSaveData.Instance.awardedUnique.ContainsKey(mementoId))
                    {
                        Debug.Log("[Health_DamageMedalPatch] Injury medal is unique and already awarded, skipping award.");
                        return;
                    }
                }
                if (!mementoInfo.repeatable)
                {
                    if (medalInfo != null && medalInfo.HasAwardedNonRepeatableMemento(mementoId))
                    {
                        Debug.Log("[Health_DamageMedalPatch] Injury medal not repeatable and already awarded to this dupe, skipping award.");
                        return;
                    }
                }
                MementoConfig.CreateMemento(mementoInfo, minion);
                if (mementoInfo.unique)
                    MedalsSaveData.Instance.awardedUnique[mementoId] = minion.GetProperName();
                if (!mementoInfo.repeatable && medalInfo != null)
                    medalInfo.SetAwardedNonRepeatableMemento(mementoId);
                Debug.Log($"[Health_DamageMedalPatch] (after)");
            }
        }
    }

    [HarmonyPatch(typeof(AssignmentManager), "MinionMigration")]
    public static class AssignmentManager_MinionMigration_Patch
    {
        public static void Postfix(AssignmentManager __instance, object data)
        {
            var migrationEventArgs = data as MinionMigrationEventArgs;
            if (data == null)
            {
                Debug.Log("[Medals] MinionMigration data is null.");
                return;
            }
            if (migrationEventArgs == null)
            {
                Debug.Log("[Medals] MinionMigrationEventArgs cast failed.");
                return;
            }

            var minion = migrationEventArgs.minionId;
            var medalInfo = minion != null ? minion.FindOrAddComponent<MedalInfo>() : null;

            int oldWorldId = migrationEventArgs.prevWorldId;
            int newWorldId = migrationEventArgs.targetWorldId;

            var selectable = minion.GetComponent<KSelectable>();
            string minionName = selectable != null ? selectable.GetProperName() : "Unknown Minion";

            if (oldWorldId == newWorldId) // first in space
            {
                var mementoInfo = MementoPrototypes.Mementos["Space"];
                string mementoId = "Space";

                if (mementoInfo.unique)
                {
                    if (MedalsSaveData.Instance.awardedUnique.ContainsKey(mementoId))
                    {
                        Debug.Log("[AssignmentManager_MinionMigration_Patch] Space medal is unique and already awarded, skipping award.");
                        return;
                    }
                }
                if (!mementoInfo.repeatable)
                {
                    if (medalInfo != null && medalInfo.HasAwardedNonRepeatableMemento(mementoId))
                    {
                        Debug.Log("[AssignmentManager_MinionMigration_Patch] Space medal not repeatable and already awarded to this dupe, skipping award.");
                        return;
                    }
                }
                MementoConfig.CreateMemento(mementoInfo, minion);
                if (mementoInfo.unique)
                    MedalsSaveData.Instance.awardedUnique[mementoId] = minion.GetProperName();
                if (!mementoInfo.repeatable && medalInfo != null)
                    medalInfo.SetAwardedNonRepeatableMemento(mementoId);
            }
            else // first visit to a new world
            {
                if (!MementoPrototypes.Mementos.ContainsKey("FirstVisit")) return;

                var world = ClusterManager.Instance.GetWorld(newWorldId);
                if (world == null)
                {
                    Debug.Log($"[Medals] ClusterManager.Instance.GetWorld({newWorldId}) returned null.");
                }
                else
                {
                    Debug.Log($"[Medals] world id: {world.id}, world name: {world.name}, world type: {world.GetType().FullName}");
                }

                string mementoId = "FirstVisit";
                var mementoInfo = MementoPrototypes.Mementos[mementoId];

                if (mementoInfo.unique)
                {
                    if (MedalsSaveData.Instance.awardedUnique.ContainsKey(mementoId))
                    {
                        Debug.Log("[AssignmentManager_MinionMigration_Patch] FirstVisit medal is unique and already awarded, skipping award.");
                        return;
                    }
                }
                if (!mementoInfo.repeatable)
                {
                    if (medalInfo != null && medalInfo.HasAwardedNonRepeatableMemento(mementoId))
                    {
                        Debug.Log("[AssignmentManager_MinionMigration_Patch] FirstVisit medal not repeatable and already awarded to this dupe, skipping award.");
                        return;
                    }
                }
                string name = mementoInfo.GetName();
                string desc = mementoInfo.GetDesc();

                string worldName = world != null
                    ? (world.GetComponent<ClusterGridEntity>()?.GetProperName() ?? world.name)
                    : $"World {newWorldId}";
                if (newWorldId == 0 || MedalsSaveData.Instance.awardedFirstVisitWorlds.Contains(newWorldId))
                {
                    Debug.Log($"[Medals] Homeworld || FirstVisit already awarded for world '{newWorldId}', skipping.");
                    return;
                }
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
                Debug.Log("[RescuePatch] Rescue already awarded for this chore instance, skipping.");
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
                            Debug.Log("[RescueIncapacitatedChore_RescuedDupeMedalPatch] Rescue medal is unique and already awarded, skipping award.");
                            return;
                        }
                    }
                    if (!mementoInfo.repeatable)
                    {
                        if (medalInfo != null && medalInfo.HasAwardedNonRepeatableMemento(mementoId))
                        {
                            Debug.Log("[RescueIncapacitatedChore_RescuedDupeMedalPatch] Rescue medal not repeatable and already awarded to this dupe, skipping award.");
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
                    Debug.Log($"[RescuePatch] Called for minion: {minion?.GetProperName()}, rescued: {rescuedName}, isMedicalCot: {isMedicalCot}");
Debug.Log($"[RescuePatch] MedalInfo.Medals count: {medalInfo.Medals.Count}, alreadyAwarded: {alreadyAwarded}");
                }
            }
            Debug.Log("[RescuePatch] Postfix called");
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

}
