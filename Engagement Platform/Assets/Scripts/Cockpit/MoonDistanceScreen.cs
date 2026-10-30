using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Live "distance to the Moon" display on the cockpit screen.
/// The distance follows how far the ship is along its route: 384,400 km at the start of the route and exactly 0 km
/// at its end (at the Moon). Pedalling faster moves the ship faster along the route, so the number counts down
/// faster; in the slowdown zones it counts down very slowly. The rocket icon slides along the line from its start
/// position to the Moon, and the line in front of it gets shorter.
/// </summary>
public class MoonDistanceScreen : MonoBehaviour
{
    [Header("Source")]
    public RouteMover routeMover;

    [Header("Display parts")]
    [Tooltip("The big distance number.")]
    public TMP_Text number;
    [Tooltip("Text after the number (rich text).")]
    public string unitSuffix = "<size=55%> km</size>";
    [Tooltip("The rocket icon that slides toward the Moon.")]
    public RectTransform rocket;
    [Tooltip("The line from the rocket's nose to the Moon (anchored on its right edge; its width is set).")]
    public RectTransform line;
    [Tooltip("Optional soft glow around the line (same width as the line).")]
    public RectTransform lineGlow;

    [Header("Scale")]
    [Tooltip("Distance to the Moon at the start of the route, in km.")]
    public float startDistanceKm = 384400f;
    [Tooltip("Rocket centre at the start and at the end of the route, in the screen's canvas units.")]
    public float rocketStartX = -641f, rocketEndX = 368f;
    [Tooltip("Distance from the rocket's centre to its nose, and where the line ends (at the arrowhead), in canvas units.")]
    public float noseOffset = 64f, lineEndX = 436f;

    [Header("Live (read only)")]
    [SerializeField] private float progress;
    [SerializeField] private float distanceKm;

    string lastText;

    void LateUpdate()
    {
        float length = routeMover != null ? routeMover.RouteLength : 0f;
        progress = length > 0f ? Mathf.Clamp01(routeMover.Distance / length) : 0f;
        distanceKm = startDistanceKm * (1f - progress);

        string text = Mathf.RoundToInt(distanceKm).ToString("#,0", CultureInfo.InvariantCulture);
        if (number != null && text != lastText)
        {
            number.text = text + unitSuffix;
            lastText = text;
        }

        float rocketX = Mathf.Lerp(rocketStartX, rocketEndX, progress);
        if (rocket != null) rocket.anchoredPosition = new Vector2(rocketX, rocket.anchoredPosition.y);
        float width = Mathf.Max(0f, lineEndX - (rocketX + noseOffset));
        if (line != null) line.sizeDelta = new Vector2(width, line.sizeDelta.y);
        if (lineGlow != null) lineGlow.sizeDelta = new Vector2(width, lineGlow.sizeDelta.y);
    }
}
