using System;
using System.Collections.Generic;
using UnityEngine;
using PeterHan.PLib.Actions;
using PeterHan.PLib.PatchManager;

namespace Mementos
{
    public static class KeybindHandler
    {
        public static MinionIdentity SelectedMinion { get; set; }

        public static void Register(PPatchManager patchManager)
        {
            // Register keybinds here, implementation depends on your mod's needs.
        }

        public static void HandleIncapacitateHotkey()
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

        public static void HandleDamageHotkey()
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
