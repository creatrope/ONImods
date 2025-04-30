using HarmonyLib;

namespace SmartSweep2
{
    [HarmonyPatch(typeof(Localization))]
    [HarmonyPatch("Initialize")]
    public static class LocalizationPatch
    {
        public static void Postfix()
        {
            Strings.Add("STRINGS.BUILDINGS.PREFABS.DOUBLESWEEPER.NAME", "Double Auto-Sweeper");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.DOUBLESWEEPER.DESC", "Sweeps twice as far.");
            Strings.Add("STRINGS.BUILDINGS.PREFABS.DOUBLESWEEPER.EFFECT", "Automatically sweeps items within an extended range.");
        }
    }
}
