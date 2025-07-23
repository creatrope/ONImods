using Database;
using Epic.OnlineServices.Platform;
using HarmonyLib;
using HLib;
using KMod;
using KSerialization;
using Newtonsoft.Json; // Ensure this using directive is present
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
using static Rendering.BlockTileRenderer;
using Klei.AI; // <-- Add this using directive for AttributeModifiers
using System.Linq; // Already present, required for LINQ methods

namespace QOL
{
    internal sealed class MinimalKeybindHandler : IInputHandler
    {
        private static PAction KeyTestAction;
        private static PAction KeyTestAction2;
        private readonly Action snapshotAction;
        private readonly Action snapshotAction2;

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
                MoraleReportPatch.PrintMoraleReport();
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
                "QOL.KeyTestAction", "Test Key Action", new PKeyBinding(KKeyCode.F11, Modifier.Ctrl));
            KeyTestAction2 = new PActionManager().CreateAction(
                "QOL.KeyTestAction2", "Test Key Action 2", new PKeyBinding(KKeyCode.F12, Modifier.Ctrl));
        }
    }

    [HarmonyPatch(typeof(ReportManager), "OnNightTime")]
    public static class MoraleReportPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            PrintMoraleReport();
        }

        public static void PrintMoraleReport()
        {
            var minionIdentities = Components.MinionIdentities?.Items;
            if (minionIdentities == null)
                return;

            Debug.Log("YY[QOL] Morale Report for this cycle:");
            var moraleList = new List<(string Name, float Morale, float Expectation)>();
            foreach (var minion in minionIdentities)
            {
                var name = minion.GetProperName();
                var minionResume = minion.GetComponent<MinionResume>();
                float morale = 0f;
                float expectation = 0f;
                if (minionResume != null)
                {
                    var moraleInstance = Db.Get().Attributes.QualityOfLife.Lookup((UnityEngine.Component)minionResume);
                    var expectationInstance = Db.Get().Attributes.QualityOfLifeExpectation.Lookup((UnityEngine.Component)minionResume);
                    morale = moraleInstance != null ? moraleInstance.GetTotalValue() : 0f;
                    expectation = expectationInstance != null ? expectationInstance.GetTotalValue() : 0f;
                }
                moraleList.Add((name, morale, expectation));
            }

            moraleList.Sort((a, b) => b.Morale.CompareTo(a.Morale));

            int idx = 1;
            foreach (var entry in moraleList)
            {
                Debug.Log($"YY[QOL] #{idx++} {entry.Name}: Morale = {entry.Morale} / Expectation = {entry.Expectation}");
            }
        }
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll();
            PUtil.InitLibrary();
            MinimalKeybindHandler.Register(new PPatchManager(harmony));
        }
    }
}
