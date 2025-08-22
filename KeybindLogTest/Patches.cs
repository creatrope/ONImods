using HarmonyLib;
using PeterHan.PLib.Actions;
using PeterHan.PLib.PatchManager;
using UnityEngine;
using PeterHan.PLib.Core;

namespace KeybindLogTest
{
    internal static class KeybindActions
    {
        public static void OnKeyA() => Debug.Log("[Keybind4] A combo pressed");
        public static void OnKeyB() => Debug.Log("[Keybind4] B combo pressed");
        public static void OnKeyC() => Debug.Log("[Keybind4] C combo pressed");
    }

    public static class ActionKeys
    {
        public static readonly string KeyA = "KeybindLogTest.KeyA";
        public static readonly string KeyB = "KeybindLogTest.KeyB";
        public static readonly string KeyC = "KeybindLogTest.KeyC";
    }

    public static class Actions
    {
        public static PAction KeyAAction { get; set; }
        public static PAction KeyBAction { get; set; }
        public static PAction KeyCAction { get; set; }
    }

    public static class KeybindConfig
    {
        public static readonly KKeyCode KeyA = KKeyCode.F7;
        public static readonly KKeyCode KeyB = KKeyCode.F8;
        public static readonly KKeyCode KeyC = KKeyCode.F9;
    }

    internal sealed class KeybindHandler : IInputHandler
    {
        static KeybindHandler()
        {
            var myModifier = Modifier.Shift | Modifier.Ctrl;

            Actions.KeyAAction = new PActionManager().CreateAction(
                ActionKeys.KeyA, "Test KeyA", new PKeyBinding(KeybindConfig.KeyA, myModifier));
            Actions.KeyBAction = new PActionManager().CreateAction(
                ActionKeys.KeyB, "Test KeyB", new PKeyBinding(KeybindConfig.KeyB, myModifier));
            Actions.KeyCAction = new PActionManager().CreateAction(
                ActionKeys.KeyC, "Test KeyC", new PKeyBinding(KeybindConfig.KeyC, myModifier));

            if (Actions.KeyAAction != null && Actions.KeyBAction != null && Actions.KeyCAction != null)
                Debug.Log("[KeybindLogTest] Successfully created keybind actions (static ctor)");
            else
                Debug.LogError("[KeybindLogTest] Failed to create one or more keybind actions (static ctor)");
        }

        public string handlerName => "KeybindHandler";
        public KInputHandler inputHandler { get; set; }

        public void OnKeyDown(KButtonEvent e)
        {
            Debug.Log($"[KeybindLogTest] KeyDown: {e.GetAction()} Modifiers: {e.Controller?.mActiveModifiers}");
            Debug.Log("[KeybindLogTest] Trying KeyCAction");
            if (e.TryConsume(Actions.KeyCAction.GetKAction()))
            {
                Debug.Log("[KeybindLogTest] Calling OnKeyC()");
                KeybindActions.OnKeyC();
            }
            Debug.Log("[KeybindLogTest] Trying KeyAAction");
            if (e.TryConsume(Actions.KeyAAction.GetKAction()))
            {
                Debug.Log("[KeybindLogTest] Calling OnKeyA()");
                KeybindActions.OnKeyA();
            }
            Debug.Log("[KeybindLogTest] Trying KeyBAction");
            if (e.TryConsume(Actions.KeyBAction.GetKAction()))
            {
                Debug.Log("[KeybindLogTest] Calling OnKeyB()");
                KeybindActions.OnKeyB();
            }
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
        }
    }

    public class Mod : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            KeybindHandler.Register(new PPatchManager(harmony));
        }
    }
}