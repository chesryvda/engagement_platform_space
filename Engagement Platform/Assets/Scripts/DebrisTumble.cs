using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the space debris close to the route slowly tumble in place, so the player sees different sides
/// of the pieces while flying past. Pieces only rotate (around their own middle); they never move away.
/// - Picks the pieces under "root" that come within "maxDistanceToRoute" of the route, plus every piece
///   whose name starts with one of "alwaysInclude" (the props, like the teddy bear).
/// - Pieces named in "exclude" (e.g. the Technopolis tower) stay still.
/// - Each piece gets its own random axis and speed, so nothing turns in sync.
/// One script for all pieces keeps it cheap.
/// </summary>
public class DebrisTumble : MonoBehaviour
{
    [Header("Which pieces")]
    [Tooltip("The debris field; its direct children are the pieces.")]
    public Transform root;
    public RouteMover routeMover;
    [Tooltip("Pieces whose middle comes this close to the route (in metres) will tumble.")]
    public float maxDistanceToRoute = 20f;
    [Tooltip("Pieces whose name starts with one of these always tumble (the props).")]
    public List<string> alwaysInclude = new List<string> { "DebrisProp" };
    [Tooltip("Pieces with these names never tumble.")]
    public List<string> exclude = new List<string> { "TechnopolisTower" };

    [Header("Rotation")]
    [Tooltip("Slowest rotation, in degrees per second.")]
    public float minSpeed = 3f;
    [Tooltip("Fastest rotation, in degrees per second.")]
    public float maxSpeed = 10f;
    [Tooltip("Same number = same random axes and speeds every time.")]
    public int seed = 7;

    [Header("Live (read only)")]
    [SerializeField] private int tumblingPieces;

    struct Piece
    {
        public Transform transform;
        public Vector3 middle;
        public Vector3 axis;
        public float speed;
    }

    readonly List<Piece> pieces = new List<Piece>();

    void Start()
    {
        var random = new System.Random(seed);
        float Rand(float a, float b) => a + (float)random.NextDouble() * (b - a);

        // Sample the route once to measure how close each piece comes.
        var route = routeMover.route;
        float length = route.CalculateLength();
        var routePoints = new List<Vector3>();
        for (float d = 0f; d <= length; d += 2f)
            routePoints.Add(route.EvaluatePosition(d / length));

        foreach (Transform child in root)
        {
            if (exclude.Contains(child.name)) continue;
            var renderers = child.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);

            bool include = false;
            foreach (var prefix in alwaysInclude)
                if (child.name.StartsWith(prefix)) include = true;
            if (!include)
            {
                float nearest = float.MaxValue;
                foreach (var p in routePoints)
                    nearest = Mathf.Min(nearest, (p - b.center).sqrMagnitude);
                include = Mathf.Sqrt(nearest) <= maxDistanceToRoute;
            }
            if (!include) continue;

            Vector3 axis = new Vector3(Rand(-1f, 1f), Rand(-1f, 1f), Rand(-1f, 1f));
            if (axis.sqrMagnitude < 0.01f) axis = Vector3.up;
            float speed = Rand(minSpeed, maxSpeed) * (random.NextDouble() < 0.5 ? -1f : 1f);

            pieces.Add(new Piece { transform = child, middle = b.center, axis = axis.normalized, speed = speed });
        }
        tumblingPieces = pieces.Count;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        foreach (var p in pieces)
            p.transform.RotateAround(p.middle, p.axis, p.speed * dt);
    }
}
