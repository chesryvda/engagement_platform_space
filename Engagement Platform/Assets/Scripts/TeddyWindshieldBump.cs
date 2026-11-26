using System.Reflection;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Lets a teddy bear from the space debris float straight at the player during the debris slowdown zone,
/// bump against the cockpit windscreen and bounce away tumbling.
/// - At the start of the ride the bear is moved to a spot straight ahead on the route (inside the zone where
///   the ship glides slowly), at the height of the player's eyes. It hangs there, bobbing gently.
/// - Once the slowdown stop is running and the ship is close, the bear drifts towards the ship and lines up
///   with the windscreen in front of the player's face.
/// - On contact it bounces off the glass (upwards and to the side) and keeps floating away, spinning.
/// Only the bear is moved. It reads RouteMover and FocusStops (the private "activeStop" field, through
/// reflection, like the other scripts do) and never changes them. Runs in LateUpdate, so it overrides the
/// slow tumble DebrisTumble gives the bear.
/// </summary>
public class TeddyWindshieldBump : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The teddy bear (leave empty to use this object).")]
    public Transform teddy;
    public RouteMover routeMover;
    public FocusStops focusStops;
    [Tooltip("The ship that carries the cockpit (RouteMover's ship).")]
    public Transform ship;
    [Tooltip("Name of the slowdown stop in FocusStops during which the bear comes in.")]
    public string stopName = "Ruimteafval";

    [Header("Where the bear waits")]
    [Tooltip("Place on the route (in metres) where the bear waits, straight ahead of the ship.")]
    public float waitDistanceOnRoute = 1162f;
    [Tooltip("Where the bear flies, in the ship's own space: x = sideways, y = height (eyes are at about 2.2 m).")]
    public Vector2 lane = new Vector2(0.10f, 2.45f);
    [Tooltip("Gentle up-and-down bobbing while it waits (metres).")]
    public float bobHeight = 0.15f;

    [Header("Coming in")]
    [Tooltip("When the ship is this close (metres ahead of the ship), the bear starts drifting towards it.")]
    public float approachStartDistance = 14f;
    [Tooltip("How fast the bear itself drifts towards the ship (m/s), on top of the ship's own speed.")]
    public float driftSpeed = 1.2f;
    [Tooltip("How quickly the bear lines up with the windscreen (per second).")]
    public float lineUpRate = 1.2f;

    [Header("Windscreen (in the ship's own space)")]
    [Tooltip("A point on the outside of the windscreen, straight in front of the player's eyes.")]
    public Vector3 glassPoint = new Vector3(0.10f, 2.21f, 1.11f);
    [Tooltip("Outward direction of the windscreen at that point.")]
    public Vector3 glassNormal = new Vector3(0f, 0.89f, 0.45f);
    [Tooltip("Distance from the bear's middle to its belly, used to detect contact with the glass.")]
    public float teddyRadius = 0.5f;

    [Header("Bounce (speeds relative to the ship, for a normal bump of about 4 m/s)")]
    [Tooltip("How fast it bounces back forward, away from the player (m/s). Keeps it in view.")]
    public float bounceForward = 1.6f;
    [Tooltip("How fast it rises after the bump (m/s).")]
    public float bounceUp = 0.9f;
    [Tooltip("How fast it drifts sideways after the bump (m/s), so it clears the ship's path.")]
    public float pushSideways = 0.7f;
    [Tooltip("Spin right after the bump (degrees per second); it slowly calms down.")]
    public float bumpSpin = 140f;
    enum Phase { Waiting, Approaching, Bounced }

    [Header("Live (read only)")]
    [SerializeField] private Phase phase = Phase.Waiting;

    private FieldInfo activeStopField;
    private Vector3 centerLocal;       // the bear's visual middle, in its own space
    private Vector3 center;            // the bear's middle in the world
    private Quaternion rotation;
    private Vector3 velocity;          // world velocity of the bear
    private Vector3 spinAxis;
    private float spinSpeed;
    private Vector3 waitPoint;
    private Vector3 lastShipPos;
    private Vector3 shipVelocity;
    private float bobTime;

    void Start()
    {
        if (teddy == null) teddy = transform;
        if (ship == null && routeMover != null) ship = routeMover.ship;
        activeStopField = typeof(FocusStops).GetField("activeStop", BindingFlags.NonPublic | BindingFlags.Instance);

        var renderers = teddy.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        centerLocal = teddy.InverseTransformPoint(b.center);
        rotation = teddy.rotation;

        // Wait straight ahead on the route, in the lane in front of the windscreen.
        var route = routeMover.route;
        float length = route.CalculateLength();
        route.Evaluate(Mathf.Clamp01(waitDistanceOnRoute / length), out float3 pos, out float3 tangent, out float3 up);
        Quaternion frame = Quaternion.LookRotation((Vector3)tangent, (Vector3)up);
        waitPoint = (Vector3)pos + frame * new Vector3(lane.x, lane.y, 0f);
        center = waitPoint;

        var random = new System.Random(11);
        spinAxis = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f).normalized;
        spinSpeed = 8f;
        lastShipPos = ship.position;
        Apply();
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        shipVelocity = (ship.position - lastShipPos) / dt;
        if (shipVelocity.magnitude > 200f) shipVelocity = Vector3.zero; // ignore jumps (e.g. testing)
        lastShipPos = ship.position;

        Vector3 n = ship.TransformDirection(glassNormal.normalized);
        Vector3 local = ship.InverseTransformPoint(center);

        switch (phase)
        {
            case Phase.Waiting:
                bobTime += dt;
                center = waitPoint + ship.up * Mathf.Sin(bobTime * 0.8f) * bobHeight;
                if (InStop() && local.z > 0f && local.z <= approachStartDistance)
                {
                    phase = Phase.Approaching;
                    velocity = Vector3.zero;
                    spinSpeed = 15f;
                }
                break;

            case Phase.Approaching:
                // Drift towards the ship and line up with the lane in front of the player's face.
                Vector3 lineUp = ship.TransformDirection(new Vector3(lane.x - local.x, lane.y - local.y, 0f)) * lineUpRate;
                Vector3 wanted = -ship.forward * driftSpeed + lineUp;
                velocity = Vector3.MoveTowards(velocity, wanted, 2f * dt);
                center += velocity * dt;

                // Contact with the windscreen: the bear's belly reaches the glass.
                float gap = Vector3.Dot(center - ship.TransformPoint(glassPoint), n);
                if (gap <= teddyRadius && ship.InverseTransformPoint(center).z < glassPoint.z + 1.5f)
                {
                    center += n * (teddyRadius - gap); // no sinking into the glass
                    Bounce(n);
                }
                break;

            case Phase.Bounced:
                center += velocity * dt;
                spinSpeed = Mathf.MoveTowards(spinSpeed, 35f, 30f * dt);
                // Never let the ship catch up with it again: keep it out of the glass.
                float again = Vector3.Dot(center - ship.TransformPoint(glassPoint), n);
                Vector3 l = ship.InverseTransformPoint(center);
                if (again < teddyRadius && Mathf.Abs(l.x) < 2f && l.z > -3f && l.z < glassPoint.z + 1.5f)
                {
                    center += n * (teddyRadius - again);
                    Vector3 rel = velocity - shipVelocity;
                    if (Vector3.Dot(rel, n) < 0f) velocity -= Vector3.Dot(rel, n) * n;
                }
                break;
        }

        rotation = Quaternion.AngleAxis(spinSpeed * dt, spinAxis) * rotation;
        Apply();
    }

    // Bounce off the glass. A true mirror bounce off the sloped windscreen would throw the bear backwards over
    // the player's head and out of sight, so it bounces forward (away from the player), up and to the side,
    // harder when it hit harder. Speeds are relative to the ship, so the ship never runs into it again.
    private void Bounce(Vector3 n)
    {
        float impact = Mathf.Max(0f, -Vector3.Dot(velocity - shipVelocity, n.normalized) / 0.45f); // ~ closing speed
        float strength = Mathf.Clamp(impact / 4f, 0.6f, 1.8f);
        Vector3 rel = ship.forward * bounceForward * strength
                    + ship.up * bounceUp * strength
                    + ship.right * pushSideways;
        velocity = shipVelocity + rel;

        spinAxis = Vector3.Cross(n, rel).normalized;
        if (spinAxis.sqrMagnitude < 0.01f) spinAxis = ship.right;
        spinSpeed = bumpSpin;
        phase = Phase.Bounced;
    }

    private bool InStop()
    {
        if (focusStops == null || activeStopField == null) return false;
        return (string)activeStopField.GetValue(focusStops) == stopName;
    }

    // Put the bear so that its visual middle sits at "center".
    private void Apply()
    {
        teddy.rotation = rotation;
        Vector3 offset = teddy.TransformPoint(centerLocal) - teddy.position;
        teddy.position = center - offset;
    }
}
