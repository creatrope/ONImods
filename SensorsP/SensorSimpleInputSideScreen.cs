// ...existing code...

        private void UpdateDerivativeLabel()
        {
            float firstDerivative = 0.0f;

            if (pressureSensor != null)
            {
                if (SensorsPlus.LogicPressureSensor_Sim200ms_Patch.DerivativeStates.TryGetValue(pressureSensor, out var derivativeState))
                {
                    // Use the global moving average window
                    firstDerivative = derivativeState.ComputeMovingAverageFirstDerivative();
                }
            }
            else if (temperatureSensor != null)
            {
                if (SensorsPlus.LogicTemperatureSensor_Sim200ms_Patch.DerivativeStates.TryGetValue(temperatureSensor, out var derivativeState))
                {
                    // Use the global moving average window
                    firstDerivative = derivativeState.ComputeMovingAverageFirstDerivative();
                }
            }

            if (derivativeText == null)
            {
                string sensorType = pressureSensor != null ? "Pressure" : (temperatureSensor != null ? "Temperature" : "Unknown");
                HLib.CustomLogger.Log($"[UI] UpdateDerivativeLabel: derivativeText is null for {sensorType} sensor!");
                return;
            }

            if (!(derivativeText is LocText locText))
            {
                string sensorType = pressureSensor != null ? "Pressure" : (temperatureSensor != null ? "Temperature" : "Unknown");
                HLib.CustomLogger.Log($"[UI] UpdateDerivativeLabel: derivativeText is not LocText (actual type: {derivativeText.GetType().Name}) for {sensorType} sensor!");
                return;
            }

            locText.text = firstDerivative.ToString("0.###");
        }

// ...existing code...