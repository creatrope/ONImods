using System.Collections.Generic;
using KSerialization; // Add this namespace for the Serialize attribute

namespace SensorsPlus
{
    public interface IThresholdSwitchState
    {
        Dictionary<string, string> CustomFields { get; set; }
        Dictionary<string, bool> ButtonStates { get; set; }
        float GetValue(string fieldId);
        void RegisterSwitch(MyThresholdSwitch sw);
        void ClearRegisteredSwitches();
    }

    public abstract class ThresholdSwitchStateComponentBase : KMonoBehaviour, IThresholdSwitchState
    {
        [Serialize]
        public Dictionary<string, string> CustomFields { get; set; } = new Dictionary<string, string>();

        [Serialize]
        public Dictionary<string, bool> ButtonStates { get; set; } = new Dictionary<string, bool>();

        private readonly List<MyThresholdSwitch> registeredSwitches = new List<MyThresholdSwitch>();

        public void RegisterSwitch(MyThresholdSwitch sw)
        {
            if (!registeredSwitches.Contains(sw))
                registeredSwitches.Add(sw);
        }

        public void ClearRegisteredSwitches()
        {
            registeredSwitches.Clear();
        }

        public abstract float GetValue(string fieldId);

        // Optionally, provide access to registered switches for derived classes:
        protected IEnumerable<MyThresholdSwitch> RegisteredSwitches => registeredSwitches;

        // You can also add a helper for switch signal evaluation if desired:
        protected int GetSwitchSignalBitmask()
        {
            int signal = 0;
            foreach (var sw in registeredSwitches)
            {
                if (sw.GetSignalOn())
                    signal |= 1 << sw.OutputBit;
            }
            return signal;
        }
    }

    public partial class ThermoSensorStateComponent : ThresholdSwitchStateComponentBase, ISim1000ms
    {
        public override float GetValue(string fieldId)
        {
            switch (fieldId)
            {
                case "threshold1": return SmoothedFirst;
                case "threshold2": return SmoothedSecond;
                default: return LastValue;
            }
        }
    }
}