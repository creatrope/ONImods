using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class SensorMathUtils
{
    public class DerivativeState<T>
    {
        public Queue<(float time, float value)> Samples = new Queue<(float, float)>();
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