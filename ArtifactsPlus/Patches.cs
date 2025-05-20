using HarmonyLib;
using UnityEngine;
using System.IO;
using KMod;
using System.Collections.Generic;
using System.Linq;
using System;
using Klei.AI;
using System.Reflection;
using Newtonsoft.Json.Linq;

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
            CustomLog("Test message: custom log initialized and working.");
            //PrintAllAttributes(); // <-- Add this line to call the function when the mod loads
            // All other debug messages should use CustomLog
            //RegisterArtifactPowers();
        }

        private static void RegisterArtifactPowers()
        {
            string configPath = ArtifactPowersConfigPath;

            if (File.Exists(configPath))
            {
                CustomLog($"Loaded configuration from: {configPath}");
                // TODO: Deserialize and use the config as needed
            }
            else
            {
                CustomLog($"Configuration file not found: {configPath}");
            }
        }

        public static void PrintAllAttributes()
        {
            try
            {
                // Ensure Db is initialized before accessing attributes
                if (Db.Get() != null && Db.Get().Attributes != null)
                {
                    foreach (var attribute in Db.Get().Attributes.resources)
                    {
                        CustomLog($"Attribute ID: {attribute.Id}, Name: {attribute.Name}, Description: {attribute.Description}");
                    }
                }
                else
                {
                    CustomLog("Db or Db.Get().Attributes is not initialized yet.");
                }
            }
            catch (Exception ex)
            {
                CustomLog($"Exception while printing attributes: {ex}");
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
        internal static readonly HashSet<GameObject> ArtifactsOnPedestals = new HashSet<GameObject>();

        private static Dictionary<string, Dictionary<string, float>> artifactAttributeMap;

        public static void RegisterArtifactOnPedestal(GameObject artifact)
        {
            if (artifact != null)
                ArtifactsOnPedestals.Add(artifact);
        }

        public static void UnregisterArtifactOnPedestal(GameObject artifact)
        {
            if (artifact != null)
            {
                UpdateArtifactState(artifact, false, false);
                ArtifactsOnPedestals.Remove(artifact);
            }
        }

        public static void UpdateArtifactState(GameObject artifact, bool onPedestal, bool meetsRoomSize)
        {
            int id = artifact.GetInstanceID();
            if (!ArtifactStates.TryGetValue(id, out var state))
            {
                state = new ArtifactState();
                ArtifactStates[id] = state;
            }

            bool wasActive = state.IsActive;
            state.OnPedestal = onPedestal;
            state.MeetsRoomSize = meetsRoomSize;
            state.IsActive = onPedestal && meetsRoomSize;

            string internalName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
            // Try to get the in-game display name
            string displayName = artifact.GetComponent<KSelectable>()?.GetProperName()
                ?? artifact.GetComponent<KPrefabID>()?.PrefabTag.Name
                ?? internalName;

            if (wasActive != state.IsActive)
            {
                string stateText = state.IsActive ? "ACTIVE" : "INACTIVE";
                ModInit.CustomLog($"***** [ArtifactState] Artifact '{displayName}' (ID={artifact.name}) changed state: {stateText}");

                PopFXManager.Instance.SpawnFX(
                    state.IsActive ? PopFXManager.Instance.sprite_Plus : PopFXManager.Instance.sprite_Negative,
                    $"Artifact '{displayName}' is now {stateText}",
                    artifact.transform,
                    new Vector3(0, 0, 0),
                    2f,
                    false
                );

                AdjustAllDupesAttributes(internalName, state.IsActive);
            }
        }

        private static void LoadArtifactAttributeMap()
        {
            artifactAttributeMap = new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var arr = JArray.Parse(File.ReadAllText(ModInit.ArtifactPowersConfigPath));
                foreach (var obj in arr)
                {
                    var artifactId = (string)obj["ArtifactId"];
                    if (artifactId != null && obj["Attributes"] is JObject attributes)
                    {
                        var dict = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
                        foreach (var prop in attributes.Properties())
                        {
                            if (float.TryParse(prop.Value.ToString(), out float val))
                                dict[prop.Name] = val;
                        }
                        artifactAttributeMap[artifactId] = dict;
                    }
                }
                ModInit.CustomLog("[DEBUG] Loaded artifact attribute map from config.");
            }
            catch (Exception ex)
            {
                ModInit.CustomLog($"[ERROR] Failed to load artifact config: {ex}");
            }
        }

        public static void AdjustAllDupesAttributes(string artifactName, bool isActive)
        {
            if (artifactAttributeMap == null)
                LoadArtifactAttributeMap();

            if (!artifactAttributeMap.TryGetValue(artifactName, out var attributes) || attributes.Count == 0)
            {
                ModInit.CustomLog($"[DEBUG] No attribute adjustments found for artifact '{artifactName}'. Skipping adjustment.");
                return;
            }

            ModInit.CustomLog($"[DEBUG] Found {attributes.Count} attribute adjustment(s) for artifact '{artifactName}' (isActive={isActive}).");

            float sign = isActive ? 1f : -1f;
            foreach (var minion in UnityEngine.Object.FindObjectsOfType<MinionIdentity>())
            {
                if (minion.GetComponent<MinionModifiers>() is MinionModifiers minionModifiers)
                {
                    foreach (var kvp in attributes)
                    {
                        string attrName = kvp.Key;
                        float value = kvp.Value * sign;

                        // Try to match by Id or Name (case-insensitive)
                        Klei.AI.Attribute attribute = Db.Get().Attributes.resources
                            .FirstOrDefault(a => string.Equals(a.Id, attrName, StringComparison.OrdinalIgnoreCase) ||
                                                 string.Equals(a.Name, attrName, StringComparison.OrdinalIgnoreCase));

                        if (attribute == null)
                        {
                            ModInit.CustomLog($"[DEBUG] Attribute '{attrName}' not found by Id or Name in Db for {minion.name}.");
                            continue;
                        }

                        var attrInstance = minionModifiers.attributes?.Get(attribute);
                        if (attrInstance != null)
                        {
                            var modifier = new AttributeModifier(attribute.Id, value, $"Artifact Effect: {artifactName}");
                            attrInstance.Add(modifier);
                            ModInit.CustomLog($"[DEBUG] {minion.name}: modified '{attribute.Id}' by {value} from '{artifactName}'.");
                        }
                        else
                        {
                            ModInit.CustomLog($"[DEBUG] {minion.name} does not have attribute '{attribute.Id}'.");
                        }
                    }
                }
                else
                {
                    ModInit.CustomLog($"[DEBUG] {minion.name} has no MinionModifiers.");
                }
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

        public static void PollAllArtifacts()
        {
            foreach (var artifact in ArtifactsOnPedestals)
            {
                if (artifact == null)
                    continue;
                int cell = Grid.PosToCell(artifact.transform.position);
                int roomSize = -1;
                if (Game.Instance != null && Game.Instance.roomProber != null)
                {
                    var cavity = Game.Instance.roomProber.GetCavityForCell(cell);
                    var room = cavity?.room;
                    if (room != null && room.cavity != null)
                        roomSize = room.cavity.numCells;
                }
                bool meetsRoomSize = roomSize > 0 && roomSize < 32;
                UpdateArtifactState(artifact, true, meetsRoomSize);
            }
        }
    }

    public class ArtifactStatePoller : MonoBehaviour
    {
        private int tickCounter = 0;
        private const int PollInterval = 20;

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
            ModInit.OnLoad();

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
        public static void Postfix(ItemPedestal __instance)
        {
            var receptacleField = typeof(ItemPedestal).GetField("receptacle", BindingFlags.NonPublic | BindingFlags.Instance);
            var receptacle = receptacleField?.GetValue(__instance) as SingleEntityReceptacle;
            var occupant = receptacle?.Occupant;

            foreach (var artifact in ArtifactStateTracker.ArtifactsOnPedestals.ToArray())
            {
                if (artifact == null) continue;
                bool stillOnPedestal = false;
                foreach (var pedestal in GameObject.FindObjectsOfType<ItemPedestal>())
                {
                    var recField = typeof(ItemPedestal).GetField("receptacle", BindingFlags.NonPublic | BindingFlags.Instance);
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

            if (occupant != null)
                ArtifactStateTracker.RegisterArtifactOnPedestal(occupant);
        }
    }

    [HarmonyPatch(typeof(Game), "OnPrefabInit")]
    public static class Game_OnPrefabInit_Patch
    {
        public static void Postfix()
        {
            if (Game.Instance != null && Game.Instance.gameObject.GetComponent<ArtifactStatePoller>() == null)
            {
                Game.Instance.gameObject.AddComponent<ArtifactStatePoller>();
            }
        }
    }
}