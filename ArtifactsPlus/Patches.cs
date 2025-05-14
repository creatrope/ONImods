using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using Klei.AI;
using Database;

namespace ArtifactsPlus
{
    [HarmonyPatch(typeof(ItemPedestal), "OnOccupantChanged")]
    public static class ItemPedestal_OnOccupantChanged_Patch
    {
        private static readonly Dictionary<Tag, Action<ItemPedestal, GameObject>> ArtifactActions =
            new Dictionary<Tag, Action<ItemPedestal, GameObject>>
            {
                {
                    "RubiksCube".ToTag(),
                    (pedestal, artifact) =>
                    {
                        Debug.Log("[ArtifactsPlus] Rubik's Cube placed!");
                        RubiksCubeEffect.BoostScienceAttributeForAllDupes();
                    }
                }
            };

        public static void Postfix(ItemPedestal __instance)
        {
            var storage = __instance.GetComponent<Storage>();
            var occupant = storage != null ? storage.FindFirst(GameTags.Artifact) : null;
            if (occupant == null)
                return;

            var artifactTag = occupant.PrefabID();
            if (ArtifactActions.TryGetValue(artifactTag, out var action))
            {
                action(__instance, occupant);
            }
        }
    }

    public static class RubiksCubeEffect
    {
        private const float BoostAmount = 2f;
        private const string ModifierDescription = "Rubik's Cube Science Boost";
        private static readonly HashSet<string> boostedDupes = new HashSet<string>();

        public static void BoostScienceAttributeForAllDupes()
        {
            Klei.AI.Attribute researchAttr = Db.Get().Attributes.Get("Research");

            foreach (var resume in Components.MinionResumes.Items)
            {
                var go = resume.gameObject;
                var dupeId = go.name;

                if (boostedDupes.Contains(dupeId))
                    continue;

                var attributes = go.GetAttributes();
                var modifier = new AttributeModifier(
                    researchAttr.Id,
                    BoostAmount,
                    ModifierDescription,
                    is_readonly: false,
                    is_multiplier: false,
                    uiOnly: false
                );

                attributes.Add(modifier);
                boostedDupes.Add(dupeId);
                Debug.Log("[ArtifactsPlus] Boosted science for: " + go.GetProperName());
            }
        }
    }
} 