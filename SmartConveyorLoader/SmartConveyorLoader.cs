using HarmonyLib;
using UnityEngine;
using TUNING;
using STRINGS;
using Database;
using static STRINGS.UI;
using System.Collections.Generic;

namespace BaseConveyorLoader
{
    public class BaseConveyorLoaderConfig : IBuildingConfig
    {
        public const string ID = "BaseConveyorLoader";

        public override BuildingDef CreateBuildingDef()
        {
            var anim = Assets.GetAnim("conveyorin_kanim");

            var def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 1,
                height: 2,
                anim: "conveyorin_kanim",
                hitpoints: 30,
                construction_time: 30f,
                construction_mass: TUNING.BUILDINGS.CONSTRUCTION_MASS_KG.TIER2,
                construction_materials: MATERIALS.ALL_METALS,
                melting_point: 800f,
                build_location_rule: BuildLocationRule.Anywhere,
                decor: TUNING.BUILDINGS.DECOR.PENALTY.TIER1,
                noise: TUNING.NOISE_POLLUTION.NOISY.TIER1  // ✅ fixed

            );

            def.OutputConduitType = ConduitType.Solid;
            def.UtilityOutputOffset = new CellOffset(0, 0); // or wherever the port is visually

            def.AnimFiles = new[] { anim };
            def.AudioCategory = "Metal";
            def.SceneLayer = Grid.SceneLayer.Building;
            def.RequiresPowerInput = true;
            def.PowerInputOffset = new CellOffset(0, 1); // Adjust for where the power plug appears

            def.UseStructureTemperature = true;
            def.ThermalConductivity = 1f;
            def.SelfHeatKilowattsWhenActive = 0.5f; // ✅ Required


            def.SceneLayer = Grid.SceneLayer.BuildingFront;


            def.PermittedRotations = PermittedRotations.R360;

            def.LogicInputPorts = new List<LogicPorts.Port> {
    LogicPorts.Port.InputPort(
        LogicOperationalController.PORT_ID,
        new CellOffset(0, 1),
        STRINGS.BUILDINGS.PREFABS.LOGICSWITCH.LOGIC_PORT,
        STRINGS.BUILDINGS.PREFABS.LOGICSWITCH.LOGIC_PORT_ACTIVE,
        STRINGS.BUILDINGS.PREFABS.LOGICSWITCH.LOGIC_PORT_INACTIVE
        )
};


            return def;
        }



        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.GetComponent<KPrefabID>().AddTag(GameTags.HasChores);
            go.GetComponent<KPrefabID>().AddTag(GameTags.NotRocketInteriorBuilding, false);

            var storage = go.AddOrGet<Storage>();
            storage.capacityKg = 1000f;
            storage.showInUI = true;
            storage.showDescriptor = true;
            storage.allowItemRemoval = false;
            storage.allowSettingOnlyFetchMarkedItems = true;
            storage.storageFilters = STORAGEFILTERS.NOT_EDIBLE_SOLIDS;
            storage.fetchCategory = Storage.FetchCategory.GeneralStorage;
            storage.showCapacityStatusItem = true;  // ✅ This enables the "0 / 1000kg" line
            storage.showCapacityAsMainStatus = true; // ✅ This makes it appear as the main status line
            go.AddOrGet<StorageLocker>();

            go.AddOrGet<TreeFilterable>();

            var delivery = go.AddOrGet<ManualDeliveryKG>();
            delivery.capacity = storage.capacityKg;
            delivery.refillMass = storage.capacityKg * 0.25f;
            delivery.choreTypeIDHash = Db.Get().ChoreTypes.MachineFetch.IdHash;
            delivery.SetStorage(storage);


            go.AddOrGet<CopyBuildingSettings>();
            go.AddOrGet<Prioritizable>();

            go.AddOrGet<SolidConduitDispenser>();

            go.AddOrGet<LogicOperationalController>();
            go.AddOrGet<Operational>();

        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGetDef<StorageController.Def>();
            go.AddOrGet<BuildingCellVisualizer>();

        }
    }

    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    public static class BaseConveyorLoaderStringsPatch
    {
        public static void Prefix()
        {
            Strings.Add("STRINGS.BUILDINGS.PREFABS.BaseCONVEYORLOADER.NAME", "Base Conveyor Filter");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.BaseCONVEYORLOADER.DESC", "Base Conveyor Filter.");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.BaseCONVEYORLOADER.EFFECT", "Base Conveyor Filter.");
            ModUtil.AddBuildingToPlanScreen("Base", BaseConveyorLoaderConfig.ID);
        }
    }
}
