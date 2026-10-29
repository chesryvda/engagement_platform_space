using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the asteroids you fly through drift slowly around their spot and tumble around their own middle.
/// - Each asteroid floats on a smooth, slow path (a mix of gentle waves) around where it was placed.
/// - It never moves closer to the route than it is now, so the opening the ship flies through stays the same
///   and no asteroid can touch the ship.
/// - How far each asteroid may drift is limited by its neighbours, so asteroids never move into each other.
///   Asteroids that sit close together drift less; free ones drift up to "maxDrift".
/// - Rotation works like DebrisTumble: random axis, slow speed.
/// </summary>
public class AsteroidDrift : MonoBehaviour
{
    [Header("Which asteroids")]
    [Tooltip("Parent of the asteroids that drift and tumble (its direct children).")]
    public Transform root;
    public RouteMover routeMover;
    [Tooltip("Other rocks the drifting asteroids must stay clear of (e.g. the belt). They don't move themselves.")]
    public List<Transform> neighbourRoots = new List<Transform>();

    [Header("Drift")]
    [Tooltip("Furthest an asteroid may drift from its spot, in metres.")]
    public float maxDrift = 8f;
    [Tooltip("Shortest time for one slow wave, in seconds. Shorter = faster drifting.")]
    public float minWavePeriod = 50f;
    [Tooltip("Longest time for one slow wave, in seconds.")]
    public float maxWavePeriod = 120f;
    [Tooltip("Extra space kept between two asteroids, in metres.")]
    public float spacing = 1f;

    [Header("Rotation")]
    public float minSpinSpeed = 3f;
    public float maxSpinSpeed = 10f;
    public int seed = 11;

    [Header("Measured shapes (filled by 'Measure Rocks')")]
    [Tooltip("Rocks whose real size was measured in the Editor. Right-click this component > Measure Rocks after moving or adding rocks.")]
    [SerializeField] private List<Transform> measuredRocks = new List<Transform>();
    [SerializeField] private List<float> measuredRadii = new List<float>();

    [Header("Live (read only)")]
    [SerializeField] private int asteroids;
    [SerializeField] private int drifting;
    [SerializeField] private float averageDrift;

    class Rock
    {
        public Transform transform;
        public Vector3 startPosition, startMiddle, awayFromRoute;
        public Quaternion startRotation;
        public float radius, drift;
        public Vector3 spinAxis; public float spinSpeed;
        public Vector3[] waveDirections = new Vector3[3];
        public float[] waveSpeeds = new float[3], wavePhases = new float[3];
    }

    readonly List<Rock> rocks = new List<Rock>();
    readonly Dictionary<Transform, float> knownRadius = new Dictionary<Transform, float>();
    float time;

#if UNITY_EDITOR
    // Measures how far each rock's mesh reaches from its middle (its real shape, not its box).
    // Builds can't read mesh vertices, so this is done once in the Editor and saved with the scene.
    [ContextMenu("Measure Rocks")]
    public void MeasureRocks()
    {
        UnityEditor.Undo.RecordObject(this, "Measure rocks");
        measuredRocks.Clear();
        measuredRadii.Clear();
        var roots = new List<Transform> { root };
        roots.AddRange(neighbourRoots);
        foreach (var r in roots)
        {
            if (r == null) continue;
            foreach (Transform c in r)
            {
                var mf = c.GetComponentInChildren<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                Vector3 middle = mf.sharedMesh.bounds.center;
                float max = 0f;
                foreach (var v in mf.sharedMesh.vertices) max = Mathf.Max(max, Vector3.Scale(v - middle, mf.transform.lossyScale).magnitude);
                measuredRocks.Add(c);
                measuredRadii.Add(max * 1.02f);
            }
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    void Start()
    {
        for (int i = 0; i < measuredRocks.Count && i < measuredRadii.Count; i++)
            if (measuredRocks[i] != null) knownRadius[measuredRocks[i]] = measuredRadii[i];

        var random = new System.Random(seed);
        float Rand(float a, float b) => a + (float)random.NextDouble() * (b - a);
        Vector3 RandDir() { var v = new Vector3(Rand(-1, 1), Rand(-1, 1), Rand(-1, 1)); return v.sqrMagnitude < 0.01f ? Vector3.up : v.normalized; }

        var route = routeMover.route;
        float length = route.CalculateLength();
        var routePoints = new List<Vector3>();
        for (float d = 0f; d <= length; d += 2f) routePoints.Add(route.EvaluatePosition(d / length));

        foreach (Transform child in root)
        {
            var r = child.GetComponentInChildren<Renderer>();
            if (r == null) continue;
            var rock = new Rock { transform = child, startPosition = child.position, startRotation = child.rotation };
            rock.startMiddle = MiddleOf(child);
            rock.radius = SpinRadius(child, rock.startMiddle);

            Vector3 nearest = routePoints[0];
            foreach (var p in routePoints)
                if ((p - rock.startMiddle).sqrMagnitude < (nearest - rock.startMiddle).sqrMagnitude) nearest = p;
            rock.awayFromRoute = (rock.startMiddle - nearest).normalized;

            rock.spinAxis = RandDir();
            rock.spinSpeed = Rand(minSpinSpeed, maxSpinSpeed) * (random.NextDouble() < 0.5 ? -1f : 1f);
            for (int k = 0; k < 3; k++)
            {
                rock.waveDirections[k] = RandDir();
                rock.waveSpeeds[k] = 2f * Mathf.PI / Rand(minWavePeriod, maxWavePeriod);
                rock.wavePhases[k] = Rand(0f, 2f * Mathf.PI);
            }
            rocks.Add(rock);
        }

        // Rocks that don't move but must not be hit.
        var fixedRocks = new List<(Vector3 middle, float radius)>();
        foreach (var n in neighbourRoots)
            if (n != null)
                foreach (Transform c in n)
                    if (c.GetComponentInChildren<Renderer>() != null)
                    {
                        Vector3 m = MiddleOf(c);
                        fixedRocks.Add((m, SpinRadius(c, m)));
                    }

        // Each rock may drift at most half the free space to a moving neighbour, all of it to a fixed one.
        foreach (var a in rocks)
        {
            float free = maxDrift;
            foreach (var b in rocks)
                if (b != a)
                    free = Mathf.Min(free, ((a.startMiddle - b.startMiddle).magnitude - a.radius - b.radius - spacing) * 0.5f);
            foreach (var f in fixedRocks)
                free = Mathf.Min(free, (a.startMiddle - f.middle).magnitude - a.radius - f.radius - spacing);
            a.drift = Mathf.Max(0f, free);
        }

        asteroids = rocks.Count;
        float total = 0f;
        foreach (var rock in rocks) { total += rock.drift; if (rock.drift > 0.5f) drifting++; }
        averageDrift = rocks.Count > 0 ? total / rocks.Count : 0f;
    }

    void Update()
    {
        time += Time.deltaTime;
        foreach (var rock in rocks)
        {
            Vector3 offset = Vector3.zero;
            for (int k = 0; k < 3; k++)
                offset += rock.waveDirections[k] * Mathf.Sin(rock.waveSpeeds[k] * time + rock.wavePhases[k]);
            offset *= rock.drift / 3f * 1.6f;
            if (offset.magnitude > rock.drift) offset = offset.normalized * rock.drift;

            // Never towards the route: drop any part of the drift that points at it.
            float towards = Vector3.Dot(offset, -rock.awayFromRoute);
            if (towards > 0f) offset += rock.awayFromRoute * towards;

            Quaternion spin = Quaternion.AngleAxis(rock.spinSpeed * time, rock.spinAxis);
            Vector3 middle = rock.startMiddle + offset;
            rock.transform.SetPositionAndRotation(middle + spin * (rock.startPosition - rock.startMiddle), spin * rock.startRotation);
        }
    }

    // Middle of the rock's mesh (works in builds too; no need to read the mesh's vertices).
    static Vector3 MiddleOf(Transform t)
    {
        var mf = t.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) return mf.transform.TransformPoint(mf.sharedMesh.bounds.center);
        return t.GetComponentInChildren<Renderer>().bounds.center;
    }

    // Space the rock needs while spinning: a sphere around its middle that holds the whole mesh.
    // Uses the measured size when there is one, otherwise a safe (larger) guess from the mesh's box.
    float SpinRadius(Transform t, Vector3 middle)
    {
        if (knownRadius.TryGetValue(t, out float measured)) return measured;
        var mf = t.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            return Vector3.Scale(mf.sharedMesh.bounds.extents, mf.transform.lossyScale).magnitude;
        return t.GetComponentInChildren<Renderer>().bounds.extents.magnitude;
    }
}
