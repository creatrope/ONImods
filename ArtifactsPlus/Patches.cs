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

    public static class ArtifactStateTracker
    {
        internal static readonly Dictionary<int, ArtifactState> ArtifactStates = new Dictionary<int, ArtifactState>();
        internal static readonly HashSet<GameObject> ArtifactsOnPedestals = new HashSet<GameObject>();

        internal static int globalRoomSizeMin = 6;
        internal static int globalRoomSizeMax = 32;
        private static int decorMinimum = 0;
        private static readonly string GlowChildName = "ArtifactGlowFX";

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

            if (wasActive != state.IsActive)
            {
                string stateText = state.IsActive ? "ACTIVE" : "INACTIVE";
                Debug.Log($"[ArtifactsPlus] Artifact '{displayName}' (ID={artifact.name}) changed state: {stateText}");
                CustomLogger.Log($"[ArtifactState] Artifact '{displayName}' (ID={artifact.name}) changed state: {stateText}");

                PopFXManager.Instance.SpawnFX(
                    state.IsActive ? PopFXManager.Instance.sprite_Plus : PopFXManager.Instance.sprite_Negative,
                    $"Artifact '{displayName}' is now {stateText}",
                    artifact.transform,
                    new Vector3(0, 0, 0),
                    2f,
                    false
                );

                string filter = "All";
                try
                {
                    var configText = File.ReadAllText(ModInit.ArtifactPowersConfigPath);
                    var configJson = JObject.Parse(configText);
                    filter = (string)configJson["Filter"] ?? "All";
                }
                catch { }

                // Log the filter being applied
                CustomLogger.Log($"[DEBUG] Minion filter being applied: {filter}");

                List<GameObject> minionList;
                if (filter == "InRoom")
                    minionList = GetMinionsInSameRoom(artifact);
                else if (filter == "InWorld")
                    minionList = GetMinionsInSameWorld(artifact);
                else
                    minionList = GetAllMinions();

                ArtifactEffectTracker.OnArtifactStateChanged(artifact, internalName, state.IsActive, minionList);
                CustomLogger.Log($"[DEBUG] Called ArtifactEffectTracker.OnArtifactStateChanged for '{internalName}' (active={state.IsActive})");
            }

            ApplyGlowEffect(artifact, state.IsActive);
        }

        public static bool TryGetArtifactAttributes(string artifactId, out Dictionary<string, float> attributes)
        {
            if (artifactAttributeMap != null && artifactAttributeMap.TryGetValue(artifactId, out attributes))
            {
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
            artifactAttributeMap = new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var configText = File.ReadAllText(ModInit.ArtifactPowersConfigPath);
                var configJson = JObject.Parse(configText);

                if (!(configJson["Artifacts"] is JArray arr))
                {
                    CustomLogger.Log("[ERROR] 'Artifacts' array missing or not an array in config JSON.");
                    return;
                }

                if (configJson.TryGetValue("GlobalRoomSizeMinimum", out var minToken) && minToken.Type == JTokenType.Integer)
                    globalRoomSizeMin = (int)minToken;
                if (configJson.TryGetValue("GlobalRoomSizeMaximum", out var maxToken) && maxToken.Type == JTokenType.Integer)
                    globalRoomSizeMax = (int)maxToken;
                if (configJson.TryGetValue("DecorMinimum", out var decorToken) && decorToken.Type == JTokenType.Integer)
                    decorMinimum = (int)decorToken;

                CustomLogger.Log($"[CONFIG] GlobalRoomSizeMinimum: {globalRoomSizeMin}");
                CustomLogger.Log($"[CONFIG] GlobalRoomSizeMaximum: {globalRoomSizeMax}");
                CustomLogger.Log($"[CONFIG] DecorMinimum: {decorMinimum}");

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
                CustomLogger.Log("[DEBUG] Loaded artifact attribute map and config values from config.");
            }
            catch (Exception ex)
            {
                CustomLogger.Log($"[ERROR] Failed to load artifact config: {ex}");
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
                float roomDecor = float.MinValue;
                bool meetsRoomSize = false;
                bool meetsDecor = false;

                if (Game.Instance != null && Game.Instance.roomProber != null)
                {
                    var cavity = Game.Instance.roomProber.GetCavityForCell(cell);
                    var room = cavity?.room;
                    if (room != null && room.cavity != null)
                    {
                        meetsRoomSize = room.cavity.numCells >= globalRoomSizeMin && room.cavity.numCells <= globalRoomSizeMax;

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
                        meetsDecor = roomDecor >= decorMinimum;
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