using System;
using System.Collections.Generic;
using System.IO; // Add this namespace for 'Path'
using UnityEngine; // Add this namespace for 'Debug' and 'Application'

namespace SensorsPlus
{
    public static class SensorHelpers
    {
        private static string logFilePath;
        private static bool logInitialized = false;

        static SensorHelpers()
        {
            // Try to find the directory of Players.log and use it for SensorsPlus.log
            string playersLogPath = Path.Combine(Application.persistentDataPath, "Players.log");
            string logDir = Path.GetDirectoryName(playersLogPath);
            logFilePath = Path.Combine(logDir, "SensorsPlus.log");
        }

        public static void LogToFile(string message)
        {
            try
            {
                if (!logInitialized)
                {
                    // Unlink (delete) the log file at the start of the run
                    if (File.Exists(logFilePath))
                        File.Delete(logFilePath);
                    logInitialized = true;
                }
                File.AppendAllText(logFilePath, $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}\n");
            }
            catch
            {
                // Swallow all exceptions silently, no Debug.Log or other output
            }
        }

        /// <summary>
        /// Calculates first and second derivatives and applies smoothing.
        /// </summary>
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
            firstDerivative = 0f;
            secondDerivative = 0f;

            if (lastValue.HasValue)
            {
                float prevValue = lastValue.Value;
                float prevFirst = lastFirstDerivative ?? 0f;
                firstDerivative = (currentValue - prevValue) / deltaT;
                smoothedFirst = smoothingAlpha * firstDerivative + (1 - smoothingAlpha) * smoothedFirst;
                secondDerivative = (smoothedFirst - prevFirst) / deltaT;
                smoothedSecond = smoothingAlpha * secondDerivative + (1 - smoothingAlpha) * smoothedSecond;
            }
            lastFirstDerivative = smoothedFirst;
            lastValue = currentValue;
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
                if (!ports.Exists(p => p.id == portId))
                    ports.Add(newPort);
                logicPorts.outputPortInfo = ports.ToArray();
            }
        }
    }
}