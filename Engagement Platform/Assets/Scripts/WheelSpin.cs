using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lets the Tesla's wheels swing gently back and forth around their axles, like loose wheels drifting in
/// space with the motor off — they never spin all the way round. The car itself doesn't move.
/// Each wheel rocks around its own sideways (X) axis, starting from the rotation it has in the scene;
/// the brake calipers are separate objects and stay still.
/// Every wheel moves on its own: its own random start direction, pace and swing size, built from a few
/// out-of-step waves so the wheels never fall into the same rhythm.
/// </summary>
public class WheelSpin : MonoBehaviour
{
    [Tooltip("The wheel objects to swing (their pivot sits on the axle).")]
    public List<Transform> wheels = new List<Transform>();

    [Tooltip("Average swing to either side of the rest position, in degrees.")]
    [Range(0f, 90f)] public float swingAngle = 25f;

    [Tooltip("Shortest and longest time (seconds) for one full swing. Each wheel picks its own pace in this range.")]
    public Vector2 swingPeriodRange = new Vector2(3f, 6f);

    [Tooltip("How much the swing size differs between wheels (0 = all the same).")]
    [Range(0f, 0.6f)] public float sizeVariation = 0.35f;

    [Tooltip("On: a new random pattern every time you press Play. Off: the same pattern every run.")]
    public bool newPatternEachRun = true;

    [Tooltip("Seed used when 'New Pattern Each Run' is off.")]
    public int seed = 4711;

    // Per wheel: three waves (frequency, phase, weight) and an overall direction + size.
    private struct WheelMotion
    {
        public Quaternion rest;
        public float direction;   // +1 or -1: which way it swings first
        public float size;        // degrees
        public Vector3 freq;      // rad/s for the three waves
        public Vector3 phase;
        public Vector3 weight;
    }

    private WheelMotion[] motions;

    void Start()
    {
        var random = newPatternEachRun ? new System.Random() : new System.Random(seed);
        float R(float min, float max) => min + (float)random.NextDouble() * (max - min);

        motions = new WheelMotion[wheels.Count];
        for (int i = 0; i < wheels.Count; i++)
        {
            float period = R(Mathf.Max(0.5f, swingPeriodRange.x), Mathf.Max(swingPeriodRange.x, swingPeriodRange.y));
            float baseFreq = Mathf.PI * 2f / period;

            motions[i] = new WheelMotion
            {
                rest = wheels[i] != null ? wheels[i].localRotation : Quaternion.identity,
                direction = random.NextDouble() < 0.5 ? -1f : 1f,
                size = swingAngle * (1f + R(-sizeVariation, sizeVariation)),
                // Main swing plus two slower, out-of-step waves: the pattern drifts and never repeats exactly.
                freq = new Vector3(baseFreq, baseFreq * R(0.31f, 0.47f), baseFreq * R(0.13f, 0.23f)),
                phase = new Vector3(R(0f, 6.283f), R(0f, 6.283f), R(0f, 6.283f)),
                weight = new Vector3(1f, R(0.2f, 0.35f), R(0.15f, 0.3f)),
            };
        }
    }

    void Update()
    {
        float t = Time.time;
        for (int i = 0; i < wheels.Count; i++)
        {
            var wheel = wheels[i];
            if (wheel == null) continue;

            var m = motions[i];
            float swing = m.weight.x * Mathf.Sin(m.freq.x * t + m.phase.x)
                        + m.weight.y * Mathf.Sin(m.freq.y * t + m.phase.y)
                        + m.weight.z * Mathf.Sin(m.freq.z * t + m.phase.z);
            swing /= m.weight.x + m.weight.y + m.weight.z; // keep within -1..1

            float angle = m.direction * m.size * swing;
            wheel.localRotation = m.rest * Quaternion.AngleAxis(angle, Vector3.right);
        }
    }
}
