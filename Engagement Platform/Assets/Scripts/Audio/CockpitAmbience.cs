using System.Collections;
using UnityEngine;

/// <summary>
/// Everything you hear just by sitting in the cockpit.
/// - Startup when the experience begins: the ship's systems switch on and the air starts to flow. No chimes:
///   Lisa wakes the player with her voice.
/// - Beds that keep running all ride: the deep-space drone, air circulation and the dashboard hum.
/// - Small details at random moments: pipe ticks and relay clicks near the dashboard, and the hull
///   creaking somewhere around you.
/// Needs an ExperienceAudio in the scene.
/// </summary>
public class CockpitAmbience : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The ship. Random details are placed around the player, in this object's axes.")]
    public Transform ship;

    [Header("Startup")]
    public bool playStartupSequence = true;
    [Tooltip("The low hum of the ship's systems coming on.")]
    public AudioClip systemWake;

    [Header("Beds (loop all ride)")]
    public AudioClip spaceDrone;
    public AudioClip lifesupportLoop;
    public AudioClip dashboardHumLoop;

    [Header("Random details")]
    public SoundScatter dashboardTicks = new SoundScatter
    {
        interval = new Vector2(5f, 12f),
        level = new Vector2(0.3f, 0.5f),
        distance = new Vector2(0.6f, 1.2f),
        height = new Vector2(-0.7f, -0.1f),
        angle = new Vector2(-50f, 50f),
        hearingRange = 8f
    };
    public SoundScatter relayClicks = new SoundScatter
    {
        interval = new Vector2(9f, 20f),
        level = new Vector2(0.35f, 0.55f),
        distance = new Vector2(0.6f, 1.2f),
        height = new Vector2(-0.7f, -0.1f),
        angle = new Vector2(-60f, 60f),
        hearingRange = 8f
    };
    public SoundScatter hullCreaks = new SoundScatter
    {
        interval = new Vector2(18f, 40f),
        level = new Vector2(0.4f, 0.7f),
        pitch = new Vector2(0.9f, 1.05f),
        distance = new Vector2(2f, 3.5f),
        height = new Vector2(-0.5f, 1f),
        hearingRange = 15f
    };

    private ExperienceAudio audioSystem;
    private bool systemsOn;

    IEnumerator Start()
    {
        audioSystem = ExperienceAudio.Instance;
        if (audioSystem == null)
        {
            Debug.LogError("CockpitAmbience: there is no ExperienceAudio in the scene.");
            enabled = false;
            yield break;
        }

        // The drone is the silence of space: it is there from the very start.
        var drone = audioSystem.Loop(spaceDrone, AudioCategory.Ambience, 0f);
        if (drone != null) drone.FadeTo(0.7f, 4f);

        var air = audioSystem.Loop(lifesupportLoop, AudioCategory.Ambience, 0f);
        var hum = audioSystem.Loop(dashboardHumLoop, AudioCategory.Ambience, 0f);

        if (!playStartupSequence)
        {
            if (air != null) air.Set(0.5f);
            if (hum != null) hum.Set(0.35f);
            systemsOn = true;
            yield break;
        }

        yield return new WaitForSeconds(0.6f);

        // The ship's systems come on and the air starts to flow.
        audioSystem.Play2D(systemWake, AudioCategory.Effects, 0.8f);
        if (air != null) air.FadeTo(0.5f, 3f);
        if (hum != null) hum.FadeTo(0.35f, 3f);
        yield return new WaitForSeconds(3.5f);

        systemsOn = true;
    }

    void Update()
    {
        if (audioSystem == null) return;
        dashboardTicks.Tick(systemsOn, audioSystem, ship);
        relayClicks.Tick(systemsOn, audioSystem, ship);
        hullCreaks.Tick(systemsOn, audioSystem, ship);
    }
}
