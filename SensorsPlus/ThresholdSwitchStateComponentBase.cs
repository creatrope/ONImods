using System;
using System.Collections.Generic;
using UnityEngine;
using KSerialization;

namespace SensorsPlus
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public abstract class ThresholdSwitchStateComponentBase : KMonoBehaviour, IThresholdSwitchState
    {
        [Serialize]
        public Dictionary<string, string> CustomFields { get; set; } = new Dictionary<string, string>();

        [Serialize]
        public Dictionary<string, bool> ButtonStates { get; set; } = new Dictionary<string, bool>();

        [Serialize]
        private int randomID = 0;
        public int RandomID => randomID;

        private readonly List<MyThresholdSwitch> registeredSwitches = new List<MyThresholdSwitch>();
        protected IEnumerable<MyThresholdSwitch> RegisteredSwitches => registeredSwitches;

        // Add missing properties
        protected float LastValue { get; set; }
        protected float FirstDerivative { get; set; }
        protected float SecondDerivative { get; set; }

        // Add this dictionary for switch signal states if needed by all sensors
        protected Dictionary<int, bool> switchSignalStates = new Dictionary<int, bool>();

        protected virtual int RandomIdMin => 100000;
        protected virtual int RandomIdMax => 999999;

        protected virtual void EnsureRandomID()
        {
            if (randomID == 0)
                randomID = UnityEngine.Random.Range(RandomIdMin, RandomIdMax);
        }

        public void RegisterSwitch(MyThresholdSwitch sw)
        {
            if (sw != null && !registeredSwitches.Contains(sw))
                registeredSwitches.Add(sw);
        }

        public void ClearRegisteredSwitches()
        {
            registeredSwitches.Clear();
        }

        public virtual float GetValue(string fieldId)
        {
            // Map known threshold fieldIds to actual values or custom fields
            if (CustomFields != null && CustomFields.TryGetValue(fieldId, out var valueStr) && float.TryParse(valueStr, out var value))
                return value;

            // Add handling for default fields if needed
            switch (fieldId)
            {
                case "LastValue":
                    return LastValue;
                case "FirstDerivative":
                    return FirstDerivative;
                case "SecondDerivative":
                    return SecondDerivative;
                default:
                    // Instead of throwing, return a default or log a warning
                    return 0f;
            }
        }

        // Optionally, make RibbonPortId abstract or virtual if you want each sensor to specify its port.
        protected virtual HashedString RibbonPortId => new HashedString("GenericSensorRibbonOutput");

        // Add or update this in the base class
        protected virtual void SendRibbonSignal(int signal)
        {
            SensorHelpers.SendRibbonSignal(
                RegisteredSwitches,
                switchSignalStates,
                gameObject,
                RibbonPortId,
                signal
            );
        }

        protected int GetRegisteredSwitchSignal()
        {
            int signal = 0;
            signal |= SensorHelpers.GetSwitchSignalBitmask(RegisteredSwitches);
            foreach (var sw in RegisteredSwitches)
            {
                bool signalOn = sw.GetSignalOn();
                // Optionally store or process per-switch signal here
            }
            return signal;
        }

        // Add this to the base class
        public virtual float GetThresholdValue(string fieldId, float smoothedFirst, float smoothedSecond, float lastValue)
        {
            switch (fieldId)
            {
                case "threshold1": return smoothedFirst;
                case "threshold2": return smoothedSecond;
                default: return lastValue;
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            EnsureRandomID();
        }
    }
}