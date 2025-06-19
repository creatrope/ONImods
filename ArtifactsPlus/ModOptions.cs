// ...existing code...
// Add an option to enable/disable logging if not present
using PeterHan.PLib.Options;

public class ModOptions
{
    // ...existing options...

    [Option("Enable Custom Output Log", "Enable or disable writing the custom output log file.")]
    public bool EnableCustomLog { get; set; } = true;

    // ...existing options...

    // Singleton pattern for easy access if needed
    public static ModOptions Instance { get; private set; }

    public ModOptions()
    {
        Instance = this;
    }
}
// ...existing code...