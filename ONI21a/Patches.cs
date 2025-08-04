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

namespace ONI21a
{
    // Add this class to provide options for the FewOptionSideScreen
    public class EspressoMachineFewOptions : KMonoBehaviour, FewOptionSideScreen.IFewOptionSideScreen
    {
        private static readonly FewOptionSideScreen.IFewOptionSideScreen.Option[] options = new[]
        {
            new FewOptionSideScreen.IFewOptionSideScreen.Option
            {
                tag = new Tag("Option1"),
                labelText = "Option 1",
                tooltipText = "First option",
                iconSpriteColorTuple = new Tuple<UnityEngine.Sprite, UnityEngine.Color>(null, UnityEngine.Color.white)
            },
            new FewOptionSideScreen.IFewOptionSideScreen.Option
            {
                tag = new Tag("Option2"),
                labelText = "Option 2",
                tooltipText = "Second option",
                iconSpriteColorTuple = new Tuple<UnityEngine.Sprite, UnityEngine.Color>(null, UnityEngine.Color.white)
            }
        };

        private Tag selectedOption = options[0].tag;

        public FewOptionSideScreen.IFewOptionSideScreen.Option[] GetOptions() => options;

        public void OnOptionSelected(FewOptionSideScreen.IFewOptionSideScreen.Option option)
        {
            selectedOption = option.tag;
            Debug.Log("[ONI21a] EspressoMachineFewOptions: Selected " + option.labelText);
        }

        public Tag GetSelectedOption() => selectedOption;
    }

    [HarmonyPatch(typeof(EspressoMachineConfig), "ConfigureBuildingTemplate")]
    public static class EspressoMachineConfig_ConfigureBuildingTemplate_FewOptionsPatch
    {
        static void Postfix(GameObject go, Tag prefab_tag)
        {
            // Attach the FewOption side screen component
            go.AddOrGet<EspressoMachineFewOptions>();
            Debug.Log("[ONI21a] Added FewOptionSideScreen to EspressoMachine.");
        }
    }

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
                    "ONI21a.KeyTestAction", "Test Key Action", new PKeyBinding(KKeyCode.F11, Modifier.Ctrl));
                KeyTestAction2 = new PActionManager().CreateAction(
                    "ONI21a.KeyTestAction2", "Test Key Action 2", new PKeyBinding(KKeyCode.F12, Modifier.Ctrl));
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
