using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Controls the Tesla's headlights. By default they are on from the start of the ride ("On From Start"); with that
/// off, they stay dark until the ship reaches its slow crawl at the Tesla and then switch on.
/// - Glow: the headlight materials' emission is switched with a MaterialPropertyBlock, so the materials themselves
///   stay untouched (the Tesla's fade-in swaps materials and would otherwise undo the change).
/// - Beams: two spot lights in front of the headlights fade in together with the glow.
/// Once on, the headlights stay on.
/// </summary>
public class TeslaHeadlights : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The car body renderer that holds the headlight materials.")]
    public Renderer body;
    [Tooltip("Names of the headlight materials that glow when the lights are on.")]
    public List<string> headlightMaterials = new List<string> { "M_Emissivelights", "M_Projectorlights" };
    [Tooltip("Spot lights for the headlight beams (their intensity is faded in).")]
    public List<Light> beams = new List<Light>();
    public FocusStops focusStops;
    [Tooltip("Name of the stop in FocusStops for the Tesla.")]
    public string stopName = "Tesla";

    [Header("Feel")]
    [Tooltip("On = the headlights are already burning when the ride starts. Off = they switch on at the slow crawl.")]
    public bool onFromStart = true;
    [Tooltip("How long the lights take to come on, in seconds.")]
    [Min(0.05f)] public float fadeTime = 0.5f;
    [Tooltip("Brightness of each beam when fully on.")]
    public float beamIntensity = 10f;
    [Tooltip("How much brighter the headlight bulbs glow than their material's own emission.")]
    [Min(0f)] public float glowBoost = 2f;

    [Header("Live (read only)")]
    [SerializeField, Range(0f, 1f)] private float level;
    [SerializeField] private bool on;

    readonly List<int> indices = new List<int>();
    readonly List<Color> fullEmission = new List<Color>();
    MaterialPropertyBlock block;
    FieldInfo activeStopField, stopTimeField, brakeTimeField;
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        block = new MaterialPropertyBlock();
        var mats = body.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
            if (mats[i] != null && headlightMaterials.Contains(mats[i].name))
            {
                indices.Add(i);
                fullEmission.Add(mats[i].HasProperty(EmissionColor) ? mats[i].GetColor(EmissionColor) : Color.white);
            }

        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        activeStopField = typeof(FocusStops).GetField("activeStop", flags);
        stopTimeField = typeof(FocusStops).GetField("stopTime", flags);
        brakeTimeField = typeof(FocusStops).GetField("tBrake", flags);
        if (activeStopField == null || stopTimeField == null || brakeTimeField == null)
            Debug.LogWarning("TeslaHeadlights: can't read the stop timeline from FocusStops; call TurnOn() yourself.");

        on = onFromStart;
        Apply(on ? 1f : 0f);
    }

    void Update()
    {
        if (!on && focusStops != null && activeStopField != null && stopTimeField != null && brakeTimeField != null)
        {
            bool inStop = (string)activeStopField.GetValue(focusStops) == stopName;
            if (inStop && (float)stopTimeField.GetValue(focusStops) >= (float)brakeTimeField.GetValue(focusStops))
                TurnOn();
        }

        float target = on ? 1f : 0f;
        if (!Mathf.Approximately(level, target))
            Apply(Mathf.MoveTowards(level, target, Time.deltaTime / fadeTime));
    }

    /// <summary>Switch the headlights on (they fade in).</summary>
    public void TurnOn() => on = true;

    void Apply(float value)
    {
        level = value;
        for (int k = 0; k < indices.Count; k++)
        {
            body.GetPropertyBlock(block, indices[k]);
            block.SetColor(EmissionColor, fullEmission[k] * (value * glowBoost));
            body.SetPropertyBlock(block, indices[k]);
        }
        foreach (var beam in beams)
            if (beam != null)
            {
                beam.intensity = beamIntensity * value;
                beam.enabled = value > 0f;
            }
    }
}
