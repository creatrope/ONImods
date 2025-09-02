using HarmonyLib;
using PeterHan.PLib.Actions;
using PeterHan.PLib.PatchManager;
using UnityEngine;
using PeterHan.PLib.Core;
using System.Collections.Generic;
using HLib;

namespace KeyBindLogTest
{
    internal static class HotKeys
    {
        public static void OnA() => Debug.Log("[Keybind4] A combo pressed");
        public static void OnB() => Debug.Log("[Keybind4] B combo pressed");
        public static void OnC() => Debug.Log("[Keybind4] C combo pressed");

        // Reference KeybindDef from Keybinder namespace (now in Patches.cs)
        public static readonly List<Keybinder.KeybindDef> All = new List<Keybinder.KeybindDef>
        {
            new Keybinder.KeybindDef { Id = "KeybindLogTest.KeyA", DisplayName = "Test KeyA", Key = KKeyCode.F7, Modifiers = Modifier.Shift | Modifier.Ctrl, Handler = OnA },
            new Keybinder.KeybindDef { Id = "KeybindLogTest.KeyB", DisplayName = "Test KeyB", Key = KKeyCode.F8, Modifiers = Modifier.Shift | Modifier.Ctrl, Handler = OnB },
            new Keybinder.KeybindDef { Id = "KeybindLogTest.KeyC", DisplayName = "Test KeyC", Key = KKeyCode.F9, Modifiers = Modifier.Shift | Modifier.Ctrl, Handler = OnC }
        };
    }

}