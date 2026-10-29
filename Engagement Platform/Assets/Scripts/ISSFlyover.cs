using System.Reflection;
using UnityEngine;

/// <summary>
/// Lets the ISS fly slowly over the ship along an orbit around the Earth, like a real station.
/// - The ISS moves on a circle around the Earth's centre, so its path bends gently with the Earth's curve
///   and it stays parallel to the surface. It crosses the route from right to left, a bit above and ahead
///   of where the ship slows down for it.
/// - It starts at the right as soon as the ride starts (first pedal) and adjusts its pace so it is close by
///   when the ship enters the slow zone (the "ISS" stop in FocusStops).
/// - During the slow zone it drifts over the ship at a slow, steady pace, and afterwards it keeps drifting
///   away along its orbit.
/// Speed changes are always gradual, so the motion never jerks.
/// The ISS's place in the scene is used as the reference for the slow zone (FocusStops reads it at Start),
/// so leave it where it is; this script moves it to its start point when Play begins.
/// </summary>
[DefaultExecutionOrder(10)] // after FocusStops, so FocusStops measures the ISS at its scene position
public class ISSFlyover : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The ISS object that moves.")]
    public Transform target;
    public RouteMover routeMover;
    [Tooltip("Used to know when the ship starts slowing down for the ISS.")]
    public FocusStops focusStops;
    [Tooltip("Name of the stop in FocusStops for the ISS.")]
    public string stopName = "ISS";
    [Tooltip("The Earth sphere. The ISS orbits around its centre.")]
    public Transform earth;

    [Header("Path")]
    [Tooltip("Where the ISS crosses over the route, in metres along the route.")]
    public float crossDistanceOnRoute = 560f;
    [Tooltip("How high above the route the ISS crosses, in metres (centre of the ISS).")]
    public float heightAboveRoute = 51f;
    [Tooltip("How far to the right of the crossing point the ISS starts, in metres along its orbit.")]
    public float startOffset = 120f;

    [Header("Speed")]
    [Tooltip("The slow pace while it passes over the ship, in m/s.")]
    public float slowSpeed = 4f;
    [Tooltip("Never faster than this, in m/s.")]
    public float maxSpeed = 10f;
    [Tooltip("How quickly the speed may change, in m/s per second. Smaller = smoother.")]
    public float speedChange = 1f;
    [Tooltip("Roughly where on the route the ship starts slowing down for the ISS, in metres.")]
    public float slowZoneStartOnRoute = 450f;
    [Tooltip("When the slow zone starts, the ISS should be this many seconds (at the slow pace) from crossing.")]
    public float secondsBeforeCrossing = 8f;

    [Header("Live (read only)")]
    [SerializeField] private string phase = "waiting";
    [SerializeField] private float positionOnOrbit; // metres from the crossing point, negative = right side
    [SerializeField] private float speed;

    Vector3 center, radial, along, offsetFromCenter;
    float radius;
    Quaternion crossRotation;
    FieldInfo activeStopField;
    bool slowZoneSeen;

    void Start()
    {
        var route = routeMover.route;
        float length = route.CalculateLength();
        float t = Mathf.Clamp01(crossDistanceOnRoute / length);
        Vector3 routePoint = route.EvaluatePosition(t);
        Vector3 routeUp = ((Vector3)route.EvaluateUpVector(t)).normalized;
        Vector3 routeForward = ((Vector3)route.EvaluateTangent(t)).normalized;
        Vector3 routeRight = Vector3.Cross(routeUp, routeForward).normalized;

        // The orbit: a circle around the Earth's centre through the crossing point, heading left.
        center = earth.position;
        Vector3 crossPoint = routePoint + routeUp * heightAboveRoute;
        radius = Vector3.Distance(crossPoint, center);
        radial = (crossPoint - center).normalized;
        along = Vector3.ProjectOnPlane(-routeRight, radial).normalized;

        // Keep the ISS's current look; move its middle (not its far-away pivot) onto the orbit.
        Vector3 issMiddle = MiddleOf(target);
        offsetFromCenter = target.position - issMiddle;
        crossRotation = target.rotation;

        activeStopField = typeof(FocusStops).GetField("activeStop", BindingFlags.NonPublic | BindingFlags.Instance);
        if (activeStopField == null)
            Debug.LogWarning("ISSFlyover: can't read the active stop from FocusStops; the ISS keeps its approach pace.");

        positionOnOrbit = -startOffset;
        Apply();
    }

    void Update()
    {
        // Waiting for the first pedal.
        if (!routeMover.enabled && phase == "waiting") return;

        if (!slowZoneSeen && activeStopField != null && (string)activeStopField.GetValue(focusStops) == stopName)
            slowZoneSeen = true;

        float wanted;
        if (!slowZoneSeen && routeMover.Distance < slowZoneStartOnRoute)
        {
            // Approach: pace ourselves to be close by when the ship reaches the slow zone.
            phase = "approach";
            float shipSpeed = Mathf.Max(5f, routeMover.shipSpeed.CurrentSpeed);
            float timeLeft = Mathf.Max(1f, (slowZoneStartOnRoute - routeMover.Distance) / shipSpeed);
            float goal = -slowSpeed * secondsBeforeCrossing;
            wanted = Mathf.Clamp((goal - positionOnOrbit) / timeLeft, slowSpeed, maxSpeed);
        }
        else
        {
            // Slow zone and after: drift over the ship and away at the slow pace.
            phase = positionOnOrbit < 0f ? "passing over" : "drifting away";
            wanted = slowSpeed;
        }

        speed = Mathf.MoveTowards(speed, wanted, speedChange * Time.deltaTime);
        positionOnOrbit += speed * Time.deltaTime;
        Apply();
    }

    // Place the ISS on its orbit; it tilts along with the Earth's curve so it stays parallel to the surface.
    void Apply()
    {
        float angle = positionOnOrbit / radius;
        Vector3 dir = radial * Mathf.Cos(angle) + along * Mathf.Sin(angle);
        Quaternion tilt = Quaternion.FromToRotation(radial, dir);
        Vector3 middle = center + dir * radius;
        target.SetPositionAndRotation(middle + tilt * offsetFromCenter, tilt * crossRotation);
    }

    static Vector3 MiddleOf(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return t.position;
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b.center;
    }
}
