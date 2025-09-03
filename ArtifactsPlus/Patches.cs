using HarmonyLib;
using Klei.AI;
using KMod;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ArtifactsPlus.ArtifactStateTracker;

namespace ArtifactsPlus
{
    public static class Patches
    {
        public static HLib.Logger logger = new HLib.Logger("ArtifactsPlus");

        // Add this static flag
        public static bool IsRoomsExpandedPresent = false;

        public static void OnLoad()
        {
        }

        public static void LogArtifactShortCircuitIssue(GameObject artifact, ArtifactStateTracker.ArtifactCriteriaResult criteria)
        {
            if (artifact == null || string.IsNullOrEmpty(criteria.ShortCircuited))
                return;

            string artifactId = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Artifact";
            int instanceId = artifact.GetInstanceID();
            string shortCircuitIssue = criteria.ShortCircuited;

            logger.LogDebug($"[ArtifactsPlus] Artifact '{artifactId}' ({instanceId}) failed due to: {shortCircuitIssue}.");
        }

        [HarmonyPatch(typeof(Game), "OnPrefabInit")]
        public class Game_OnPrefabInit_Patch
        {
            public static void Postfix()
            {
                if (Game.Instance != null && Game.Instance.gameObject.GetComponent<ArtifactStatePoller>() == null)
                {
                    var poller = Game.Instance.gameObject.AddComponent<ArtifactStatePoller>();
                }
            }
        }

        [HarmonyPatch(typeof(Game), "OnSpawn")]
        public static class Game_OnSpawn_Patch
        {
            public static void Postfix(Game __instance)
            {
                // This is the best place to do your "game is ready" logic
                //ArtifactStateTracker.BuildGlobalAllArtifacts();
                //ArtifactStateTracker.InitializeAllMinions();
                // Optionally, force a poll/update here
                // ArtifactStatePoller.ForcePollNow(); // if you have such a method
                Patches.logger.LogDebug("[ArtifactsPlus] Game is fully loaded and ready.");
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
        public static class Localization_Initialize_Patch
        {
            public static void Postfix()
            {
            }
        }

        [HarmonyPatch(typeof(AssignmentManager), "MinionMigration")]
        public static class AssignmentManager_MinionMigration_Patch
        {
            public static void Postfix(object data)
            {
                var migrationEventArgs = data as MinionMigrationEventArgs;
                if (migrationEventArgs != null)
                {
                    var minionGo = migrationEventArgs.minionId?.gameObject;
                    if (minionGo == null) return;

                    int oldWorldId = migrationEventArgs.prevWorldId;
                    int newWorldId = migrationEventArgs.targetWorldId;

                    string minionName = minionGo.GetComponent<KSelectable>()?.GetProperName() ?? "Unknown Minion";
                    string oldWorldName = ClusterManager.Instance.GetWorld(oldWorldId)?.GetProperName() ?? $"World_{oldWorldId}";
                    string newWorldName = ClusterManager.Instance.GetWorld(newWorldId)?.GetProperName() ?? $"World_{newWorldId}";

                    logger.LogDebug($"[ArtifactsPlus] Minion '{minionName}' migrated from '{oldWorldName}' to '{newWorldName}'.");

                    var prefabId = minionGo.GetComponent<KPrefabID>();
                    if (prefabId)
                    {
                        prefabId.AddTag($"worldChanged-{oldWorldId}"); // Add the "worldChanged" tag with the oldWorldId to indicate a world change
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BionicMinionConfig), "OnSpawn")]
        public static class BionicMinionConfig_OnSpawn_Patch
        {
            public static void Postfix(GameObject go)
            {
                HandleMinionSpawn(go, "BionicMinionConfig");
            }
        }

        [HarmonyPatch(typeof(MinionConfig), "OnSpawn")]
        public static class MinionConfig_OnSpawn_Patch
        {
            public static void Postfix(GameObject go)
            {
                HandleMinionSpawn(go, "MinionConfig");
            }
        }

        private static void HandleMinionSpawn(GameObject go, string configType)
        {
            if (go == null)
                return;

            ArtifactStateTracker.InitializeAllMinions();

            string minionName = go.GetComponent<KSelectable>()?.GetProperName() ?? "Unknown Minion";
            Patches.logger.LogDebug($"[{configType}] Minion '{minionName}' spawned.");

            var prefabId = go.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                prefabId.AddTag("worldChanged"); // Add the "worldChanged" tag to indicate the minion was freshly spawned
            }
        }

        [HarmonyPatch(typeof(SpaceArtifact), "OnSpawn")]
        public static class SpaceArtifact_OnSpawn_Patch
        {
            public static void Postfix(SpaceArtifact __instance)
            {
                GameObject artifact = __instance.gameObject;
                var type = __instance.artifactType;
                int instanceId = artifact.GetInstanceID();

                if (type == ArtifactType.Terrestrial)
                    Patches.logger.LogDebug($"[ArtifactsPlus] Terrestrial artifact spawned: {artifact.name} (instance id: {instanceId})");
                else if (type == ArtifactType.Space)
                    Patches.logger.LogDebug($"[ArtifactsPlus] Space artifact spawned: {artifact.name} (instance id: {instanceId})");
                else
                    Patches.logger.LogDebug($"[ArtifactsPlus] Artifact (type: {type}) spawned: {artifact.name} (instance id: {instanceId})");
                BuildGlobalAllArtifacts();
            }
        }
    }

    public class ArtifactState
    {
        public bool OnPedestal;
        public bool IsActive;
        public bool IsAnalyzed;
        public bool StateChanged; // New field to track if the state has changed
    }

    public class ArtifactConfig
    {
        public int Neighbors;
        public string Scope;
        public Dictionary<string, float> Attributes;
        /// <summary>
        /// Dictionary of status effect IDs and their durations to apply to minions.
        /// </summary>
        public Dictionary<string, float> Effects;

        public ArtifactConfig(string globalScope, int globalNeighbors = 1)
        {
            Neighbors = globalNeighbors;
            Scope = globalScope;
            Attributes = new Dictionary<string, float>();
            Effects = new Dictionary<string, float>();
        }
    }

    public static class ArtifactStateTracker
    {
        internal static readonly Dictionary<int, ArtifactState> ArtifactStates = new Dictionary<int, ArtifactState>();
        internal static readonly HashSet<GameObject> ArtifactsOnPedestals = new HashSet<GameObject>();

        private static readonly string GlowChildName = "ArtifactGlowFX";

        private static Dictionary<string, ArtifactConfig> artifactConfigMap;

        private static GameObject[] GlobalAllArtifacts; // Renamed from allArtifacts
        private static GameObject[] allMinions; // Cache for all minions

        public struct ArtifactCriteriaResult
        {
            public string Scope;
            public bool isFree;
            public bool IsAnalyzed;
            public int ArtifactCountInRoom;
            public bool NeighborsOk;
            public bool OnPedestal; // Adjusted case
            public string ShortCircuited;
            public bool MeetsAll;

        }

        public static ArtifactCriteriaResult EvaluateArtifactCriteria(GameObject artifact, ArtifactConfig config)
        {
            var result = new ArtifactCriteriaResult
            {
                Scope = config.Scope
            };

            result.MeetsAll = false;

            if (artifact == null)
            {
                return result;
            }

            if (!isInActivatingRoom(artifact))
            {
                result.ShortCircuited = "not in ActivatingRoom";
                return result;
            }

            if (!CheckOnPedestal(artifact, ref result)) return result;
            if (!CheckAnalyzed(artifact, ref result)) return result;
            if (!CheckFree(artifact, ref result)) return result;
            if (!CheckNeighbors(artifact, config, ref result)) return result;

            result.MeetsAll = true;
            return result;
        }

        public static bool isInActivatingRoom(GameObject artifact)
        {
            if (artifact == null || artifact.transform == null)
                return false;
            int cell = Grid.PosToCell(artifact.transform.position);
            var cavity = Game.Instance?.roomProber?.GetCavityForCell(cell);
            var room = cavity?.room;
            if (room != null && room.roomType != null)
            {
                if (room.roomType.Id == "DecorRoom" ||
                    (room.roomType.Id != null && room.roomType.Id.IndexOf("Museum", StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;
            }
            return false;
        }

        public static bool isInDecorRoom(GameObject artifact)
        {
            if (artifact == null || artifact.transform == null)
                return false;

            int cell = Grid.PosToCell(artifact.transform.position);
            var cavity = Game.Instance?.roomProber?.GetCavityForCell(cell);
            var room = cavity?.room;
            if (room != null && room.roomType != null && room.roomType.Id == "DecorRoom")
                return true;

            return false;
        }

        private static bool CheckOnPedestal(GameObject artifact, ref ArtifactCriteriaResult result)
        {
            result.OnPedestal = ArtifactStateTracker.ArtifactsOnPedestals.Contains(artifact);
            result.ShortCircuited = result.OnPedestal ? null : "OnPedestal";
            return result.OnPedestal;
        }

        private static bool CheckAnalyzed(GameObject artifact, ref ArtifactCriteriaResult result)
        {
            var artifactId = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name;
            result.IsAnalyzed = !string.IsNullOrEmpty(artifactId) && ArtifactSelector.Instance?.GetAnalyzedArtifactIDs().Contains(artifactId) == true;
            result.ShortCircuited = result.IsAnalyzed ? null : "isAnalyzed";
            return result.IsAnalyzed;
        }

        private static bool CheckFree(GameObject artifact, ref ArtifactCriteriaResult result) // not entombed
        {
            int cell = Grid.PosToCell(artifact.transform.position);
            result.isFree = Grid.Element[cell].id != SimHashes.Unobtanium;
            result.ShortCircuited = result.isFree ? null : "isFree";
            return result.isFree;
        }
        private static bool CheckNeighbors(GameObject artifact, ArtifactConfig config, ref ArtifactCriteriaResult result)
        {
            result.NeighborsOk = false;
            int cell = Grid.PosToCell(artifact.transform.position);
            var cavity = Game.Instance?.roomProber?.GetCavityForCell(cell);
            var room = cavity?.room;

            if (room != null && room.cavity != null)
            {
                result.ArtifactCountInRoom = CountArtifactsOnPedestalsInRoom(room);
                result.NeighborsOk = result.ArtifactCountInRoom <= config.Neighbors;
            }
            result.ShortCircuited = result.NeighborsOk ? null : "NeighborsOk";
            return result.NeighborsOk;
        }


        public static void InitializeAllMinions()
        {
            allMinions = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && (kp.HasTag("Minion") || kp.HasTag("BionicMinion")))
                .Select(kp => kp.gameObject)
                .ToArray();
        }

        private static List<GameObject> GetMinionsInSameWorld(GameObject artifact)
        {
            return ArtifactEffectTracker.GetMinionsInSameWorld(artifact);
        }

        private static bool ActiveAndInScope(GameObject minion, GameObject artifact)
        {
            return ArtifactEffectTracker.ActiveAndInScope(minion, artifact);
        }

        public static bool UpdateArtifactState(GameObject artifact)
        {
            if (artifact == null)
            {
                Patches.logger.LogDebug("[ERROR] UpdateArtifactState called with a null artifact.");
                return false;
            }

            int id = artifact.GetInstanceID();

            if (!ArtifactStates.TryGetValue(id, out var state)) // Initialize state if not already tracked  
            {
                state = new ArtifactState();
                state.IsActive = false;
                ArtifactStates[id] = state;
            }

            bool wasActive = state.IsActive;

            string internalName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
            var config = GetArtifactConfig(internalName);
            if (config == null)
            {
                Patches.logger.LogDebug($"[WARN] No config found for artifact '{internalName}'");
                return false;
            }

            var criteria = EvaluateArtifactCriteria(artifact, config);

            state.IsActive = criteria.MeetsAll;

            state.StateChanged = wasActive != state.IsActive; // Determine if the state changed  

            if (state.StateChanged)
            {
                string stateText = state.IsActive ? "ACTIVE" : "INACTIVE";

                Patches.logger.LogDebug($"[ArtifactsPlus] Artifact '{internalName}' ({id}) {stateText}");

                PopFXManager.Instance.SpawnFX(
                    state.IsActive ? PopFXManager.Instance.sprite_Plus : PopFXManager.Instance.sprite_Negative,
                    $"Artifact '{internalName}' {stateText}",
                    artifact.transform,
                    new Vector3(0, 0, 0),
                    2f,
                    false
                );
                ApplyGlowEffect(artifact, state.IsActive);
            }

            return state.StateChanged; // Return whether the state changed  
        }

        public static bool TryGetArtifactAttributes(string artifactId, out Dictionary<string, float> attributes)
        {
            if (artifactConfigMap != null && artifactConfigMap.TryGetValue(artifactId, out var config))
            {
                attributes = config.Attributes;
                return true;
            }
            attributes = null;
            return false;
        }

        public static void ApplyGlowEffect(GameObject artifact, bool enable)
        {
            if (artifact == null)
            {
                return;
            }

            var parent = artifact.transform;
            var glowChild = parent.Find(GlowChildName)?.gameObject;

            if (enable)
            {
                if (glowChild == null)
                {
                    glowChild = new GameObject(GlowChildName);
                    glowChild.transform.SetParent(parent, false);

                    var light2D = glowChild.AddComponent<Light2D>();
                    light2D.overlayColour = new Color(1f, 1f, 1f, 0.2f);
                    light2D.Color = Color.yellow;
                    light2D.Range = 4f;
                    light2D.Lux = 1800;
                    light2D.Offset = new Vector2(0, 0.5f);
                    light2D.shape = LightShape.Circle;
                    light2D.drawOverlay = true;
                }
                else
                {
                    var light2D = glowChild.GetComponent<Light2D>();
                    if (light2D != null)
                    {
                        light2D.enabled = true;
                    }
                }
            }
            else if (glowChild != null)
            {
                GameObject.Destroy(glowChild);
            }
        }

        public static void LoadArtifactConfig()
        {
            Patches.logger.LogDebug("[ArtifactsPlus] LoadArtifactConfig: loading embedded resource first, then user overrides if present.");

            artifactConfigMap = new Dictionary<string, ArtifactConfig>(StringComparer.OrdinalIgnoreCase);

            int globalNeighbors = 1;
            string globalScope = "InWorld";

            // Helper to parse and merge config
            int ParseAndMergeConfig(JObject configJson, bool overwriteGlobals)
            {
                int artifactsRead = 0;
                if (configJson == null)
                    return 0;

                if (overwriteGlobals)
                {
                    if (configJson["Neighbors"] != null)
                    {
                        globalNeighbors = (int)configJson["Neighbors"];

                        // After loading both embedded and user configs, set all artifact Neighbors to globalNeighbors
                        if (artifactConfigMap != null)
                            foreach (var config in artifactConfigMap.Values)
                                config.Neighbors = globalNeighbors;
                    }
                    if (configJson["Scope"] != null)
                        globalScope = (string)configJson["Scope"];

                }

                if (configJson["Artifacts"] is JArray arr)
                {
                    foreach (var obj in arr)
                    {
                        var artifactId = (string)obj["ArtifactId"];
                        if (artifactId == null) continue;

                        var artifactConfig = new ArtifactConfig(globalScope, globalNeighbors);

                        //if (obj["Neighbors"] != null) artifactConfig.Neighbors = (int)obj["Neighbors"];
                        //if (obj["Scope"] != null) artifactConfig.Scope = (string)obj["Scope"];

                        if (obj["Attributes"] is JObject attributes)
                        {
                            foreach (var prop in attributes.Properties())
                            {
                                if (float.TryParse(prop.Value.ToString(), out float val))
                                    artifactConfig.Attributes[prop.Name] = val;
                            }
                        }

                        if (obj["Effects"] is JObject effects)
                        {
                            foreach (var prop in effects.Properties())
                            {
                                float val = 0f;
                                if (prop.Value != null && float.TryParse(prop.Value.ToString(), out float parsed))
                                    val = parsed;
                                artifactConfig.Effects[prop.Name] = val;
                            }
                        }

                        // Overwrite or add
                        artifactConfigMap[artifactId] = artifactConfig;
                        artifactsRead++;
                    }
                }
                else
                {
                    Patches.logger.LogDebug("[ERROR] 'Artifacts' array missing or not an array in config JSON.");
                }
                return artifactsRead;
            }

            int embeddedArtifacts = 0;
            int userArtifacts = 0;

            // 1. Load embedded resource
            try
            {
                string resourceName = "ArtifactsPlus.ArtifactsConfig.json"; // Adjust if your namespace/folder differs
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Patches.logger.LogDebug($"[ArtifactsPlus] Embedded resource '{resourceName}' not found.");
                        return;
                    }
                    using (var reader = new StreamReader(stream))
                    {
                        string configText = reader.ReadToEnd();
                        JObject configJson = JObject.Parse(configText);
                        embeddedArtifacts = ParseAndMergeConfig(configJson, true);
                        Patches.logger.LogDebug($"[ArtifactsPlus] Embedded config loaded {embeddedArtifacts} artifacts.");
                    }
                }
            }
            catch (Exception ex)
            {
                Patches.logger.LogDebug($"[ArtifactsPlus] ERROR loading embedded config: {ex}");
                return;
            }

            // 2. Load user override file if present
            try
            {
                string modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string userConfigPath = Path.Combine(modDir, "User.ArtifactsConfig.json");
                if (File.Exists(userConfigPath))
                {
                    string userConfigText = File.ReadAllText(userConfigPath);
                    JObject userConfigJson = JObject.Parse(userConfigText);
                    userArtifacts = ParseAndMergeConfig(userConfigJson, true);
                    Patches.logger.LogDebug($"[ArtifactsPlus] Loaded {userArtifacts} user artifact overrides from User.ArtifactsConfig.json.");
                }
                else
                {
                    Patches.logger.LogDebug("[ArtifactsPlus] No User.ArtifactsConfig.json present.");
                }
            }
            catch (Exception ex)
            {
                Patches.logger.LogDebug($"[ArtifactsPlus] Error loading or parsing User.ArtifactsConfig.json: {ex}");
            }

            Debug.Log($"[ArtifactsPlus] Loaded {embeddedArtifacts} default artifact configs, {userArtifacts} user artifact overrides, total {artifactConfigMap.Count}.");
        }

        public static ArtifactConfig GetArtifactConfig(string artifactId)
        {
            if (artifactConfigMap != null && artifactConfigMap.TryGetValue(artifactId, out var config))
                return config;

            // Return null if not found, so callers can handle missing config appropriately
            return null;
        }

        private static int CountArtifactsOnPedestalsInRoom(Room room)
        {
            int count = 0;
            if (room != null && room.cavity != null)
            {
                foreach (var building in room.cavity.buildings)
                {
                    if (building == null)
                    {
                        continue;
                    }

                    var pedestal = building.GetComponent<ItemPedestal>();
                    if (pedestal == null)
                    {
                        continue;
                    }

                    var receptacleField = typeof(ItemPedestal).GetField("receptacle", BindingFlags.NonPublic | BindingFlags.Instance);
                    var receptacle = receptacleField?.GetValue(pedestal) as SingleEntityReceptacle;
                    var occupant = receptacle?.Occupant;

                    if (occupant == null)
                    {
                        continue;
                    }

                    var prefabId = occupant.GetComponent<KPrefabID>();
                    if (prefabId == null)
                    {
                        continue;
                    }

                    count++;
                }
            }
            return count;
        }

        public static void BuildGlobalAllArtifacts()
        {
            GlobalAllArtifacts = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && kp.HasTag("Artifact") && kp.gameObject != null)
                .Select(kp => kp.gameObject)
                .Where(artifact => artifact != null) // Ensure null artifacts are excluded
                .ToArray();
            Patches.logger.LogDebug($"GlobalAllArtifacts length: {GlobalAllArtifacts?.Length ?? 0}");
        }

        public static GameObject[] GetAllArtifacts()
        {
            if (GlobalAllArtifacts == null)
                BuildGlobalAllArtifacts();
            return GlobalAllArtifacts;
        }

        public static GameObject[] GetAllMinions()
        {
            if (allMinions == null)
            {
                InitializeAllMinions();
            }
            return allMinions;
        }

        public static bool PollAllArtifacts(Dictionary<int, List<GameObject>> minionsPerWorld)
        {
            bool anyStateChanged = false;

            foreach (var artifact in GetAllArtifacts())
            {
                if (artifact == null) continue;

                int cell = Grid.PosToCell(artifact.transform.position);
                int worldId = Grid.WorldIdx[cell];

                if (!minionsPerWorld.TryGetValue(worldId, out var minions) || minions.Count == 0)
                {
                    continue;
                }

                if (UpdateArtifactState(artifact))
                {
                    anyStateChanged = true;
                }
            }

            return anyStateChanged;
        }

        public static void UpdateMinions()
        {
            foreach (var minion in GetAllMinions())
            {
                UpdateMinion(minion, GetAllArtifacts());
            }
        }

        public static void UpdateMinions(IEnumerable<GameObject> allArtifacts)
        {
            foreach (var minion in GetAllMinions())
            {
                UpdateMinion(minion, allArtifacts);
            }
        }

        public static void UpdateMinion(GameObject minion, IEnumerable<GameObject> allArtifacts)
        {
            if (minion == null) return;

            foreach (var artifact in allArtifacts)
            {
                if (artifact == null) continue;

                ArtifactEffectTracker.RemoveArtifactModifiersFromMinion(minion, artifact);

                int artifactId = artifact.GetInstanceID();
                if (ArtifactStates.TryGetValue(artifactId, out var state) && state.IsActive)
                {
                    var config = GetArtifactConfig(artifact.GetComponent<KPrefabID>()?.PrefabTag.Name);
                    if (config == null) continue;

                    bool inScope = GetMinionsInSameWorld(artifact).Contains(minion);

                    if (inScope)
                    {
                        ArtifactEffectTracker.ApplyArtifactModifiersToMinion(minion, artifact);
                    }
                }
            }
        }

        public static void RegisterArtifactOnPedestal(GameObject artifact)
        {
            if (artifact != null && !ArtifactsOnPedestals.Contains(artifact))
            {
                ArtifactsOnPedestals.Add(artifact);
                Patches.logger.LogDebug($"[ArtifactsPlus] Artifact '{artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Artifact"}' registered on pedestal.");
            }
        }

        public static void UnregisterArtifactOnPedestal(GameObject artifact)
        {
            if (artifact != null && ArtifactsOnPedestals.Contains(artifact))
            {
                ArtifactsOnPedestals.Remove(artifact);
                Patches.logger.LogDebug($"[ArtifactsPlus] Artifact '{artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Artifact"}' unregistered from pedestal.");
            }
        }

        public static void PerformIntegrityCheck()
        {
            //Patches.logger.LogDebug("[ArtifactStateTracker] Performing integrity check on artifacts.");

            // Store the initial count of artifacts
            if (GlobalAllArtifacts == null)
            {
                Patches.logger.LogDebug("[ArtifactStateTracker] GlobalAllArtifacts null.");
                return;
            }

            int initialCount = GlobalAllArtifacts.Length;

            // Remove null artifacts
            GlobalAllArtifacts = ArtifactStateTracker.GetAllArtifacts();
            int cleanedCount = GlobalAllArtifacts?.Length ?? 0;

            // Log counts only if they are different
            if (initialCount != cleanedCount)
            {
                Patches.logger.LogDebug($"[ArtifactStateTracker] Artifact count before cleanup: {initialCount}");
                Patches.logger.LogDebug($"[ArtifactStateTracker] Artifact count after cleanup: {cleanedCount}");
            }

            foreach (var artifact in GlobalAllArtifacts)
            {
                if (artifact == null)
                {
                    Patches.logger.LogDebug("[ArtifactStateTracker] Skipping null artifact.");
                    continue;
                }

                // Validate artifact state
                int artifactId = artifact.GetInstanceID();
                if (!ArtifactStateTracker.ArtifactStates.TryGetValue(artifactId, out var state))
                {
                    state = new ArtifactState();
                    ArtifactStateTracker.ArtifactStates[artifactId] = state;
                    Patches.logger.LogDebug($"[ArtifactStateTracker] Initialized state for artifact ID: {artifactId}");
                }

                // Validate artifact components
                var prefabId = artifact.GetComponent<KPrefabID>();
                if (prefabId == null)
                {
                    Patches.logger.LogDebug($"[ArtifactStateTracker] Artifact ID: {artifactId} is missing KPrefabID. Skipping.");
                    continue;
                }

                // Validate artifact configuration
                string artifactName = prefabId.PrefabTag.Name ?? "Unknown Artifact";
                var config = ArtifactStateTracker.GetArtifactConfig(artifactName);
                if (config == null)
                {
                    Patches.logger.LogDebug($"[ArtifactState Tracker] No configuration found for artifact '{artifactName}' (ID: {artifactId}).");
                }
            }
        }
    }

    public class ArtifactStatePoller : MonoBehaviour
    {
        private int tickCounter = 0;
        private static int pollInterval; // Cache poll interval for the current save/loading  

        void Awake()
        {
        }

        void Start()
        {
            var options = ArtifactsPlusOptions.Instance;
            pollInterval = options.PollingIntervalSeconds * 60;
            Patches.logger.LogDebug($"[ArtifactStatePoller] Awake poll interval {pollInterval}");
        }

        void Update()
        {
            tickCounter++;

            if (tickCounter >= pollInterval)  // ticks
            {
                tickCounter = 0;
                var allArtifacts = ArtifactStateTracker.GetAllArtifacts();

                PerformIntegrityCheck();

                var minionsPerWorld = new Dictionary<int, List<GameObject>>();
                var allMinions = ArtifactStateTracker.GetAllMinions();

                foreach (var minion in allMinions)
                {
                    if (minion == null || minion.transform == null)
                    {
                        Patches.logger.LogDebug("[ArtifactsPlus] Null minion or minion.transform encountered during Update.");
                        continue;
                    }

                    // Update the minion's worldId here if needed
                    // Example: MinionWorldTracker.UpdateMinionWorldId(minion);
                    int cell = Grid.PosToCell(minion.transform.position);
                    int worldId = Grid.WorldIdx[cell];

                    if (!minionsPerWorld.ContainsKey(worldId))
                    {
                        minionsPerWorld[worldId] = new List<GameObject>();
                    }

                    minionsPerWorld[worldId].Add(minion);
                }

                bool anyStateChanged = ArtifactStateTracker.PollAllArtifacts(minionsPerWorld);

                foreach (var artifact in allArtifacts)
                {
                    if (artifact == null || artifact.transform == null)
                    {
                        Patches.logger.LogDebug("[WARN] Null artifact or artifact.transform encountered during Update.");
                        continue;
                    }

                    var artifactTransform = artifact.transform;
                    var prefabId = artifact.GetComponent<KPrefabID>();
                    if (prefabId == null)
                    {
                        Patches.logger.LogDebug("[WARN] Artifact is missing KPrefabID component.");
                        continue;
                    }

                    int artifactId = artifact.GetInstanceID();
                    var state = ArtifactStateTracker.ArtifactStates.TryGetValue(artifactId, out var artifactState) ? artifactState : null;
                    bool active = state?.IsActive ?? false;
                    bool stateChanged = state?.StateChanged ?? false;

                    if (stateChanged)
                    {
                        int cell = Grid.PosToCell(artifactTransform.position);
                        int worldId = Grid.WorldIdx[cell];
                        if (!minionsPerWorld.ContainsKey(worldId))
                            continue;

                        if (active)
                        {
                            foreach (var minion in minionsPerWorld[worldId])
                            {
                                if (minion == null)
                                {
                                    Patches.logger.LogDebug("[ArtifactsPlus] Null minion encountered while applying modifiers.");
                                    continue;
                                }
                                Patches.logger.LogDebug($"[ArtifactsPlus] Applying artifact modifiers to minion '{minion.GetComponent<KSelectable>()?.GetProperName() ?? "Unknown Minion"}' for artifact '{prefabId.PrefabTag.Name ?? "Unknown Artifact"}'.");
                                ArtifactEffectTracker.ApplyArtifactModifiersToMinion(minion, artifact);
                            }
                        }
                        else
                        {
                            foreach (var minion in minionsPerWorld[worldId])
                            {
                                if (minion == null)
                                {
                                    Patches.logger.LogDebug("[ArtifactsPlus] Null minion encountered while removing modifiers.");
                                    continue;
                                }
                                Patches.logger.LogDebug($"[ArtifactsPlus] Removing artifact modifiers to minion '{minion.GetComponent<KSelectable>()?.GetProperName() ?? "Unknown Minion"}' for artifact '{prefabId.PrefabTag.Name ?? "Unknown Artifact"}'.");
                                ArtifactEffectTracker.RemoveArtifactModifiersFromMinion(minion, artifact);
                            }
                        }
                    }
                }

                //logger.LogDebug($"[ArtifactsPlus] Updating Minions affected by state-changed Artifacts.");

                foreach (var artifact in allArtifacts)
                {
                    if (artifact == null || artifact.transform == null)
                    {
                        Patches.logger.LogDebug("[WARN] Null artifact or artifact.transform encountered during Update.");
                        continue;
                    }

                    var artifactTransform = artifact.transform;
                    var prefabId = artifact.GetComponent<KPrefabID>();
                    if (prefabId == null)
                    {
                        Patches.logger.LogDebug("[WARN] Artifact is missing KPrefabID component.");
                        continue;
                    }

                    int artifactId = artifact.GetInstanceID();
                    var state = ArtifactStateTracker.ArtifactStates.TryGetValue(artifactId, out var artifactState) ? artifactState : null;
                    bool active = state?.IsActive ?? false;
                    bool stateChanged = state?.StateChanged ?? false;

                    if (stateChanged)
                    {
                        int cell = Grid.PosToCell(artifactTransform.position);
                        int worldId = Grid.WorldIdx[cell];
                        if (!minionsPerWorld.ContainsKey(worldId))
                            continue;

                        if (active)
                        {
                            foreach (var minion in minionsPerWorld[worldId])
                            {
                                if (minion == null)
                                {
                                    Patches.logger.LogDebug("[ArtifactsPlus] Null minion encountered while applying modifiers.");
                                    continue;
                                }
                                Patches.logger.LogDebug($"[ArtifactsPlus] Applying artifact modifiers to minion '{minion.GetComponent<KSelectable>()?.GetProperName() ?? "Unknown Minion"}' for artifact '{prefabId.PrefabTag.Name ?? "Unknown Artifact"}'.");
                                ArtifactEffectTracker.ApplyArtifactModifiersToMinion(minion, artifact);
                            }
                        }
                        else
                        {
                            foreach (var minion in minionsPerWorld[worldId])
                            {
                                if (minion == null)
                                {
                                    Patches.logger.LogDebug("[ArtifactsPlus] Null minion encountered while removing modifiers.");
                                    continue;
                                }
                                Patches.logger.LogDebug($"[ArtifactsPlus] Removing artifact modifiers to minion '{minion.GetComponent<KSelectable>()?.GetProperName() ?? "Unknown Minion"}' for artifact '{prefabId.PrefabTag.Name ?? "Unknown Artifact"}'.");
                                ArtifactEffectTracker.RemoveArtifactModifiersFromMinion(minion, artifact);
                            }
                        }
                    }
                }


                var minionsWithWorldChangedFlag = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                    .Where(kp => kp != null && kp.HasTag("Minion") && kp.Tags != null && kp.Tags.Any(tag => tag != null && tag.Name != null && tag.Name.StartsWith("worldChanged")))
                    .Select(kp => kp.gameObject)
                    .Where(go => go != null)
                    .ToList();

                if (minionsWithWorldChangedFlag.Count > 1)
                    Patches.logger.LogDebug($"[ArtifactsPlus] Updating {minionsWithWorldChangedFlag.Count} minion(s) with worldChanged flag.");

                foreach (var minion in minionsWithWorldChangedFlag)
                {
                    if (minion == null)
                        continue;

                    string minionName = minion.GetComponent<KSelectable>()?.GetProperName() ?? "Unknown Minion";
                    var prefabId = minion.GetComponent<KPrefabID>();
                    if (prefabId == null || prefabId.Tags == null)
                        continue;

                    // get oldworld name
                    var worldChangedTag = prefabId.Tags.FirstOrDefault(tag => tag != null && tag.Name != null && tag.Name.StartsWith("worldChanged"));
                    if (worldChangedTag == null || string.IsNullOrEmpty(worldChangedTag.Name))
                        continue;

                    var worldChangedTagName = worldChangedTag.Name;
                    var parts = worldChangedTagName.Split('-');

                    var oldWorldId = -1;
                    if (parts.Length == 2 && int.TryParse(parts[1], out var parsedWorldId))
                    {
                        oldWorldId = parsedWorldId;
                    }
                    prefabId.RemoveTag(worldChangedTag);

                    // get new world id
                    if (minion.transform == null)
                        continue;
                    int cell = Grid.PosToCell(minion.transform.position);
                    int newWorldId = Grid.WorldIdx[cell];

                    Patches.logger.LogDebug($"[ArtifactsPlus] Processing world change for minion: {minionName} (OldWorldId: {oldWorldId}, NewWorldId: {newWorldId})");

                    if (oldWorldId >= 0)
                    {
                        if (oldWorldId == newWorldId) // rocket launch is weird, doesn't keep oldworld
                        {
                            Patches.logger.LogDebug($"[ArtifactsPlus] Rocket world change detected for minion '{minionName}");
                            Patches.logger.LogDebug($"[ArtifactsPlus] (Rocket) RemoveAllArtifactModifiersFromMinion '{minionName}'.");
                            ArtifactEffectTracker.RemoveArtifactModifiersFromMinion(minion);
                        }
                        else
                        {
                            var artifactsInPreviousWorld = ArtifactStateTracker.GetAllArtifacts()
                                 .Where(artifact => artifact != null && artifact.transform != null && Grid.WorldIdx[Grid.PosToCell(artifact.transform.position)] == oldWorldId)
                                 .ToList();

                            foreach (var artifact in artifactsInPreviousWorld)
                            {
                                string artifactName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Artifact";
                                Patches.logger.LogDebug($"[ArtifactsPlus] RemoveArtifactModifiersFromMinion '{minionName}' Artifact '{artifactName}'.");
                                ArtifactEffectTracker.RemoveArtifactModifiersFromMinion(minion, artifact);
                            }
                        }
                    }

                    int ncell = Grid.PosToCell(minion.transform.position);
                    int worldId = Grid.WorldIdx[ncell];

                    var artifactsInNewWorld = ArtifactStateTracker.GetAllArtifacts()
                        .Where(artifact =>
                            artifact != null &&
                            artifact.transform != null &&
                            Grid.WorldIdx[Grid.PosToCell(artifact.transform.position)] == worldId)
                        .ToList();

                    foreach (var artifact in artifactsInNewWorld)
                    {
                        if (artifact == null)
                            continue;
                        int artifactId = artifact.GetInstanceID();
                        string artifactName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Artifact";
                        if (ArtifactStateTracker.ArtifactStates.TryGetValue(artifactId, out var state) && state.IsActive)
                        {
                            Patches.logger.LogDebug($"[ArtifactsPlus] ApplyArtifactModifiersToMinion '{minionName}' Artifact '{artifactName}' (isActive={state.IsActive}).");
                            ArtifactEffectTracker.ApplyArtifactModifiersToMinion(minion, artifact);
                        }
                        else
                        {
                            Patches.logger.LogDebug($"[ArtifactsPlus] Skipping ApplyArtifactModifiersToMinion for '{artifactName}' (isActive={state?.IsActive ?? false}).");
                        }
                    }
                }
            }
        }
    }

    public class Mod : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);

            new POptions().RegisterOptions(this, typeof(ArtifactsPlusOptions)); // Register the options

            var options = ArtifactsPlusOptions.Instance;
            if (options != null)
            {
                string optionsJson = JsonConvert.SerializeObject(options, Formatting.Indented);
            }
            else
            {
                Debug.Log("[ArtifactsPlus] Options instance is null. Ensure ArtifactsPlusOptions is properly initialized.");
            }

            PUtil.InitLibrary();

            Patches.OnLoad();

            if (harmony == null)
            {
                Debug.Log("[ArtifactsPlus] Harmony instance is null.");
                return;
            }

            harmony.PatchAll();


            ArtifactStateTracker.LoadArtifactConfig(); // fallback to default

            Keybinder.KeyInputHandler.Register(new PPatchManager(harmony), HotKeys.All);
        }

        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<KMod.Mod> mods)
        {
            base.OnAllModsLoaded(harmony, mods);
            List<string> activeMods = new List<string>();
            foreach (KMod.Mod mod in mods)
            {
                if (mod.IsActive())
                {
                    activeMods.Add(mod.staticID);
                    // Set the flag if RoomsExpanded is present
                    if (mod.staticID == "pether-pg.RoomsExpanded")
                    {
                        Patches.IsRoomsExpandedPresent = true;
                        Patches.logger.LogDebug("[ArtifactsPlus] Detected pether-pg.RoomsExpanded mod. Setting IsRoomsExpandedPresent = true.");
                    }
                }
            }

            //CrossModManager.Initalize(activeMods);
        }
    }

    [HarmonyPatch(typeof(ItemPedestal), "OnOccupantChanged")]
    public static class ItemPedestal_OnOccupantChanged_Patch
    {
        public static void Postfix(ItemPedestal __instance, object data)
        {
            //logger.LogDebug("[ArtifactsPlus] OnOccupantChanged triggered.");

            var receptacleField = typeof(ItemPedestal).GetField("receptacle", BindingFlags.NonPublic | BindingFlags.Instance);
            if (receptacleField == null)
            {
                Patches.logger.LogDebug("[ArtifactsPlus] Failed to find 'receptacle' field in ItemPedestal.");
                return;
            }

            var receptacle = receptacleField.GetValue(__instance) as SingleEntityReceptacle;
            if (receptacle == null)
            {
                Patches.logger.LogDebug("[ArtifactsPlus] 'receptacle' is null.");
                return;
            }

            GameObject occupant = receptacle.Occupant;
            if (occupant == null)
            {
                //logger.LogDebug("[ArtifactsPlus] Occupant is null. This was likely a removal.");
                return; // This was a removal, ignore it, handled elsewhere
            }

            //logger.LogDebug($"[ArtifactsPlus] Occupant found: {occupant.name}");

            var prefabID = occupant.GetComponent<KPrefabID>();
            if (prefabID == null)
            {
                Patches.logger.LogDebug("[ArtifactsPlus] Occupant does not have a KPrefabID component.");
                return;
            }

            if (!prefabID.HasTag("Artifact"))
            {
                //logger.LogDebug($"[ArtifactsPlus] Occupant '{occupant.name}' does not have the 'Artifact' tag.");
                return;
            }

            string artifactName = prefabID.PrefabTag.Name ?? "Unknown Artifact";
            //logger.LogDebug($"[ArtifactsPlus] Registering artifact '{artifactName}' on pedestal.");

            ArtifactStateTracker.RegisterArtifactOnPedestal(occupant);
        }
    }

    [HarmonyPatch(typeof(SingleEntityReceptacle), "ClearOccupant")]
    public static class SingleEntityReceptacle_ClearOccupant_Patch
    {
        public static void Prefix(SingleEntityReceptacle __instance)
        {
            GameObject removedOccupant = __instance.Occupant;

            if (removedOccupant != null && removedOccupant.GetComponent<KPrefabID>()?.HasTag("Artifact") == true)
            {
                string occupantName = removedOccupant.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Artifact";
                //logger.LogDebug($"[ArtifactsPlus] Artifact '{occupantName}' is being removed from SingleEntityReceptacle.");

                // Call the unregister function to remove the artifact from the pedestal tracking
                ArtifactStateTracker.UnregisterArtifactOnPedestal(removedOccupant);
            }
        }
    }

    [HarmonyPatch(typeof(SaveLoader), "Save", new Type[] { typeof(string), typeof(bool), typeof(bool) })]
    public static class SaveLoader_Save_Patch
    {
        public static void Postfix(string filename, bool isAutoSave, bool updateSavePointer)
        {
        }
    }

    [HarmonyPatch(typeof(SaveLoader), "Load", new Type[] { typeof(string) })]
    public static class SaveLoader_Load_Patch
    {
        public static void Postfix(string filename)
        {
        }
    }
}


