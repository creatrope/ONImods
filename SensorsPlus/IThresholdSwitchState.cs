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
    }
}