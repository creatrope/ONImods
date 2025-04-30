using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace SmartAutoSweeper
{
    [HarmonyPatch(typeof(SolidTransferArm), "UpdateFetches")]
    public static class SolidTransferArm_UpdateFetches_Patch
    {
        public static void Postfix(SolidTransferArm __instance)
        {
            // This is called every time the autosweeper updates fetch targets

            ListPool<Pickupable, SolidTransferArm>.PooledList fetchables = Traverse.Create(__instance)
                .Field("fetchables")
                .GetValue<ListPool<Pickupable, SolidTransferArm>.PooledList>();

            if (fetchables == null)
                return;

            foreach (var pickupable in fetchables)
            {
                if (pickupable == null || pickupable.gameObject == null)
                    continue;

                var pe = pickupable.PrimaryElement;
                if (pe == null)
                    continue;

                float temp = pe.Temperature;
                string name = pickupable.gameObject.name;
                Debug.Log($"[SmartAutoSweeper] Pickupable: {name} | Temp: {temp:F1} K");
            }
        }
    }
}
