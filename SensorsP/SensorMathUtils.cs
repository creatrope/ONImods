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
        T instance,
        float now,
        float value,
        float windowSeconds)
        where T : class
    {
        var state = table.GetOrCreateValue(instance);
        state.Samples.Enqueue((now, value));
        while (state.Samples.Count > 0 && now - state.Samples.Peek().time > windowSeconds)
            state.Samples.Dequeue();

        if (state.Samples.Count >= 2)
        {
            var oldest = state.Samples.Peek();
            var newest = state.Samples.ToArray()[state.Samples.Count - 1];
            float dt = newest.time - oldest.time;
            if (dt > 0.0001f)
                return (newest.value - oldest.value) / dt;
        }
        return 0f;
    }

    public static bool HasRibbonPort(LogicPorts ports, HashedString portId)
    {
        if (ports == null || ports.outputPortInfo == null)
            return false;
        return Array.FindIndex(ports.outputPortInfo, p => p.id == portId) >= 0;
    }
}