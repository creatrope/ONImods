using PeterHan.PLib.Actions;
using PeterHan.PLib.PatchManager;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using Mementos;

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
            public bool LocalOnly; // Add this flag

            public Keybind(string id, string displayName, PKeyBinding binding, System.Action handler, bool localOnly = false)
            {
                Id = id;
                DisplayName = displayName;
                Binding = binding;
                Handler = handler;
                LocalOnly = localOnly;
            }
        }

        // Use System.Action for all references
        private static readonly List<Keybind> keybinds = new List<Keybind>
        {
            new Keybind("Medals.incapacitateAction", "Incapacitate", new PKeyBinding(KKeyCode.F5, Modifier.Ctrl), HandleIncapacitateHotkey, true),
            new Keybind("Medals.damageAction", "Damage", new PKeyBinding(KKeyCode.F4, Modifier.Ctrl), HandleDamageHotkey, true),
            new Keybind("Medals.eraseAllAction", "Erase All", new PKeyBinding(KKeyCode.F6, Modifier.Ctrl), HandleEraseAllHotkey, false),
            new Keybind("Medals.printAllMementosAction", "Print All Mementos", new PKeyBinding(KKeyCode.F8, Modifier.Ctrl), HandlePrintAllMementosHotkey, true),
            new Keybind("Medals.printDetailsScreens", "Print Details Screens", new PKeyBinding(KKeyCode.F9, Modifier.Ctrl), HandlePrintDetailsScreensHotkey, true),
            new Keybind("Medals.createMementoAction", "Create Memento", new PKeyBinding(KKeyCode.F7, Modifier.Ctrl), HandleCreateMementoHotkey, true),
            new Keybind("Medals.printIssuedMementosAction", "Print Issued Mementos", new PKeyBinding(KKeyCode.F10, Modifier.Ctrl), HandlePrintIssuedMementosHotkey, true),
        };

        private float lastSnapshotTime = 0f;
        private readonly float debounceInterval = 1.0f; // seconds

        public string handlerName => "KeybindHandler";
        public KInputHandler inputHandler { get; set; }
        internal static MinionIdentity SelectedMinion;

        private bool keyIsDown = false;

        public KeybindHandler()
        {
            bool isLocal = ModEnvironment.IsLocal();
            Debug.Log($"[Mementos] installed " + (isLocal ? "locally." : "via steamapp."));
            foreach (var kb in keybinds)
            {
                if (!kb.LocalOnly || isLocal)
                    kb.Snapshot = (kb.Action != null) ? kb.Action.GetKAction() : Action.Invalid;
                else
                    kb.Snapshot = Action.Invalid;
            }
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

        private static void HandleEraseAllHotkey()
        {
            Debug.Log("[EraseMedals] Erase all hotkey pressed. Removing all MementoModifiable objects, medals.");

            var mementos = UnityEngine.Object.FindObjectsOfType<Mementos.MementoModifiable>();
            Debug.Log($"[EraseMedals] Cleared {mementos.Length} mementos.");

            foreach (var memento in mementos)
                UnityEngine.Object.Destroy(memento.gameObject);

            var medalInfos = UnityEngine.Object.FindObjectsOfType<Mementos.MedalInfo>();
            Debug.Log("[EraseMedals] Cleared {medalInfos.Count} MedalInfo components(s).");

            foreach (var medalInfo in medalInfos)
                medalInfo.Medals.Clear();

            // 3. Delete all minions (MinionIdentity components)
            //var minions = UnityEngine.Object.FindObjectsOfType<MinionIdentity>();
            //foreach (var minion in minions)
            //{
            //    Debug.Log($"[EraseMedals] Destroying minion: {minion.GetProperName()}");
            //    UnityEngine.Object.Destroy(minion.gameObject);
            //}

            var globalData = Mementos.MementosGlobalData.Instance;

            if (globalData != null && globalData.Issued != null)
            {
                Debug.Log("[EraseMedals] Cleared {globalData.Issued.Count} state flag(s).");
                globalData.Issued.Clear();
            }
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

        private static void HandlePrintDetailsScreensHotkey()
        {
            Debug.Log("[Mementos] Print DetailsScreen side screens hotkey pressed.");
            var detailsScreen = DetailsScreen.Instance;
            if (detailsScreen != null && detailsScreen.sideScreens != null)
            {
                foreach (var sideScreenRef in detailsScreen.sideScreens)
                {
                    Debug.Log($"[Mementos] SideScreen: {sideScreenRef.name}, Prefab: {sideScreenRef.screenPrefab?.GetType().Name}");
                }
            }
            else
            {
                Debug.LogWarning("[Mementos] DetailsScreen.Instance or sideScreens is null.");
            }
        }

        private static void HandleTestLoadMedalsHotkey()
        {
        }

        private static void HandlePrintMedalsSaveDataHotkey()
        {
        }

        private static void PrintMedalsSaveDataHotkey()
        {

        }

        private static void HandleCreateMementoHotkey()
        {
            Debug.Log("[OnKeyDown] Create Memento hotkey detected.");
            if (SelectedMinion != null)
            {
                foreach (var kvp in MementoPrototypes.Mementos)
                {
                    var mementoData = kvp.Value;
                    MementoUtils.CreateMemento(mementoData, SelectedMinion, "");
                }
                Debug.Log($"[OnKeyDown] Created mementos for '{SelectedMinion.GetProperName()}' via hotkey.");
            }
            else
            {
                Debug.Log("[OnKeyDown] No minion selected.");
            }
        }

        private static void HandlePrintIssuedMementosHotkey()
        {
            Debug.Log("[PrintIssuedMementos] Print all true keys in Issued hotkey pressed.");
            var issued = MementosGlobalData.Instance?.Issued;
            if (issued == null)
            {
                Debug.LogWarning("[PrintIssuedMementos] Issued dictionary is null.");
                return;
            }
            foreach (var kvp in issued)
            {
                if (kvp.Value)
                    Debug.Log($"[PrintIssuedMementos] Key: {kvp.Key}");
            }
        }
    }

    // Add this class somewhere in your project, e.g., in a Utils or Settings file.
    internal static class ModEnvironment
    {
        public static bool IsLocal()
        {
            // Example: check if the mod is running from a local folder (not Steam)
            // You may need to adjust the logic based on your mod loader/environment.
            // This checks if the assembly location contains "steamapps" (Steam) or not.
            var location = typeof(ModEnvironment).Assembly.Location;
            return !location.ToLowerInvariant().Contains("steamapps");
        }
    }
}