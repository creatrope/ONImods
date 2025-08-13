using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                string name = $"Injured Medal";
                string desc = $"Awarded for being injured.";

                var medalInfo = minion.FindOrAddComponent<MedalInfo>();
                if (medalInfo != null)
                {
                    var medal = new Medal("Injured", "Injuries in the Line of Duty", MedalType.Citation, true);
                    medalInfo.Medals.Add(medal);

                    TrophyConfig.CreateAndAwardTrophy(name, desc, minion);
                }
                Debug.Log($"[Health_DamageMedalPatch] (after)");
            }
        }
    }

    [HarmonyPatch(typeof(AssignmentManager), "MinionMigration")]
    public static class AssignmentManager_MinionMigration_Patch
    {
        public static void Postfix(AssignmentManager __instance, object data)
        {
            Debug.Log("[Medals] AssignmentManager_MinionMigration_Patch.Postfix called.");
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

            Debug.Log($"[Medals] MinionMigration data type: {data.GetType().FullName}");
            Debug.Log($"[Medals] migrationEventArgs: prevWorldId={migrationEventArgs.prevWorldId}, targetWorldId={migrationEventArgs.targetWorldId}, minionId={migrationEventArgs.minionId}");

            var minion = migrationEventArgs.minionId; // Use the minion from the event args
            var medalInfo = minion != null ? minion.FindOrAddComponent<MedalInfo>() : null;

            int oldWorldId = migrationEventArgs.prevWorldId;
            int newWorldId = migrationEventArgs.targetWorldId;

            var selectable = minion.GetComponent<KSelectable>();
            string minionName = selectable != null ? selectable.GetProperName() : "Unknown Minion";

            Debug.Log($"[Medals] minionName: {minionName}, oldWorldId: {oldWorldId}, newWorldId: {newWorldId}");

            if (oldWorldId == newWorldId) // first in space
            {
                Debug.Log("[Medals] oldWorldId == newWorldId, awarding SpaceLaunchMedal.");
                string name = $"Space Launch";
                string desc = $"First in space.";
                var medal = new Medal(name, desc, MedalType.Citation, true);
                medalInfo.Medals.Add(medal);
                TrophyConfig.CreateAndAwardTrophy(name, desc, minion);
                Debug.Log($"[ArtifactsPlus] Minion migrated and awarded SpaceLaunchMedal.");
            }
            else // first visit to a new world
            {
                var world = ClusterManager.Instance.GetWorld(newWorldId);
                if (world == null)
                {
                    Debug.Log($"[Medals] ClusterManager.Instance.GetWorld({newWorldId}) returned null.");
                }
                else
                {
                    Debug.Log($"[Medals] world id: {world.id}, world name: {world.name}, world type: {world.GetType().FullName}");
                }

                string worldName = world != null
                    ? (world.GetComponent<ClusterGridEntity>()?.GetProperName() ?? world.name)
                    : $"World {newWorldId}";

                string name = $"First Visit To {worldName}";
                string desc = $"First Visit to {worldName}.";
                var medal = new Medal(name, desc, MedalType.Citation, true);
                medalInfo.Medals.Add(medal);
                TrophyConfig.CreateAndAwardTrophy(name, desc, minion);
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
                // Try to get the rescued duplicant's name
                // Get the rescued duplicant's name from rescueTarget
                string rescuedName = "Unknown";
                var rescueTargetObj = smi.sm.rescueTarget.Get(smi);
                if (rescueTargetObj != null)
                {
                    var rescuedMinion = rescueTargetObj.GetComponent<MinionIdentity>();
                    if (rescuedMinion != null)
                        rescuedName = rescuedMinion.GetProperName();
                }

                string name = $"Rescuer Medal for rescuing {rescuedName}";
                var medalInfo = minion.FindOrAddComponent<MedalInfo>();
                string desc = $"For rescuing {rescuedName}.";
                var medal = new Medal(name, desc, MedalType.Citation, true);
                medalInfo.Medals.Add(medal);
                TrophyConfig.CreateAndAwardTrophy(name, desc, minion);
            }
        }
    }

}
