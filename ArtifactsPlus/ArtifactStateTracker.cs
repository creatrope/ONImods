using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace ArtifactsPlus
{
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

        public static void LoadArtifactAttributeMap()
        {
            // ...existing implementation...
        }

        public static void PollAllArtifacts()
        {
            // ...existing implementation...
        }

        public static void RegisterArtifactOnPedestal(GameObject artifact)
        {
            // ...existing implementation...
        }

        public static void UnregisterArtifactOnPedestal(GameObject artifact)
        {
            // ...existing implementation...
        }

        public static ArtifactConfig GetArtifactConfig(string artifactId)
        {
            if (artifactConfigMap != null && artifactConfigMap.TryGetValue(artifactId, out var config))
                return config;

            // Return a config using global values if not found
            return new ArtifactConfig(globalRoomSizeMin, globalRoomSizeMax, decorMinimum, "All");
        }

        // ...All methods from ArtifactStateTracker in Patches.cs...
        // (Copy all methods: EvaluateArtifactCriteria, RegisterArtifactOnPedestal, UnregisterArtifactOnPedestal, GetAllMinions, GetMinionsInSameRoom, GetMinionsInSameWorld, UpdateArtifactState, TryGetArtifactAttributes, TryGetArtifactEffects, ApplyGlowEffect, LoadArtifactAttributeMap, GetArtifactConfig, RemoveArtifact, PollAllArtifacts, CountArtifactsOnPedestalsInRoom)
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