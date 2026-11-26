using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Live telemetry screen with four rings: oxygen, fuel, outside temperature and cabin pressure. No words, only
/// numbers, units and icons, so it works in every language.
/// Every value sways a little all the time (slow, smooth noise) so the screen looks alive without distracting;
/// the dark part of each ring is the "empty" part and follows the value smoothly.
/// - Oxygen (%): around 98 %.
/// - Fuel (%): starts at 85 % and goes down along the route. Pedalling harder burns a little more.
/// - Outside temperature (°C): around -120 °C. Ring scale: tempRingMin (empty) to tempRingMax (full).
/// - Cabin pressure (kPa): around 101 kPa. Ring scale: pressureRingMin (empty) to pressureRingMax (full).
/// </summary>
public class TelemetryScreen : MonoBehaviour
{
    [Header("Sources")]
    public ShipSpeed shipSpeed;
    public RouteMover routeMover;

    [Header("Oxygen")]
    public TMP_Text oxygenText;
    [Tooltip("Dark 'empty' part of the ring (Image, Filled, Radial 360, from the top, counter-clockwise).")]
    public Image oxygenEmpty;
    public float oxygenPercent = 98f;
    [Tooltip("How many percent the oxygen sways up and down.")]
    public float oxygenSway = 1.2f;

    [Header("Fuel")]
    public TMP_Text fuelText;
    public Image fuelEmpty;
    [Tooltip("Fuel at the start and at the end of the route (%).")]
    public float fuelStart = 85f, fuelEnd = 62f;
    [Tooltip("Extra fuel used over the whole ride when pedalling at full speed all the time (%).")]
    public float fuelExtraAtFullSpeed = 6f;
    [Tooltip("Real ship speed (m/s) that counts as full speed.")]
    public float fullSpeed = 60f;
    [Tooltip("How many percent the fuel reading flutters (sloshing).")]
    public float fuelSway = 0.6f;

    [Header("Outside temperature")]
    public TMP_Text temperatureText;
    public Image temperatureEmpty;
    public float outsideTemperature = -120f;
    [Tooltip("How many degrees the temperature sways up and down.")]
    public float temperatureDrift = 3f;
    [Tooltip("Temperatures for an empty and a full ring (°C).")]
    public float tempRingMin = -160f, tempRingMax = -80f;

    [Header("Cabin pressure")]
    public TMP_Text pressureText;
    public Image pressureEmpty;
    public float pressureKPa = 101.3f;
    [Tooltip("How many kPa the pressure sways up and down.")]
    public float pressureSway = 0.5f;
    [Tooltip("Pressures for an empty and a full ring (kPa).")]
    public float pressureRingMin = 90f, pressureRingMax = 110f;

    [Header("Sway")]
    [Tooltip("How fast the values sway. Bigger = livelier. Keep it low for a calm screen.")]
    public float swaySpeed = 0.25f;

    [Header("Units (rich text after the number)")]
    public string percentSuffix = "<size=35%>%</size>";
    public string temperatureSuffix = "<size=35%>°C</size>";
    public string pressureSuffix = "<size=35%>kPa</size>";

    [Header("Live (read only)")]
    [SerializeField] private float oxygen;
    [SerializeField] private float fuel;
    [SerializeField] private float extraBurn;
    [SerializeField] private float temperatureNow;
    [SerializeField] private float pressureNow;

    float seed;
    string lastO2, lastFuel, lastTemp, lastPressure;

    void Start()
    {
        seed = Random.value * 100f;
        fuel = fuelStart;
    }

    // Smooth noise between -1 and 1: two slow waves mixed so it never looks like a regular pulse.
    float Sway(float offset)
    {
        float t = Time.time * swaySpeed;
        return (Mathf.PerlinNoise(seed + offset, t) - 0.5f) * 1.4f + (Mathf.PerlinNoise(seed + offset + 50f, t * 2.7f) - 0.5f) * 0.6f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        bool started = routeMover == null || routeMover.enabled;
        float length = routeMover != null ? routeMover.RouteLength : 0f;
        float progress = length > 0f ? Mathf.Clamp01(routeMover.Distance / length) : 0f;

        // Fuel: goes down along the route, a bit faster when pedalling hard, and sloshes a little.
        if (started && shipSpeed != null && length > 0f)
        {
            float load = Mathf.Clamp01(shipSpeed.CurrentSpeed / Mathf.Max(0.01f, fullSpeed));
            extraBurn += fuelExtraAtFullSpeed * load * Mathf.Max(0f, shipSpeed.CurrentSpeed) * dt / length;
        }
        float fuelTarget = Mathf.Lerp(fuelStart, fuelEnd, progress) - extraBurn;
        fuel = Mathf.Lerp(fuel, Mathf.Max(0f, fuelTarget), 1f - Mathf.Exp(-dt / 0.5f));
        float fuelShown = Mathf.Max(0f, fuel + Sway(31f) * fuelSway);

        oxygen = Mathf.Clamp(oxygenPercent + Sway(0f) * oxygenSway, 0f, 100f);
        temperatureNow = outsideTemperature + Sway(13f) * temperatureDrift;
        pressureNow = pressureKPa + Sway(7f) * pressureSway;

        Show(oxygenText, Mathf.RoundToInt(oxygen).ToString(CultureInfo.InvariantCulture), percentSuffix, ref lastO2);
        Show(fuelText, Mathf.RoundToInt(fuelShown).ToString(CultureInfo.InvariantCulture), percentSuffix, ref lastFuel);
        Show(temperatureText, Mathf.RoundToInt(temperatureNow).ToString(CultureInfo.InvariantCulture), temperatureSuffix, ref lastTemp);
        Show(pressureText, Mathf.RoundToInt(pressureNow).ToString(CultureInfo.InvariantCulture), pressureSuffix, ref lastPressure);

        SetRing(oxygenEmpty, oxygen / 100f);
        SetRing(fuelEmpty, fuelShown / 100f);
        SetRing(temperatureEmpty, Mathf.InverseLerp(tempRingMin, tempRingMax, temperatureNow));
        SetRing(pressureEmpty, Mathf.InverseLerp(pressureRingMin, pressureRingMax, pressureNow));
    }

    static void SetRing(Image empty, float full)
    {
        if (empty != null) empty.fillAmount = 1f - Mathf.Clamp01(full);
    }

    static void Show(TMP_Text label, string text, string suffix, ref string last)
    {
        if (label == null || text == last) return;
        label.text = text + suffix;
        last = text;
    }
}
