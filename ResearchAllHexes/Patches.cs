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
    internal sealed class MinimalKeybindHandler : IInputHandler
    {
        private static PAction KeyTestAction;
        private static PAction KeyTestAction2; // Add second action
        private readonly Action snapshotAction;
        private readonly Action snapshotAction2; // Add second action field

        public string handlerName => "MinimalKeybindHandler";
        public KInputHandler inputHandler { get; set; }

        internal MinimalKeybindHandler()
        {
            snapshotAction = KeyTestAction != null ? KeyTestAction.GetKAction() : PAction.MaxAction;
            snapshotAction2 = KeyTestAction2 != null ? KeyTestAction2.GetKAction() : PAction.MaxAction;
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (e.TryConsume(snapshotAction))
            {
                Debug.Log("[MinimalKeybindHandler] Hotkey 1 pressed!");
            }
            else if (e.TryConsume(snapshotAction2))
            {
                Debug.Log("[MinimalKeybindHandler] Hotkey 2 pressed!");
            }
        }

        [PLibMethod(RunAt.AfterLayerableLoad)]
        internal static void AddKeycodeHandler()
        {
            KInputHandler.Add(Global.GetInputManager().GetDefaultController(),
                new MinimalKeybindHandler(), 512);
        }

        internal static void Register(PPatchManager manager)
        {
            manager.RegisterPatchClass(typeof(MinimalKeybindHandler));
            KeyTestAction = new PActionManager().CreateAction(
                "ResearchAllHexes.KeyTestAction", "Test Key Action", new PKeyBinding(KKeyCode.F11, Modifier.Ctrl));
            KeyTestAction2 = new PActionManager().CreateAction(
                "ResearchAllHexes.KeyTestAction2", "Test Key Action 2", new PKeyBinding(KKeyCode.F12, Modifier.Ctrl));
        }
    }

    public class Mod : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Debug.Log("[ResearchAllHexes] ResearchAllHexes mod loaded.");

            base.OnLoad(harmony);
            harmony.PatchAll();
            PUtil.InitLibrary();
            MinimalKeybindHandler.Register(new PPatchManager(harmony));
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
