using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using SensorsPlus; // Add this namespace to access Patches.Logger

public static class SensorMathUtils
{
    // Sampling interval in seconds (adjust here)
    public static float SamplingIntervalSeconds { get; set; } = 1.0f;

    // Public static property for the moving average window, default 3, min 1
    private static int _movingAverageWindow = 3;
    public static int MovingAverageWindow
    {
        get => _movingAverageWindow;
        set => _movingAverageWindow = value < 1 ? 1 : value;
    }

    public class DerivativeState<T>
    {
        public Queue<(float time, float value)> Samples = new Queue<(float, float)>();

        // Store the most recent first derivative for UI access
        public float LastFirstDerivative { get; set; } = 0f;
        public float LastNonzeroFirstDerivative { get; set; } = 0f;

        // Store the moving average of the first derivative
        public float MovingAverageFirstDerivative { get; set; } = 0f;

        public void AddSample(float time, float value)
        {
            Samples.Enqueue((time, value));
            if (Samples.Count > 32)
                Samples.Dequeue();
        }

        public float GetFirstDerivative()
        {
            if (Samples.Count < 2)
                return 0f;
            var arr = Samples.ToArray();
            var last = arr[arr.Length - 1];
            var prev = arr[arr.Length - 2];
            float dt = last.time - prev.time;
            float dv = last.value - prev.value;
            if (dt == 0)
                return 0f;
            return dv / dt;
        }

        // Use the global MovingAverageWindow if window <= 0
        public float ComputeMovingAverageFirstDerivative(int window = -1)
        {
            int actualWindow = window > 0 ? window : SensorMathUtils.MovingAverageWindow;
            if (Samples.Count < 2)
                return 0f;
            var arr = Samples.ToArray();
            int count = Math.Min(actualWindow, arr.Length - 1);
            float sum = 0f;
            int actual = 0;
            for (int i = arr.Length - count; i < arr.Length; i++)
            {
                float dt = arr[i].time - arr[i - 1].time;
                if (dt != 0)
                {
                    sum += (arr[i].value - arr[i - 1].value) / dt;
                    actual++;
                }
            }
            return actual > 0 ? sum / actual : 0f;
        }
    }

    public static float UpdateAndGetFirstDerivative<T>(
        ConditionalWeakTable<T, DerivativeState<T>> table,
        T instance,
        float time,
        float value,
        float dtExpected)
        where T : class
    {
        if (!table.TryGetValue(instance, out var state))
        {
            state = new DerivativeState<T>();
            table.Add(instance, state);
        }
        state.AddSample(time, value);

        float firstDerivative = state.GetFirstDerivative();
        state.LastFirstDerivative = firstDerivative; // Store for UI
        if (firstDerivative != 0f)
            state.LastNonzeroFirstDerivative = firstDerivative;

        // Use the global MovingAverageWindow
        state.MovingAverageFirstDerivative = state.ComputeMovingAverageFirstDerivative();

        return firstDerivative;
    }

    public static bool HasRibbonPort(LogicPorts ports, HashedString portId)
    {
        if (ports == null || ports.outputPortInfo == null)
            return false;
        return Array.FindIndex(ports.outputPortInfo, p => p.id == portId) >= 0;
    }

    // Add a reusable method for sample gathering, derivative calculation, and ribbon signal generation
    public static int ProcessSensorData<TSensor>(
        TSensor sensor,
        ConditionalWeakTable<TSensor, DerivativeState<TSensor>> derivativeStates,
        ref Dictionary<TSensor, float> lastSampleTimes,
        HashedString ribbonPortId,
        float samplingIntervalSeconds,
        int movingAverageWindow,
        Func<TSensor, float> getCurrentValue,
        Func<TSensor, bool> isSwitchedOn,
        Func<TSensor, bool> activateAboveThreshold,
        Func<TSensor, float> getThreshold,
        Func<TSensor, LogicPorts> getLogicPorts // Added parameter
    ) where TSensor : class
    {
        //Patches.Logger.Log($"[ProcessSensorData] ProcessSensorData for {id}");

        var ports = getLogicPorts(sensor); // Use the provided function to get LogicPorts
        if (!HasRibbonPort(ports, ribbonPortId))
        {
            Patches.Logger.Log($"[ProcessSensorData] Sensor {sensor} does not have ribbon port {ribbonPortId}.");
            return 0;
        }

        if (lastSampleTimes == null)
            lastSampleTimes = new Dictionary<TSensor, float>();

        float now = Time.time;
        float lastSampleTime = -1f;
        lastSampleTimes.TryGetValue(sensor, out lastSampleTime);

        //Patches.Logger.Log($"[ProcessSensorData] Current time: {now}, Last sample time: {lastSampleTime}");

        // Only add a new sample if time has advanced
        if (now > lastSampleTime)
        {
            float value = getCurrentValue(sensor);
            //Patches.Logger.Log($"[ProcessSensorData] Adding new sample. Value: {value}");
            UpdateAndGetFirstDerivative(derivativeStates, sensor, now, value, samplingIntervalSeconds);
            lastSampleTimes[sensor] = now;
        }

        float smoothedDerivative = 0.0f;
        if (derivativeStates.TryGetValue(sensor, out var state))
        {
            smoothedDerivative = state.ComputeMovingAverageFirstDerivative(movingAverageWindow);
            //Patches.Logger.Log($"[ProcessSensorData] Smoothed derivative: {smoothedDerivative}");
        }

        float threshold = getThreshold(sensor);
        bool above = activateAboveThreshold(sensor);
        bool bit0 = isSwitchedOn(sensor);
        //Patches.Logger.Log($"[ProcessSensorData] Threshold: {threshold}, Above: {above}, Bit0: {bit0}");

        bool p = bit0; // above or below target pressure
        bool f = Math.Abs(smoothedDerivative) > Math.Abs(threshold); // fast change flag
        bool d = smoothedDerivative > 0; // direction flag (positive or negative change)

        bool bit1 = (!p) && !(d && f); // signal falling fast, turn on source
        bool bit2 = p && d && f; // signal rising fast, turn on vent

        bit1 = above ? bit1 : !bit1;
        bit2 = above ? bit2 : !bit2;

        Patches.Logger.Log($"[ProcessSensorData] p: {p}, d: {d}, f: {f} => {bit0} {bit1} {bit2}");
        int result = (bit0 ? 1 : 0)
                   | (bit1 ? (1 << 1) : 0)
                   | (bit2 ? (1 << 2) : 0);

        //Patches.Logger.Log($"[ProcessSensorData] Final result: {result}");
        return result;
    }
}