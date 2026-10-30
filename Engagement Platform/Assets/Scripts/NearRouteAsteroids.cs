using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small and medium asteroids that fly close to the route, and slowly drift away from it.
/// - Each rock tumbles around its own middle (random axis, slow speed, like AsteroidDrift).
/// - When the ship gets near a rock (see "Start Distance"), the rock starts to drift straight away from the
///   route. It starts at its own speed and slows down as it nears its limit, so it never goes further than
///   that and never moves towards the route or the ship.
/// - How far each rock may drift is measured in the Editor (right-click this component > Measure Travel
///   Limits) so it never reaches another rock. Run it again after moving or adding rocks.
/// - Put the rocks as direct children of this object. They are not part of AsteroidDrift, so the two scripts
///   never move the same rock. Add this object to DebrisFadeIn's roots to let the rocks fade in with the rest.
/// </summary>
public class NearRouteAsteroids : MonoBehaviour
{
    [Header("References")]
    public RouteMover routeMover;

    [Header("Drift away from the route")]
    [Tooltip("A rock starts to drift when the ship is this many metres (along the route) from it.")]
    public float startDistance = 250f;
    [Tooltip("Speed away from the route at the start of the drift, in m/s (random per rock between the two numbers).")]
    public Vector2 driftSpeed = new Vector2(0.8f, 2f);
    [Tooltip("Furthest a rock drifts from where it was placed, in metres. It slows down as it gets there.")]
    public float maxTravel = 12f;

    [Header("Rotation")]
    [Tooltip("Degrees per second (random per rock between the two numbers).")]
    public Vector2 spinSpeed = new Vector2(4f, 14f);
    public int seed = 21;

    [Header("Keep clear of (used by 'Measure Travel Limits')")]
    [Tooltip("Rocks the drifting rocks must never reach (e.g. Asteroids and AsteroidBelt).")]
    public List<Transform> obstacleRoots = new List<Transform>();
    [Tooltip("Space kept between a drifting rock and another rock, in metres. Keep it above AsteroidDrift's 'Max Drift' (8), because those rocks move too.")]
    public float clearance = 9f;

    [Header("Measured limits (filled by 'Measure Travel Limits')")]
    [SerializeField] private List<Transform> measuredRocks = new List<Transform>();
    [SerializeField] private List<float> travelLimits = new List<float>();

    class Rock
    {
        public Transform transform;
        public Vector3 startPosition;
        public Quaternion startRotation;
        public Vector3 middle;          // the middle of the mesh (its pivot can be far off), where it spins around
        public Vector3 away;            // unit vector, straight away from the nearest point on the route
        public float routeDistance;     // where along the route this rock is, in metres
        public float speed;             // m/s at the start of the drift
        public float limit;             // furthest it may drift, in metres
        public Vector3 spinAxis;
        public float spin;              // degrees per second
        public float driftStartedAt = -1f;
    }

    readonly List<Rock> rocks = new List<Rock>();
    float time;

#if UNITY_EDITOR
    // Measures how far each rock can drift away from the route before it would come within 'clearance' of
    // another rock, using the real shapes. Builds can't read mesh vertices, so this is done once in the Editor.
    [ContextMenu("Measure Travel Limits")]
    public void MeasureTravelLimits()
    {
        UnityEditor.Undo.RecordObject(this, "Measure travel limits");
        measuredRocks.Clear();
        travelLimits.Clear();

        var points = RoutePoints();
        var obstacles = new List<(Vector3 centre, float radius)>();
        foreach (var root in obstacleRoots)
            if (root != null)
                foreach (Transform c in root)
                    if (TryShape(c, out Vector3 centre, out float radius)) obstacles.Add((centre, radius));

        foreach (Transform rock in transform)
        {
            if (!TryShape(rock, out Vector3 middle, out float radius)) continue;
            Vector3 away = AwayFromRoute(points, middle, out _);

            float limit = maxTravel;
            foreach (var o in obstacles)
            {
                // Where does the rock's path (a straight line away from the route) come within reach of this obstacle?
                Vector3 from = middle - o.centre;
                float reach = o.radius + radius + clearance;
                float along = Vector3.Dot(from, away);
                float c = from.sqrMagnitude - reach * reach;
                if (c < 0f) { limit = 0f; break; }                 // already too close: it stays put
                float disc = along * along - c;
                if (disc < 0f) continue;                           // the path misses it
                float hit = -along - Mathf.Sqrt(disc);
                if (hit >= 0f) limit = Mathf.Min(limit, hit);
            }
            measuredRocks.Add(rock);
            travelLimits.Add(limit);
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }

    static bool TryShape(Transform t, out Vector3 centre, out float radius)
    {
        centre = t.position;
        radius = 0f;
        var mf = t.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return false;
        Vector3 middle = mf.sharedMesh.bounds.center;
        float max = 0f;
        foreach (var v in mf.sharedMesh.vertices) max = Mathf.Max(max, (v - middle).magnitude);
        centre = mf.transform.TransformPoint(middle);
        radius = max * mf.transform.lossyScale.x * 1.02f;
        return true;
    }
#endif

    List<Vector3> RoutePoints()
    {
        var route = routeMover.route;
        float length = route.CalculateLength();
        var points = new List<Vector3>();
        for (float d = 0f; d <= length; d += 2f) points.Add(route.EvaluatePosition(d / length));
        return points;
    }

    static Vector3 AwayFromRoute(List<Vector3> points, Vector3 position, out float routeDistance)
    {
        int nearest = 0;
        float best = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            float sq = (points[i] - position).sqrMagnitude;
            if (sq < best) { best = sq; nearest = i; }
        }
        routeDistance = nearest * 2f;
        return (position - points[nearest]).normalized;
    }

    void Start()
    {
        var random = new System.Random(seed);
        float Rand(float a, float b) => a + (float)random.NextDouble() * (b - a);

        var limits = new Dictionary<Transform, float>();
        for (int i = 0; i < measuredRocks.Count && i < travelLimits.Count; i++)
            if (measuredRocks[i] != null) limits[measuredRocks[i]] = travelLimits[i];

        var points = RoutePoints();
        foreach (Transform child in transform)
        {
            var rock = new Rock { transform = child, startPosition = child.position, startRotation = child.rotation };
            rock.middle = MiddleOf(child);
            rock.away = AwayFromRoute(points, rock.middle, out rock.routeDistance);
            rock.limit = limits.TryGetValue(child, out float limit) ? limit : maxTravel;

            rock.speed = Rand(driftSpeed.x, driftSpeed.y);
            var axis = new Vector3(Rand(-1f, 1f), Rand(-1f, 1f), Rand(-1f, 1f));
            rock.spinAxis = axis.sqrMagnitude < 0.01f ? Vector3.up : axis.normalized;
            rock.spin = Rand(spinSpeed.x, spinSpeed.y) * (random.NextDouble() < 0.5 ? -1f : 1f);
            rocks.Add(rock);
        }
    }

    void Update()
    {
        time += Time.deltaTime;
        float shipDistance = routeMover != null ? routeMover.Distance : 0f;

        foreach (var rock in rocks)
        {
            // The ship is near: the drift starts.
            if (rock.driftStartedAt < 0f && shipDistance >= rock.routeDistance - startDistance)
                rock.driftStartedAt = time;

            float travel = 0f;
            if (rock.driftStartedAt >= 0f && rock.limit > 0.01f)
                travel = rock.limit * (1f - Mathf.Exp(-rock.speed * (time - rock.driftStartedAt) / rock.limit));

            // Spin around the middle of the mesh, and move that middle away from the route.
            Quaternion spin = Quaternion.AngleAxis(rock.spin * time, rock.spinAxis);
            Vector3 middle = rock.middle + rock.away * travel;
            rock.transform.SetPositionAndRotation(middle + spin * (rock.startPosition - rock.middle), spin * rock.startRotation);
        }
    }

    // Middle of the rock's mesh (works in builds too; no need to read the mesh's vertices).
    static Vector3 MiddleOf(Transform t)
    {
        var mf = t.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) return mf.transform.TransformPoint(mf.sharedMesh.bounds.center);
        return t.position;
    }
}
