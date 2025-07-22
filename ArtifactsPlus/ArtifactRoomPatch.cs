using HarmonyLib;
using Database;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace ArtifactsPlus
{
    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class DecoratedRoomTypePatch
    {
        private static RoomConstraints.Constraint[] TESTROOM_CONSTRAINTS = new RoomConstraints.Constraint[] {
            RoomConstraints.NO_INDUSTRIAL_MACHINERY,
            RoomConstraints.MINIMUM_SIZE_12,
            RoomConstraints.MAXIMUM_SIZE_64,
            CustomConstraints.DECORATIVE_ITEM_SCORE // Use the existing decor constraint
        };

        [HarmonyPostfix]
        public static void Postfix(Db __instance)
        {
            var decorRoomType = new RoomType(
                id: "DecorRoom",
                name: "Decor Room",
                description: "A room focused on decor. Passes all constraints.",
                tooltip: "A decor room type.",
                effect: null,
                category: Db.Get().RoomTypeCategories.Park, // or Recreation if it exists
                primary_constraint: CustomConstraints.DECORATIVE_ITEM_SCORE, // Use the existing decor constraint
                additional_constraints: new RoomConstraints.Constraint[] {
                    RoomConstraints.MINIMUM_SIZE_12,
                    RoomConstraints.MAXIMUM_SIZE_64,
                    RoomConstraints.NO_INDUSTRIAL_MACHINERY
                },
                display_details: new RoomDetails.Detail[] { RoomDetails.SIZE, RoomDetails.BUILDING_COUNT },
                priority: 10,
                upgrade_paths: null,
                single_assignee: false,
                priority_building_use: false,
                effects: new string[] { },
                sortKey: 1
            );
            Db.Get().RoomTypes.Add(decorRoomType);

            // Debugging code to verify patch
            var decorType = Db.Get().RoomTypes.TryGet("DecorRoom");
            if (decorType != null)
            {
                Debug.Log("[ArtifactsPlus] DecorRoomType successfully added: " + decorType.Id);
            }
            else
            {
                Debug.LogError("[ArtifactsPlus] DecorRoomType NOT found after patch!");
            }
        }
    }

    public static class CustomConstraints
    {
        private const int DECOR_SCORE_MINIMUM = 50;

        public static RoomConstraints.Constraint DECORATIVE_ITEM_SCORE = new RoomConstraints.Constraint(
            (Func<KPrefabID, bool>) (bc => bc.HasTag(GameTags.Decoration)),
            (Func<Room, bool>) (room => CalculateDecorScore(room) >= DECOR_SCORE_MINIMUM),
            name: $"Minimum Decor {DECOR_SCORE_MINIMUM}",
            description: $"Room must have at least {DECOR_SCORE_MINIMUM} decor."
        );

        private static int CalculateDecorScore(Room room)
        {
            int decorScore = 0;
            foreach (var building in room.buildings)
            {
                var decorProvider = building.GetComponent<DecorProvider>();
                if (decorProvider != null)
                    decorScore += (int)decorProvider.baseDecor;
            }
            return decorScore;
        }
    }
}