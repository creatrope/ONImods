using HarmonyLib;
using PeterHan.PLib.Actions;
using PeterHan.PLib.PatchManager;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Keybinds.Keybinder;

namespace Keybinds
{

    internal static class ModEnvironment
    {
        public static bool IsLocal()
        {
            var location = typeof(ModEnvironment).Assembly.Location;
            return !location.ToLowerInvariant().Contains("steamapps");
        }
    }

    internal sealed class Keybinder : IInputHandler
    {

        internal enum EnvType
        {
            Release,
            Debug
        }

        internal class Keybind
        {
            public string Id;
            public string DisplayName;
            public PKeyBinding Binding;
            public System.Action Handler;
            public PAction Action;
            public Action Snapshot;
            public EnvType Env;

            public Keybind(string id, string displayName, PKeyBinding binding, System.Action handler, EnvType env = EnvType.Release)
            {
                Id = id;
                DisplayName = displayName;
                Binding = binding;
                Handler = handler;
                Env = env;
            }
        }

        // Change from readonly to allow registration at runtime
        public static List<Keybind> Keybinds { get; } = new List<Keybind>();

        private float lastSnapshotTime = 0f;
        private readonly float debounceInterval = 1.0f; // seconds

        public string handlerName => "KeybindsHandler";
        public KInputHandler inputHandler { get; set; }

        private bool keyIsDown = false;

        public Keybinder(List<Keybind> keybinds)
        {
            if (keybinds == null)
                throw new ArgumentNullException(nameof(keybinds));

            // Use a local instance instead of assigning to the static readonly field
            foreach (var kb in keybinds)
            {
                if (kb.Env == EnvType.Release || (kb.Env == EnvType.Debug && ModEnvironment.IsLocal()))
                    kb.Snapshot = (kb.Action != null) ? kb.Action.GetKAction() : Action.Invalid;
                else
                    kb.Snapshot = Action.Invalid;
            }
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (keyIsDown)
                return;

            keyIsDown = true;

            float now = Time.time;
            foreach (var kb in Keybinds) // Use the static field 'Keybinds' instead of 'keybinds'
            {
                if (kb.Snapshot == null && kb.Action != null)
                    kb.Snapshot = kb.Action.GetKAction();

                if (e.TryConsume(kb.Snapshot))
                {
                    if (now - lastSnapshotTime >= debounceInterval)
                    {
                        lastSnapshotTime = now;
                        kb.Handler?.Invoke();
                    }
                    break;
                }
            }
        }

        public void OnKeyUp(KButtonEvent e)
        {
            keyIsDown = false;
        }

        private static bool handlerRegistered = false;

        [PLibMethod(RunAt.AfterLayerableLoad)]
        public static void AddKeycodeHandler()
        {
            if (!handlerRegistered)
            {
                var keybinds = new List<Keybind>(); // Provide an empty list or populate it as needed
                KInputHandler.Add(Global.GetInputManager().GetDefaultController(),
                    new Keybinder(keybinds), 512);
                handlerRegistered = true;
            }
        }
        public static void Register(PPatchManager manager)
        {
            manager.RegisterPatchClass(typeof(Keybinder));
            var keybinds = new List<Keybind>(); // Create a local instance of keybinds
            foreach (var kb in keybinds)
            {
                kb.Action = new PActionManager().CreateAction(kb.Id, kb.DisplayName, kb.Binding);
                kb.Snapshot = kb.Action.GetKAction();
            }
        }

        // Add a registration method
        public static void RegisterKeybind(Keybind keybind)
        {
            if (keybind != null)
                Keybinds.Add(keybind);
        }
    }
}