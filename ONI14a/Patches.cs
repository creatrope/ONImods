using Database;
using Epic.OnlineServices.Platform;
using HarmonyLib;
using HLib;
using KMod;
using KSerialization;
using Newtonsoft.Json; // Ensure this using directive is present
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TUNING;
using UnityEngine;
using UnityEngine.UI; // Add this using directive
using static Rendering.BlockTileRenderer;
using TMPro; // Add this using directive

namespace KeybindLogTest
{
    internal sealed class MinimalKeybindHandler : IInputHandler
    {
        private static PAction KeyTestAction;
        private static PAction KeyTestAction2; // Add second action
        private readonly Action snapshotAction;
        private readonly Action snapshotAction2; // Add second action field

        public string handlerName => "MinimalKeybindHandler";
        public KInputHandler inputHandler { get; set; }

        internal MinimalKeybindHandler()
        {
            snapshotAction = KeyTestAction != null ? KeyTestAction.GetKAction() : PAction.MaxAction;
            snapshotAction2 = KeyTestAction2 != null ? KeyTestAction2.GetKAction() : PAction.MaxAction;
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (e.TryConsume(snapshotAction))
            {
                Debug.Log("[MinimalKeybindHandler] Hotkey 1 pressed!");
            }
            else if (e.TryConsume(snapshotAction2))
            {
                Debug.Log("[MinimalKeybindHandler] Hotkey 2 pressed! Printing all diseases and sicknesses:");
            }
        }

        [PLibMethod(RunAt.AfterLayerableLoad)]
        internal static void AddKeycodeHandler()
        {
            KInputHandler.Add(Global.GetInputManager().GetDefaultController(),
                new MinimalKeybindHandler(), 512);
        }

        internal static void Register(PPatchManager manager)
        {
            manager.RegisterPatchClass(typeof(MinimalKeybindHandler));
            KeyTestAction = new PActionManager().CreateAction(
                "KeybindLogTest.KeyTestAction", "Test Key Action", new PKeyBinding(KKeyCode.F11, Modifier.Ctrl));
            KeyTestAction2 = new PActionManager().CreateAction(
                "KeybindLogTest.KeyTestAction2", "Test Key Action 2", new PKeyBinding(KKeyCode.F12, Modifier.Ctrl));
        }
        // Helper function to get MinionIdentity from a CrewPortrait

    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll();
            PUtil.InitLibrary();
            MinimalKeybindHandler.Register(new PPatchManager(harmony));
        }
    }
}

// Patch for AssignableSideScreenRow.SetContent to add "Extra" column
[HarmonyPatch(typeof(AssignableSideScreenRow), "SetContent")]
public static class AssignableSideScreenRow_AddColumn_Patch
{
    public static void Postfix(AssignableSideScreenRow __instance)
    {
        var existing = __instance.transform.Find("ExtraColumnLabel");
        if (existing == null)
        {
            var refLabel = __instance.GetComponentInChildren<LocText>();
            if (refLabel != null)
            {
                {
                    // Inline null checks and return if null for each assignment
                    var crewPortrait = __instance.GetComponentInChildren<CrewPortrait>(true);
                    if (crewPortrait == null)
                    {
                        Debug.Log("CrewPortrait is null");
                        return;
                    }
                    var identity = crewPortrait.identityObject;
                    MinionIdentity minionIdentity = null;

                    if (identity is MinionIdentity directMinion)
                    {
                        minionIdentity = directMinion;
                    }
                    else if (identity is MinionAssignablesProxy proxy)
                    {
                        // Try to get the target from the proxy
                        if (proxy.target is MinionIdentity proxyMinion)
                        {
                            minionIdentity = proxyMinion;
                        }
                        else
                        {
                            // Fallback: try to get MinionIdentity from the proxy's GameObject
                            minionIdentity = proxy.GetTargetGameObject()?.GetComponent<MinionIdentity>();
                        }
                    }

                    if (minionIdentity == null)
                    {
                        Debug.Log("Could not resolve MinionIdentity from CrewPortrait.identityObject. Type: " + (identity?.GetType().FullName ?? "null"));
                        return;
                    }
                    Debug.Log("Resolved MinionIdentity: " + minionIdentity.gameObject.name);

                    var minionGO = minionIdentity.gameObject;
                    if (minionGO == null)
                    {
                        Debug.Log("minionGO is null");
                        return;
                    }

                    // Get the dupe's world name
                    string worldName = GetDupeWorldName(minionIdentity);

                    var extraLabel = UnityEngine.Object.Instantiate(refLabel, __instance.transform);
                    extraLabel.name = "ExtraColumnLabel";
                    extraLabel.text = worldName;
                    var rectTransform = extraLabel.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.anchorMin = new Vector2(0, rectTransform.anchorMin.y);
                        rectTransform.anchorMax = new Vector2(1, rectTransform.anchorMax.y);
                        rectTransform.offsetMin = new Vector2(0, rectTransform.offsetMin.y);
                        rectTransform.offsetMax = new Vector2(0, rectTransform.offsetMax.y);
                        rectTransform.sizeDelta = new Vector2(0, rectTransform.sizeDelta.y);
                    }
                    extraLabel.alignment = TMPro.TextAlignmentOptions.Center;
                    extraLabel.enableWordWrapping = false;
                    extraLabel.transform.SetSiblingIndex(2);
                }
            }

            // Try to expand the parent container's width
            var parentRect = __instance.transform.parent?.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                parentRect.sizeDelta = new Vector2(parentRect.sizeDelta.x + 100, parentRect.sizeDelta.y);
            }

            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.GetComponent<RectTransform>());
        }
    }

    // Helper function to get the current world name of the dupe from MinionIdentity or GameObject
    private static string GetDupeWorldName(MinionIdentity minionIdentity)
    {
        if (minionIdentity == null)
        {
            Debug.Log("minionIdentity is null");
            return "Unknown World";
        }

        var clusterManager = ClusterManager.Instance;
        WorldContainer world = null;
        if (clusterManager != null)
        {
            world = clusterManager.GetWorldFromPosition(minionIdentity.transform.position);
        }

        if (world == null)
        {
            // Fallback: Try to get world from parent objects
            var parent = minionIdentity.transform.parent;
            while (parent != null)
            {
                var worldContainer = parent.GetComponent<WorldContainer>();
                if (worldContainer != null)
                {
                    world = worldContainer;
                    break;
                }
                parent = parent.parent;
            }
        }

        if (world != null)
        {
            var asteroidEntity = world.GetComponent<AsteroidGridEntity>();
            if (asteroidEntity != null)
                return asteroidEntity.Name;
        }

        Debug.Log("No world found for dupe.");
        return "Unknown World";
    }
}
