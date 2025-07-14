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
                KeybindLogTest.Patches.Logger.Log("[MinimalKeybindHandler] Hotkey pressed!");
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

    public class Patches
    {
        public static readonly CustomLogger Logger = new CustomLogger("KeybindLogTest");

        public static void OnLoad()
        {
            var options = POptions.ReadSettings<ModOptions>() ?? new ModOptions();
            Logger.SetLoggingEnabled(options.EnableCustomLog);
            Logger.Reset();
            Patches.Logger.Log("[KeybindLogTest] Mod loaded. Custom logging is " + (options.EnableCustomLog ? "enabled" : "disabled"));

            // Register the keybind handler here, after input system is ready
            MinimalKeybindHandler.Register();
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        public class Db_Initialize_Patch
        {
            public static void Prefix()
            {
            }

            public static void Postfix()
            {
            }
        }
    }

    public class ModOptions
    {
        [Option("Enable Custom Output Log", "Enable or disable writing the custom output log file.")]
        [JsonProperty] // Add JSON property for serialization
        public bool EnableCustomLog { get; set; } = true;
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            KeybindLogTest.Patches.OnLoad();
            base.OnLoad(harmony);

            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(ModOptions));
            harmony.PatchAll();
        }
    }
}
