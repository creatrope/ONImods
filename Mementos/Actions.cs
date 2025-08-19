using HarmonyLib;
using KSerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static MementosGlobalData;

namespace Mementos
{
    public static class MementosEvents
    {
        public static void OnLanded(object data)
        {
            Debug.Log("[Mementos] OnLanded called with data: " + (data != null ? data.ToString() : "null"));
        }

        public static void OnModuleLanderLanded(object data)
        {
            Debug.Log("[Mementos] OnModuleLanderLanded called with data: " + (data != null ? data.ToString() : "null"));
        }

        public static void OnRocketLanded(object data)
        {
            if (data is GameObject rocket)
            {
                var rocketModule = rocket.GetComponent<RocketModuleCluster>();
                if (rocketModule == null)
                    return;

                int worldIdValue = rocketModule.GetMyWorldId();
                var world = ClusterManager.Instance.GetWorld(worldIdValue);
                string worldName = MementoUtils.GetWorldName(world);
                var clustercraft = rocketModule.CraftInterface.GetComponent<Clustercraft>();

                if (clustercraft != null && clustercraft.ModuleInterface != null)
                {
                    var interiorWorld = clustercraft.ModuleInterface.GetInteriorWorld();

                    if (interiorWorld != null && interiorWorld.IsModuleInterior)
                    {
                        var minionsInInterior = Components.MinionIdentities.GetWorldItems(interiorWorld.id);
                        if (minionsInInterior.Count == 0)
                            return;

                        MementoUtils.AwardMementosOnce("FirstVisit", minionsInInterior, world);

                    }
                }
                else
                {
                    Debug.Log("[Mementos] OnRocketLanded: clustercraft or clustercraft.ModuleInterface is null.");
                }
            }
            else
            {
                Debug.LogWarning("[Mementos] OnRocketLanded: data is not a GameObject.");
            }
        }

        [HarmonyPatch(typeof(Health), "Damage")]
        public static class Health_DamageMedalPatch
        {
            private static void Postfix(Health __instance, float amount)
            {
                var minion = __instance.GetComponent<MinionIdentity>();
                if (minion != null && amount > 0)
                {
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
                        return;

                    MementoUtils.AwardMementosOnce("Injury", new List<MinionIdentity> { minion }, minion);
                }
            }
        }

        // first to space award, for everyone onboard
        [HarmonyPatch(typeof(Clustercraft), "Launch")]
        public static class Clustercraft_Launch_Postfix
        {
            public static void Postfix(Clustercraft __instance, bool automated = false)
            {
                Debug.Log($"[Mementos] Clustercraft '{__instance.m_name}' launched. Automated: {automated}");
                var interiorWorld = __instance.ModuleInterface.GetInteriorWorld();
                if (interiorWorld != null)
                {
                    var minions = Components.MinionIdentities.GetWorldItems(interiorWorld.id);
                    foreach (var minion in minions)
                    {
                        Debug.Log($"[Mementos] Minion onboard: {minion.GetProperName()}");
                    }

                    var mementoInfo = MementoPrototypes.Mementos["Space"];
                    var worldName = MementoUtils.GetWorldName(interiorWorld);

                    MementoUtils.AwardMementosOnce("Space", minions);
                }
            }
        }

        // teleporter case to a new world
        [HarmonyPatch(typeof(AssignmentManager), "MinionMigration")]
        public static class AssignmentManager_MinionMigration_Patch
        {
            public static void Postfix(AssignmentManager __instance, object data)
            {
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

                var minion = migrationEventArgs.minionId;
                int oldWorldId = migrationEventArgs.prevWorldId;
                int newWorldId = migrationEventArgs.targetWorldId;

                if (oldWorldId != newWorldId)
                {
                    MementoUtils.AwardMementosOnce("FirstVisit", new List<MinionIdentity> { minion }, newWorldId);
                }
            }
        }


        [HarmonyPatch(typeof(JettisonableCargoModule.StatesInstance), "FinalDeploy")]
        public static class JettisonableCargoModule_FinalDeploy_MementosPrefix
        {
            public static void Prefix(JettisonableCargoModule.StatesInstance __instance)
            {
                if (__instance == null)
                {
                    Debug.LogWarning("[Mementos] (Prefix) FinalDeploy: __instance is null.");
                    return;
                }

                string minionName = "Unknown";
                MinionIdentity minion = null;
                string worldName = "Unknown World";
                int worldId = -1;

                if (__instance.chosenDuplicant != null)
                {
                    minion = __instance.chosenDuplicant.GetComponent<MinionIdentity>();
                    minionName = __instance.chosenDuplicant.GetProperName();
                    worldId = __instance.chosenDuplicant.gameObject.GetMyWorldId();
                    var world = ClusterManager.Instance.GetWorld(worldId);
                    worldName = MementoUtils.GetWorldName(world);
                    MementoUtils.AwardMementosOnce("FirstVisit", new List<MinionIdentity> { minion }, world);
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

                        MementoUtils.AwardMementosOnce("Rescue", new List<MinionIdentity> { minion }, rescuedMinion); 
                    }
                }
            }
        }
    
    }
 }

// this is global memento information
[SerializationConfig(MemberSerialization.OptIn)]
public class MementosGlobalData : KMonoBehaviour
{
    [Serialize]
    public Dictionary<string, bool> Issued = new Dictionary<string, bool>();

    private static MementosGlobalData _instance;

    public static MementosGlobalData Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindObjectOfType<MementosGlobalData>();
                if (_instance == null)
                {
                    var go = new GameObject("MementosGlobalData");
                    _instance = go.AddComponent<MementosGlobalData>();
                    Debug.Log("[Mementos] MementosGlobalData Instance created.");
                }
            }
            return _instance;
        }
    }
}
