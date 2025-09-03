using HarmonyLib;
using PeterHan.PLib.Actions;
using PeterHan.PLib.PatchManager;
using UnityEngine;
using PeterHan.PLib.Core;
using System.Collections.Generic;
using HLib;

namespace Mementos
{
    internal static class HotKeys
    {
        public static void OnIncapacitate() => HandleIncapacitateHotkey();
        public static void OnDamage() => HandleDamageHotkey();
        public static void OnEraseAll() => HandleEraseAllHotkey();
        public static void OnPrintAllMementos() => HandlePrintAllMementosHotkey();
        public static void OnPrintDetailsScreens() => HandlePrintDetailsScreensHotkey();
        public static void OnCreateMemento() => HandleCreateMementoHotkey();
        public static void OnPrintIssuedMementos() => HandlePrintIssuedMementosHotkey();

        public static readonly List<Keybinder.KeybindDef> All = new List<Keybinder.KeybindDef>
        {
            new Keybinder.KeybindDef { Id = "Mementos.Incapacitate", DisplayName = "Incapacitate Minion", Key = KKeyCode.F7, Modifiers = Modifier.Shift | Modifier.Ctrl, Handler = OnIncapacitate },
            new Keybinder.KeybindDef { Id = "Mementos.Damage", DisplayName = "Damage Minion", Key = KKeyCode.F8, Modifiers = Modifier.Shift | Modifier.Ctrl, Handler = OnDamage },
            new Keybinder.KeybindDef { Id = "Mementos.EraseAll", DisplayName = "Erase All Mementos", Key = KKeyCode.F9, Modifiers = Modifier.Shift | Modifier.Ctrl, Handler = OnEraseAll },
            new Keybinder.KeybindDef { Id = "Mementos.PrintAllMementos", DisplayName = "Print All Mementos", Key = KKeyCode.F10, Modifiers = Modifier.Shift | Modifier.Ctrl, Handler = OnPrintAllMementos },
            new Keybinder.KeybindDef { Id = "Mementos.PrintDetailsScreens", DisplayName = "Print Details Screens", Key = KKeyCode.F11, Modifiers = Modifier.Shift | Modifier.Ctrl, Handler = OnPrintDetailsScreens },
            new Keybinder.KeybindDef { Id = "Mementos.CreateMemento", DisplayName = "Create Memento", Key = KKeyCode.F12, Modifiers = Modifier.Shift | Modifier.Ctrl, Handler = OnCreateMemento },
            new Keybinder.KeybindDef { Id = "Mementos.PrintIssuedMementos", DisplayName = "Print Issued Mementos", Key = KKeyCode.F6, Modifiers = Modifier.Shift | Modifier.Ctrl, Handler = OnPrintIssuedMementos }
        };

        public static void HandleIncapacitateHotkey()
        {
            var selectedMinion = MinionSelectionManager.SelectedMinion;
            Debug.Log("[OnKeyDown] Incapacitate hotkey detected.");
            if (selectedMinion != null)
            {
                var health = selectedMinion.GetComponent<Health>();
                if (health != null)
                {
                    Debug.Log($"[OnKeyDown] Health component found. Can be incapacitated: {health.canBeIncapacitated}, IsIncapacitated: {health.IsIncapacitated()}");
                    if (health.canBeIncapacitated && !health.IsIncapacitated())
                    {
                        health.Incapacitate(new Tag("ManualIncapacitate"));
                        Debug.Log($"[OnKeyDown] Incapacitated '{selectedMinion.GetProperName()}' via hotkey.");
                        // No need to clear selection here unless desired
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

        public static void HandleDamageHotkey()
        {
            var selectedMinion = MinionSelectionManager.SelectedMinion;
            Debug.Log("[OnKeyDown] Damage hotkey detected.");
            if (selectedMinion != null)
            {
                var health = selectedMinion.GetComponent<Health>();
                if (health != null)
                {
                    float damageAmount = 10f;
                    Debug.Log($"[OnKeyDown] (before health.Damage) Damaged '{selectedMinion.GetProperName()}' for {damageAmount} HP via hotkey.");
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

        public static void HandleEraseAllHotkey()
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

            var globalData = Mementos.MementosGlobalData.Instance;

            if (globalData != null && globalData.Issued != null)
            {
                Debug.Log("[EraseMedals] Cleared {globalData.Issued.Count} state flag(s).");
                globalData.Issued.Clear();
            }
        }

        public static void HandlePrintAllMementosHotkey()
        {
            Debug.Log("[PrintAllMementos] Print all mementos hotkey pressed.");
            var allMementos = UnityEngine.Object.FindObjectsOfType<Mementos.MementoModifiable>();
            foreach (var memento in allMementos)
            {
                Debug.Log($"Memento: {memento.GetName()} - {memento.GetDesc()}");
            }
        }

        public static void HandlePrintDetailsScreensHotkey()
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

        public static void HandleCreateMementoHotkey()
        {
            var selectedMinion = MinionSelectionManager.SelectedMinion;
            Debug.Log("[OnKeyDown] Create Memento hotkey detected.");
            if (selectedMinion != null)
            {
                foreach (var kvp in MementoPrototypes.Mementos)
                {
                    var mementoData = kvp.Value;
                    MementoUtils.CreateMemento(mementoData, selectedMinion, "");
                }
                Debug.Log($"[OnKeyDown] Created mementos for '{selectedMinion.GetProperName()}' via hotkey.");
            }
            else
            {
                Debug.Log("[OnKeyDown] No minion selected.");
            }
        }

        public static void HandlePrintIssuedMementosHotkey()
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
}
