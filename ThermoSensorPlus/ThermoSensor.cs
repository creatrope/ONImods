using UnityEngine;
using KSerialization;
using HarmonyLib;
using static GameUtil;
using PeterHan.PLib.UI;

public class ThermoSensorPlus : KMonoBehaviour, ISim200ms
{
    private const float TIME_WINDOW = 10f;

    private float lastSampleTime = -1f;
    private float lastSampleTemp = float.NaN;
    private float lastDelta = float.NaN;

    [Serialize] public float thresholdDelta = 1.0f;
    [Serialize] public bool useAboveForDelta = true;

    [Serialize] public float thresholdSecondDelta = 1.0f;
    [Serialize] public bool useAboveForSecondDelta = true;

    private LogicPorts ports;
    private static readonly HashedString PORT_ID = "ThermoSensorPlusRibbonOut";

    protected override void OnSpawn()
    {
        base.OnSpawn();
        lastSampleTime = Time.time;
        lastSampleTemp = GetTemperature();

        ports = GetComponent<LogicPorts>();
        if (ports == null)
        {
            Debug.LogWarning("[ThermoSensorPlus] No LogicPorts found on LogicTemperatureSensor, adding default.");
            ports = gameObject.AddOrGet<LogicPorts>();
            ports.outputPortInfo = new LogicPorts.Port[] {
                LogicPorts.Port.RibbonOutputPort(
                    cell_offset: new CellOffset(0, 0),
                    id: PORT_ID,
                    description: "Delta Ribbon Output",
                    activeDescription: "Sending signal",
                    inactiveDescription: "No signal")
            };
        }
    }

    public void Sim200ms(float dt)
    {
        float now = Time.time;
        float currentTemp = GetTemperature();
        float delta;
        float secondDelta = float.NaN;

        float elapsed = now - lastSampleTime;
        if (elapsed >= TIME_WINDOW)
        {
            if (float.IsNaN(lastSampleTemp))
            {
                lastSampleTemp = currentTemp;
                lastSampleTime = now;
                return;
            }

            delta = (currentTemp - lastSampleTemp) / elapsed;
            if (!float.IsNaN(lastDelta))
                secondDelta = (delta - lastDelta) / elapsed;

            lastDelta = delta;
            lastSampleTemp = currentTemp;
            lastSampleTime = now;

            bool bit1 = !float.IsNaN(delta) && (useAboveForDelta ? delta > thresholdDelta : delta < thresholdDelta);
            bool bit2 = !float.IsNaN(secondDelta) && (useAboveForSecondDelta ? secondDelta > thresholdSecondDelta : secondDelta < thresholdSecondDelta);

            int output = (bit1 ? 2 : 0) | (bit2 ? 4 : 0);
            ports?.SendSignal(PORT_ID, output);

            string unit = GetUnitSuffix();
            float convertedDelta = ConvertDelta(delta);
            float convertedSecondDelta = ConvertDelta(secondDelta);
            float convertedTemp = ConvertTemperature(currentTemp);

            Debug.Log($"[ThermoSensorPlus] Temp={convertedTemp:F2} {unit} | dT={convertedDelta:F2} {unit}/s | d2T={convertedSecondDelta:F2} {unit}/s2 | Output={output}");
        }
    }

    private float GetTemperature()
    {
        int cell = Grid.PosToCell(transform.GetPosition());
        return Grid.IsValidCell(cell) ? Grid.Temperature[cell] : 0f;
    }

    private float ConvertTemperature(float tempK)
    {
        switch (temperatureUnit)
        {
            case TemperatureUnit.Celsius:
                return tempK - 273.15f;
            case TemperatureUnit.Fahrenheit:
                return (tempK - 273.15f) * 9f / 5f + 32f;
            default:
                return tempK;
        }
    }

    private string GetUnitSuffix()
    {
        switch (temperatureUnit)
        {
            case TemperatureUnit.Celsius:
                return "C";
            case TemperatureUnit.Fahrenheit:
                return "F";
            default:
                return "K";
        }
    }

    private float ConvertDelta(float deltaK)
    {
        switch (temperatureUnit)
        {
            case TemperatureUnit.Celsius:
                return deltaK;
            case TemperatureUnit.Fahrenheit:
                return deltaK * 9f / 5f;
            default:
                return deltaK;
        }
    }
}

public class ThermoSensorPlusSideScreen : PPanelSideScreen
{
    private GameObject label;

    public override void SetTarget(GameObject target)
    {
        Debug.Log("[ThermoSensorPlus] Side screen SetTarget called for: " + target?.name);
        label?.SetActive(true);
    }

    protected override void Build(PPanel container)
    {
        Debug.Log("[ThermoSensorPlus] Side screen prefab Build() called.");
        label = new GameObject("DeltaLabel");
        var text = label.AddComponent<UnityEngine.UI.Text>();
        text.text = "Delta Tracker Enabled";
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var rect = label.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200f, 30f);
        container.AddChild(label);
    }

    public override bool IsValidForTarget(GameObject target)
    {
        return target != null && target.GetComponent<ThermoSensorPlus>() != null;
    }
}

[HarmonyPatch(typeof(LogicTemperatureSensor), "OnSpawn")]
public static class ThermoSensorPlusPatch
{
    public static void Postfix(LogicTemperatureSensor __instance)
    {
        __instance.gameObject.AddOrGet<ThermoSensorPlus>();
    }
}

public sealed class ThermoSensorPlusMod : KMod.UserMod2
{
    public override void OnLoad(Harmony harmony)
    {
        base.OnLoad(harmony);
        PUIUtils.AddSideScreenContent<ThermoSensorPlusSideScreen>();
        Debug.Log("[ThermoSensorPlus] Side screen registered.");
    }
}
