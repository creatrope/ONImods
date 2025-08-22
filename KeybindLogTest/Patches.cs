using HarmonyLib;
using PeterHan.PLib.Actions;
using PeterHan.PLib.PatchManager;
using UnityEngine;
using PeterHan.PLib.Core; // Add this using directive

namespace KeybindLogTest
{
    internal static class KeybindActions
    {
        public static void OnF10() => Debug.Log("[KeybindLogTestX] CTRL+F10 pressed");
        public static void OnF11() => Debug.Log("[KeybindLogTestX] CTRL+F11 pressed");
        public static void OnF12() => Debug.Log("[KeybindLogTestX] CTRL+F12 pressed");
    }

    internal sealed class KeybindHandler : IInputHandler
    {
        private static PAction F10Action;
        private static PAction F11Action;
        private static PAction F12Action;

        public string handlerName => "KeybindHandler";
        public KInputHandler inputHandler { get; set; }

        public void OnKeyDown(KButtonEvent e)
        {
            Debug.Log($"[KeybindLogTest] KeyDown: {e.GetAction()} Modifiers: {e.Controller?.mActiveModifiers}");
            if (F10Action != null && e.TryConsume(F10Action.GetKAction()))
                KeybindActions.OnF10();
            if (F11Action != null && e.TryConsume(F11Action.GetKAction()))
                KeybindActions.OnF11();
            if (F12Action != null && e.TryConsume(F12Action.GetKAction()))
                KeybindActions.OnF12();
        }

        public void OnKeyUp(KButtonEvent e) { }

        [PLibMethod(RunAt.AfterLayerableLoad)]
        public static void AddKeycodeHandler()
        {
            KInputHandler.Add(Global.GetInputManager().GetDefaultController(),
                new KeybindHandler(), 512);
        }

        public static void Register(PPatchManager manager)
        {
            manager.RegisterPatchClass(typeof(KeybindHandler));
            var actionManager = new PActionManager();
            F10Action = actionManager.CreateAction(
            "KeybindLogTest.F10", "Test F10", new PKeyBinding(KKeyCode.F10, Modifier.Ctrl | Modifier.Shift));
            F12Action = actionManager.CreateAction(
                "KeybindLogTest.F12", "Test F12", new PKeyBinding(KKeyCode.F12, Modifier.Ctrl | Modifier.Shift));
            F11Action = actionManager.CreateAction(
                "KeybindLogTest.F11", "Test F11", new PKeyBinding(KKeyCode.F11, Modifier.Ctrl | Modifier.Shift));
        }
    }

    public class Mod : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary(false);
            base.OnLoad(harmony);
            KeybindHandler.Register(new PPatchManager(harmony));
            Debug.Log("[KeybindLogTest] loaded");
        }
    }
}