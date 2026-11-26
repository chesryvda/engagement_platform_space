using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Route overview on the cockpit screen: Earth - ISS - space debris - Tesla - asteroids - Moon, joined by lines.
/// It follows the ship along the route (RouteMover):
/// - Zones already passed: icon in the light "done" blue.
/// - The zone the ship is in now: icon in bright cyan with a soft glow that breathes a little.
/// - Zones still to come: icon in the dim colour.
/// - The line to the next zone fills up (in cyan) while the ship flies there; finished lines are light blue.
/// Each zone starts at the route segment with the same name (RouteMover.segments), so moving a segment in
/// RouteMover moves its zone here too. No words, only icons, so it works in every language.
/// </summary>
public class RouteProgressScreen : MonoBehaviour
{
    [System.Serializable]
    public class Zone
    {
        [Tooltip("Name of the RouteMover segment where this zone starts (empty = start of the route).")]
        public string segmentName;
        public Image icon;
        public Image glow;
    }

    [System.Serializable]
    public class Line
    {
        [Tooltip("The dim line (always the full length).")]
        public Image track;
        [Tooltip("The bright part (anchored on its left edge; its width is set).")]
        public RectTransform fill;
        public float fullWidth;
    }

    [Header("Source")]
    public RouteMover routeMover;

    [Header("Parts (in route order)")]
    public List<Zone> zones = new List<Zone>();
    [Tooltip("Line i joins zone i and zone i+1.")]
    public List<Line> lines = new List<Line>();

    [Header("Colours (taken from the design)")]
    public Color doneColor = new Color(0.45f, 0.67f, 0.76f, 1f);
    public Color activeColor = new Color(0.48f, 0.90f, 0.94f, 1f);
    public Color upcomingColor = new Color(0.17f, 0.24f, 0.32f, 1f);
    public Color lineDoneColor = new Color(0.31f, 0.49f, 0.57f, 1f);
    public Color lineUpcomingColor = new Color(0.15f, 0.22f, 0.30f, 1f);

    [Header("Feel")]
    [Tooltip("How long a colour change takes, in seconds.")]
    [Min(0.01f)] public float fade = 0.6f;
    [Tooltip("Breaths per second of the glow on the active zone (0 = steady).")]
    public float glowPulse = 0.35f;

    [Header("Live (read only)")]
    [SerializeField] private int activeZone;
    [SerializeField] private float[] zoneStart;

    float[] level;   // per zone: 0 = upcoming, 1 = done, 2 = active (blended smoothly)

    void Start()
    {
        zoneStart = new float[zones.Count];
        for (int i = 0; i < zones.Count; i++)
        {
            zoneStart[i] = 0f;
            if (routeMover == null || string.IsNullOrEmpty(zones[i].segmentName)) continue;
            foreach (var s in routeMover.segments)
                if (s.name == zones[i].segmentName) { zoneStart[i] = s.startDistance; break; }
        }
        level = new float[zones.Count];
        Apply(0f, true);
    }

    void LateUpdate() => Apply(routeMover != null ? routeMover.Distance : 0f, false);

    void Apply(float distance, bool instant)
    {
        activeZone = 0;
        for (int i = 0; i < zones.Count; i++) if (distance >= zoneStart[i]) activeZone = i;

        float k = instant ? 1f : 1f - Mathf.Exp(-Time.deltaTime / fade);
        float breathe = glowPulse > 0f ? 0.8f + 0.2f * Mathf.Cos(Time.time * glowPulse * 2f * Mathf.PI) : 1f;
        for (int i = 0; i < zones.Count; i++)
        {
            float target = i < activeZone ? 1f : i == activeZone ? 2f : 0f;
            level[i] = Mathf.Lerp(level[i], target, k);
            Color c = level[i] <= 1f ? Color.Lerp(upcomingColor, doneColor, level[i])
                                     : Color.Lerp(doneColor, activeColor, level[i] - 1f);
            if (zones[i].icon != null) zones[i].icon.color = c;
            if (zones[i].glow != null)
            {
                Color g = activeColor;
                g.a = Mathf.Clamp01(level[i] - 1f) * breathe;
                zones[i].glow.color = g;
            }
        }

        for (int i = 0; i < lines.Count && i + 1 < zones.Count; i++)
        {
            Line l = lines[i];
            float t = Mathf.Clamp01(Mathf.InverseLerp(zoneStart[i], zoneStart[i + 1], distance));
            if (l.track != null) l.track.color = lineUpcomingColor;
            if (l.fill != null)
            {
                l.fill.sizeDelta = new Vector2(l.fullWidth * t, l.fill.sizeDelta.y);
                var img = l.fill.GetComponent<Image>();
                if (img != null) img.color = t >= 1f ? lineDoneColor : activeColor;
            }
        }
    }
}
