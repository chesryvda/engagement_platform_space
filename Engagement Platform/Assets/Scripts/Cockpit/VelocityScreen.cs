using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Live speed display on the cockpit's velocity screen.
/// Shows the ship's real speed in m/s (ShipSpeed.CurrentSpeed), so it follows the pedalling: it rises when the
/// player pedals faster, drops when they stop, and in a slowdown zone it shows the steady crawl speed (FocusStops
/// holds the ship's speed there, whatever the player pedals). The bar and its marker show the same speed on a
/// 0 to maxSpeed scale. A little smoothing keeps the number calm.
/// </summary>
public class VelocityScreen : MonoBehaviour
{
    [Header("Source")]
    public ShipSpeed shipSpeed;
    [Tooltip("Until the ride starts (RouteMover enabled on the first pedal) the ship stands still, so the screen shows 0.")]
    public RouteMover routeMover;
    [Tooltip("Text after the number, drawn smaller (rich text).")]
    public string unitSuffix = "<size=40%> m/s</size>";

    [Header("Display parts")]
    [Tooltip("The big speed number.")]
    public TMP_Text number;
    [Tooltip("The bright bar fill (anchored on its left edge; its width is set).")]
    public RectTransform fill;
    [Tooltip("Optional soft glow behind the fill (same width as the fill).")]
    public RectTransform fillGlow;
    [Tooltip("The marker (triangle + line) that sits at the end of the fill.")]
    public RectTransform marker;

    [Header("Realistic numbers")]
    [Tooltip("Real ship speed (m/s) when cruising without pedalling, and what the screen shows then " +
             "(about 7,800 m/s: orbital speed of a Space Shuttle or the ISS).")]
    public float cruiseSpeed = 27.8f, cruiseDisplay = 7800f;
    [Tooltip("Real ship speed (m/s) at full pedalling, and what the screen shows then " +
             "(about 11,000 m/s: the realistic maximum, just below Earth's escape velocity).")]
    public float topSpeed = 61f, topDisplay = 11000f;
    [Tooltip("Shape of the curve below cruising. Smaller = slow speeds show higher numbers. " +
             "0.7 makes the 3 m/s slowdown crawl show about 1,600 m/s (lunar orbit speed).")]
    [Range(0.2f, 1f)] public float lowCurve = 0.7f;

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

    string lastText;

    void LateUpdate()
    {
        bool moving = routeMover == null || routeMover.enabled;
        float speed = moving && shipSpeed != null ? Mathf.Max(0f, shipSpeed.CurrentSpeed) : 0f;
        shown = Mathf.Lerp(shown, speed, 1f - Mathf.Exp(-Time.deltaTime / smoothing));
        if (Mathf.Abs(shown - speed) < 0.01f) shown = speed;

        float display = ToDisplay(shown);
        string text = display >= 100f
            ? display.ToString("#,0", CultureInfo.InvariantCulture)   // e.g. 7,784
            : display.ToString("0.0", CultureInfo.InvariantCulture);
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

    // Real ship speed -> realistic spacecraft speed on the screen. Always rises with the real speed and has no jumps:
    // a gentle curve from 0 up to cruising, then a straight line from cruising to the realistic maximum.
    float ToDisplay(float speed)
    {
        if (speed <= cruiseSpeed)
            return cruiseDisplay * Mathf.Pow(Mathf.Clamp01(speed / cruiseSpeed), lowCurve);
        float t = (speed - cruiseSpeed) / Mathf.Max(0.01f, topSpeed - cruiseSpeed);
        return Mathf.Lerp(cruiseDisplay, topDisplay, Mathf.Clamp01(t));
    }
}
