using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Live speed display on the cockpit's velocity screen, in km/s.
/// The speed matches the "distance to the Moon" screen: the whole route stands for the 384,400 km to the Moon, so
/// every metre the ship really flies counts as (384,400 km / route length) km. The shown speed is the ship's real
/// speed (ShipSpeed.CurrentSpeed) times that same factor. So it follows the pedalling exactly: it rises when the
/// player pedals faster, drops when they stop, and in a slowdown zone it shows the steady crawl speed (FocusStops
/// holds the ship's speed there).
/// The arc around the number fills with the real speed on a 0 to maxSpeed scale.
/// While the ship brakes and crawls past an object (FocusStops.SlowingOrGliding) an icon fades in under the number
/// (no text, so it works in every language) and the arc turns to the warning colour. As soon as the ship speeds
/// up again, both fade back.
/// A little smoothing keeps the number calm.
/// </summary>
public class VelocityScreen : MonoBehaviour
{
    [Header("Source")]
    public ShipSpeed shipSpeed;
    [Tooltip("Until the ride starts (RouteMover enabled on the first pedal) the ship stands still, so the screen shows 0. " +
             "Also gives the route length for the km/s conversion.")]
    public RouteMover routeMover;
    [Tooltip("Text after the number, drawn smaller (rich text). Leave empty when the unit has its own text.")]
    public string unitSuffix = "";

    [Header("Display parts")]
    [Tooltip("The big speed number.")]
    public TMP_Text number;
    [Tooltip("The arc around the number (Image, Filled, Radial 360). Its fill amount follows the speed.")]
    public Image arcFill;
    [Tooltip("Fill amount of the arc at full speed (the arc is not a full circle).")]
    [Range(0f, 1f)] public float arcSweep = 0.744f;
    [Tooltip("Arc colour while driving normally and while in a slowdown zone.")]
    public Color arcColor = new Color(0.47f, 0.86f, 0.92f, 1f);
    public Color arcSlowdownColor = new Color(1f, 0.67f, 0.24f, 1f);

    [Header("Slowdown zone warning")]
    [Tooltip("Icon shown while the ship brakes or crawls in a slowdown zone (not while it speeds up again).")]
    public Graphic slowdownIcon;
    public FocusStops focusStops;
    [Tooltip("How long the warning takes to fade in or out, in seconds.")]
    [Min(0.05f)] public float labelFade = 0.4f;
    [Tooltip("Pulses per second of the warning while it is shown (0 = no pulse).")]
    public float labelPulse = 0.8f;

    [Header("Distance to the Moon")]
    [Tooltip("The distance the whole route stands for, in km (same as on the Moon screen).")]
    public float routeDistanceKm = 384400f;

    [Header("Scale")]
    [Tooltip("Real ship speed (m/s) that fills the whole arc.")]
    public float maxSpeed = 60f;

    [Header("Feel")]
    [Tooltip("How quickly the display follows the real speed, in seconds. Bigger = calmer.")]
    [Min(0.01f)] public float smoothing = 0.2f;

    [Header("Live (read only)")]
    [SerializeField] private float shown;
    [SerializeField] private float kmPerMetre;

    string lastText;
    float labelLevel;

    void Start()
    {
        float length = routeMover != null && routeMover.route != null ? routeMover.route.CalculateLength() : 0f;
        kmPerMetre = length > 0f ? routeDistanceKm / length : 0f;
        SetIconAlpha(0f);
    }

    void Update()
    {
        // Only while braking or crawling past the object: as soon as the ship speeds up again the warning goes.
        bool inZone = focusStops != null && focusStops.SlowingOrGliding;
        labelLevel = Mathf.MoveTowards(labelLevel, inZone ? 1f : 0f, Time.deltaTime / labelFade);
        float pulse = labelPulse > 0f ? 0.75f + 0.25f * Mathf.Cos(Time.time * labelPulse * 2f * Mathf.PI) : 1f;
        SetIconAlpha(labelLevel * pulse);
        if (arcFill != null) arcFill.color = Color.Lerp(arcColor, arcSlowdownColor, labelLevel);
    }

    void SetIconAlpha(float a)
    {
        if (slowdownIcon == null) return;
        Color c = slowdownIcon.color;
        c.a = a;
        slowdownIcon.color = c;
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

        if (arcFill != null) arcFill.fillAmount = arcSweep * Mathf.Clamp01(shown / maxSpeed);
    }
}
