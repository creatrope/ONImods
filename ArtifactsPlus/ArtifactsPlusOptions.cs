using Newtonsoft.Json;
using PeterHan.PLib.Options;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArtifactsPlus
{
    [Serializable]
    [ConfigFile(SharedConfigLocation: true)]
    internal class ArtifactsPlusConfig : SingletonOptions<ArtifactsPlusConfig>, IOptions
    {
        [Option("Enable Custom Output Log", "Enable or disable writing the custom output log file.")]
        [JsonProperty]
        public bool EnableCustomLog { get; set; } = true;

        [Option("Artifact Config File", "Set the path to the artifact configuration file.")]
        [JsonProperty]
        public string ArtifactConfigFile { get; set; } = "ArtifactsConfig.json";

        [Option("Artifact Polling Interval", "Set the interval (in ticks) for artifact polling.")]
        [Limit(1, 10000)]
        [JsonProperty]
        public int ArtifactPollingInterval { get; set; } = 900;

        public IEnumerable<IOptionsEntry> CreateOptions()
        {
            return new List<IOptionsEntry>();
        }

        public void OnOptionsChanged()
        {
            ArtifactsPlusConfig.Instance = this; // Update the instance manually when options change
        }
    }
}