using HarmonyLib;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using System;
using System.Collections.Generic;
using UnityEngine;
using TestSerialize;

namespace KeybindLogTest
{
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

    internal sealed class KeybindHandler : IInputHandler
    {
        private class Keybind
        {
            public string Id;
            public string DisplayName;
            public PKeyBinding Binding;
            public System.Action Handler;
            public PAction Action;
            public Action Snapshot;

            public Keybind(string id, string displayName, PKeyBinding binding, System.Action handler)
            {
                Id = id;
                DisplayName = displayName;
                Binding = binding;
                Handler = handler;
            }
        }

        private static readonly List<Keybind> keybinds = new List<Keybind>
        {
            new Keybind("TestF1", "Test F1", new PKeyBinding(KKeyCode.F1, Modifier.Ctrl), OnF1),
            new Keybind("TestF2", "Test F2", new PKeyBinding(KKeyCode.F2, Modifier.Ctrl), OnF2)
        };

        private float lastSnapshotTime = 0f;
        private readonly float debounceInterval = 1.0f; // seconds

        public string handlerName => "KeybindHandler";
        public KInputHandler inputHandler { get; set; }

        private bool keyIsDown = false;

        public KeybindHandler()
        {
            foreach (var kb in keybinds)
                kb.Snapshot = (kb.Action != null) ? kb.Action.GetKAction() : PAction.MaxAction;
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (keyIsDown)
                return; // Ignore repeated keydown until keyup

            keyIsDown = true;

            float now = Time.time;

            foreach (var kb in keybinds)
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
                KInputHandler.Add(Global.GetInputManager().GetDefaultController(),
                    new KeybindHandler(), 512);
                handlerRegistered = true;
            }
        }

        public static void Register(PPatchManager manager)
        {
            manager.RegisterPatchClass(typeof(KeybindHandler));
            foreach (var kb in keybinds)
            {
                kb.Action = new PActionManager().CreateAction(kb.Id, kb.DisplayName, kb.Binding);
                kb.Snapshot = kb.Action.GetKAction();
            }
        }

        // --- SUPPORT FUNCTIONS BELOW ---

        private static void OnF1()
        {
            var instance = UnityEngine.Object.FindObjectOfType<TestSerialize.TestSerialize.TestData>();
            if (instance != null)
            {
                Debug.Log("[KeybindLogTest] onF1 clearing & creating test data (0-9) into testHashSet.");
                instance.LoadTestData();
                instance.PrintTestData();
            }
            else
            {
                Debug.LogWarning("[KeybindLogTest] TestData instance is null (cannot load test data).");
            }
        }

        private static void OnF2()
        {
            var instance = UnityEngine.Object.FindObjectOfType<TestSerialize.TestSerialize.TestData>();
            if (instance != null)
            {
                Debug.Log("[KeybindLogTest] OnF2 printing saved data");
                instance.PrintTestData();
            }
            else
                Debug.LogWarning("[KeybindLogTest] TestData instance is null (cannot print test data).");
        }
    }

    [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.OnPrefabInit))]
    public static class SaveGameOnSpawnTestData
    {
        public static void Postfix(Game __instance)
        {
            if (__instance.GetComponent<TestSerialize.TestSerialize.TestData>() == null)
            {
                __instance.gameObject.AddComponent<TestSerialize.TestSerialize.TestData>();
                Debug.Log("[KeybindLogTest] TestData component added to SaveGame object (OnPrefabInit).");
            }
        }
    }
}