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
using System.Collections.Generic; // For List<> and Dictionary<>
using System.Runtime.CompilerServices; // For ConditionalWeakTable
using TUNING;
using UnityEngine;
using static Rendering.BlockTileRenderer;
using static STRINGS.CODEX.CRITTERSTATUS.FERTILITY;

namespace KeybindLogTest
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
                "KeybindLogTest.KeyTestAction", "Test Key Action", new PKeyBinding(KKeyCode.F11, Modifier.Ctrl));
            KeyTestAction2 = new PActionManager().CreateAction(
                "KeybindLogTest.KeyTestAction2", "Test Key Action 2", new PKeyBinding(KKeyCode.F12, Modifier.Ctrl));
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
