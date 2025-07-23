using Newtonsoft.Json;
using PeterHan.PLib.Options;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArtifactsPlus
{
    [Serializable]
    [ConfigFile(SharedConfigLocation: true)]
    internal class ArtifactsPlusOptions : SingletonOptions<ArtifactsPlusOptions>, IOptions
    {
        [Option("Enable Custom Output Log", "Enable or disable writing the custom output log file.")]
        [JsonProperty]
        public bool EnableCustomLog { get; set; } = false;

        [Option("Artifact Polling Interval", "Set the interval (in ticks (60 ticks/sec)) for artifact polling.")]
        [Limit(1, 10000)]
        [JsonProperty]
        public int ArtifactPollingInterval { get; set; } = 900;

        [Option("Decor Minimum", "Minimum decor required for artifact activation.")]
        [Limit(0, 1000)]
        [JsonProperty]
        public int DecorMinimum { get; set; } = 100;

        public IEnumerable<IOptionsEntry> CreateOptions()
        {
            return new List<IOptionsEntry>();
        }

        public void OnOptionsChanged()
        {
            ArtifactsPlusOptions.Instance = this; // Update the instance manually when options change
        }
    }
}