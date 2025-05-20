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

namespace ArtifactsPlus
{
    public static class ModInit
    {
        public static readonly string DesktopLogPath =
            Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "ArtifactsPlus.log");

        private static bool _logInitialized = false;

        public static void CustomLog(string message)
        {
            if (!_logInitialized)
            {
                File.WriteAllText(DesktopLogPath, string.Empty);
                _logInitialized = true;
            }
            string timestamped = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            using (var writer = new StreamWriter(DesktopLogPath, true, System.Text.Encoding.UTF8))
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

            ApplyGlowEffect(artifact, state.IsActive);
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
                    light2D.overlayColour = new Color(1f, 1f, 1f, 0.2f); // Replace LIGHT2D.FLOORLAMP_OVERLAYCOLOR
                    light2D.Color = Color.yellow; // Replace LIGHT2D.FLOORLAMP_COLOR
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
                    ModInit.CustomLog("[ERROR] 'Artifacts' array missing or not an array in config JSON.");
                    return;
                }

                if (configJson.TryGetValue("GlobalRoomSizeMinimum", out var minToken) && minToken.Type == JTokenType.Integer)
                    globalRoomSizeMin = (int)minToken;
                if (configJson.TryGetValue("GlobalRoomSizeMaximum", out var maxToken) && maxToken.Type == JTokenType.Integer)
                    globalRoomSizeMax = (int)maxToken;
                if (configJson.TryGetValue("DecorMinimum", out var decorToken) && decorToken.Type == JTokenType.Integer)
                    decorMinimum = (int)decorToken;

                ModInit.CustomLog($"[CONFIG] GlobalRoomSizeMinimum: {globalRoomSizeMin}");
                ModInit.CustomLog($"[CONFIG] GlobalRoomSizeMaximum: {globalRoomSizeMax}");
                ModInit.CustomLog($"[CONFIG] DecorMinimum: {decorMinimum}");

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
                ModInit.CustomLog("[DEBUG] Loaded artifact attribute map and config values from config.");
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
                float roomDecor = float.MinValue;
                bool meetsRoomSize = false;
                bool meetsDecor = false;

                if (Game.Instance != null && Game.Instance.roomProber != null)
                {
                    var cavity = Game.Instance.roomProber.GetCavityForCell(cell);
                    var room = cavity?.room;
                    if (room != null && room.cavity != null)
                    {
                        roomSize = room.cavity.numCells;
                        meetsRoomSize = roomSize >= globalRoomSizeMin && roomSize <= globalRoomSizeMax;

                        // Calculate average decor in the room
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

                // Artifact is active only if both room size and decor requirements are met
                bool meetsAll = meetsRoomSize && meetsDecor;
                if (!meetsAll && meetsRoomSize && !meetsDecor)
                {
                    ModInit.CustomLog($"[DEBUG] Artifact '{artifact.name}' inactive due to decor: {roomDecor} (minimum required: {decorMinimum})");
                }
                else if (meetsAll)
                {
                    ModInit.CustomLog($"[DEBUG] Artifact '{artifact.name}' active with decor: {roomDecor}");
                }
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