using HarmonyLib;

namespace SmartSweep5
{
    [HarmonyPatch(typeof(Db))]
    [HarmonyPatch("Initialize")]
    public static class AddToTechTree
    {
        public static void Postfix()
        {
            ModUtil.AddBuildingToPlanScreen("Base", DoubleSweeperConfig.ID);
            Db.Get().Techs.Get("SolidTransport").unlockedItemIDs.Add(DoubleSweeperConfig.ID);
        }
    }
}
