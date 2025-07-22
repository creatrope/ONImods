using HarmonyLib;
using Database;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using PeterHan.PLib.Options;

namespace NatureReserveFix
{
    [ConfigFile(SharedConfigLocation: true)]
    public class NatureReserveFixOptions
    {
        [Option("Update Frame Interval", "How many frames to wait before processing room updates.")]
        [Limit(1, 600)]
        public int BatchUpdateFrameInterval { get; set; } = 60;
    }

    public class Mod : KMod.UserMod2
    {
        public static NatureReserveFixOptions Options { get; private set; }

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll();
            Options = new NatureReserveFixOptions();
            new POptions().RegisterOptions(this, typeof(NatureReserveFixOptions));
        }
    }

    // Static queue for batch room updates, processed every N frames
    internal static class RoomUpdateBatcher
    {
        private static readonly HashSet<CavityInfo> cavitiesToUpdate = new HashSet<CavityInfo>();
        private static int frameCounter = 0;

        public static void QueueUpdate(CavityInfo cavity)
        {
            if (cavity != null)
            {
                bool added = cavitiesToUpdate.Add(cavity);
                if (added)
                {
                    // Debug.Log($"[NatureReserveFix] Queued cavity for update: {cavity.GetHashCode()} (total queued: {cavitiesToUpdate.Count})");
                }
                else
                {
                    // Debug.Log($"[NatureReserveFix] Cavity already queued: {cavity.GetHashCode()}");
                }
            }
        }

        public static void ProcessBatch()
        {
            int n = Mod.Options?.BatchUpdateFrameInterval ?? 60;
            frameCounter++;
            if (frameCounter < n)
                return;
            frameCounter = 0;

            if (Game.Instance?.roomProber == null)
                return;

            if (cavitiesToUpdate.Count > 0)
            {
                // Debug.Log($"[NatureReserveFix] Processing batch update for {cavitiesToUpdate.Count} cavities: [{string.Join(", ", cavitiesToUpdate.Select(c => c.GetHashCode()))}]");
            }

            foreach (var cavity in cavitiesToUpdate)
            {
                Game.Instance.roomProber.UpdateRoom(cavity);
            }
            cavitiesToUpdate.Clear();
        }
    }

    [HarmonyPatch(typeof(Game), "Update")]
    public static class Game_Update_BatchRoomPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            RoomUpdateBatcher.ProcessBatch();
        }
    }

    [HarmonyPatch(typeof(Game), "OnPrefabInit")]
    public static class WildPlantsConstraintPatch
    {
        [HarmonyPostfix]
        public static void OnGamePrefabInit()
        {
            // Patch Nature Reserve (WILDPLANTS) constraint
            var wildPlantsConstraint = RoomConstraints.WILDPLANTS;
            wildPlantsConstraint.room_criteria = new System.Func<Room, bool>((room) =>
            {
                int num = 0;
                foreach (KPrefabID plant in room.cavity.plants)
                {
                    if ((UnityEngine.Object)plant != (UnityEngine.Object)null)
                    {
                        var wiltCondition = plant.GetComponent<WiltCondition>();
                        if (wiltCondition != null && wiltCondition.IsWilting())
                            continue;

                        BasicForagePlantPlanted forage = plant.GetComponent<BasicForagePlantPlanted>();
                        ReceptacleMonitor receptacle = plant.GetComponent<ReceptacleMonitor>();
                        if ((UnityEngine.Object)receptacle != (UnityEngine.Object)null && !receptacle.Replanted)
                            ++num;
                        else if ((UnityEngine.Object)forage != (UnityEngine.Object)null)
                            ++num;
                    }
                }
                // Debug.Log($"[NatureReserveFix] WildPlantsConstraint: healthy wild plant count = {num}, room = {room?.ToString() ?? "null"}");
                return num >= 4;
            });

            // Patch Park (WILDPLANT) constraint
            var wildPlantConstraint = RoomConstraints.WILDPLANT;
            wildPlantConstraint.room_criteria = new System.Func<Room, bool>((room) =>
            {
                int num = 0;
                foreach (KPrefabID plant in room.cavity.plants)
                {
                    if ((UnityEngine.Object)plant != (UnityEngine.Object)null)
                    {
                        var wiltCondition = plant.GetComponent<WiltCondition>();
                        if (wiltCondition != null && wiltCondition.IsWilting())
                            continue;

                        BasicForagePlantPlanted forage = plant.GetComponent<BasicForagePlantPlanted>();
                        ReceptacleMonitor receptacle = plant.GetComponent<ReceptacleMonitor>();
                        if ((UnityEngine.Object)receptacle != (UnityEngine.Object)null && !receptacle.Replanted)
                            ++num;
                        else if ((UnityEngine.Object)forage != (UnityEngine.Object)null)
                            ++num;
                    }
                }
                // Debug.Log($"[NatureReserveFix] WildPlantConstraint: healthy wild plant count = {num}, room = {room?.ToString() ?? "null"}");
                return num >= 2;
            });
            // Debug.Log("[NatureReserveFix] Patched WILDPLANTS and WILDPLANT constraints for Nature Reserve and Park (OnPrefabInit).");
        }
    }

    [HarmonyPatch(typeof(WiltCondition), "DoWilt")]
    public static class WiltCondition_DoWilt_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(WiltCondition __instance)
        {
            if (Game.Instance != null && Game.Instance.roomProber != null)
            {
                Room room = Game.Instance.roomProber.GetRoomOfGameObject(__instance.gameObject);
                if (room?.cavity != null)
                {
                    RoomUpdateBatcher.QueueUpdate(room.cavity);
                    // Debug.Log($"[NatureReserveFix] Queued room update for wilted plant: {__instance.gameObject.name}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(WiltCondition), "DoRecover")]
    public static class WiltCondition_DoRecover_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(WiltCondition __instance)
        {
            if (Game.Instance != null && Game.Instance.roomProber != null)
            {
                Room room = Game.Instance.roomProber.GetRoomOfGameObject(__instance.gameObject);
                if (room?.cavity != null)
                {
                    RoomUpdateBatcher.QueueUpdate(room.cavity);
                    // Debug.Log($"[NatureReserveFix] Queued room update for recovered plant: {__instance.gameObject.name}");
                }
            }
        }
    }
}
