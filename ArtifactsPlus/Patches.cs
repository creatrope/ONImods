using HarmonyLib;
using HLib;
using Klei.AI; // Add this import for Analyzable
using KMod;
using KSerialization; // Add this import for HotkeyListener
using Newtonsoft.Json; // Add this import for JsonObject and JsonObjectAttribute
using Newtonsoft.Json.Linq;
using PeterHan.PLib.Core; // Add this import for PUtil
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text; // <-- Add this for StringBuilder
using UnityEngine;
using Object = UnityEngine.Object; // Explicitly alias UnityEngine.Object to avoid ambiguity
using ArtifactsPlus; // Add this import for ArtifactEffectTracker

namespace ArtifactsPlus
{
    public static class Patches
    {
        public static HLib.HotkeyListener hotkeyListener;

        public static readonly CustomLogger Logger = new CustomLogger("ArtifactsPlus");

        public static string ArtifactPowersConfigPath
        {
            get
            {
                try
                {
                    var config = ArtifactsPlusConfig.Instance; // Access the configuration using PLib's SingletonOptions
                    Patches.Logger.Log($"[ArtifactsPlus] Config object: {JsonConvert.SerializeObject(config, Formatting.Indented)}");

                    var configFile = config.ArtifactConfigFile;
                    var fullPath = Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), configFile
                    );

                    Patches.Logger.Log($"[ArtifactsPlus] Using ArtifactConfig file: {fullPath}");
                    return fullPath;
                }
                catch (Exception ex)
                {
                    Patches.Logger.Log($"[ArtifactsPlus] Failed to retrieve ArtifactConfig file: {ex.Message}");
                    throw;
                }
            }
        }

        public static void OnLoad()
        {
            try
            {
                ArtifactStateTracker.LoadArtifactConfig();

                // Initialize and register hotkeys
                hotkeyListener = new HotkeyListener();

                hotkeyListener.RegisterHotkey("Ctrl+F12", () =>
                {
                    PrintActiveArtifactsWithWorlds();
                });

                var config = ArtifactsPlusConfig.Instance; // Access the configuration using PLib's SingletonOptions
                Patches.Logger.SetLoggingEnabled(config.EnableCustomLog);

                if (config.EnableCustomLog)
                {
                    Patches.Logger.Reset(); // Reset the log file at the start of the game
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArtifactsPlus] Failed to initialize OnLoad: {ex.Message}");
            }
        }

        public static void PrintActiveArtifactsWithWorlds()
        {
            var activeArtifacts = ArtifactStateTracker.ArtifactsOnPedestals
                .Where(artifact => artifact != null && ArtifactStateTracker.ArtifactStates.TryGetValue(artifact.GetInstanceID(), out var state) && state.IsActive);

            if (!activeArtifacts.Any())
            {
                Patches.Logger.Log("[ArtifactsPlus] No active artifacts found.");
                return;
            }

            Patches.Logger.Log("[ArtifactsPlus] Active Artifacts and their Worlds:");
            foreach (var artifact in activeArtifacts)
            {
                int cell = Grid.PosToCell(artifact.transform.position);
                int worldId = Grid.WorldIdx[cell];
                string worldName = ClusterManager.Instance.GetWorld(worldId)?.name ?? $"World_{worldId}";
                string artifactName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Artifact";

                Patches.Logger.Log($"- {artifactName} in {worldName}");
            }
        }

        public static void LogArtifactShortCircuitIssue(GameObject artifact, ArtifactStateTracker.ArtifactCriteriaResult criteria)
        {
            if (artifact == null || string.IsNullOrEmpty(criteria.ShortCircuited))
                return;

            string artifactId = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "Unknown Artifact";
            int instanceId = artifact.GetInstanceID();
            string shortCircuitIssue = criteria.ShortCircuited;

            Patches.Logger.Log($"[ArtifactsPlus] Artifact '{artifactId}' ({instanceId}) failed due to: {shortCircuitIssue}.");
        }

        [HarmonyPatch(typeof(Game), "OnPrefabInit")]
        public class Game_OnPrefabInit_Patch
        {
            public static void Postfix()
            {
                // Access the configuration using PLib's SingletonOptions
                var config = ArtifactsPlusConfig.Instance;

                if (config.EnableCustomLog)
                {
                    Patches.Logger.Log("[ArtifactsPlus] Custom logging is enabled.");
                }

                Patches.Logger.Log($"[ArtifactsPlus] Using ArtifactConfig file: {config.ArtifactConfigFile}");
                Patches.Logger.Log($"[ArtifactsPlus] Artifact polling interval: {config.ArtifactPollingInterval}");
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
        public static class Localization_Initialize_Patch
        {
            public static void Postfix()
            {
                // Example of localization logic
                Patches.Logger.Log("[ArtifactsPlus] Localization initialized.");
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
                    string oldWorldName = ClusterManager.Instance.GetWorld(oldWorldId)?.name ?? $"World_{oldWorldId}";
                    string newWorldName = ClusterManager.Instance.GetWorld(newWorldId)?.name ?? $"World_{newWorldId}";

                    Patches.Logger.Log($"[MinionMigration] Minion '{minionName}' migrated from '{oldWorldName}' to '{newWorldName}'.");
                }
            }
        }

        [HarmonyPatch(typeof(MinionConfig), "OnSpawn")]
        public static class MinionConfig_OnSpawn_Patch
        {
            public static void Postfix(GameObject go)
            {
                if (go == null)
                    return;

                string minionName = go.GetComponent<KSelectable>()?.GetProperName() ?? "Unknown Minion";
                Patches.Logger.Log($"[MinionConfig] Minion '{minionName}' spawned.");
            }
        }
    }

    public class ArtifactState
    {
        public bool OnPedestal;
        public bool MeetsRoomSize;
        public bool IsActive;
        public bool IsAnalyzed; // Renamed from IsUnanalyzed
    }

    public class ArtifactConfig
    {
        public int RoomSizeMin;
        public int RoomSizeMax;
        public int DecorMinimum;
        public int Neighbors;
        public string Scope;
        public Dictionary<string, float> Attributes;
        /// <summary>
        /// Dictionary of status effect IDs and their durations to apply to minions.
        /// </summary>
        public Dictionary<string, float> Effects;

        public ArtifactConfig(int globalMin, int globalMax, int globalDecor, string globalScope, int globalNeighbors = 1)
        {
            RoomSizeMin = globalMin;
            RoomSizeMax = globalMax;
            DecorMinimum = globalDecor;
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

        internal static int globalRoomSizeMin = 6;
        internal static int globalRoomSizeMax = 32;
        private static int decorMinimum = 0;
        private static readonly string GlowChildName = "ArtifactGlowFX";

        private static Dictionary<string, ArtifactConfig> artifactConfigMap;

        public struct ArtifactCriteriaResult
        {
            public string Scope;
            public bool Transform; // Adjusted case
            public bool isFree;
            public bool IsAnalyzed;
            public int ActualRoomSize; // Adjusted case
            public bool MeetsRoomSize; // Adjusted case
            public float ActualDecor; // Adjusted case
            public bool MeetsDecor; // Adjusted case
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


            // 0. Check if the artifact is on a pedestal
            if (!CheckTransform(artifact, ref result)) return result;

            // 1. Check if the artifact is on a pedestal
            if (!CheckOnPedestal(artifact, ref result)) return result;

            // 2. Check if the artifact is analyzed
            if (!CheckAnalyzed(artifact, ref result)) return result;

            // 3. Check if the artifact is entombed
            if (!CheckFree(artifact, ref result)) return result;

            // 4. Check room size
            if (!CheckRoomSize(artifact, config, ref result)) return result;

            // 5. Check neighbor count
            if (!CheckNeighbors(artifact, config, ref result)) return result;

            // 6. Check decor
            if (!CheckDecor(artifact, config, ref result)) return result;

            result.MeetsAll = true;
            return result;
        }

        private static bool CheckTransform(GameObject artifact, ref ArtifactCriteriaResult result)
        {
            result.Transform = artifact.transform ? true : false; // Adjusted case to match ArtifactCriteriaResult
            return result.Transform;
        }

        private static bool CheckOnPedestal(GameObject artifact, ref ArtifactCriteriaResult result)
        {
            result.OnPedestal = false; // Adjusted case to match ArtifactCriteriaResult
            foreach (var pedestal in UnityEngine.Object.FindObjectsOfType<ItemPedestal>())
            {
                var receptacleField = typeof(ItemPedestal).GetField("receptacle", BindingFlags.NonPublic | BindingFlags.Instance);
                var receptacle = receptacleField?.GetValue(pedestal) as SingleEntityReceptacle;
                if (receptacle?.Occupant == artifact)
                {
                    result.OnPedestal = true; // Adjusted case to match ArtifactCriteriaResult
                    return true;
                }
            }
            return result.OnPedestal;
        }

        private static bool CheckAnalyzed(GameObject artifact, ref ArtifactCriteriaResult result)
        {
            result.IsAnalyzed = false;

            var artifactId = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name;
            result.IsAnalyzed = !string.IsNullOrEmpty(artifactId) && ArtifactSelector.Instance?.GetAnalyzedArtifactIDs().Contains(artifactId) == true;
            result.ShortCircuited = result.IsAnalyzed ? null : "isAnalyzed";
            return result.IsAnalyzed;
        }

        private static bool CheckFree(GameObject artifact, ref ArtifactCriteriaResult result) // not entombed
        {
            result.isFree = false;
            int cell = Grid.PosToCell(artifact.transform.position);
            result.isFree = Grid.Element[cell].id != SimHashes.Unobtanium;
            result.ShortCircuited = result.isFree ? null : "isFree";
            return result.isFree;
        }

        private static bool CheckRoomSize(GameObject artifact, ArtifactConfig config, ref ArtifactCriteriaResult result)
        {
            result.MeetsRoomSize = false; // Adjusted case to match ArtifactCriteriaResult
            int cell = Grid.PosToCell(artifact.transform.position);
            var cavity = Game.Instance?.roomProber?.GetCavityForCell(cell);
            var room = cavity?.room;

            if (room != null && room.cavity != null)
            {
                result.ActualRoomSize = room.cavity.numCells; // Adjusted case to match ArtifactCriteriaResult
                result.MeetsRoomSize = result.ActualRoomSize >= config.RoomSizeMin && result.ActualRoomSize <= config.RoomSizeMax; // Adjusted case to match ArtifactCriteriaResult
            }
            return result.MeetsRoomSize;
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

        private static bool CheckDecor(GameObject artifact, ArtifactConfig config, ref ArtifactCriteriaResult result)
        {
            result.MeetsDecor = false; // Adjusted case to match ArtifactCriteriaResult
            int cell = Grid.PosToCell(artifact.transform.position);
            var cavity = Game.Instance?.roomProber?.GetCavityForCell(cell);
            var room = cavity?.room;

            if (room != null && room.cavity != null)
            {
                int decorSum = 0;
                int decorCount = 0;
                foreach (var building in room.cavity.buildings)
                {
                    if (Grid.IsValidCell(Grid.PosToCell(building.transform.position)))
                    {
                        decorSum += (int)Grid.Decor[Grid.PosToCell(building.transform.position)];
                        decorCount++;
                    }
                }
                result.ActualDecor = decorCount > 0 ? (float)decorSum / decorCount : 0f; // Adjusted case to match ArtifactCriteriaResult

                bool isActive = ArtifactStateTracker.ArtifactStates.TryGetValue(artifact.GetInstanceID(), out var state) && state.IsActive;
                float requiredDecor = isActive ? config.DecorMinimum * 0.9f : config.DecorMinimum;
                result.MeetsDecor = result.ActualDecor >= requiredDecor; // Adjusted case to match ArtifactCriteriaResult
            }
            return result.MeetsDecor;
        }

        private static List<GameObject> GetAllMinions()
        {
            return UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && kp.HasTag("Minion"))
                .Select(kp => kp.gameObject)
                .ToList();
        }

        private static List<GameObject> GetMinionsInSameRoom(GameObject artifact)
        {
            return ArtifactEffectTracker.GetMinionsInSameRoom(artifact);
        }

        private static List<GameObject> GetMinionsInSameWorld(GameObject artifact)
        {
            return ArtifactEffectTracker.GetMinionsInSameWorld(artifact);
        }

        private static bool ActiveAndInScope(GameObject minion, GameObject artifact)
        {
            return ArtifactEffectTracker.ActiveAndInScope(minion, artifact);
        }

        public static void UpdateArtifactState(GameObject artifact)
        {
            if (artifact == null)
            {
                Patches.Logger.Log("[ERROR] UpdateArtifactState called with a null artifact.");
                return;
            }

            int id = artifact.GetInstanceID();

            if (!ArtifactStates.TryGetValue(id, out var state)) // checking this artifact state for first time.
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
                Patches.Logger.Log($"[WARN] No config found for artifact '{internalName}'");
                return;
            }

            var criteria = EvaluateArtifactCriteria(artifact, config);
            state.IsActive = criteria.MeetsAll;
            if (!criteria.MeetsAll)
            {
                //LogArtifactShortCircuitIssue(artifact, criteria);
            }

            string displayName = artifact.GetComponent<KSelectable>()?.GetProperName()
                    ?? artifact.GetComponent<KPrefabID>()?.PrefabTag.Name
                    ?? internalName;

            if (wasActive != state.IsActive)
            {
                string stateText = state.IsActive ? "ACTIVE" : "INACTIVE";

                PopFXManager.Instance.SpawnFX(
                    state.IsActive ? PopFXManager.Instance.sprite_Plus : PopFXManager.Instance.sprite_Negative,
                    $"Artifact '{displayName}' {stateText}",
                    artifact.transform,
                    new Vector3(0, 0, 0),
                    2f,
                    false
                );
            }

            // Commented out the log line as requested
            // Patches.Logger.Log($"Artifact '{internalName}' ({artifact.GetInstanceID()})  WasActive={wasActive} to IsActive={state.IsActive}.");
            ApplyGlowEffect(artifact, state.IsActive);
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
                Patches.Logger.Log("[ERROR] ApplyGlowEffect called with a null artifact.");
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
            artifactConfigMap = new Dictionary<string, ArtifactConfig>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var configText = File.ReadAllText(Patches.ArtifactPowersConfigPath);
                var configJson = JObject.Parse(configText);

                int globalRoomSizeMin = (int)(configJson["RoomSizeMinimum"] ?? 32);
                int globalRoomSizeMax = (int)(configJson["RoomSizeMaximum"] ?? 96);
                int globalDecorMinimum = (int)(configJson["DecorMinimum"] ?? 0);
                int globalNeighbors = (int)(configJson["Neighbors"] ?? 1);
                string globalScope = (string)(configJson["Scope"] ?? "InWorld");

                if (!(configJson["Artifacts"] is JArray arr))
                {
                    Patches.Logger.Log("[ERROR] 'Artifacts' array missing or not an array in config JSON.");
                    return;
                }

                foreach (var obj in arr)
                {
                    var artifactId = (string)obj["ArtifactId"];
                    if (artifactId == null) continue;

                    var artifactConfig = new ArtifactConfig(globalRoomSizeMin, globalRoomSizeMax, globalDecorMinimum, globalScope, globalNeighbors);

                    if (obj["RoomSizeMin"] != null) artifactConfig.RoomSizeMin = (int)obj["RoomSizeMin"];
                    if (obj["RoomSizeMax"] != null) artifactConfig.RoomSizeMax = (int)obj["RoomSizeMax"];
                    if (obj["DecorMinimum"] != null) artifactConfig.DecorMinimum = (int)obj["DecorMinimum"];
                    if (obj["Neighbors"] != null) artifactConfig.Neighbors = (int)obj["Neighbors"];
                    if (obj["Scope"] != null) artifactConfig.Scope = (string)obj["Scope"];

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

                    artifactConfigMap[artifactId] = artifactConfig;
                }
            }
            catch (Exception ex)
            {
                Patches.Logger.Log($"[ERROR] Failed to load artifact config: {ex}");
            }
        }

        public static ArtifactConfig GetArtifactConfig(string artifactId)
        {
            if (artifactConfigMap != null && artifactConfigMap.TryGetValue(artifactId, out var config))
                return config;

            return new ArtifactConfig(globalRoomSizeMin, globalRoomSizeMax, decorMinimum, "All");
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

        // need to run through all artifacts because things around them could have changed.
        public static void PollAllArtifacts()
        {
            var allArtifacts = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && kp.HasTag("Artifact"))
                .Select(kp => kp.gameObject)
                .ToArray(); // Use ToArray to avoid multiple enumerations.

            foreach (var artifact in allArtifacts)
            {
                if (artifact == null) continue;
                UpdateArtifactState(artifact);
            }
        }

        public static void UpdateMinions()
        {
            var allMinions = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && kp.HasTag("Minion"))
                .Select(kp => kp.gameObject)
                .ToArray(); // Use ToArray to avoid multiple enumerations.

            var allArtifacts = UnityEngine.Object.FindObjectsOfType<KPrefabID>()
                .Where(kp => kp != null && kp.HasTag("Artifact"))
                .Select(kp => kp.gameObject)
                .ToArray(); // Use ToArray to avoid multiple enumerations.

            foreach (var minion in allMinions)
            {
                foreach (var artifact in allArtifacts)
                {
                    if (artifact == null) continue;

                    ArtifactEffectTracker.RemoveArtifactModifiersToMinion(minion, artifact);

                    int artifactId = artifact.GetInstanceID();
                    if (ArtifactStates.TryGetValue(artifactId, out var state) && state.IsActive)
                    {
                        var config = GetArtifactConfig(artifact.GetComponent<KPrefabID>()?.PrefabTag.Name);
                        if (config == null) continue;

                        bool inScope = config.Scope == "All" ||
                                       (config.Scope == "InRoom" && GetMinionsInSameRoom(artifact).Contains(minion)) ||
                                       (config.Scope == "InWorld" && GetMinionsInSameWorld(artifact).Contains(minion));

                        if (inScope)
                        {
                            ArtifactEffectTracker.ApplyArtifactModifiersToMinion(minion, artifact);
                        }
                    }
                }
            }
        }
    }

    public class ArtifactStatePoller : MonoBehaviour
    {
        private int tickCounter = 0;
        private static int pollInterval; // Cache poll interval for the current save/load  

        public HLib.HotkeyListener hotkeyListener { get; set; }

        public ArtifactStatePoller(HLib.HotkeyListener hotkeyListener)
        {
            this.hotkeyListener = hotkeyListener;
            var config = ArtifactsPlusConfig.Instance;
            pollInterval = config.ArtifactPollingInterval; // Read polling interval from configuration
        }

        void Awake() { }
        void Start()
        {
            // Read and cache the poll interval once during initialization  
            var config = ArtifactsPlusConfig.Instance;
            pollInterval = config.ArtifactPollingInterval;
        }

        void Update()
        {
            tickCounter++;

            if (tickCounter >= pollInterval*60)
            {
                tickCounter = 0;
                ArtifactStateTracker.PollAllArtifacts();
                ArtifactStateTracker.UpdateMinions();
            }

            hotkeyListener?.Update();
        }
    }

    public class Mod : UserMod2
    {
        private static int onLoadCount = 0;

        public override void OnLoad(Harmony harmony)
        {
            new POptions().RegisterOptions(this, typeof(ArtifactsPlusConfig)); // Register the options
            Patches.OnLoad();

            if (harmony == null)
            {
                Patches.Logger.Log("[ArtifactsPlus] Harmony instance is null.");
                return;
            }

            harmony.PatchAll();
            PUtil.InitLibrary();
        }
    }

    [HarmonyPatch(typeof(ItemPedestal), "OnOccupantChanged")]
    public static class ItemPedestal_OnOccupantChanged_Patch
    {
        public static void Postfix(ItemPedestal __instance)
        {
        }
    }

    [HarmonyPatch(typeof(Game), "OnPrefabInit")]
    public static class Game_OnPrefabInit_Patch
    {
        public static void Postfix()
        {
            if (Game.Instance != null && Game.Instance.gameObject.GetComponent<ArtifactStatePoller>() == null)
            {
                var poller = Game.Instance.gameObject.AddComponent<ArtifactStatePoller>();
                poller.hotkeyListener = Patches.hotkeyListener;
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

    [JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)] // Explicitly specify the namespace
    public sealed class ArtifactsPlusOptions
    {
        [Option("Enable Custom Output Log", "Enable or disable writing the custom output log file.")]
        [JsonProperty]
        public bool EnableCustomLog { get; set; }

        [Option("Artifact Config File", "Set the path to the artifact configuration file.", Format = "F")]
        [JsonProperty]
        public string ArtifactConfigFile { get; set; }

        [Option("Artifact Polling Interval", "Set the interval (in ticks) for artifact polling.")]
        [Limit(1, 10000)]
        [JsonProperty]
        public int ArtifactPollingInterval { get; set; }

        public ArtifactsPlusOptions()
        {
            EnableCustomLog = true;
            ArtifactConfigFile = "ArtifactsConfig.json";
            ArtifactPollingInterval = 15; // Default value, 15 seconds
        }

        public override string ToString()
        {
            return string.Format("ArtifactsPlusOptions[EnableCustomLog={0}, ArtifactConfigFile={1}, ArtifactPollingInterval={2}]",
                EnableCustomLog, ArtifactConfigFile, ArtifactPollingInterval);
        }
    }

    public sealed class ArtifactsPlusOptionsSingleton
    {
        private static readonly Lazy<ArtifactsPlusOptionsSingleton> _instance = new Lazy<ArtifactsPlusOptionsSingleton>(() => new ArtifactsPlusOptionsSingleton());

        public static ArtifactsPlusOptionsSingleton Instance => _instance.Value;

        public const string DefaultArtifactConfigFile = "ArtifactsConfig.json";

        public string ArtifactConfigFile { get; set; }
        public bool EnableCustomLog { get; set; }
    }
}
