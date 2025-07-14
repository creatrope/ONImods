using Database;
using Epic.OnlineServices.Platform;
using HarmonyLib;
using HLib;
using KMod;
using KSerialization;
using Newtonsoft.Json; // Ensure this using directive is present
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using PeterHan.PLib.Actions;
using System;
using System.Collections.Generic; // For List<> and Dictionary<>
using System.Runtime.CompilerServices; // For ConditionalWeakTable
using TUNING;
using UnityEngine;
using static Rendering.BlockTileRenderer;

namespace KeybindLogTest
{
    internal sealed class MinimalKeybindHandler : IInputHandler
    {
        private static PAction KeyTestAction;
        private readonly Action snapshotAction;

        public string handlerName => "MinimalKeybindHandler";
        public KInputHandler inputHandler { get; set; }

        internal MinimalKeybindHandler()
        {
            var action = KeyTestAction;
            snapshotAction = action != null ? action.GetKAction() : PAction.MaxAction;
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (e.TryConsume(snapshotAction))
            {
                Debug.Log("[MinimalKeybindHandler] Hotkey pressed!");
            }
        }

        internal static void Register()
        {
            KeyTestAction = new PActionManager().CreateAction(
                "KeybindLogTest.KeyTestAction", "Test Key Action", new PKeyBinding(KKeyCode.F11, Modifier.Ctrl));
            KInputHandler.Add(Global.GetInputManager().GetDefaultController(), new MinimalKeybindHandler(), 512);
        }
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll();
            PUtil.InitLibrary();
            MinimalKeybindHandler.Register();
        }
    }
}
