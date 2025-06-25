using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PeterHan.PLib.Options; // Ensure this is included
using System.Collections.Generic;

namespace ArtifactsPlus
{
    [ConfigFile(SharedConfigLocation: true)]
    [JsonObject(MemberSerialization.OptIn)]
    [RestartRequired]
    public sealed class ArtifactsPlusOptions
    {
        public const int CURRENT_CONFIG_VERSION = 1;

        private static ArtifactsPlusOptions instance;

        public static ArtifactsPlusOptions Instance
        {
            get
            {
                var opts = instance;
                if (opts == null)
                {
                    opts = POptions.ReadSettings<ArtifactsPlusOptions>();
                    if (opts == null || opts.ConfigVersion < CURRENT_CONFIG_VERSION)
                    {
                        opts = new ArtifactsPlusOptions();
                        POptions.WriteSettings(opts);
                        HLib.CustomLogger.Log("[ArtifactsPlusOptions] Default configuration created.");
                    }
                    else
                    {
                        HLib.CustomLogger.Log("[ArtifactsPlusOptions] Configuration loaded successfully.");
                    }
                    instance = opts;
                }
                return opts;
            }
        }

        [JsonProperty]
        public int ConfigVersion { get; set; } = CURRENT_CONFIG_VERSION;

        [Option("Artifact Config File", "Select which artifact config JSON file to use.")]
        [JsonProperty]
        public string ArtifactConfigFile { get; set; } = "ArtifactsConfig.json";

        [Option("Verbose Logging", "Enable detailed logging for debugging purposes.")]
        [JsonProperty]
        public bool Verbose { get; set; } = true;

        [Option("Custom Logging", "Enable logging to a custom log file for debugging purposes.")]
        [JsonProperty]
        public bool CustomLogging { get; set; } = true;

        public static IList<string> GetConfigFiles()
        {
            var dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var files = Directory.GetFiles(dir, "ArtifactsConfig*.json")
                .Select(Path.GetFileName)
                .ToList();
            HLib.CustomLogger.Log($"[ArtifactsPlusOptions] Found {files.Count} configuration files.");
            return files;
        }

        public ArtifactsPlusOptions() { }
    }
}