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
            Debug.Log("[Mementos] MinionMigration: Postfix called.");
            if (data == null)
            {
                Debug.LogWarning("[Mementos] MinionMigration: data is null.");
                return;
            }

            Debug.Log($"[Mementos] MinionMigration: data type is {data.GetType().FullName}");

            var migrationEventArgs = data as MinionMigrationEventArgs;

            if (migrationEventArgs == null)
            {
                Debug.LogWarning($"[Mementos] MinionMigration: data is not MinionMigrationEventArgs, it is {data.GetType().FullName}");
                return;
            }

            Debug.Log($"[Mementos] MinionMigration: migrationEventArgs found. minionId={migrationEventArgs.minionId}, prevWorldId={migrationEventArgs.prevWorldId}, targetWorldId={migrationEventArgs.targetWorldId}");

            var minion = migrationEventArgs.minionId;
            Debug.Log($"[Mementos] MinionMigration: minion={(minion != null ? minion.ToString() : "null")}");

            var medalInfo = minion != null ? minion.FindOrAddComponent<MedalInfo>() : null;
            Debug.Log($"[Mementos] MinionMigration: medalInfo {(medalInfo != null ? "found/created" : "is null")}");

            int oldWorldId = migrationEventArgs.prevWorldId;
            int newWorldId = migrationEventArgs.targetWorldId;

            var selectable = minion != null ? minion.GetComponent<KSelectable>() : null;
            string minionName = selectable != null ? selectable.GetProperName() : "Unknown Minion";
            Debug.Log($"[Mementos] MinionMigration: minionName={minionName}");

            Debug.Log($"[Mementos] MinionMigration: oldWorldId={oldWorldId}, newWorldId={newWorldId}");

            if (oldWorldId == newWorldId) // first in space
            {
                Debug.Log("[Mementos] MinionMigration: oldWorldId == newWorldId, treating as 'first in space'.");

                var mementoInfo = MementoPrototypes.Mementos["Space"];
                string mementoId = "Space";

                Debug.Log($"[Mementos] MinionMigration: mementoInfo for 'Space' found: unique={mementoInfo.unique}, repeatable={mementoInfo.repeatable}");

                if (mementoInfo.unique)
                {
                    if (MedalsSaveData.Instance.awardedUnique.ContainsKey(mementoId))
                    {
                        Debug.LogWarning("[Mementos] MinionMigration: unique memento already awarded.");
                        return;
                    }
                }
                if (!mementoInfo.repeatable)
                {
                    if (medalInfo != null && medalInfo.HasAwardedNonRepeatableMemento(mementoId))
                    {
                        Debug.LogWarning("[Mementos] MinionMigration: non-repeatable memento already awarded to this minion.");
                        return;
                    }
                }
                Debug.Log("[Mementos] MinionMigration: Creating memento for 'Space'.");
                MementoConfig.CreateMemento(mementoInfo, minion);
                if (mementoInfo.unique)
                {
                    Debug.Log("[Mementos] MinionMigration: Recording unique memento award.");
                    MedalsSaveData.Instance.awardedUnique[mementoId] = minion.GetProperName();
                }
                if (!mementoInfo.repeatable && medalInfo != null)
                {
                    Debug.Log("[Mementos] MinionMigration: Recording non-repeatable memento award in MedalInfo.");
                    medalInfo.SetAwardedNonRepeatableMemento(mementoId);
                }
            }
            else // seems to be teleporter only case
            {
                Debug.Log("[Mementos] MinionMigration: oldWorldId != newWorldId, treating as teleporter/first visit case.");

                if (!MementoPrototypes.Mementos.ContainsKey("FirstVisit"))
                {
                    Debug.LogWarning("[Mementos] MinionMigration: No FirstVisit memento defined.");
                    return;
                }

                var world = ClusterManager.Instance.GetWorld(newWorldId);
                Debug.Log($"[Mementos] MinionMigration: ClusterManager.Instance.GetWorld({newWorldId}) = {world}");

                Debug.Log("[Mementos] MinionMigration: Attempting to award FirstVisit if eligible.");
                bool awarded = MementosEvents.AwardFirstVisitifEligible("FirstVisit", minion, newWorldId);
                Debug.Log($"[Mementos] MinionMigration: AwardFirstVisitifEligible returned {awarded}");
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

    // --- MedalsSaveData singleton and initialization fixes ---

    // this is global memento information
    [SerializationConfig(MemberSerialization.OptIn)]
    public class MedalsSaveData : KMonoBehaviour
    {
        [Serialize]
        public HashSet<int> awardedFirstVisitWorlds = new HashSet<int>();

        [Serialize]
        public Dictionary<string, string> awardedUnique = new Dictionary<string, string>();

        private static MedalsSaveData _instance;

        public static MedalsSaveData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindObjectOfType<MedalsSaveData>();
                    if (_instance == null)
                    {
                        var go = new GameObject("MedalsSaveData");
                        _instance = go.AddComponent<MedalsSaveData>();
                        Debug.Log("[Mementos] MedalsSaveData Instance created.");
                    }
                }
                return _instance;
            }
        }
    }

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
            Debug.Log($"[Mementos] OnRocketLanded called. data: {(data != null ? data.ToString() : "null")}");

            if (data is GameObject rocket)
            {
                Debug.Log($"[Mementos] OnRocketLanded: data is GameObject, name: {rocket.name}");

                var rocketModule = rocket.GetComponent<RocketModuleCluster>();
                if (rocketModule == null)
                {
                    Debug.LogWarning("[Mementos] OnRocketLanded: RocketModuleCluster not found on rocket GameObject.");
                    return;
                }
                Debug.Log($"[Mementos] OnRocketLanded: RocketModuleCluster found: {rocketModule}");

                int worldIdValue = rocketModule.GetMyWorldId();
                Debug.Log($"[Mementos] OnRocketLanded: RocketModuleCluster.GetMyWorldId() = {worldIdValue}");

                var world = ClusterManager.Instance.GetWorld(worldIdValue);
                Debug.Log($"[Mementos] OnRocketLanded: ClusterManager.Instance.GetWorld({worldIdValue}) = {world}");

                string worldName = MementosUtils.GetWorldName(worldIdValue);
                Debug.Log($"[Mementos] OnRocketLanded: worldName = {worldName}");

                var clustercraft = rocketModule.CraftInterface.GetComponent<Clustercraft>();
                Debug.Log($"[Mementos] OnRocketLanded: clustercraft = {clustercraft}");

                MinionIdentity oldestMinion = null;

                if (clustercraft != null && clustercraft.ModuleInterface != null)
                {
                    Debug.Log("[Mementos] OnRocketLanded: clustercraft.ModuleInterface is not null.");
                    var interiorWorld = clustercraft.ModuleInterface.GetInteriorWorld();
                    Debug.Log($"[Mementos] OnRocketLanded: interiorWorld = {interiorWorld}");

                    if (interiorWorld != null && interiorWorld.IsModuleInterior)
                    {
                        Debug.Log("[Mementos] OnRocketLanded: interiorWorld.IsModuleInterior is true.");
                        var minionsInInterior = Components.MinionIdentities.GetWorldItems(interiorWorld.id);
                        Debug.Log($"[Mementos] OnRocketLanded: minionsInInterior.Count = {minionsInInterior.Count}");
                        if (minionsInInterior.Count == 0)
                        {
                            Debug.Log("[Mementos] OnRocketLanded: No minions in interior.");
                            return;
                        }

                        float oldestAge = -1f;

                        foreach (var tminion in minionsInInterior)
                        {
                            float age = MementosUtils.GetMinionAge(tminion);
                            Debug.Log($"[Mementos] OnRocketLanded: Checking minion {tminion.GetProperName()} (age={age})");
                            if (age > oldestAge)
                            {
                                oldestAge = age;
                                oldestMinion = tminion;
                                Debug.Log($"[Mementos] OnRocketLanded: New oldest minion: {tminion.GetProperName()} (age={age})");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("[Mementos] OnRocketLanded: interiorWorld is null or not a module interior.");
                    }

                    if (oldestMinion != null)
                    {
                        Debug.Log($"[Mementos] OnRocketLanded: Awarding FirstVisit to oldest minion: {oldestMinion.GetProperName()}");
                        bool awarded = AwardFirstVisitifEligible("FirstVisit", oldestMinion, worldIdValue);
                        Debug.Log($"[Mementos] OnRocketLanded: AwardFirstVisitifEligible returned {awarded}");
                    }
                    else
                    {
                        Debug.Log("[Mementos] OnRocketLanded: No eligible minion found for FirstVisit award.");
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
        public static bool AwardFirstVisitifEligible(string mementoId, MinionIdentity minion, int worldID)
        {
            Debug.Log($"[Mementos] AwardFirstVisitifEligible called with mementoId='{mementoId}', minion='{(minion != null ? minion.GetProperName() : "null")}', worldID={worldID}");


            if (worldID == 0)
            {
                Debug.LogWarning("[Mementos] AwardFirstVisitifEligible: worldID is 0, aborting.");
                return false;
            }

            if (string.IsNullOrEmpty(mementoId) || string.IsNullOrEmpty(mementoId) || minion == null || !MementoPrototypes.Mementos.ContainsKey(mementoId))
            {
                Debug.LogWarning($"[Mementos] AwardFirstVisitifEligible: Invalid arguments. mementoId='{mementoId}', minion={(minion != null ? minion.GetProperName() : "null")}, mementoId found={MementoPrototypes.Mementos.ContainsKey(mementoId)}");
                return false;
            }

            if (!MementoPrototypes.Mementos.ContainsKey(mementoId))
            {
                Debug.LogWarning($"[Mementos] AwardFirstVisitifEligible: mementoId '{mementoId}' not found in prototypes.");
                return false;
            }

            var mementoInfo = MementoPrototypes.Mementos[mementoId];
            var medalInfo = minion.FindOrAddComponent<MedalInfo>();

            Debug.Log($"[Mementos] AwardFirstVisitifEligible (before) awardedFirstVisitWorlds: {string.Join(", ", MedalsSaveData.Instance.awardedFirstVisitWorlds)}");

            if (MedalsSaveData.Instance.awardedFirstVisitWorlds.Contains(worldID))
            {
                Debug.LogWarning($"[Mementos] AwardFirstVisitifEligible: worldID {worldID} already awarded. Not eligible.");
                return false;
            }

            var worldName = MementosUtils.GetWorldName(worldID);

            Debug.Log($"[Mementos] AwardFirstVisitifEligible: about to create memento for minion '{minion.GetProperName()}' in world '{worldName}'");
            MementoConfig.CreateMemento(mementoInfo, minion, worldName);

            Debug.Log($"[Mementos] AwardFirstVisitifEligible: Adding worldID {worldID} to awardedFirstVisitWorlds.");
            MedalsSaveData.Instance.awardedFirstVisitWorlds.Add(worldID);

            Debug.Log($"[Mementos] AwardFirstVisitifEligible (after) awardedFirstVisitWorlds: {string.Join(", ", MedalsSaveData.Instance.awardedFirstVisitWorlds)}");

            Debug.Log("[Mementos] AwardFirstVisitifEligible: Awarded successfully.");
            return true;
        }
    }


    [HarmonyPatch(typeof(JettisonableCargoModule.StatesInstance), "FinalDeploy")]
    public static class JettisonableCargoModule_FinalDeploy_MementosPrefix
    {
        public static void Prefix(JettisonableCargoModule.StatesInstance __instance)
        {
            Debug.Log("[Mementos] (Prefix) FinalDeploy called.");
            if (__instance == null)
            {
                Debug.LogWarning("[Mementos] (Prefix) FinalDeploy: __instance is null.");
                return;
            }

            Debug.Log($"[Mementos] (Prefix) FinalDeploy: __instance type: {__instance.GetType().FullName}");

            string minionName = "Unknown";
            MinionIdentity minion = null;
            string worldName = "Unknown World";
            int worldId = -1;

            if (__instance.chosenDuplicant != null)
            {
                Debug.Log($"[Mementos] (Prefix) FinalDeploy: chosenDuplicant is not null, type: {__instance.chosenDuplicant.GetType().FullName}");
                minion = __instance.chosenDuplicant.GetComponent<MinionIdentity>();
                if (minion != null)
                {
                    Debug.Log($"[Mementos] (Prefix) FinalDeploy: MinionIdentity found: {minion}");
                }
                else
                {
                    Debug.LogWarning("[Mementos] (Prefix) FinalDeploy: MinionIdentity not found on chosenDuplicant.");
                }
                minionName = __instance.chosenDuplicant.GetProperName();
                worldId = __instance.chosenDuplicant.gameObject.GetMyWorldId();
                worldName = MementosUtils.GetWorldName(worldId);

                Debug.Log($"[Mementos] (Prefix) FinalDeploy: Minion '{minionName}' about to deploy to world '{worldName}' (id={worldId})");
                MementosEvents.AwardFirstVisitifEligible("FirstVisit", minion, worldId);
            }
        }
    }
    public static class MementosUtils
    {
        public static string GetWorldName(int worldId)
        {
            string worldName = "Unknown World";
            var world = ClusterManager.Instance.GetWorld(worldId);
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
    }

    // Ensure MedalsSaveData is created only if not present, and never in the Instance getter
    [HarmonyPatch(typeof(Game), "OnSpawn")]
    public static class Game_OnSpawn_MedalsSaveDataEnsure
    {
        public static void Postfix()
        {
            if (UnityEngine.Object.FindObjectOfType<Mementos.MedalsSaveData>() == null)
            {
                var go = new GameObject("MedalsSaveData");
                go.AddComponent<Mementos.MedalsSaveData>();
                Debug.Log("[Mementos] MedalsSaveData GameObject created and registered for serialization.");
            }
        }
    }
}