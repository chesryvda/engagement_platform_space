using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sweeps the Tesla's windshield wipers back and forth, like a car's normal wiper setting.
/// - Each wiper turns around its pivot (its local Y axis, which points away from the glass), from its parked
///   position along the bottom of the windshield up to "sweepAngle" and back, slowing down at both ends.
/// - The windshield is curved, so each wiper also tilts a little during the sweep (its "tilt" curve, measured from
///   the glass in the Editor) to keep the blade on the glass.
/// - Runs from the start of the ride.
/// </summary>
public class WiperMotion : MonoBehaviour
{
    [Serializable]
    public class Wiper
    {
        public Transform transform;
        [Tooltip("Tilt (degrees, + lifts the tip) for each sweep angle, so the blade follows the curved glass.")]
        public AnimationCurve tilt = AnimationCurve.Constant(0f, 180f, 0f);
        [HideInInspector] public Quaternion parked;
    }

    public List<Wiper> wipers = new List<Wiper>();

    [Tooltip("How far the wipers swing up from their parked position, in degrees.")]
    [Range(0f, 180f)] public float sweepAngle = 180f;
    [Tooltip("Time for one full sweep up and back, in seconds.")]
    [Min(0.2f)] public float period = 1.5f;
    [Tooltip("Run the wipers from the start of the ride.")]
    public bool onFromStart = true;

    [Header("Live (read only)")]
    [SerializeField] private bool running;
    float time;

    void Awake()
    {
        foreach (var w in wipers)
            if (w.transform != null) w.parked = w.transform.localRotation;
        running = onFromStart;
    }

    void Update()
    {
        if (!running) return;
        time += Time.deltaTime;
        // 0 -> 1 -> 0 with a smooth slow-down at both ends of the sweep
        float t = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * time / period);
        float angle = sweepAngle * t;
        foreach (var w in wipers)
        {
            if (w.transform == null) continue;
            w.transform.localRotation = w.parked
                * Quaternion.AngleAxis(angle, Vector3.up)
                * Quaternion.AngleAxis(w.tilt.Evaluate(angle), Vector3.forward);
        }
    }

    /// <summary>Start or stop the wipers (they stop where they are).</summary>
    public void SetRunning(bool on) => running = on;
}
