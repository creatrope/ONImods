using HarmonyLib;
using UnityEngine;
using System.IO;
using KMod;
using System.Collections.Generic;
using System.Linq; // Add this at the top of the file

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
            // Use StreamWriter with AutoFlush to ensure immediate flush
            using (var writer = new StreamWriter(DesktopLogPath, true))
            {
                writer.AutoFlush = true;
                writer.WriteLine(timestamped);
            }
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

    public class ArtifactState
    {
        public bool OnPedestal;
        public bool MeetsRoomSize;
        public bool IsActive;
    }

    public static class ArtifactStateTracker
    {
        // Tracks the state of each artifact by instance ID
        internal static readonly Dictionary<int, ArtifactState> ArtifactStates = new Dictionary<int, ArtifactState>();

        // Changed from private to internal to make it accessible within the same assembly
        internal static readonly HashSet<GameObject> ArtifactsOnPedestals = new HashSet<GameObject>();

        public static void RegisterArtifactOnPedestal(GameObject artifact)
        {
            if (artifact != null)
            {
                ArtifactsOnPedestals.Add(artifact);
            }
        }

        public static void UnregisterArtifactOnPedestal(GameObject artifact)
        {
            if (artifact != null)
            {
                UpdateArtifactState(artifact, false, false); // Ensure state is set to inactive and debug message is logged
                ArtifactsOnPedestals.Remove(artifact);
            }
        }

        public static void UpdateArtifactState(GameObject artifact, bool onPedestal, bool meetsRoomSize)
        {
            int id = artifact.GetInstanceID();
            ArtifactState state;
            if (!ArtifactStates.TryGetValue(id, out state))
            {
                state = new ArtifactState();
                ArtifactStates[id] = state;
            }

            bool wasActive = state.IsActive;
            state.OnPedestal = onPedestal;
            state.MeetsRoomSize = meetsRoomSize;
            state.IsActive = onPedestal && meetsRoomSize;

            string displayName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
            if (wasActive != state.IsActive)
            {
                ModInit.CustomLog($"***** [ArtifactState] Artifact '{displayName}' (ID={artifact.name}) changed state: {(state.IsActive ? "ACTIVE" : "INACTIVE")}");
            }
        }

        public static void RemoveArtifact(GameObject artifact)
        {
            if (artifact != null)
            {
                int id = artifact.GetInstanceID();
                ArtifactStates.Remove(id);
                ArtifactsOnPedestals.Remove(artifact);
            }
        }

        // Poll only artifacts that are on pedestals
        public static void PollAllArtifacts()
        {
            foreach (var artifact in ArtifactsOnPedestals)
            {
                if (artifact == null)
                {
                    continue;
                }
                bool meetsRoomSize = false;
                Vector3 position = artifact.transform.position;
                int cell = Grid.PosToCell(position);
                int roomSize = -1;
                if (Game.Instance != null && Game.Instance.roomProber != null)
                {
                    var cavity = Game.Instance.roomProber.GetCavityForCell(cell);
                    var room = cavity?.room;
                    if (room != null && room.cavity != null)
                    {
                        roomSize = room.cavity.numCells;
                    }
                }
                meetsRoomSize = roomSize > 0 && roomSize < 32;
                UpdateArtifactState(artifact, true, meetsRoomSize);
            }
        }
    }

    public class ArtifactStatePoller : MonoBehaviour
    {
        private int tickCounter = 0;
        private const int PollInterval = 20; // Adjust as needed

        public ArtifactStatePoller() { }
        void Awake() { }
        void Start() { }
        void Update()
        {
            tickCounter++;
            if (tickCounter >= PollInterval)
            {
                tickCounter = 0;
                ArtifactStateTracker.PollAllArtifacts();
            }
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

            if (Game.Instance != null)
            {
                if (Game.Instance.gameObject.GetComponent<ArtifactStatePoller>() == null)
                {
                    Game.Instance.gameObject.AddComponent<ArtifactStatePoller>();
                }
            }
        }
    }

    [HarmonyPatch(typeof(ItemPedestal), "OnOccupantChanged")]
    public static class ItemPedestal_OnOccupantChanged_Patch
    {
        public static void Postfix(ItemPedestal __instance, object data)
        {
            var receptacleField = typeof(ItemPedestal).GetField("receptacle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var receptacle = receptacleField?.GetValue(__instance) as SingleEntityReceptacle;
            var occupant = receptacle?.Occupant;

            // Remove all artifacts that were previously registered for this pedestal
            foreach (var artifact in ArtifactStateTracker.ArtifactsOnPedestals.ToArray())
            {
                if (artifact == null) continue;
                // If this artifact is no longer on any pedestal, unregister it
                bool stillOnPedestal = false;
                foreach (var pedestal in GameObject.FindObjectsOfType<ItemPedestal>())
                {
                    var recField = typeof(ItemPedestal).GetField("receptacle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var rec = recField?.GetValue(pedestal) as SingleEntityReceptacle;
                    if (rec != null && rec.Occupant == artifact)
                    {
                        stillOnPedestal = true;
                        break;
                    }
                }
                if (!stillOnPedestal)
                    ArtifactStateTracker.UnregisterArtifactOnPedestal(artifact);
            }

            // Register the new occupant if present
            if (occupant != null)
                ArtifactStateTracker.RegisterArtifactOnPedestal(occupant);
        }
    }

    [HarmonyPatch(typeof(Game), "OnPrefabInit")]
    public static class Game_OnPrefabInit_Patch
    {
        public static void Postfix(Game __instance)
        {
            if (__instance != null && __instance.gameObject.GetComponent<ArtifactStatePoller>() == null)
            {
                __instance.gameObject.AddComponent<ArtifactStatePoller>();
            }
        }
    }
}