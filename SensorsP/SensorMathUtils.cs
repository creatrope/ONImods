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

        public float GetFirstDerivative()
        {
            if (Samples.Count < 2)
                return 0f;
            var arr = Samples.ToArray();
            var last = arr[arr.Length - 1];
            var prev = arr[arr.Length - 2];
            float dt = last.time - prev.time;
            float dv = last.value - prev.value;
            HLib.CustomLogger.Log($"[SensorMathUtils] GetFirstDerivative: last=({last.time}, {last.value}), prev=({prev.time}, {prev.value}), dt={dt}, dv={dv}");
            if (dt == 0)
                return 0f;
            return dv / dt;
        }

        // Compute moving average of the first derivative over the last N samples
        public float ComputeMovingAverageFirstDerivative(int window = 5)
        {
            if (Samples.Count < 2)
                return 0f;
            var arr = Samples.ToArray();
            int count = Math.Min(window, arr.Length - 1);
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
        // Debug: Log input to derivative calculation
        HLib.CustomLogger.Log($"[SensorMathUtils] UpdateAndGetFirstDerivative: instance={instance}, time={time}, value={value}, dtExpected={dtExpected}");

        if (!table.TryGetValue(instance, out var state))
        {
            state = new DerivativeState<T>();
            table.Add(instance, state);
        }
        state.AddSample(time, value);

        // Debug: Log sample count after adding
        HLib.CustomLogger.Log($"[SensorMathUtils] DerivativeState sample count for {instance}: {state.Samples.Count}");

        float firstDerivative = state.GetFirstDerivative();
        state.LastFirstDerivative = firstDerivative; // Store for UI
        if (firstDerivative != 0f)
            state.LastNonzeroFirstDerivative = firstDerivative;

        // Update moving average of the first derivative (window size 3)
        state.MovingAverageFirstDerivative = state.ComputeMovingAverageFirstDerivative(3);

        return firstDerivative;
    }

    public static bool HasRibbonPort(LogicPorts ports, HashedString portId)
    {
        if (ports == null || ports.outputPortInfo == null)
            return false;
        return Array.FindIndex(ports.outputPortInfo, p => p.id == portId) >= 0;
    }
}