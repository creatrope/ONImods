using HarmonyLib;
using Database;
using UnityEngine;
using System.Reflection;

namespace NatureReserveFix
{
    public class Mod : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll();
            // Debug.Log("[NatureReserveFix] Harmony patches applied.");
        }
    }

    [HarmonyPatch(typeof(Game), "OnPrefabInit")]
    public static class WildPlantsConstraintPatch
      {
        [HarmonyPostfix]
        public static void OnGamePrefabInit()
        {
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
            // Debug.Log("[NatureReserveFix] Patched WILDPLANTS constraint for Nature Reserve (OnPrefabInit).");
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
                // Only refresh the room of the wilted plant
                Room room = Game.Instance.roomProber.GetRoomOfGameObject(__instance.gameObject);
                if (room?.cavity != null)
                {
                    Game.Instance.roomProber.UpdateRoom(room.cavity);
                    // Debug.Log($"[NatureReserveFix] RoomProber.UpdateRoom() called for wilted plant: {__instance.gameObject.name}");
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
                // Only refresh the room of the recovered plant
                Room room = Game.Instance.roomProber.GetRoomOfGameObject(__instance.gameObject);
                if (room?.cavity != null)
                {
                    Game.Instance.roomProber.UpdateRoom(room.cavity);
                    // Debug.Log($"[NatureReserveFix] RoomProber.UpdateRoom() called for recovered plant: {__instance.gameObject.name}");
                }
            }
        }
    }
}
