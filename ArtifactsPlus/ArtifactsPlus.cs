using HLib; // For CustomLogger

namespace ArtifactsPlus
{
    public class ArtifactsPlusMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            // Initialize or configure the shared logger as needed
            CustomLogger.Enabled = ModOptions.Instance.EnableCustomLog;
            CustomLogger.LogPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(CustomLogger.LogPath),
                "ArtifactsPlus.log"
            );
            if (CustomLogger.Enabled)
                CustomLogger.ResetLog();

            CustomLogger.Log("ArtifactsPlus: Mod.OnLoad called.");
            // All hotkey logic has been removed.
        }
    }
}