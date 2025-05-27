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
                    }
                    instance = opts;
                }
                return opts;
            }
        }

        [JsonProperty]
        public int ConfigVersion { get; set; } = CURRENT_CONFIG_VERSION;

        [Option("Enable Feature X", "Enable or disable Feature X for the mod.")]
        [JsonProperty]
        public bool EnableFeatureX { get; set; } = true;

        [Option("Artifact Glow Intensity", "Set the intensity of the artifact glow effect.")]
        [JsonProperty]
        public float GlowIntensity { get; set; } = 1.0f;

        [Option("Room Size Threshold", "Set the minimum room size for artifact activation.")]
        [JsonProperty]
        public int RoomSizeThreshold { get; set; } = 6;

        [Option("Artifact Config File", "Select which artifact config JSON file to use.")]
        [JsonProperty]
        public string ArtifactConfigFile { get; set; } = "ArtifactsConfig.json";

        public static IList<string> GetConfigFiles()
        {
            var dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return Directory.GetFiles(dir, "ArtifactsConfig*.json")
                .Select(Path.GetFileName)
                .ToList();
        }

        public ArtifactsPlusOptions() { }
    }
}