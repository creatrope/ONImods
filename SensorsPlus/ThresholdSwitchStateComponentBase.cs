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

        private readonly List<MyThresholdSwitch> registeredSwitches = new List<MyThresholdSwitch>();
        protected IEnumerable<MyThresholdSwitch> RegisteredSwitches => registeredSwitches;

        // Add missing properties
        protected float LastValue { get; set; }
        protected float FirstDerivative { get; set; }
        protected float SecondDerivative { get; set; }

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

        protected virtual HashedString RibbonPortId => new HashedString("GenericSensorRibbonOutput");

        protected virtual void SendRibbonSignal(int signal)
        {
            if (TryGetComponent<LogicPorts>(out var ports))
            {
                ports.SendSignal(RibbonPortId, signal);
            }
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
    }
}