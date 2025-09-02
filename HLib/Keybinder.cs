using HarmonyLib;
using PeterHan.PLib.Actions;
using PeterHan.PLib.PatchManager;
using UnityEngine;
using PeterHan.PLib.Core;
using System.Collections.Generic;

namespace Keybinder
{
    public class KeybindDef
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public KKeyCode Key { get; set; }
        public Modifier Modifiers { get; set; }
        public System.Action Handler { get; set; }
        public PAction Action { get; set; }
    }

    public sealed class KeyInputHandler : IInputHandler
    {
        private readonly List<KeybindDef> _keybinds;

        public KeyInputHandler(List<KeybindDef> keybinds)
        {
            _keybinds = keybinds;
            var actionManager = new PActionManager();
            foreach (var def in _keybinds)
            {
                def.Action = actionManager.CreateAction(
                    def.Id, def.DisplayName, new PKeyBinding(def.Key, def.Modifiers));
            }
        }

        public string handlerName
        {
            get { return "KeyInputHandler"; }
        }
        public KInputHandler inputHandler { get; set; }

        public void OnKeyDown(KButtonEvent e)
        {
            foreach (var def in _keybinds)
            {
                if (e.TryConsume(def.Action.GetKAction()))
                {
                    def.Handler?.Invoke();
                }
            }
        }

        public void OnKeyUp(KButtonEvent e)
        {
        }

        [PLibMethod(RunAt.AfterLayerableLoad)]
        public static void AddKeycodeHandler()
        {
            if (Instance == null)
            {
                return;
            }
            KInputHandler.Add(Global.GetInputManager().GetDefaultController(), Instance, 512);
        }

        public static KeyInputHandler Instance;
        public static void Register(PPatchManager manager, List<KeybindDef> keybinds)
        {
            manager.RegisterPatchClass(typeof(KeyInputHandler));
            Instance = new KeyInputHandler(keybinds);
            // Do NOT call AddKeycodeHandler directly; PLib will call it after layerable load.
        }
    }
}