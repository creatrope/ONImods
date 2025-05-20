using HarmonyLib;
using UnityEngine;
using System.IO;
using KMod;
using System.Collections.Generic;
// using PeterHan.PLib.Core; // Uncomment if you use PLib

namespace ArtifactsPlus
{
    public static class ModInit
    {
        // Variable holding the path to the user's desktop directory
        public static readonly string DesktopLogPath =
            Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "ArtifactsPlus.log");

        private static bool _logInitialized = false;

        // Custom logger writes to ArtifactsPlus.log on the desktop
        public static void CustomLog(string message)
        {
            if (!_logInitialized)
            {
                File.WriteAllText(DesktopLogPath, string.Empty); // Start fresh each run
                _logInitialized = true;
            }
            string timestamped = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            File.AppendAllText(DesktopLogPath, timestamped + System.Environment.NewLine);
        }

        public static string ArtifactPowersConfigPath =>
            @"C:\Users\sendh\Documents\Klei\OxygenNotIncluded\mods\Local\ArtifactsPlus\ArtifactsConfig.json";

        public static void OnLoad()
        {
            Debug.Log("[ArtifactsPlus] OnLoad() was called!");
            Debug.Log($"[ArtifactsPlus] Custom log file location: {DesktopLogPath}");
            // Write a test message to the custom log
            CustomLog("Test message: custom log initialized and working.");
            // All other debug messages should use CustomLog
            //RegisterArtifactPowers();
        }

        private static void RegisterArtifactPowers()
        {
            string configPath = ArtifactPowersConfigPath;

            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                CustomLog($"Loaded configuration from: {configPath}");
                // TODO: Deserialize and use the config as needed
            }
            else
            {
                CustomLog($"Configuration file not found: {configPath}");
            }
        }
    }

    [HarmonyPatch(typeof(ItemPedestal), "OnOccupantChanged")]
    public static class ItemPedestal_OnOccupantChanged_Patch
    {
        // Store the occupant before the method runs
        private static readonly Dictionary<int, GameObject> PreviousOccupant = new Dictionary<int, GameObject>();

        static ItemPedestal_OnOccupantChanged_Patch()
        {
            Debug.Log("[ArtifactsPlus] Patch class loaded");
        }

        public static void Prefix(ItemPedestal __instance)
        {
            GameObject before = null;
            var receptacleField = typeof(ItemPedestal).GetField("receptacle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (__instance != null && receptacleField != null)
            {
                var receptacle = receptacleField.GetValue(__instance) as SingleEntityReceptacle;
                if (receptacle != null)
                    before = receptacle.Occupant;
            }
            PreviousOccupant[__instance.GetInstanceID()] = before;
        }

        public static void Postfix(ItemPedestal __instance, object data)
        {
            int id = __instance.GetInstanceID();
            GameObject before = null;
            PreviousOccupant.TryGetValue(id, out before);
            GameObject after = data as GameObject;

            if (before == null && after != null)
            {
                // Placement
                string objId = after.name;
                string displayName = after.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                ModInit.CustomLog($"[Pedestal] Placed object: ID={objId}, Name={displayName}");
            }
            else if (before != null && after == null)
            {
                // Removal
                string objId = before.name;
                string displayName = before.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                ModInit.CustomLog($"[Pedestal] Removed object: ID={objId}, Name={displayName}");
            }
            else if (before != null && after != null && before != after)
            {
                // Replacement
                string beforeId = before.name;
                string beforeName = before.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                string afterId = after.name;
                string afterName = after.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                ModInit.CustomLog($"[Pedestal] Replaced object: ID={beforeId}, Name={beforeName} -> ID={afterId}, Name={afterName}");
            }
            // else: no change or redundant event

            PreviousOccupant.Remove(id);
        }
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(HarmonyLib.Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log("[ArtifactsPlus] Mod loaded and Harmony patches applied.");
            harmony.PatchAll();
            ModInit.OnLoad(); // Ensure your custom initialization runs
        }
    }
}