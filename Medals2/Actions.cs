using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GarbageCollectionProfiler;
using static STRINGS.UI.UISIDESCREENS.AUTOPLUMBERSIDESCREEN.BUTTONS;

namespace Medals2
{

    [HarmonyPatch(typeof(Health), "Damage")]
    public static class Health_DamageMedalPatch
    {
        private static void Postfix(Health __instance, float amount)
        {
            var minion = __instance.GetComponent<MinionIdentity>();
            if (minion != null && amount > 0)
            {
                var trophyInfo = TrophyDb.Trophies["Injury"];
                MedalInfo.CreateMedal(trophyInfo, minion);
                TrophyConfig.CreateTrophy(trophyInfo, minion);
                Debug.Log($"[Health_DamageMedalPatch] (after)");
            }
        }
    }

    [HarmonyPatch(typeof(AssignmentManager), "MinionMigration")]
    public static class AssignmentManager_MinionMigration_Patch
    {
        public static void Postfix(AssignmentManager __instance, object data)
        {
            //Debug.Log("[Medals] AssignmentManager_MinionMigration_Patch.Postfix called.");
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

            var minion = migrationEventArgs.minionId; // Use the minion from the event args
            var medalInfo = minion != null ? minion.FindOrAddComponent<MedalInfo>() : null;

            int oldWorldId = migrationEventArgs.prevWorldId;
            int newWorldId = migrationEventArgs.targetWorldId;

            var selectable = minion.GetComponent<KSelectable>();
            string minionName = selectable != null ? selectable.GetProperName() : "Unknown Minion";

            if (oldWorldId == newWorldId) // first in space
            {
                var trophyInfo = TrophyDb.Trophies["Space"];
                MedalInfo.CreateMedal(trophyInfo, minion);
                TrophyConfig.CreateTrophy(trophyInfo, minion);
            }
            else // first visit to a new world
            {
                if (!TrophyDb.Trophies.ContainsKey("FirstVisit")) return;

                var world = ClusterManager.Instance.GetWorld(newWorldId);
                if (world == null)
                {
                    Debug.Log($"[Medals] ClusterManager.Instance.GetWorld({newWorldId}) returned null.");
                }
                else
                {
                    Debug.Log($"[Medals] world id: {world.id}, world name: {world.name}, world type: {world.GetType().FullName}");
                }

                var trophyInfo = TrophyDb.Trophies["FirstVisit"];
                string name = trophyInfo.GetName();
                string desc = trophyInfo.GetDesc();

                string worldName = world != null
                    ? (world.GetComponent<ClusterGridEntity>()?.GetProperName() ?? world.name)
                    : $"World {newWorldId}";
                MedalInfo.CreateMedal(trophyInfo, minion, worldName);
                TrophyConfig.CreateTrophy(trophyInfo, minion, worldName);
            }
        }
    }

    [HarmonyPatch(typeof(RescueIncapacitatedChore), "DropIncapacitatedDuplicant")]
    public static class RescueIncapacitatedChore_RescuedDupeMedalPatch
    {
        public static void Postfix(RescueIncapacitatedChore __instance)
        {

            var smi = __instance.smi;
            if (smi == null || smi.sm == null) return;
            var rescuerObj = smi.sm.rescuer.Get(smi);
            if (rescuerObj == null) return;

            var minion = rescuerObj.GetComponent<MinionIdentity>();
            if (minion == null) return;

            var deliverTarget = smi.sm.deliverTarget.Get(smi);
            string targetName = deliverTarget != null ? deliverTarget.name : "null";
            bool isMedicalCot = deliverTarget != null && deliverTarget.HasTag(new Tag("MedicalCot"));

            // Debug.Log($"[Medals] DropIncapacitatedDuplicant called. deliverTarget: {targetName}, isMedicalCot: {isMedicalCot}");

            if (isMedicalCot)
            {
                string rescuedName = "Unknown";
                var rescueTargetObj = smi.sm.rescueTarget.Get(smi);
                if (rescueTargetObj != null)
                {
                    var rescuedMinion = rescueTargetObj.GetComponent<MinionIdentity>();
                    if (rescuedMinion != null)
                        rescuedName = rescuedMinion.GetProperName();

                    var trophyInfo = TrophyDb.Trophies["Rescue"];
                    var medalInfo = minion.FindOrAddComponent<MedalInfo>();
                    string medalName = trophyInfo.GetName() + $" {rescuedName}";
                    string medalDesc = trophyInfo.GetDesc() + $" {rescuedName}";

                    // Prevent duplicate medals
                    bool alreadyAwarded = medalInfo.Medals.Any(m => m.Name == medalName && m.Description == medalDesc);
                    if (!alreadyAwarded)
                    {
                        MedalInfo.CreateMedal(trophyInfo, minion, rescuedName);
                        TrophyConfig.CreateTrophy(trophyInfo, minion, rescuedName);
                    }
                }
            }
        }
    }

}
