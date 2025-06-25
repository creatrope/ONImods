using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json.Linq;
using PeterHan.PLib.Core;
using HLib;
using Object = UnityEngine.Object;

namespace ArtifactsPlus
{
    public static class ModInit
    {
        public static string DesktopLogPath => HLib.CustomLogger.LogPath;

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
            if (ArtifactsPlusOptions.Instance.Verbose)
            {
                HLib.CustomLogger.Log($"[ArtifactsPlus] Custom log file target location: {DesktopLogPath}");
            }
            ArtifactStateTracker.LoadArtifactAttributeMap();
        }
    }

    public class ArtifactState
    {
        public bool OnPedestal;
        public bool MeetsRoomSize;
        public bool IsActive;
        public bool IsAnalyzed;
    }

    public class ArtifactConfig
    {
        public int RoomSizeMin;
        public int RoomSizeMax;
        public int DecorMinimum;
        public int Neighbors;
        public string Scope;
        public Dictionary<string, float> Attributes;
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
            if (artifact == null)
            {
                HLib.CustomLogger.Log("[ERROR] Artifact is null in EvaluateArtifactCriteria.");
                return new ArtifactCriteriaResult
                {
                    MeetsAll = false,
                    ShortCircuited = true
                };
            }

            if (artifact.transform == null)
            {
                HLib.CustomLogger.Log($"[ERROR] Artifact '{artifact.name}' has a null transform in EvaluateArtifactCriteria.");
                return new ArtifactCriteriaResult
                {
                    MeetsAll = false,
                    ShortCircuited = true
                };
            }

            // Debug log for artifact evaluation start
            HLib.CustomLogger.Log($"[DEBUG] Evaluating criteria for artifact: {artifact.name}");

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

            isEntombed = Grid.Element[cell].id == SimHashes.Unobtanium;
            if (shortCircuit && isEntombed)
            {
                meetsAll = false;
                wasShortCircuited = true;
            }

            if (meetsAll && Game.Instance != null && Game.Instance.roomProber != null)
            {
                var cavity = Game.Instance.roomProber.GetCavityForCell(cell);
                var room = cavity?.room;
                if (room != null && room.cavity != null)
                {
                    actualRoomSize = room.cavity.numCells;
                    meetsRoomSize = actualRoomSize >= config.RoomSizeMin && actualRoomSize <= config.RoomSizeMax;
                    if (shortCircuit && !meetsRoomSize)
                    {
                        meetsAll = false;
                        wasShortCircuited = true;
                    }

                    if (meetsAll)
                    {
                        artifactCount = CountArtifactsOnPedestals(room);
                        neighborsOk = artifactCount <= config.Neighbors;
                        if (shortCircuit && !neighborsOk)
                        {
                            meetsAll = false;
                            wasShortCircuited = true;
                        }
                    }
                }
            }

            if (!shortCircuit)
                meetsAll = meetsRoomSize && meetsDecor && !isEntombed && neighborsOk;

            // Debug log for evaluation result
            HLib.CustomLogger.Log($"[DEBUG] Evaluation result for artifact {artifact.name}: MeetsAll={meetsAll}");

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

        public static void LoadArtifactAttributeMap()
        {
            artifactConfigMap = new Dictionary<string, ArtifactConfig>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var configText = File.ReadAllText(ModInit.ArtifactPowersConfigPath);
                var configJson = JObject.Parse(configText);

                HLib.CustomLogger.Log("[ArtifactsPlus] Loading artifact configuration...");

                foreach (var artifact in configJson)
                {
                    var artifactConfig = artifact.Value.ToObject<ArtifactConfig>();
                    artifactConfigMap[artifact.Key] = artifactConfig;
                }

                HLib.CustomLogger.Log("[ArtifactsPlus] Artifact configuration loaded successfully.");
            }
            catch (Exception ex)
            {
                HLib.CustomLogger.Log($"[ERROR] Failed to load artifact config: {ex}");
            }
        }

        public static void PollAllArtifacts()
        {
            foreach (var artifact in ArtifactsOnPedestals)
            {
                if (artifact != null)
                {
                    var artifactState = ArtifactStates[artifact.GetInstanceID()];
                    HLib.CustomLogger.Log($"[DEBUG] Polling artifact: {artifact.name}, State: {artifactState}");
                    // Update artifact state logic here
                }
                else
                {
                    HLib.CustomLogger.Log("[WARN] Null artifact encountered during PollAllArtifacts.");
                }
            }
        }

        private static int CountArtifactsOnPedestals(Room room)
        {
            return room.cavity.artifacts.Count(a => ArtifactsOnPedestals.Contains(a));
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
}