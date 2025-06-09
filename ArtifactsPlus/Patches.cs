using HarmonyLib;
using UnityEngine;
using System.IO;
using KMod;
using System.Collections.Generic;
using System.Linq;
using System;
using Klei.AI; // Add this import for Analyzable
using System.Reflection;
using Newtonsoft.Json.Linq;
using PeterHan.PLib.UI;
using PeterHan.PLib.Options;
using PeterHan.PLib.Core; // Add this import for PUtil
using Object = UnityEngine.Object; // Explicitly alias UnityEngine.Object to avoid ambiguity
using System.Text; // <-- Add this for StringBuilder

namespace ArtifactsPlus
{
    public static class ModInit
    {
        public static string DesktopLogPath => CustomLogger.LogPath;

        public static string ArtifactPowersConfigPath
        {
            get
            {
                var configFile = ArtifactsPlusOptions.Instance.ArtifactConfigFile;
                return Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    string.IsNullOrEmpty(configFile) ? "ArtifactsConfig.json" : configFile
                );
            }
        }

        public static void OnLoad()
        {
            // Only print custom log file location and log to custom log if verbose is enabled
            if (ArtifactsPlusOptions.Instance.Verbose)
            {
                CustomLogger.Log($"[ArtifactsPlus] Custom log file target location: {DesktopLogPath}");
            }

            ArtifactStateTracker.LoadArtifactAttributeMap();
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
            public int ActualRoomSize;
            public bool MeetsRoomSize;
            public float ActualDecor;
            public bool MeetsDecor;
            public string Scope;
            public int ArtifactCountInRoom;
            public bool NeighborsOk;
            public bool MeetsAll;
            public bool ShortCircuited;
        }

        public static ArtifactCriteriaResult EvaluateArtifactCriteria(GameObject artifact, ArtifactConfig config, bool shortCircuit = false)
        {
            int actualRoomSize = -1;
            float actualDecor = float.NaN;
            bool meetsRoomSize = false;
            bool meetsDecor = false;
            bool isEntombed = false;
            int artifactCount = 0;
            bool neighborsOk = true;
            bool meetsAll = true;
            bool wasShortCircuited = false;

            int cell = Grid.PosToCell(artifact.transform.position);

            // 1. Entombed check (cheapest)
            isEntombed = Grid.Element[cell].id == SimHashes.Unobtanium;
            if (shortCircuit && isEntombed)
            {
                meetsAll = false;
                wasShortCircuited = true;
            }

            // 2. Room/cavity lookup
            if (meetsAll && Game.Instance != null && Game.Instance.roomProber != null)
            {
                var cavity = Game.Instance.roomProber.GetCavityForCell(cell);
                var room = cavity?.room;
                if (room != null && room.cavity != null)
                {
                    // 3. Room size check
                    actualRoomSize = room.cavity.numCells;
                    meetsRoomSize = actualRoomSize >= config.RoomSizeMin && actualRoomSize <= config.RoomSizeMax;
                    if (shortCircuit && !meetsRoomSize)
                    {
                        meetsAll = false;
                        wasShortCircuited = true;
                    }

                    // 4. Neighbor count
                    if (meetsAll)
                    {
                        artifactCount = CountArtifactsOnPedestalsInRoom(room);
                        neighborsOk = artifactCount <= config.Neighbors;
                        if (shortCircuit && !neighborsOk)
                        {
                            meetsAll = false;
                            wasShortCircuited = true;
                        }
                    }

                    // 5. Decor
                    if (meetsAll)
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
                        if (decorCount > 0)
                            actualDecor = (float)decorSum / decorCount;
                        else
                            actualDecor = 0f;
                        meetsDecor = actualDecor >= config.DecorMinimum;
                        if (shortCircuit && !meetsDecor)
                        {
                            meetsAll = false;
                            wasShortCircuited = true;
                        }
                    }
                    else
                    {
                        // For debug output, always calculate decor
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
                        if (decorCount > 0)
                            actualDecor = (float)decorSum / decorCount;
                        else
                            actualDecor = 0f;
                        meetsDecor = actualDecor >= config.DecorMinimum;
                    }
                }
            }

            if (!shortCircuit)
                meetsAll = meetsRoomSize && meetsDecor && !isEntombed && neighborsOk;

            return new ArtifactCriteriaResult
            {
                ActualRoomSize = actualRoomSize,
                MeetsRoomSize = meetsRoomSize,
                ActualDecor = actualDecor,
                MeetsDecor = meetsDecor,
                Scope = config.Scope,
                ArtifactCountInRoom = artifactCount,
                NeighborsOk = neighborsOk,
                MeetsAll = meetsAll,
                ShortCircuited = wasShortCircuited
            };
        }

        public static void RegisterArtifactOnPedestal(GameObject artifact)
        {
            if (artifact != null)
                ArtifactsOnPedestals.Add(artifact);
        }

        public static void UnregisterArtifactOnPedestal(GameObject artifact)
        {
            if (artifact != null)
            {
                // Take a snapshot of the artifact before modifying the collection
                var artifactId = artifact.GetInstanceID();
                // Remove from collections first to avoid modifying during enumeration in callbacks
                ArtifactsOnPedestals.Remove(artifact);
                // Trigger state update BEFORE removing from ArtifactStates, so OnArtifactStateChanged is called
                UpdateArtifactState(artifact, false, false);
                ArtifactStates.Remove(artifactId);
            }
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
            var minionsInRoom = new List<GameObject>();
            int artifactCell = Grid.PosToCell(artifact.transform.position);
            var artifactCavity = Game.Instance?.roomProber?.GetCavityForCell(artifactCell)?.room?.cavity;
            if (artifactCavity == null)
                return minionsInRoom;

            foreach (var kp in UnityEngine.Object.FindObjectsOfType<KPrefabID>())
            {
                if (kp != null && kp.HasTag("Minion"))
                {
                    int minionCell = Grid.PosToCell(kp.transform.position);
                    var minionCavity = Game.Instance.roomProber.GetCavityForCell(minionCell)?.room?.cavity;
                    if (minionCavity == artifactCavity)
                        minionsInRoom.Add(kp.gameObject);
                }
            }
            return minionsInRoom;
        }

        private static List<GameObject> GetMinionsInSameWorld(GameObject artifact)
        {
            var minionsInWorld = new List<GameObject>();
            int artifactWorldId = Grid.WorldIdx[Grid.PosToCell(artifact.transform.position)];
            foreach (var kp in UnityEngine.Object.FindObjectsOfType<KPrefabID>())
            {
                if (kp != null && kp.HasTag("Minion"))
                {
                    int minionWorldId = Grid.WorldIdx[Grid.PosToCell(kp.transform.position)];
                    if (minionWorldId == artifactWorldId)
                        minionsInWorld.Add(kp.gameObject);
                }
            }
            return minionsInWorld;
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

            // Get artifact config
            string internalName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
            var config = GetArtifactConfig(internalName);
            if (config == null)
            {
                CustomLogger.Log($"[WARN] No config found for artifact '{internalName}'");
                return;
            }

            // Evaluate criteria
            var criteria = EvaluateArtifactCriteria(artifact, config);
            bool isAnalyzed = false;
            var artifactId = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name;
            if (!string.IsNullOrEmpty(artifactId) && ArtifactSelector.Instance != null)
            {
                isAnalyzed = ArtifactSelector.Instance.GetAnalyzedArtifactIDs().Contains(artifactId);
            }
            state.IsAnalyzed = isAnalyzed;

            state.IsActive = onPedestal && criteria.MeetsAll && isAnalyzed;

            string displayName = artifact.GetComponent<KSelectable>()?.GetProperName()
                ?? artifact.GetComponent<KPrefabID>()?.PrefabTag.Name
                ?? internalName;

            if (wasActive != state.IsActive)
            {
                int cell = Grid.PosToCell(artifact.transform.position);
                int worldId = Grid.WorldIdx[cell];
                string worldName = ClusterManager.Instance.GetWorld(worldId)?.name ?? $"World_{worldId}";
                string stateText = state.IsActive ? "ACTIVE" : "INACTIVE";
                string shortCircuitText = criteria.ShortCircuited ? " SHORTCIRCUIT" : "";
                CustomLogger.Log(
                    $"[ArtifactState]{shortCircuitText} {internalName} {stateText} " +
                    $"Pedestal(OK) " +
                    $"RoomSize: {config.RoomSizeMin}<=({criteria.ActualRoomSize})<={config.RoomSizeMax} ({criteria.MeetsRoomSize}) " +
                    $"Decor: {config.DecorMinimum}<={criteria.ActualDecor} ({criteria.MeetsDecor}) " +
                    $"Neighbors: {criteria.ArtifactCountInRoom}<={config.Neighbors} ({criteria.NeighborsOk})"
                );

                PopFXManager.Instance.SpawnFX(
                    state.IsActive ? PopFXManager.Instance.sprite_Plus : PopFXManager.Instance.sprite_Negative,
                    $"Artifact '{displayName}' {stateText}",
                    artifact.transform,
                    new Vector3(0, 0, 0),
                    2f,
                    false
                );

                List<GameObject> minionList;
                if (config.Scope == "InRoom")
                    minionList = GetMinionsInSameRoom(artifact);
                else if (config.Scope == "InWorld")
                    minionList = GetMinionsInSameWorld(artifact);
                else
                    minionList = GetAllMinions();

                ArtifactEffectTracker.OnArtifactStateChanged(artifact, internalName, state.IsActive, minionList);
            }

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

        public static bool TryGetArtifactEffects(string artifactId, out Dictionary<string, float> effects)
        {
            if (artifactConfigMap != null && artifactConfigMap.TryGetValue(artifactId, out var config))
            {
                effects = config.Effects;
                return true;
            }
            effects = null;
            return false;
        }

        public static void ApplyGlowEffect(GameObject artifact, bool enable)
        {
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
                        light2D.enabled = true;
                }
            }
            else if (glowChild != null)
            {
                GameObject.Destroy(glowChild);
            }
        }

        public static void LoadArtifactAttributeMap()
        {
            artifactConfigMap = new Dictionary<string, ArtifactConfig>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var configText = File.ReadAllText(ModInit.ArtifactPowersConfigPath);
                var configJson = JObject.Parse(configText);

                int globalRoomSizeMin = (int)(configJson["RoomSizeMinimum"] ?? 32);
                int globalRoomSizeMax = (int)(configJson["RoomSizeMaximum"] ?? 96);
                int globalDecorMinimum = (int)(configJson["DecorMinimum"] ?? 0);
                int globalNeighbors = (int)(configJson["Neighbors"] ?? 1);
                string globalScope = (string)(configJson["Scope"] ?? "InWorld");

                if (!(configJson["Artifacts"] is JArray arr))
                {
                    CustomLogger.Log("[ERROR] 'Artifacts' array missing or not an array in config JSON.");
                    return;
                }

                foreach (var obj in arr)
                {
                    var artifactId = (string)obj["ArtifactId"];
                    if (artifactId == null) continue;

                    // Inherit global config
                    var artifactConfig = new ArtifactConfig(globalRoomSizeMin, globalRoomSizeMax, globalDecorMinimum, globalScope, globalNeighbors);

                    // Local overrides
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

                    // Load Effects (statuses) from config
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
                CustomLogger.Log($"[ERROR] Failed to load artifact config: {ex}");
            }
        }

        public static ArtifactConfig GetArtifactConfig(string artifactId)
        {
            if (artifactConfigMap != null && artifactConfigMap.TryGetValue(artifactId, out var config))
                return config;

            // Return a config using global values if not found
            return new ArtifactConfig(globalRoomSizeMin, globalRoomSizeMax, decorMinimum, "All");
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
            var artifactsSnapshot = ArtifactsOnPedestals.ToArray();

            foreach (var artifact in artifactsSnapshot)
            {
                if (artifact == null)
                    continue;

                string internalName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                var config = GetArtifactConfig(internalName);

                // Use the shared criteria evaluation
                var criteria = EvaluateArtifactCriteria(artifact, config);

                bool meetsAll = criteria.MeetsAll;
                int id = artifact.GetInstanceID();
                bool wasActive = false;
                if (ArtifactStates.TryGetValue(id, out var state))
                    wasActive = state.IsActive;

                UpdateArtifactState(artifact, true, meetsAll);
            }
        }

        private static int CountArtifactsOnPedestalsInRoom(Room room)
        {
            int count = 0;
            if (room != null && room.cavity != null)
            {
                //CustomLogger.Log($"[DEBUG] Counting pedestals in room: {room.GetHashCode()}, buildings: {room.cavity.buildings.Count}");
                foreach (var building in room.cavity.buildings)
                {
                    if (building == null)
                    {
                        //CustomLogger.Log("[DEBUG] Skipping null building.");
                        continue;
                    }

                    var pedestal = building.GetComponent<ItemPedestal>();
                    if (pedestal == null)
                    {
                        // CustomLogger.Log($"[DEBUG] Building {building.name} is not an ItemPedestal.");
                        continue;
                    }

                    var receptacleField = typeof(ItemPedestal).GetField("receptacle", BindingFlags.NonPublic | BindingFlags.Instance);
                    var receptacle = receptacleField?.GetValue(pedestal) as SingleEntityReceptacle;
                    var occupant = receptacle?.Occupant;
                    if (occupant == null)
                    {
                        //CustomLogger.Log($"[DEBUG] Pedestal {building.name} has no occupant.");
                        continue;
                    }

                    var prefabId = occupant.GetComponent<KPrefabID>();
                    if (prefabId == null)
                    {
                        //CustomLogger.Log($"[DEBUG] Occupant on pedestal {building.name} has no KPrefabID.");
                        continue;
                    }

                    if (artifactConfigMap != null && artifactConfigMap.ContainsKey(prefabId.PrefabTag.Name))
                    {
                        count++;
                        //CustomLogger.Log($"[DEBUG] Counted artifact '{prefabId.PrefabTag.Name}' on pedestal {building.name}.");
                    }
                    else
                    {
                        //CustomLogger.Log($"[DEBUG] Occupant '{prefabId.PrefabTag.Name}' on pedestal {building.name} is not in artifactConfigMap.");
                    }
                }
            }
            else
            {
                //CustomLogger.Log("[DEBUG] Room or room.cavity is null in CountArtifactsOnPedestalsInRoom.");
            }
            //CustomLogger.Log($"[DEBUG] Final artifact count in room: {count}");
            return count;
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
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);

            try
            {
                new POptions().RegisterOptions(this, typeof(ArtifactsPlusOptions));
            }
            catch (Exception ex)
            {
                CustomLogger.Log($"[ArtifactsPlus] RegisterOptions threw exception: {ex}");
            }

            // Only this mod load message stays as Debug.Log, but now commented out per your request
            // Debug.Log("[ArtifactsPlus] Mod loaded and Harmony patches applied.");
            harmony.PatchAll();
            ModInit.OnLoad();

            PUtil.InitLibrary();

            if (Game.Instance != null)
            {
                if (Game.Instance.gameObject.GetComponent<ArtifactStatePoller>() == null)
                {
                    Game.Instance.gameObject.AddComponent<ArtifactStatePoller>();
                }

                if (Game.Instance.gameObject.GetComponent<ArtifactHotkeyListener>() == null)
                {
                    Game.Instance.gameObject.AddComponent<ArtifactHotkeyListener>();
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
            {
                ArtifactStateTracker.RegisterArtifactOnPedestal(occupant);
            }
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
            if (Game.Instance != null && Game.Instance.gameObject.GetComponent<ArtifactHotkeyListener>() == null)
            {
                Game.Instance.gameObject.AddComponent<ArtifactHotkeyListener>();
            }
        }
    }

    internal static class MinionMigrationHelper
    {
        public static readonly Dictionary<GameObject, (int oldWorldId, int newWorldId, bool removed, bool added)>
            MinionMigrationState = new Dictionary<GameObject, (int, int, bool, bool)>();
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
                if (minionGo == null)
                {
                    return;
                }

                int oldWorldId = migrationEventArgs.prevWorldId;
                int newWorldId = migrationEventArgs.targetWorldId;

                if (!MinionMigrationHelper.MinionMigrationState.TryGetValue(minionGo, out var state))
                {
                    MinionMigrationHelper.MinionMigrationState[minionGo] = (oldWorldId, newWorldId, false, false);
                }
                else
                {
                    foreach (var artifact in ArtifactsPlus.ArtifactStateTracker.ArtifactsOnPedestals)
                    {
                        if (artifact == null) continue;
                        int artifactWorldId = Grid.WorldIdx[Grid.PosToCell(artifact.transform.position)];
                        string internalName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                        var config = ArtifactsPlus.ArtifactStateTracker.GetArtifactConfig(internalName);

                        if (artifactWorldId == state.oldWorldId && config.Scope == "InWorld")
                        {
                            ArtifactEffectTracker.ApplyOrRemoveArtifactModifiersToMinion(minionGo, internalName, false);
                            ArtifactEffectTracker.ApplyOrRemoveArtifactStatusEffectsToMinion(minionGo, internalName, false);
                        }
                    }

                    foreach (var artifact in ArtifactsPlus.ArtifactStateTracker.ArtifactsOnPedestals)
                    {
                        if (artifact == null) continue;
                        int artifactWorldId = Grid.WorldIdx[Grid.PosToCell(artifact.transform.position)];
                        string internalName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                        var config = ArtifactsPlus.ArtifactStateTracker.GetArtifactConfig(internalName);

                        if (artifactWorldId == state.newWorldId && config.Scope == "InWorld")
                        {
                            ArtifactEffectTracker.ApplyOrRemoveArtifactModifiersToMinion(minionGo, internalName, true);
                            ArtifactEffectTracker.ApplyOrRemoveArtifactStatusEffectsToMinion(minionGo, internalName, true);
                        }
                    }

                    MinionMigrationHelper.MinionMigrationState.Remove(minionGo);
                }
            }
        }
    }

    [HarmonyPatch(typeof(SaveLoader), "Save", new Type[] { typeof(string), typeof(bool), typeof(bool) })]
    public static class SaveLoader_Save_Patch
    {
        public static void Postfix(string filename, bool isAutoSave, bool updateSavePointer)
        {
            CustomLogger.Log("[ArtifactsPlus] SaveLoader.Save called for file: " + filename);
        }
    }

    [HarmonyPatch(typeof(SaveLoader), "Load", new Type[] { typeof(string) })]
    public static class SaveLoader_Load_Patch
    {
        public static void Postfix(string filename)
        {
            CustomLogger.Log("[ArtifactsPlus] SaveLoader.Load called for file: " + filename);
        }
    }
}