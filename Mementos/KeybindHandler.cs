using PeterHan.PLib.Actions;
using PeterHan.PLib.PatchManager;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Mementos
{
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

        // Use System.Action for all references
        private static readonly List<Keybind> keybinds = new List<Keybind>
        {
            new Keybind("Medals.incapacitateAction", "Incapacitate", new PKeyBinding(KKeyCode.F5, Modifier.Ctrl), HandleIncapacitateHotkey),
            new Keybind("Medals.damageAction", "Damage", new PKeyBinding(KKeyCode.F4, Modifier.Ctrl), HandleDamageHotkey),
            new Keybind("Medals.eraseMedalsAction", "Erase All Medals", new PKeyBinding(KKeyCode.F6, Modifier.Ctrl), HandleEraseMedalsHotkey),
            new Keybind("Medals.printAllMementosAction", "Print All Mementos", new PKeyBinding(KKeyCode.F8, Modifier.Ctrl), HandlePrintAllMementosHotkey)
        };

        private float lastSnapshotTime = 0f;
        private readonly float debounceInterval = 1.0f; // seconds

        public string handlerName => "KeybindHandler";
        public KInputHandler inputHandler { get; set; }
        internal static MinionIdentity SelectedMinion;

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
            bool anyPressed = false;

            foreach (var kb in keybinds)
            {
                if (kb.Snapshot == null && kb.Action != null)
                    kb.Snapshot = kb.Action.GetKAction();

                if (e.TryConsume(kb.Snapshot))
                {
                    anyPressed = true;
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

        private static void HandleDamageHotkey()
        {
            Debug.Log("[OnKeyDown] Damage hotkey detected.");
            if (SelectedMinion != null)
            {
                var health = SelectedMinion.GetComponent<Health>();
                if (health != null)
                {
                    float damageAmount = 10f;
                    Debug.Log($"[OnKeyDown] (before health.Damage) Damaged '{SelectedMinion.GetProperName()}' for {damageAmount} HP via hotkey.");
                    health.Damage(damageAmount);
                    Debug.Log($"[OnKeyDown] (after health.Damage).");
                }
                else
                {
                    Debug.Log("[OnKeyDown] Health component not found on selected minion.");
                }
            }
            else
            {
                Debug.Log("[OnKeyDown] No minion selected.");
            }
        }

        private static void HandleIncapacitateHotkey()
        {
            Debug.Log("[OnKeyDown] Incapacitate hotkey detected.");
            if (SelectedMinion != null)
            {
                var health = SelectedMinion.GetComponent<Health>();
                if (health != null)
                {
                    Debug.Log($"[OnKeyDown] Health component found. Can be incapacitated: {health.canBeIncapacitated}, IsIncapacitated: {health.IsIncapacitated()}");
                    if (health.canBeIncapacitated && !health.IsIncapacitated())
                    {
                        health.Incapacitate(new Tag("ManualIncapacitate"));
                        Debug.Log($"[OnKeyDown] Incapacitated '{SelectedMinion.GetProperName()}' via hotkey.");
                        SelectedMinion = null;
                    }
                    else
                    {
                        Debug.Log("[OnKeyDown] Minion cannot be incapacitated or is already incapacitated.");
                    }
                }
                else
                {
                    Debug.Log("[OnKeyDown] Health component not found on selected minion.");
                }
            }
            else
            {
                Debug.Log("[OnKeyDown] No minion selected.");
            }
        }

        private static void HandleEraseMedalsHotkey()
        {
            Debug.Log("[EraseMedals] Erase medals hotkey pressed. Removing all MementoModifiable objects, medals, and minions.");

            // 1. Delete all objects with MementoModifiable component
            var mementos = UnityEngine.Object.FindObjectsOfType<Mementos.MementoModifiable>();
            foreach (var memento in mementos)
            {
                Debug.Log($"[EraseMedals] Destroying memento object: {memento.GetName()}");
                UnityEngine.Object.Destroy(memento.gameObject);
            }

            // 2. Clear awardedNonRepeatableMementos and Medals for all MedalInfo components
            var medalInfos = UnityEngine.Object.FindObjectsOfType<Mementos.MedalInfo>();
            foreach (var medalInfo in medalInfos)
            {
                // Clear awardedNonRepeatableMementos
                var field = typeof(Mementos.MedalInfo).GetField("awardedNonRepeatableMementos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var set = field.GetValue(medalInfo) as HashSet<string>;
                    if (set != null)
                    {
                        set.Clear();
                        Debug.Log("[EraseMedals] Cleared awardedNonRepeatableMementos for a MedalInfo.");
                    }
                }
                // Clear Medals list
                medalInfo.Medals.Clear();
                Debug.Log("[EraseMedals] Cleared Medals list for a MedalInfo.");
            }

            // 3. Delete all minions (MinionIdentity components)
            var minions = UnityEngine.Object.FindObjectsOfType<MinionIdentity>();
            foreach (var minion in minions)
            {
                Debug.Log($"[EraseMedals] Destroying minion: {minion.GetProperName()}");
                UnityEngine.Object.Destroy(minion.gameObject);
            }

            // 4. Clear any static or global state for "first to land on the planets"
            // Example (replace with your actual implementation):
            // Mementos.FirstToLandTracker.Clear();

            Debug.Log("[EraseMedals] All memento-related global state and minions cleared.");
        }

        private static void HandlePrintAllMementosHotkey()
        {
            Debug.Log("[PrintAllMementos] Print all mementos hotkey pressed.");
            var allMementos = UnityEngine.Object.FindObjectsOfType<Mementos.MementoModifiable>();
            foreach (var memento in allMementos)
            {
                Debug.Log($"Memento: {memento.GetName()} - {memento.GetDesc()}");
            }
        }
    }
}