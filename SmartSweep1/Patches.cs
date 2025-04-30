using HarmonyLib;
using TUNING;
using UnityEngine;
using System.Collections.Generic;

namespace SmartSweep1
{
    public class SmartSweep1Config : IBuildingConfig
    {
        public const string ID = "SmartSweep1";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 3,
                height: 3,
                anim: "conveyor_transferarm_kanim", // ✅ correct animation
                hitpoints: 30,
                construction_time: 60f,
                construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER4,
                construction_materials: MATERIALS.REFINED_METALS,
                melting_point: 1600f,
                build_location_rule: BuildLocationRule.Anywhere,
                decor: BUILDINGS.DECOR.PENALTY.TIER2,
                noise: NOISE_POLLUTION.NOISY.TIER1
            );

            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = 120f;
            def.SelfHeatKilowattsWhenActive = 0.5f;
            def.ViewMode = OverlayModes.SolidConveyor.ID;
            def.AudioCategory = "Metal";
            def.SceneLayer = Grid.SceneLayer.BuildingFront;
            def.PermittedRotations = PermittedRotations.R360;
            def.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));

            def.AnimFiles = new KAnimFile[] { Assets.GetAnim("conveyor_transferarm_kanim") };

            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<Storage>();
            go.AddOrGet<SolidTransferArm>();
            go.AddOrGet<Operational>();
            go.AddOrGet<LogicOperationalController>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGetDef<OperationalController.Def>();
        }
    }

    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    public static class SmartSweep1LoadGeneratedBuildings
    {
        public static void Prefix()
        {
            ModUtil.AddBuildingToPlanScreen("Conveyance", SmartSweep1Config.ID);

            Strings.Add("STRINGS.BUILDINGS.PREFABS.SMARTSWEEP1.NAME", "SmartSweep1 Auto-Sweeper");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.SMARTSWEEP1.DESC", "An auto-sweeper clone for testing.");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.SMARTSWEEP1.EFFECT", "Automatically sweeps items in its range.");
        }
    }
}
