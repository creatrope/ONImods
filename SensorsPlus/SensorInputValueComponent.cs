using System; // Add this for [NonSerialized]
using KSerialization;
using UnityEngine;

namespace SensorsPlus
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class SensorInputValueComponent : KMonoBehaviour
    {
        [Serialize]
        public string inputValue = "1.0";

        [NonSerialized]
        public float parsedValue = 1.0f;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // Sync parsedValue with inputValue after load
            if (!float.TryParse(inputValue, out parsedValue))
                parsedValue = 1.0f;
        }
    }
}