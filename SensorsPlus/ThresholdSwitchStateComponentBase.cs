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

        public void RegisterSwitch(MyThresholdSwitch sw)
        {
            if (sw != null && !registeredSwitches.Contains(sw))
                registeredSwitches.Add(sw);
        }

        public void ClearRegisteredSwitches()
        {
            registeredSwitches.Clear();
        }

        public abstract float GetValue(string fieldId);

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