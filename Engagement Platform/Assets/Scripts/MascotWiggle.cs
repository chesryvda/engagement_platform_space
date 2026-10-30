using System.Reflection;
using UnityEngine;

/// <summary>
/// Bobblehead wiggle for the dashboard mascot, driven by how fast the player pedals.
/// - Before the first pedal the mascot stands still.
/// - While riding, higher RPM = faster and bigger wiggle; lower RPM = slower and smaller. Changes are smoothed.
/// - In a slowdown zone (any FocusStops stop) it settles into a calm, slow sway, whatever the pedalling.
/// The upper body sways side to side and nods a little on top of the springs; the springs bend along and squash
/// slightly in rhythm. Only rotations/scale of Mascot_Upper and Mascot_Springs change; the base stays put.
/// </summary>
public class MascotWiggle : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The part above the springs (pivot on top of the springs).")]
    public Transform upper;
    [Tooltip("The springs (pivot at their bottom).")]
    public Transform springs;
    public Cadence cadence;
    public RouteMover routeMover;
    public FocusStops focusStops;

    [Header("Pedalling")]
    [Tooltip("At or above this RPM the wiggle is at its strongest.")]
    public float rpmForMax = 80f;
    [Tooltip("Sway (degrees) and wiggles per second at full pedalling.")]
    public float maxSway = 10f, maxFrequency = 3f;
    [Tooltip("Sway (degrees) and wiggles per second at very low pedalling.")]
    public float minSway = 1f, minFrequency = 0.6f;

    [Header("Slowdown zones")]
    [Tooltip("Calm sway during a slowdown: degrees and wiggles per second.")]
    public float calmSway = 3f, calmFrequency = 0.45f;

    [Header("Feel")]
    [Tooltip("How quickly the wiggle follows changes in pedalling, in seconds. Bigger = smoother.")]
    [Min(0.05f)] public float response = 0.6f;
    [Tooltip("How much the springs squash at full sway (0.05 = 5%).")]
    public float squash = 0.05f;

    [Header("Live (read only)")]
    [SerializeField] private float sway;
    [SerializeField] private float frequency;

    Quaternion upperRest, springsRest;
    Vector3 springsScaleRest, upperOffset;
    float phase, phase2;
    FieldInfo activeStopField;

    void Start()
    {
        upperRest = upper.localRotation;
        springsRest = springs.localRotation;
        springsScaleRest = springs.localScale;
        upperOffset = upper.localPosition - springs.localPosition; // top of the springs, seen from their bottom
        activeStopField = typeof(FocusStops).GetField("activeStop", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    void Update()
    {
        float targetSway = 0f, targetFreq = minFrequency;
        bool riding = routeMover != null && routeMover.enabled;
        if (riding)
        {
            bool inStop = activeStopField != null && focusStops != null && !string.IsNullOrEmpty((string)activeStopField.GetValue(focusStops));
            if (inStop)
            {
                targetSway = calmSway;
                targetFreq = calmFrequency;
            }
            else
            {
                float p = Mathf.Clamp01((cadence != null ? cadence.RPM : 0f) / rpmForMax);
                targetSway = Mathf.Lerp(minSway, maxSway, p);
                targetFreq = Mathf.Lerp(minFrequency, maxFrequency, p);
            }
        }

        float k = 1f - Mathf.Exp(-Time.deltaTime / response);
        sway = Mathf.Lerp(sway, targetSway, k);
        frequency = Mathf.Lerp(frequency, targetFreq, k);

        // Two slightly different rhythms make the motion look organic instead of mechanical.
        phase += 2f * Mathf.PI * frequency * Time.deltaTime;
        phase2 += 2f * Mathf.PI * frequency * 0.83f * Time.deltaTime;

        float side = sway * Mathf.Sin(phase);              // left-right
        float nod = sway * 0.45f * Mathf.Sin(phase2 + 1.3f); // front-back
        float twist = sway * 0.2f * Mathf.Sin(phase * 1.3f);

        // Springs bend half as much as the body and squash a little at the ends of each sway.
        Quaternion springBend = Quaternion.Euler(nod * 0.5f, 0f, side * 0.5f);
        float sq = 1f - squash * (sway / Mathf.Max(0.01f, maxSway)) * Mathf.Abs(Mathf.Sin(phase));
        springs.localRotation = springsRest * springBend;
        springs.localScale = new Vector3(springsScaleRest.x, springsScaleRest.y * sq, springsScaleRest.z);

        // The body sits on top of the (bent, squashed) springs and sways a bit more.
        Vector3 top = new Vector3(upperOffset.x, upperOffset.y * sq, upperOffset.z);
        upper.localPosition = springs.localPosition + springBend * top;
        upper.localRotation = upperRest * Quaternion.Euler(nod, twist, side);
    }
}
