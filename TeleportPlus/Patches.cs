using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace TeleportPlus
{
    public static class TeleportConfig
    {
        // Set your desired recharge time here (in seconds)
        public static float RechargeTime = 5f; // Example: 2 seconds
    }

    public class Patches
    {
        [HarmonyPatch(typeof(Db))]
        [HarmonyPatch("Initialize")]
        public class Db_Initialize_Patch
        {
            public static void Prefix()
            {
                Debug.Log("[TeleportPlus] installed!");
            }
        }

        [HarmonyPatch(typeof(Teleporter), "TeleportObjects")]
        public static class Teleporter_TeleportObjects_Patch
        {
            private static Dictionary<Teleporter, float> lastTeleportTime = new Dictionary<Teleporter, float>();

            public static bool Prefix(Teleporter __instance)
            {
                float now = Time.time;
                if (lastTeleportTime.TryGetValue(__instance, out float lastTime))
                {
                    if (now - lastTime < TeleportConfig.RechargeTime)
                    {
                        Debug.Log("[TeleportPlus] Teleporter is recharging.");
                        return false; // Block teleport
                    }
                }
                lastTeleportTime[__instance] = now;
                return true; // Allow teleport
            }
        }

        // Add a Harmony patch to listen for a key press in the DebugHandler.Update method
        [HarmonyPatch(typeof(DebugHandler), "Update")]
        public static class DebugHandler_Update_Patch
        {
            public static void Postfix()
            {
                // Example: Press F8 to log a debug message
                if (UnityEngine.Input.GetKeyDown(KeyCode.F8)) // Fully qualify Input to avoid ambiguity
                {
                    Debug.Log("[TeleportPlus] F8 key pressed!");
                }
            }
        }
    }
}
