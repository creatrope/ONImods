using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class SensorMathUtils
{
    // Sampling interval in seconds (adjust here)
    public const float SamplingIntervalSeconds = 1.0f;

    public class DerivativeState<T>
    {
        public Queue<(float time, float value)> Samples = new Queue<(float, float)>();

        // Smoothing method
        public float GetSmoothedDerivative(int window = 3)
        {
            if (Samples.Count < 2)
                return 0f;
            var arr = Samples.ToArray();
            int count = Math.Min(window, arr.Length - 1);
            float sum = 0f;
            for (int i = arr.Length - count; i < arr.Length; i++)
            {
                float dt = arr[i].time - arr[i - 1].time;
                if (dt != 0)
                    sum += (arr[i].value - arr[i - 1].value) / dt;
            }
            return sum / count;
        }
    }

    public static float UpdateAndGetFirstDerivative<T>(
        ConditionalWeakTable<T, DerivativeState<T>> table,
        T sensor,
        float time,
        float value,
        float dtMin = 0.01f)
        where T : class
    {
        if (!table.TryGetValue(sensor, out var state))
        {
            state = new DerivativeState<T>();
            table.Add(sensor, state);
        }

        // Only add sample if time has advanced
        if (state.Samples.Count == 0 || time > state.Samples.Peek().time)
        {
            state.Samples.Enqueue((time, value));
            // Optionally, limit queue size
            while (state.Samples.Count > 16)
                state.Samples.Dequeue();
        }

        // ...existing derivative calculation logic...
        // (no change needed here)
        return state.GetSmoothedDerivative(3);
    }

    public static bool HasRibbonPort(LogicPorts ports, HashedString portId)
    {
        if (ports == null || ports.outputPortInfo == null)
            return false;
        return Array.FindIndex(ports.outputPortInfo, p => p.id == portId) >= 0;
    }
}