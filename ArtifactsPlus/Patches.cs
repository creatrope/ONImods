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
using PeterHan.PLib.UI;
using Object = UnityEngine.Object; // Explicitly alias UnityEngine.Object to avoid ambiguity

namespace ArtifactsPlus
{
    public static class ModInit
    {
        public static string DesktopLogPath => CustomLogger.LogPath;

        public static string ArtifactPowersConfigPath
        {
            get
            {
                return Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "ArtifactsConfig.json"
                );
            }
        }

        public static void OnLoad()
        {
            Debug.Log("[ArtifactsPlus] OnLoad() was called!");
            Debug.Log($"[ArtifactsPlus] Custom log file target location: {DesktopLogPath}");
            CustomLogger.Log("Test message: custom log initialized and working.");

            CustomLogger.Log("[DEBUG] Calling LoadArtifactAttributeMap()");
            ArtifactStateTracker.LoadArtifactAttributeMap();
        }
    }

    public class ArtifactState
    {
        public bool OnPedestal;
        public bool MeetsRoomSize;
        public bool IsActive;
    }

    public class ArtifactConfig
    {
        public int RoomSizeMin;
        public int RoomSizeMax;
        public int DecorMinimum;
        public string Filter;
        public Dictionary<string, float> Attributes;
        public List<string> Traits;

        public ArtifactConfig(int globalMin, int globalMax, int globalDecor, string globalFilter)
        {
            RoomSizeMin = globalMin;
            RoomSizeMax = globalMax;
            DecorMinimum = globalDecor;
            Filter = globalFilter;
            Attributes = new Dictionary<string, float>();
            Traits = new List<string>();
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
            state.IsActive = onPedestal && meetsRoomSize;

            string internalName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
            string displayName = artifact.GetComponent<KSelectable>()?.GetProperName()
                ?? artifact.GetComponent<KPrefabID>()?.PrefabTag.Name
                ?? internalName;

            // Get artifact config
            var config = GetArtifactConfig(internalName);
            if (config == null)
            {
                CustomLogger.Log($"[WARN] No config found for artifact '{internalName}'");
                return;
            }

            if (wasActive != state.IsActive)
            {
                string stateText = state.IsActive ? "ACTIVE" : "INACTIVE";
                CustomLogger.Log($"[ArtifactState] Artifact '{displayName}' (ID={artifact.name}) changed state: {stateText}");

                PopFXManager.Instance.SpawnFX(
                    state.IsActive ? PopFXManager.Instance.sprite_Plus : PopFXManager.Instance.sprite_Negative,
                    $"Artifact '{displayName}' is now {stateText}",
                    artifact.transform,
                    new Vector3(0, 0, 0),
                    2f,
                    false
                );

                CustomLogger.Log(
                    $"[CONFIG] Using for '{internalName}': RoomSizeMin={config.RoomSizeMin}, RoomSizeMax={config.RoomSizeMax}, DecorMinimum={config.DecorMinimum}, Filter={config.Filter}"
                );

                List<GameObject> minionList;
                if (config.Filter == "InRoom")
                    minionList = GetMinionsInSameRoom(artifact);
                else if (config.Filter == "InWorld")
                    minionList = GetMinionsInSameWorld(artifact);
                else
                    minionList = GetAllMinions();

                CustomLogger.Log($"[DEBUG] Calling ArtifactEffectTracker.OnArtifactStateChanged for '{internalName}' (active={state.IsActive})");
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
            return TryGetArtifactAttributes(artifactId, out effects);
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

                int globalRoomSizeMin = (int)(configJson["GlobalRoomSizeMinimum"] ?? 6);
                int globalRoomSizeMax = (int)(configJson["GlobalRoomSizeMaximum"] ?? 32);
                int globalDecorMinimum = (int)(configJson["DecorMinimum"] ?? 0);
                string globalFilter = (string)(configJson["Filter"] ?? "All");

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
                    var artifactConfig = new ArtifactConfig(globalRoomSizeMin, globalRoomSizeMax, globalDecorMinimum, globalFilter);

                    // Local overrides
                    if (obj["RoomSizeMin"] != null) artifactConfig.RoomSizeMin = (int)obj["RoomSizeMin"];
                    if (obj["RoomSizeMax"] != null) artifactConfig.RoomSizeMax = (int)obj["RoomSizeMax"];
                    if (obj["DecorMinimum"] != null) artifactConfig.DecorMinimum = (int)obj["DecorMinimum"];
                    if (obj["Filter"] != null) artifactConfig.Filter = (string)obj["Filter"];

                    if (obj["Attributes"] is JObject attributes)
                    {
                        foreach (var prop in attributes.Properties())
                        {
                            if (float.TryParse(prop.Value.ToString(), out float val))
                                artifactConfig.Attributes[prop.Name] = val;
                        }
                    }

                    if (obj["Traits"] is JArray traits)
                    {
                        foreach (var trait in traits)
                        {
                            artifactConfig.Traits.Add((string)trait);
                        }
                    }

                    artifactConfigMap[artifactId] = artifactConfig;
                }
                CustomLogger.Log("[DEBUG] Loaded artifact config map with inheritance.");
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
            foreach (var artifact in ArtifactsOnPedestals)
            {
                if (artifact == null)
                    continue;

                int cell = Grid.PosToCell(artifact.transform.position);
                float roomDecor = float.MinValue;
                bool meetsRoomSize = false;
                bool meetsDecor = false;

                // Get artifact config using internal name
                string internalName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                var config = GetArtifactConfig(internalName);

                if (Game.Instance != null && Game.Instance.roomProber != null)
                {
                    var cavity = Game.Instance.roomProber.GetCavityForCell(cell);
                    var room = cavity?.room;
                    if (room != null && room.cavity != null)
                    {
                        // Use config.RoomSizeMin and config.RoomSizeMax instead of globalRoomSizeMin/globalRoomSizeMax
                        meetsRoomSize = room.cavity.numCells >= config.RoomSizeMin && room.cavity.numCells <= config.RoomSizeMax;

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
                        {
                            roomDecor = (float)decorSum / decorCount;
                        }
                        else
                        {
                            roomDecor = 0f;
                        }
                        // Use config.DecorMinimum instead of decorMinimum
                        meetsDecor = roomDecor >= config.DecorMinimum;
                    }
                }

                bool meetsAll = meetsRoomSize && meetsDecor;
                UpdateArtifactState(artifact, true, meetsAll);
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

            if (Game.Instance != null && Game.Instance.gameObject.GetComponent<ArtifactHotkeyListener>() == null)
            {
                Game.Instance.gameObject.AddComponent<ArtifactHotkeyListener>();
            }

            var go = new GameObject("ArtifactHotkeyListener");
            go.AddComponent<ArtifactHotkeyListener>();
            Object.DontDestroyOnLoad(go); // Explicitly refers to UnityEngine.Object
        }

        private static readonly Dictionary<GameObject, (int oldWorldId, int newWorldId, bool removed, bool added)> MinionMigrationState
            = new Dictionary<GameObject, (int, int, bool, bool)>();

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
                        CustomLogger.Log("[DEBUG][Migration] minionGo is null, skipping.");
                        return;
                    }

                    int oldWorldId = migrationEventArgs.prevWorldId;
                    int newWorldId = migrationEventArgs.targetWorldId;

                    if (!MinionMigrationState.TryGetValue(minionGo, out var state))
                    {
                        // First message: state 1
                        MinionMigrationState[minionGo] = (oldWorldId, newWorldId, false, false);
                        CustomLogger.Log($"[DEBUG][Migration] STATE 1: Minion {minionGo.name} oldWorldId={oldWorldId} newWorldId={newWorldId}");
                    }
                    else
                    {
                        // Second message: state 2
                        CustomLogger.Log($"[DEBUG][Migration] STATE 2: Minion {minionGo.name} oldWorldId={state.oldWorldId} newWorldId={state.newWorldId}");

                        // REMOVE artifact effects for old world
                        foreach (var artifact in ArtifactsPlus.ArtifactStateTracker.ArtifactsOnPedestals)
                        {
                            if (artifact == null) continue;
                            int artifactWorldId = Grid.WorldIdx[Grid.PosToCell(artifact.transform.position)];
                            string internalName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                            var config = ArtifactsPlus.ArtifactStateTracker.GetArtifactConfig(internalName);

                            if (artifactWorldId == state.oldWorldId && config.Filter == "InWorld")
                            {
                                CustomLogger.Log($"[DEBUG][Migration] Removing effects from minion {minionGo.name} for artifact {internalName} in old world {state.oldWorldId}");
                                ArtifactEffectTracker.ApplyOrRemoveArtifactEffectsToMinion(minionGo, internalName, false);
                            }
                        }

                        // ADD artifact effects for new world
                        foreach (var artifact in ArtifactsPlus.ArtifactStateTracker.ArtifactsOnPedestals)
                        {
                            if (artifact == null) continue;
                            int artifactWorldId = Grid.WorldIdx[Grid.PosToCell(artifact.transform.position)];
                            string internalName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                            var config = ArtifactsPlus.ArtifactStateTracker.GetArtifactConfig(internalName);

                            if (artifactWorldId == state.newWorldId && config.Filter == "InWorld")
                            {
                                CustomLogger.Log($"[DEBUG][Migration] Adding effects to minion {minionGo.name} for artifact {internalName} in new world {state.newWorldId}");
                                ArtifactEffectTracker.ApplyOrRemoveArtifactEffectsToMinion(minionGo, internalName, true);
                            }
                        }

                        CustomLogger.Log($"[DEBUG][Migration] MIGRATION COMPLETE: Minion {minionGo.name} oldWorldId={state.oldWorldId} newWorldId={state.newWorldId}");
                        MinionMigrationState.Remove(minionGo);
                    }
                }
                else
                {
                    CustomLogger.Log("[DEBUG][Migration] migrationEventArgs is null or not of expected type.");
                }
            }
        }
    }
}