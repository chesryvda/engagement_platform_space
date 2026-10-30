using System.Reflection;
using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Makes the astronaut in the Tesla wave at the ship and look at it during the Tesla stop.
/// - Waving: the Animator's "Wave" bool plays the looping wave clip (A_AstronautWave) on the left arm.
///   The Animator blends the arm up and down smoothly.
/// - Looking: an Animation Rigging rig (AstronautRig) turns the neck and head toward a target. This script keeps
///   that target on the player's camera, so the head follows the ship as it passes.
/// - It starts when the ship has braked down to its slow crawl at the Tesla (the "Tesla" stop in FocusStops),
///   and stops when the ship starts speeding away again.
/// </summary>
public class AstronautWave : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    [Tooltip("The rig that turns the head and neck (its weight is faded in and out).")]
    public Rig lookRig;
    [Tooltip("Target the head looks at; moved to the player's camera every frame.")]
    public Transform lookTarget;
    [Tooltip("What the astronaut looks at. Empty = the main camera (the player).")]
    public Transform lookAt;
    public FocusStops focusStops;
    [Tooltip("Name of the stop in FocusStops for the Tesla.")]
    public string stopName = "Tesla";

    [Header("Feel")]
    [Tooltip("How long the head takes to turn toward the ship or back, in seconds.")]
    [Min(0.1f)] public float lookFadeTime = 1.2f;

    [Header("Live (read only)")]
    [SerializeField] private bool active;
    [SerializeField] private bool done;

    static readonly int WaveParam = Animator.StringToHash("Wave");
    FieldInfo activeStopField, stopTimeField, brakeTimeField, crawlTimeField;

    void Start()
    {
        // FocusStops keeps its stop timeline private; read it without changing that script.
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var type = typeof(FocusStops);
        activeStopField = type.GetField("activeStop", flags);
        stopTimeField = type.GetField("stopTime", flags);
        brakeTimeField = type.GetField("tBrake", flags);
        crawlTimeField = type.GetField("tCrawl", flags);
        if (activeStopField == null || stopTimeField == null || brakeTimeField == null || crawlTimeField == null)
            Debug.LogWarning("AstronautWave: can't read the stop timeline from FocusStops; call Begin()/End() yourself.");
        if (lookRig != null) lookRig.weight = 0f;
    }

    void Update()
    {
        if (focusStops != null && activeStopField != null && stopTimeField != null && brakeTimeField != null && crawlTimeField != null)
        {
            bool inStop = (string)activeStopField.GetValue(focusStops) == stopName;
            float t = (float)stopTimeField.GetValue(focusStops);
            float brakeEnd = (float)brakeTimeField.GetValue(focusStops);
            float crawlEnd = brakeEnd + (float)crawlTimeField.GetValue(focusStops);
            bool crawling = inStop && t >= brakeEnd && t < crawlEnd;   // slow part: after braking, before speeding up

            if (crawling && !active && !done) Begin();
            else if (!crawling && active) End();
        }

        Transform eye = lookAt;
        if (eye == null && Camera.main != null) eye = Camera.main.transform;
        if (eye == null && Camera.allCamerasCount > 0) eye = Camera.allCameras[0].transform; // e.g. the desktop test camera
        if (eye != null && lookTarget != null) lookTarget.position = eye.position;

        if (lookRig != null)
            lookRig.weight = Mathf.MoveTowards(lookRig.weight, active ? 1f : 0f, Time.deltaTime / lookFadeTime);
    }

    /// <summary>Start waving and looking at the ship.</summary>
    public void Begin()
    {
        active = true;
        if (animator != null) animator.SetBool(WaveParam, true);
    }

    /// <summary>Lower the arm and look forward again.</summary>
    public void End()
    {
        active = false;
        done = true;
        if (animator != null) animator.SetBool(WaveParam, false);
    }
}
