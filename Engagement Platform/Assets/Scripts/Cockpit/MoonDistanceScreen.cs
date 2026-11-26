using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Live "distance to the Moon" display on the cockpit screen.
/// The distance follows how far the ship is along its route: 384,400 km at the start of the route and exactly 0 km
/// at its end (at the Moon). Pedalling faster moves the ship faster along the route, so the number counts down
/// faster; in the slowdown zones it counts down very slowly.
/// The progress bar between the rocket and the Moon icon fills up as the ship gets closer.
/// </summary>
public class MoonDistanceScreen : MonoBehaviour
{
    [Header("Source")]
    public RouteMover routeMover;

    [Header("Display parts")]
    [Tooltip("The big distance number.")]
    public TMP_Text number;
    [Tooltip("Text after the number (rich text).")]
    public string unitSuffix = "<size=70%> KM</size>";
    [Tooltip("The bar fill (anchored on its left edge; its width is set).")]
    public RectTransform barFill;

    [Header("Scale")]
    [Tooltip("Distance to the Moon at the start of the route, in km.")]
    public float startDistanceKm = 384400f;
    [Tooltip("Width of the bar fill when the ship has arrived, in the screen's canvas units.")]
    public float barFullWidth = 1054f;
    [Tooltip("Smallest width of the bar fill, so its rounded ends stay visible at the start.")]
    public float barMinWidth = 24f;

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

        if (barFill != null)
            barFill.sizeDelta = new Vector2(Mathf.Lerp(barMinWidth, barFullWidth, progress), barFill.sizeDelta.y);
    }
}
