using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using ArtifactsPlus;

namespace ArtifactsPlus
{
    [HarmonyPatch(typeof(ItemPedestal), "OnOccupantChanged")]
    public static class ItemPedestal_OnOccupantChanged_Patch
    {
        public static void Postfix(ItemPedestal __instance)
        {
            var receptacleField = typeof(ItemPedestal).GetField("receptacle", BindingFlags.NonPublic | BindingFlags.Instance);
            var receptacle = receptacleField?.GetValue(__instance) as SingleEntityReceptacle;
            var occupant = receptacle?.Occupant;

            foreach (var artifact in ArtifactStateTracker.ArtifactsOnPedestals.ToArray())
            {
                if (artifact == null) continue;
                bool stillOnPedestal = false;
                foreach (var pedestal in GameObject.FindObjectsOfType<ItemPedestal>())
                {
                    var recField = typeof(ItemPedestal).GetField("receptacle", BindingFlags.NonPublic | BindingFlags.Instance);
                    var rec = recField?.GetValue(pedestal) as SingleEntityReceptacle;
                    if (rec != null && rec.Occupant == artifact)
                    {
                        stillOnPedestal = true;
                        break;
                    }
                }
                if (!stillOnPedestal)
                    ArtifactStateTracker.UnregisterArtifactOnPedestal(artifact);
            }

            if (occupant != null)
            {
                ArtifactStateTracker.RegisterArtifactOnPedestal(occupant);
            }
        }
    }

    [HarmonyPatch(typeof(Game), "OnPrefabInit")]
    public static class Game_OnPrefabInit_Patch
    {
        public static void Postfix()
        {
            if (Game.Instance != null && Game.Instance.gameObject.GetComponent<ArtifactStatePoller>() == null)
            {
                Game.Instance.gameObject.AddComponent<ArtifactStatePoller>();
            }
        }
    }

    internal static class MinionMigrationHelper
    {
        public static readonly Dictionary<GameObject, (int oldWorldId, int newWorldId, bool removed, bool added)>
            MinionMigrationState = new Dictionary<GameObject, (int, int, bool, bool)>();
    }

    [HarmonyPatch(typeof(AssignmentManager), "MinionMigration")]
    public static class AssignmentManager_MinionMigration_Patch
    {
        public static void Postfix(object data)
        {
            var migrationEventArgs = data as MinionMigrationEventArgs;
            if (migrationEventArgs != null)
            {
                var minionGo = migrationEventArgs.minionId?.gameObject;
                if (minionGo == null)
                {
                    return;
                }

                int oldWorldId = migrationEventArgs.prevWorldId;
                int newWorldId = migrationEventArgs.targetWorldId;

                if (!MinionMigrationHelper.MinionMigrationState.TryGetValue(minionGo, out var state))
                {
                    MinionMigrationHelper.MinionMigrationState[minionGo] = (oldWorldId, newWorldId, false, false);
                }
                else
                {
                    foreach (var artifact in ArtifactsPlus.ArtifactStateTracker.ArtifactsOnPedestals)
                    {
                        if (artifact == null) continue;
                        int artifactWorldId = Grid.WorldIdx[Grid.PosToCell(artifact.transform.position)];
                        string internalName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                        var config = ArtifactStateTracker.GetArtifactConfig(internalName);

                        if (artifactWorldId == state.oldWorldId && config.Scope == "InWorld")
                        {
                            ArtifactEffectTracker.ApplyOrRemoveArtifactModifiersToMinion(minionGo, internalName, false);
                            ArtifactEffectTracker.ApplyOrRemoveArtifactStatusEffectsToMinion(minionGo, internalName, false);
                        }
                    }

                    foreach (var artifact in ArtifactsPlus.ArtifactStateTracker.ArtifactsOnPedestals)
                    {
                        if (artifact == null) continue;
                        int artifactWorldId = Grid.WorldIdx[Grid.PosToCell(artifact.transform.position)];
                        string internalName = artifact.GetComponent<KPrefabID>()?.PrefabTag.Name ?? "unknown";
                        var config = ArtifactStateTracker.GetArtifactConfig(internalName);

                        if (artifactWorldId == state.newWorldId && config.Scope == "InWorld")
                        {
                            ArtifactEffectTracker.ApplyOrRemoveArtifactModifiersToMinion(minionGo, internalName, true);
                            ArtifactEffectTracker.ApplyOrRemoveArtifactStatusEffectsToMinion(minionGo, internalName, true);
                        }
                    }

                    MinionMigrationHelper.MinionMigrationState.Remove(minionGo);
                }
            }
        }
    }

    [HarmonyPatch(typeof(SaveLoader), "Save", new Type[] { typeof(string), typeof(bool), typeof(bool) })]
    public static class SaveLoader_Save_Patch
    {
        public static void Postfix(string filename, bool isAutoSave, bool updateSavePointer)
        {
            Debug.Log("[ArtifactsPlus] SaveLoader.Save called for file: " + filename);
        }
    }

    [HarmonyPatch(typeof(SaveLoader), "Load", new Type[] { typeof(string) })]
    public static class SaveLoader_Load_Patch
    {
        public static void Postfix(string filename)
        {
            Debug.Log("[ArtifactsPlus] SaveLoader.Load called for file: " + filename);
        }
    }
}