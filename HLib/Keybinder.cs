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
            // Debug.Log("[Keybinder] KeyInputHandler constructor called.");
            _keybinds = keybinds;
            var actionManager = new PActionManager();
            foreach (var def in _keybinds)
            {
                // Debug.Log($"[Keybinder] Creating action for keybind: Id={def.Id}, DisplayName={def.DisplayName}, Key={def.Key}, Modifiers={def.Modifiers}");
                def.Action = actionManager.CreateAction(
                    def.Id, def.DisplayName, new PKeyBinding(def.Key, def.Modifiers));
            }
            // Debug.Log($"[Keybinder] Total keybinds registered: {_keybinds.Count}");
        }

        public string handlerName
        {
            get
            {
                // Debug.Log("[Keybinder] handlerName property accessed.");
                return "KeyInputHandler";
            }
        }
        public KInputHandler inputHandler { get; set; }

        public void OnKeyDown(KButtonEvent e)
        {
            // Debug.Log($"[Keybinder] OnKeyDown called. Button: {e?.GetAction()}, Event: {e}");
            bool anyMatched = false;
            foreach (var def in _keybinds)
            {
                // Debug.Log($"[Keybinder] Checking keybind: Id={def.Id}, Key={def.Key}, Modifiers={def.Modifiers}");
                if (e.TryConsume(def.Action.GetKAction()))
                {
                    // Debug.Log($"[Keybinder] Keybind matched and consumed: {def.Id} ({def.DisplayName})");
                    try
                    {
                        def.Handler?.Invoke();
                        // Debug.Log($"[Keybinder] Handler for {def.Id} invoked successfully.");
                    }
                    catch (System.Exception ex)
                    {
                        // Debug.LogError($"[Keybinder] Exception in handler for {def.Id}: {ex}");
                    }
                    anyMatched = true;
                }
            }
            if (!anyMatched)
            {
                // Debug.Log("[Keybinder] No keybinds matched for this key event.");
            }
        }

        public void OnKeyUp(KButtonEvent e)
        {
            // Debug.Log($"[Keybinder] OnKeyUp called. Button: {e?.GetAction()}, Event: {e}");
            foreach (var def in _keybinds)
            {
                // Debug.Log($"[Keybinder] KeyUp for keybind: Id={def.Id}, Key={def.Key}, Modifiers={def.Modifiers}");
            }
        }

        [PLibMethod(RunAt.AfterLayerableLoad)]
        public static void AddKeycodeHandler()
        {
            // Debug.Log("[Keybinder] AddKeycodeHandler called.");
            if (Instance == null)
            {
                // Debug.LogWarning("[Keybinder] Instance is null in AddKeycodeHandler.");
                return;
            }
            // Debug.Log("[Keybinder] Registering KeyInputHandler with KInputHandler.");
            KInputHandler.Add(Global.GetInputManager().GetDefaultController(), Instance, 512);
        }

        public static KeyInputHandler Instance;
        public static void Register(PPatchManager manager, List<KeybindDef> keybinds)
        {
            // Debug.Log("[Keybinder] Register called.");
            manager.RegisterPatchClass(typeof(KeyInputHandler));
            Instance = new KeyInputHandler(keybinds);
            // Debug.Log("[Keybinder] KeyInputHandler instance created and registered.");
            // Do NOT call AddKeycodeHandler directly; PLib will call it after layerable load.
        }
    }
}