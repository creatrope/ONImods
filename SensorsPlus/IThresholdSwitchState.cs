using System.Collections.Generic;
using KSerialization;

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