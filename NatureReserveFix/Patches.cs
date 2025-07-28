using HarmonyLib;
using Database;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using PeterHan.PLib.Options;
using Klei.AI;

namespace NatureReserveFix
{
    [ConfigFile(SharedConfigLocation: true)]
    public class NatureReserveFixOptions
    {
        [Option("Update Frame Interval", "How many frames to wait before processing room updates.")]
        [Limit(1, 600)]
        public int BatchUpdateFrameInterval { get; set; } = 240;

        [Option("Nature Reserve Quality of Life Bonus", "Extra bonus to Quality of Life for Nature Reserve effect.")]
        [Limit(-10, 10)]
        public int NatureReserveQoLBonus { get; set; } = 1;

        [Option("Park Quality of Life Bonus", "Extra bonus to Quality of Life for Park effect.")]
        [Limit(-10, 10)]
        public int ParkQoLBonus { get; set; } = 1;
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

            var go = new GameObject("BatchRoomUpdater");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<BatchRoomUpdater>();
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

            foreach (var cavity in cavitiesToUpdate)
            {
                Game.Instance.roomProber.UpdateRoom(cavity);
            }
            cavitiesToUpdate.Clear();
        }
    }

    public class BatchRoomUpdater : MonoBehaviour
    {
        void Update()
        {
            RoomUpdateBatcher.ProcessBatch();
        }
    }

    [HarmonyPatch(typeof(Game), "OnPrefabInit")]
    public static class WildPlantsConstraintPatch
    {
        private static bool bonusApplied = false;

        [HarmonyPostfix]
        public static void OnGamePrefabInit()
        {
            if (bonusApplied)
                return;
            bonusApplied = true;

            var natureReserveEffectId = "RoomNatureReserve";
            Effect natureReserveEffect = Db.Get().effects.Get(natureReserveEffectId);

            // Debug.Log($"[NatureReserveFix] Nature Reserve effect: {natureReserveEffectId}");
            if (natureReserveEffect != null)
            {
                foreach (var mod in natureReserveEffect.SelfModifiers)
                {
                    if (mod.AttributeId == "QualityOfLife")
                    {
                        mod.SetValue(mod.Value + Mod.Options.NatureReserveQoLBonus);
                        // Debug.Log($"[NatureReserveFix] Applied QoL bonus: {Mod.Options.NatureReserveQoLBonus} to Nature Reserve");
                    }
                    // Debug.Log($"[NatureReserveFix] Nature Reserve modifier: {mod.AttributeId} {mod.Value}");
                }
            }
            else
            {
                // Debug.LogError($"[NatureReserveFix] Could not find Nature Reserve effect: {natureReserveEffectId}");
            }

            var parkEffectId = "RoomPark";
            Effect parkEffect = Db.Get().effects.Get(parkEffectId);
            // Debug.Log($"[NatureReserveFix] Park effect: {parkEffectId}");
            if (parkEffect != null)
            {
                foreach (var mod in parkEffect.SelfModifiers)
                {
                    if (mod.AttributeId == "QualityOfLife")
                    {
                        mod.SetValue(mod.Value + Mod.Options.ParkQoLBonus);
                        // Debug.Log($"[NatureReserveFix] Applied QoL bonus: {Mod.Options.ParkQoLBonus} to Park");
                    }
                    // Debug.Log($"[NatureReserveFix] Park modifier: {mod.AttributeId} {mod.Value}");
                }
            }
            else
            {
                // Debug.LogError($"[NatureReserveFix] Could not find Park effect: {parkEffectId}");
            }

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
                return num >= 2;
            });
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
                }
            }
        }
    }
}
