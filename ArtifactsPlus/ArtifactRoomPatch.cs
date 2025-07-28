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
            // Use the configured minimum decor value
            int decorMin = ArtifactsPlus.ArtifactsPlusOptions.Instance.DecorMinimum;
            // Debug print for decorMin
            Debug.Log($"[ArtifactsPlus] DecorRoom decorMin value: {decorMin}");

            var decorRoomType = new RoomType(
                id: "DecorRoom",
                name: "Decor Room",
                description: "A room focused on decor. Passes all constraints.",
                tooltip: "A decor room type.",
                effect: null,
                category: Db.Get().RoomTypeCategories.Park, // or Recreation if it exists
                primary_constraint: RoomConstraints.DECORATIVE_ITEM_SCORE_20,
                additional_constraints: new RoomConstraints.Constraint[] {
                    RoomConstraints.MINIMUM_SIZE_12,
                    RoomConstraints.MAXIMUM_SIZE_96,
                    RoomConstraints.NO_INDUSTRIAL_MACHINERY,
                    new RoomConstraints.Constraint(
                        (Func<KPrefabID, bool>)(bc => bc.HasTag(GameTags.Decoration)),
                        (Func<Room, bool>)(room => CustomConstraints.CalculateDecorScore(room) >= decorMin),
                        name: $"Minimum Decor ({decorMin})",
                        description: $"Room must have at least {decorMin} decor."
                    )
                },
                display_details: new RoomDetails.Detail[] { RoomDetails.SIZE, RoomDetails.BUILDING_COUNT },
                priority: 0,
                upgrade_paths: null,
                single_assignee: false,
                priority_building_use: false,
                effects: new string[] { },
                sortKey: 100
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
        public static RoomConstraints.Constraint DECORATIVE_ITEM_SCORE = new RoomConstraints.Constraint(
            (Func<KPrefabID, bool>)(bc => bc.HasTag(GameTags.Decoration)),
            (Func<Room, bool>)(room => CalculateDecorScore(room) >= ArtifactsPlusOptions.Instance.DecorMinimum),
            name: $"Minimum Decor (Configurable)",
            description: $"Room must have at least the configured minimum decor."
        );

        public static int CalculateDecorScore(Room room)
        {
            int buildingDecor = 0;
            int plantDecor = 0;
            int buildingCount = 0;
            int plantCount = 0;

            foreach (var building in room.buildings)
            {
                if (building == null) continue; // Prevent NullReferenceException
                buildingCount++;
                var decorProvider = building.GetComponent<DecorProvider>();
                if (decorProvider != null)
                    buildingDecor += (int)decorProvider.decor.GetTotalValue();
            }
            foreach (var plant in room.plants)
            {
                if (plant == null) continue; // Prevent NullReferenceException
                plantCount++;
                var decorProvider = plant.GetComponent<DecorProvider>();
                if (decorProvider != null)
                    plantDecor += (int)decorProvider.decor.GetTotalValue();
            }
            int totalDecor = buildingDecor + plantDecor;
            //if (buildingCount > 1)
            //    Debug.Log($"[ArtifactsPlus] DecorScore Buildings={buildingDecor}(#{buildingCount}) + Plants={plantDecor}(#{plantCount}) -> {totalDecor}");
            return totalDecor;
        }
    }
 
}