using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Live speed display on the cockpit's velocity screen, in km/s.
/// The speed matches the "distance to the Moon" screen: the whole route stands for the 384,400 km to the Moon, so
/// every metre the ship really flies counts as (384,400 km / route length) km. The shown speed is the ship's real
/// speed (ShipSpeed.CurrentSpeed) times that same factor. So it follows the pedalling exactly: it rises when the
/// player pedals faster, drops when they stop, and in a slowdown zone it shows the steady crawl speed (FocusStops
/// holds the ship's speed there). The bar and its marker show the real speed on a 0 to maxSpeed scale.
/// A little smoothing keeps the number calm.
/// </summary>
public class VelocityScreen : MonoBehaviour
{
    [Header("Source")]
    public ShipSpeed shipSpeed;
    [Tooltip("Until the ride starts (RouteMover enabled on the first pedal) the ship stands still, so the screen shows 0. " +
             "Also gives the route length for the km/s conversion.")]
    public RouteMover routeMover;
    [Tooltip("Text after the number, drawn smaller (rich text).")]
    public string unitSuffix = "<size=40%> km/s</size>";

    [Header("Display parts")]
    [Tooltip("The big speed number.")]
    public TMP_Text number;
    [Tooltip("The bright bar fill (anchored on its left edge; its width is set).")]
    public RectTransform fill;
    [Tooltip("Optional soft glow behind the fill (same width as the fill).")]
    public RectTransform fillGlow;
    [Tooltip("The marker (triangle + line) that sits at the end of the fill.")]
    public RectTransform marker;

    [Header("Slowdown zone warning")]
    [Tooltip("Text shown under the speed bar while the ship is in a slowdown zone (any FocusStops stop).")]
    public TMP_Text slowdownLabel;
    public FocusStops focusStops;
    [Tooltip("How long the warning takes to fade in or out, in seconds.")]
    [Min(0.05f)] public float labelFade = 0.4f;
    [Tooltip("Pulses per second of the warning while it is shown (0 = no pulse).")]
    public float labelPulse = 0.8f;

    [Header("Distance to the Moon")]
    [Tooltip("The distance the whole route stands for, in km (same as on the Moon screen).")]
    public float routeDistanceKm = 384400f;

    [Header("Bar")]
    [Tooltip("Real ship speed (m/s) that fills the whole bar.")]
    public float maxSpeed = 60f;
    [Tooltip("Left edge of the bar and its full width, in the screen's canvas units.")]
    public float barLeft = -650f, barWidth = 1300f;

    [Header("Feel")]
    [Tooltip("How quickly the display follows the real speed, in seconds. Bigger = calmer.")]
    [Min(0.01f)] public float smoothing = 0.2f;

    [Header("Live (read only)")]
    [SerializeField] private float shown;
    [SerializeField] private float kmPerMetre;

    string lastText;
    float labelLevel;
    System.Reflection.FieldInfo activeStopField;

    void Start()
    {
        float length = routeMover != null && routeMover.route != null ? routeMover.route.CalculateLength() : 0f;
        kmPerMetre = length > 0f ? routeDistanceKm / length : 0f;
        // FocusStops keeps its active stop private; read it without changing that script.
        activeStopField = typeof(FocusStops).GetField("activeStop",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (slowdownLabel != null) slowdownLabel.alpha = 0f;
    }

    void Update()
    {
        if (slowdownLabel == null) return;
        bool inZone = focusStops != null && activeStopField != null &&
                      !string.IsNullOrEmpty((string)activeStopField.GetValue(focusStops));
        labelLevel = Mathf.MoveTowards(labelLevel, inZone ? 1f : 0f, Time.deltaTime / labelFade);
        float pulse = labelPulse > 0f ? 0.75f + 0.25f * Mathf.Cos(Time.time * labelPulse * 2f * Mathf.PI) : 1f;
        slowdownLabel.alpha = labelLevel * pulse;
    }

    void LateUpdate()
    {
        bool moving = routeMover == null || routeMover.enabled;
        float speed = moving && shipSpeed != null ? Mathf.Max(0f, shipSpeed.CurrentSpeed) : 0f;
        shown = Mathf.Lerp(shown, speed, 1f - Mathf.Exp(-Time.deltaTime / smoothing));
        if (Mathf.Abs(shown - speed) < 0.01f) shown = speed;

        float kmPerSecond = shown * kmPerMetre;
        string text = kmPerSecond >= 100f
            ? kmPerSecond.ToString("#,0", CultureInfo.InvariantCulture)   // e.g. 3,783
            : kmPerSecond.ToString("0.0", CultureInfo.InvariantCulture);  // e.g. 95.2
        if (number != null && text != lastText)
        {
            number.text = text + unitSuffix;
            lastText = text;
        }

        float width = barWidth * Mathf.Clamp01(shown / maxSpeed);
        if (fill != null) fill.sizeDelta = new Vector2(width, fill.sizeDelta.y);
        if (fillGlow != null) fillGlow.sizeDelta = new Vector2(width, fillGlow.sizeDelta.y);
        if (marker != null) marker.anchoredPosition = new Vector2(barLeft + width, marker.anchoredPosition.y);
    }
}
