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
        [Option("Artifact Polling Interval (Secs)", "Set the interval (seconds) for artifact polling.")]
        [Limit(1, 10000)]
        [JsonProperty]
        public int PollingIntervalSeconds { get; set; } = 15;

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