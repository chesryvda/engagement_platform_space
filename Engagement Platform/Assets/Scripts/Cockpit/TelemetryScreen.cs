using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Live "Flight Telemetry" screen with realistic values that react to the pedalling.
/// - Engine output (kN): idles at about 10 kN before the ride, about 110 kN when cruising, up to about 180 kN at
///   full pedalling and about 30-40 kN in a slowdown zone. Speeding up takes a bit more thrust, braking less.
///   Drawn as a slowly scrolling line graph (the last 30 seconds).
/// - Power consumption: six systems whose load follows the engine output, each with a small flutter.
/// - Cabin pressure: a steady 101.3 kPa (sea-level pressure, like the ISS) with tiny fluctuations.
/// - System temperature: 21-24 °C (like the ISS), slowly warming when the engine works harder and cooling again.
/// All motion is small and smoothed so the screen looks alive without distracting.
/// </summary>
public class TelemetryScreen : MonoBehaviour
{
    [Header("Sources")]
    public ShipSpeed shipSpeed;
    public RouteMover routeMover;
    public FocusStops focusStops;

    [Header("Engine output")]
    public UILineGraph engineGraph;
    [Tooltip("Top of the engine graph scale in kN.")]
    public float engineScaleMax = 200f;
    [Tooltip("Thrust before the ride starts, in a slowdown crawl, when cruising and at full pedalling (kN).")]
    public float idleThrust = 10f, crawlThrust = 32f, cruiseThrust = 110f, fullThrust = 180f;
    [Tooltip("Real ship speeds (m/s) that go with cruising and full pedalling.")]
    public float cruiseSpeed = 27.8f, fullSpeed = 61f;
    [Tooltip("Seconds of history shown on the graph, and how many points it uses.")]
    public float historySeconds = 30f;
    public int historyPoints = 60;

    [Header("Power consumption")]
    public RectTransform[] powerBars = new RectTransform[6];
    [Tooltip("Load of each system at cruising (on the 0-200 scale).")]
    public float[] powerBase = { 80f, 120f, 100f, 150f, 145f, 160f };
    public float powerScaleMax = 200f;
    [Tooltip("Full height of a bar at the top of the scale, in canvas units.")]
    public float barFullHeight = 337f;

    [Header("Cabin pressure")]
    public UILineGraph pressureGraph;
    public TMP_Text pressureText;
    public float pressureKPa = 101.3f;
    [Tooltip("Where 101 kPa sits on the graph (0-1), and how far 1 kPa moves the line (0-1).")]
    public float pressureGraphAt101 = 0.503f, pressureGraphPerKPa = 0.0175f;
    [Tooltip("Makes the tiny pressure changes a bit more visible on the line (the number stays real).")]
    public float pressureRippleScale = 6f;

    [Header("System temperature")]
    public Image temperatureFill;
    public TMP_Text temperatureText;
    [Tooltip("Temperature at rest and the extra warming at full engine load (°C).")]
    public float restTemperature = 21.5f, maxWarming = 2.5f;
    [Tooltip("How slowly the temperature follows the engine, in seconds.")]
    public float temperatureLag = 25f;
    [Tooltip("Temperature that fills the whole gauge arc (°C).")]
    public float gaugeMax = 30f;

    [Header("Live (read only)")]
    [SerializeField] private float thrust;
    [SerializeField] private float temperature;
    [SerializeField] private float pressure;

    readonly List<float> engineHistory = new List<float>();
    readonly List<float> pressureHistory = new List<float>();
    readonly List<Vector2> pts = new List<Vector2>();
    float[] power;
    float sampleTimer, lastSpeed, accel, seed;
    FieldInfo activeStopField;

    void Start()
    {
        seed = Random.value * 100f;
        activeStopField = typeof(FocusStops).GetField("activeStop", BindingFlags.NonPublic | BindingFlags.Instance);
        thrust = idleThrust;
        temperature = restTemperature - 0.5f;
        pressure = pressureKPa;
        power = new float[powerBase.Length];
        for (int i = 0; i < power.Length; i++) power[i] = powerBase[i] * 0.7f;
        for (int i = 0; i < historyPoints; i++) { engineHistory.Add(idleThrust); pressureHistory.Add(pressureKPa); }
    }

    void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        float t = Time.time;
        bool started = routeMover == null || routeMover.enabled;
        bool inStop = focusStops != null && activeStopField != null &&
                      !string.IsNullOrEmpty((string)activeStopField.GetValue(focusStops));
        float speed = started && shipSpeed != null ? shipSpeed.CurrentSpeed : 0f;
        accel = Mathf.Lerp(accel, (speed - lastSpeed) / dt, 1f - Mathf.Exp(-dt / 0.5f));
        lastSpeed = speed;

        // --- engine thrust (kN) ---
        float target;
        if (!started) target = idleThrust;
        else if (inStop && speed < 4f) target = crawlThrust;
        else if (speed <= cruiseSpeed) target = Mathf.Lerp(crawlThrust, cruiseThrust, speed / cruiseSpeed);
        else target = Mathf.Lerp(cruiseThrust, fullThrust, (speed - cruiseSpeed) / Mathf.Max(0.01f, fullSpeed - cruiseSpeed));
        if (started) target += Mathf.Clamp(accel * 1.5f, -25f, 25f);   // speeding up needs extra thrust, braking less
        target += (Mathf.PerlinNoise(seed, t * 0.6f) - 0.5f) * 5f;       // small flutter
        thrust = Mathf.Lerp(thrust, Mathf.Clamp(target, 0f, engineScaleMax), 1f - Mathf.Exp(-dt / 0.6f));
        float load = Mathf.InverseLerp(idleThrust, fullThrust, thrust);

        // --- cabin pressure (kPa) ---
        pressure = pressureKPa + (Mathf.PerlinNoise(seed + 7f, t * 0.15f) - 0.5f) * 0.16f;

        // --- system temperature (°C) ---
        float tempTarget = restTemperature + (started ? maxWarming * load : -0.5f);
        temperature = Mathf.Lerp(temperature, tempTarget, 1f - Mathf.Exp(-dt / temperatureLag));
        float shownTemp = temperature + (Mathf.PerlinNoise(seed + 13f, t * 0.3f) - 0.5f) * 0.1f;

        // --- history for the scrolling graphs ---
        float interval = historySeconds / Mathf.Max(2, historyPoints - 1);
        sampleTimer += dt;
        while (sampleTimer >= interval)
        {
            sampleTimer -= interval;
            engineHistory.RemoveAt(0); engineHistory.Add(thrust);
            pressureHistory.RemoveAt(0); pressureHistory.Add(pressure);
        }
        float scroll = sampleTimer / interval; // smooth sideways motion between samples

        if (engineGraph != null)
            DrawGraph(engineGraph, engineHistory, thrust, scroll, v => v / engineScaleMax);
        if (pressureGraph != null)
            DrawGraph(pressureGraph, pressureHistory, pressure, scroll,
                v => pressureGraphAt101 + (v - 101f) * pressureGraphPerKPa                       // real position
                     + (v - pressureKPa) * pressureGraphPerKPa * (pressureRippleScale - 1f));    // ripple made visible

        // --- power bars ---
        for (int i = 0; i < powerBars.Length && i < power.Length; i++)
        {
            if (powerBars[i] == null) continue;
            float v = powerBase[i] * (0.72f + 0.38f * load) + (Mathf.PerlinNoise(seed + 20f + i * 3.1f, t * 0.8f) - 0.5f) * 8f;
            power[i] = Mathf.Lerp(power[i], Mathf.Clamp(v, 0f, powerScaleMax * 0.97f), 1f - Mathf.Exp(-dt / 0.8f));
            powerBars[i].sizeDelta = new Vector2(powerBars[i].sizeDelta.x, barFullHeight * power[i] / powerScaleMax);
        }

        // --- numbers and gauge ---
        if (pressureText != null) pressureText.text = pressure.ToString("0.0", CultureInfo.InvariantCulture) + "<size=45%> kPa</size>";
        if (temperatureText != null) temperatureText.text = shownTemp.ToString("0.0", CultureInfo.InvariantCulture) + "°C";
        if (temperatureFill != null) temperatureFill.fillAmount = 0.75f * Mathf.Clamp01(shownTemp / gaugeMax);
    }

    // History (oldest -> newest) drawn right-aligned, scrolling left; the last point is the live value.
    void DrawGraph(UILineGraph graph, List<float> history, float live, float scroll, System.Func<float, float> toY)
    {
        pts.Clear();
        int n = history.Count;
        float step = 1f / (n - 1);
        for (int i = 0; i < n; i++)
        {
            float x = (i - scroll) * step;
            if (x < 0f) continue;
            pts.Add(new Vector2(x, Mathf.Clamp01(toY(history[i]))));
        }
        pts.Add(new Vector2(1f, Mathf.Clamp01(toY(live))));
        graph.SetPoints(pts);
    }
}
