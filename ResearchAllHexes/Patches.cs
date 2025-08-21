using Database;
using HarmonyLib;
using HLib;
using KMod;
using KSerialization;
using Newtonsoft.Json;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TUNING;
using UnityEngine;

namespace ResearchAllHexes
{

    public class Mod : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Debug.Log("[ResearchAllHexes] ResearchAllHexes mod loaded.");

            base.OnLoad(harmony);
            harmony.PatchAll();
            PUtil.InitLibrary();
        }
        private static void RevealAllClusterHexes()
        {
            Debug.Log("[ResearchAllHexes] RevealAllClusterHexes called.");

            if (SaveGame.Instance == null)
            {
                Debug.Log("[ResearchAllHexes] SaveGame.Instance is null.");
                return;
            }
            var fogManager = SaveGame.Instance.GetSMI<ClusterFogOfWarManager.Instance>();
            if (fogManager == null)
            {
                Debug.Log("[ResearchAllHexes] fogManager is null.");
                return;
            }
            var clusterGrid = ClusterGrid.Instance;
            if (clusterGrid == null)
            {
                Debug.Log("[ResearchAllHexes] ClusterGrid.Instance is null.");
                return;
            }
            if (clusterGrid.cellContents == null)
            {
                Debug.Log("[ResearchAllHexes] clusterGrid.cellContents is null.");
                return;
            }

            Debug.Log($"[ResearchAllHexes] clusterGrid.cellContents.Keys.Count = {clusterGrid.cellContents.Keys.Count}");

            foreach (var cell in clusterGrid.cellContents.Keys)
            {
                fogManager.RevealLocation(cell, peekRadius: 10);
            }
            Debug.Log("[ResearchAllHexes] All hexes revealed.");
        }

        private static void AnalyzeAllSpaceDestinations()
        {
            Debug.Log("[ResearchAllHexes] AnalyzeAllSpaceDestinations call.");

            var manager = SpacecraftManager.instance;
            if (manager == null)
            {
                Debug.Log("[ResearchAllHexes] SpacecraftManager.instance is null.");
                return;
            }
            if (manager.destinations == null)
            {
                Debug.Log("[ResearchAllHexes] manager.destinations is null.");
                return;
            }

            Debug.Log($"[ResearchAllHexes] manager.destinations.Count = {manager.destinations.Count}");

            foreach (var destination in manager.destinations)
            {
                manager.EarnDestinationAnalysisPoints(destination.id, (float)TUNING.ROCKETRY.DESTINATION_ANALYSIS.COMPLETE);
            }
            Debug.Log("[ResearchAllHexes] All space destinations fully analyzed.");
        }

        private static bool AllDestinationsRevealed()
        {
            var manager = SpacecraftManager.instance;
            if (manager == null || manager.destinations == null)
                return false;

            // Check if all destinations are fully analyzed (revealed)
            foreach (var destination in manager.destinations)
            {
                if (manager.GetDestinationAnalysisState(destination) != SpacecraftManager.DestinationAnalysisState.Complete)
                    return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Game), "OnSpawn")]
        public static class OnSpawnPatch
        {
            public static void Postfix()
            {
                Game.Instance.StartCoroutine(WaitForClusterGridAndReveal());
                Game.Instance.StartCoroutine(WaitForDestinationsAndAnalyze());
            }

            private static System.Collections.IEnumerator WaitForClusterGridAndReveal()
            {
                while (ClusterGrid.Instance == null)
                    yield return null;
                ResearchAllHexes.Mod.RevealAllClusterHexes();
            }

            private static System.Collections.IEnumerator WaitForDestinationsAndAnalyze()
            {
                var manager = SpacecraftManager.instance;
                while (manager == null || manager.destinations == null)
                {
                    yield return null;
                    manager = SpacecraftManager.instance;
                }
                ResearchAllHexes.Mod.AnalyzeAllSpaceDestinations();
            }
        }
    }
}
