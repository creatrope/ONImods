using System;
using System.Collections.Generic;
using System.IO; // Add this namespace for 'Path'
using UnityEngine; // Add this namespace for 'Debug' and 'Application'

namespace SensorsPlus
{
    public static class SensorHelpers
    {
        // Derivative and smoothing calculation
        public static void UpdateDerivatives(
            ref float? lastValue,
            ref float? lastFirstDerivative,
            ref float smoothedFirst,
            ref float smoothedSecond,
            out float firstDerivative,
            out float secondDerivative,
            float currentValue,
            float deltaT,
            float smoothingAlpha = 0.2f)
        {
            float first = 0f;
            float second = 0f;

            if (lastValue.HasValue)
            {
                first = (currentValue - lastValue.Value) / deltaT;
                if (lastFirstDerivative.HasValue)
                    second = (first - lastFirstDerivative.Value) / deltaT;
            }

            firstDerivative = first;
            secondDerivative = second;

            smoothedFirst = lastValue.HasValue
                ? smoothingAlpha * first + (1 - smoothingAlpha) * smoothedFirst
                : first;

            smoothedSecond = lastFirstDerivative.HasValue
                ? smoothingAlpha * second + (1 - smoothingAlpha) * smoothedSecond
                : second;

            lastValue = currentValue;
            lastFirstDerivative = first;
        }

        // Calculate bitmask for registered switches
        public static int GetSwitchSignalBitmask(IEnumerable<MyThresholdSwitch> switches)
        {
            int signal = 0;
            foreach (var sw in switches)
            {
                if (sw.GetSignalOn())
                    signal |= 1 << sw.OutputBit;
            }
            return signal;
        }

        /// <summary>
        /// Sends a ribbon signal to the specified port and updates switch signal states.
        /// </summary>
        public static void SendRibbonSignal(
            IEnumerable<MyThresholdSwitch> switches,
            Dictionary<int, bool> switchSignalStates,
            GameObject go,
            HashedString portId,
            int signal)
        {
            foreach (var sw in switches)
            {
                switchSignalStates[sw.OutputBit] = sw.GetSignalOn();
            }

            if (go != null && go.TryGetComponent<LogicPorts>(out var ports))
            {
                ports.SendSignal(portId, signal);
            }
        }

        /// <summary>
        /// Saves the state of all MyThresholdSwitch fields to the provided IThresholdSwitchState.
        /// </summary>
        public static void SaveSwitchFieldsState(
            IEnumerable<MyThresholdSwitch> fields,
            IThresholdSwitchState state)
        {
            foreach (var field in fields)
            {
                state.ButtonStates[$"{field.FieldId}_A"] = field.IsAButtonPressed;
                state.ButtonStates[$"{field.FieldId}_B"] = field.IsBButtonPressed;
                state.ButtonStates[$"{field.FieldId}_A_interactable"] = field.IsAButtonInteractable;
                state.ButtonStates[$"{field.FieldId}_B_interactable"] = field.IsBButtonInteractable;
                if (field.InputField != null)
                    state.CustomFields[field.FieldId] = field.InputField.Text;
            }
        }

        /// <summary>
        /// Configures a ribbon output port for the specified GameObject.
        /// </summary>
        public static void ConfigureRibbonOutputPort(
            GameObject go,
            HashedString portId)
        {
            if (go.GetComponent<LogicPorts>() == null)
            {
                go.AddComponent<LogicPorts>();
            }
        }

        /// <summary>
        /// Configures a ribbon output port with additional parameters for the specified GameObject.
        /// </summary>
        public static void ConfigureRibbonOutputPort(
            GameObject go,
            HashedString portId,
            CellOffset offset,
            LocString portName,
            LocString activePort,
            LocString inactivePort,
            bool showInUI)
        {
            var logicPorts = go.GetComponent<LogicPorts>();
            if (logicPorts == null)
            {
                logicPorts = go.AddComponent<LogicPorts>();
            }

            // Create a new port
            var newPort = new LogicPorts.Port(
                portId,
                offset,
                portName,
                activePort,
                inactivePort,
                showInUI,
                LogicPortSpriteType.RibbonOutput
            );

            // Add to outputPortInfo array if not already present
            if (logicPorts.outputPortInfo == null)
            {
                logicPorts.outputPortInfo = new[] { newPort };
            }
            else
            {
                var ports = new List<LogicPorts.Port>(logicPorts.outputPortInfo);
                // Avoid duplicates
                if (!ports.Exists(p => p.id == portId))
                    ports.Add(newPort);
                logicPorts.outputPortInfo = ports.ToArray();
            }
        }


    }
}

namespace SensorsPlus.Helpers
{
    public static class ThermoSensorHelper // Renamed to avoid conflict
    {
        public static void EnsureDefaults(
            Dictionary<string, string> customFields,
            Dictionary<string, bool> buttonStates)
        {
            if (!customFields.ContainsKey("threshold1"))
                customFields["threshold1"] = "1.0";
            if (!customFields.ContainsKey("threshold2"))
                customFields["threshold2"] = "1.0";

            foreach (var prefix in new[] { "threshold1", "threshold2" })
            {
                bool a = buttonStates.ContainsKey($"{prefix}_A") && buttonStates[$"{prefix}_A"];
                bool b = buttonStates.ContainsKey($"{prefix}_B") && buttonStates[$"{prefix}_B"];
                bool aInteract = !buttonStates.ContainsKey($"{prefix}_A_interactable") || buttonStates[$"{prefix}_A_interactable"];
                bool bInteract = !buttonStates.ContainsKey($"{prefix}_B_interactable") || buttonStates[$"{prefix}_B_interactable"];

                bool validA = a && !aInteract && !b && bInteract;
                bool validB = b && !bInteract && !a && aInteract;

                if (!(validA || validB))
                {
                    buttonStates[$"{prefix}_A"] = true;
                    buttonStates[$"{prefix}_A_interactable"] = false;
                    buttonStates[$"{prefix}_B"] = false;
                    buttonStates[$"{prefix}_B_interactable"] = true;
                }
            }
        }
    }
}