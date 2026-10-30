using UnityEngine;

/// <summary>
/// The ship's engine, driven by how fast the ship really goes (ShipSpeed.CurrentSpeed), so it also gets
/// quiet during the slow zones and winds down at the moon.
/// - Before the first pedal: an idle engine that sputters (an invitation to start pedaling).
/// - First pedal (the ride starts): the startup sound, then the engine layers fade in.
/// - Speed: low and high loops crossfade, pitch and sub rumble rise with speed. At top speed a soft
///   limiter tone joins.
/// - Slow zones: the ship brakes and glides, so the engine winds down (spool-down sound, engine almost
///   silent, pitch drops). When the ship starts to speed up again the engine starts up (startup sound)
///   and comes back. That is how you hear a slow zone begin and end.
/// - Arrival at the moon: the spool-down sound and the engine fades out. RouteAudio can start this a few
///   seconds early by calling SpoolDown.
/// The engine sits behind the seat (in the ship's axes), so you hear it behind you.
/// Needs an ExperienceAudio in the scene.
/// </summary>
[DefaultExecutionOrder(50)]
public class EngineAudio : MonoBehaviour
{
    [Header("References")]
    public RouteMover routeMover;
    public ShipSpeed shipSpeed;
    [Tooltip("Tells when the ship brakes and glides for an object.")]
    public FocusStops focusStops;
    [Tooltip("The ship. The engine sound is placed in this object's axes.")]
    public Transform ship;

    [Header("Clips")]
    public AudioClip idleLoop;
    public AudioClip lowLoop;
    public AudioClip highLoop;
    public AudioClip subRumbleLoop;
    public AudioClip limiterLoop;
    public AudioClip startup;
    public AudioClip spooldown;

    [Header("Feel")]
    [Tooltip("Where the engine sits, relative to the ship (x = right, y = up, z = ahead). Negative z = behind you.")]
    public Vector3 engineOffset = new Vector3(0f, 0.3f, -2.5f);
    [Tooltip("Engine pitch at the slowest and at the fastest speed.")]
    public Vector2 pitchRange = new Vector2(0.8f, 1.3f);
    [Tooltip("How loud the engine is while crawling at an object (full speed = 1).")]
    [Range(0f, 1f)] public float crawlLevel = 0.35f;
    [Tooltip("How loud the limiter tone is at top speed.")]
    [Range(0f, 1f)] public float limiterMaxLevel = 0.5f;
    [Tooltip("Speed that counts as full speed, in m/s. 0 = the fastest speed in the route.")]
    public float fullSpeed = 0f;
    [Tooltip("Seconds the engine layers take to fade in after the startup sound.")]
    [Min(0.1f)] public float startupFade = 2.5f;

    [Header("Slow zones")]
    [Tooltip("How loud the engine is while the ship glides at an object (0 = silent).")]
    [Range(0f, 1f)] public float zoneEngineLevel = 0.08f;
    [Tooltip("Engine pitch while it is wound down, as a fraction of normal.")]
    [Range(0.3f, 1f)] public float zonePitch = 0.7f;
    [Tooltip("Seconds the engine takes to wind down when the zone starts.")]
    [Min(0.1f)] public float windDownTime = 2.5f;
    [Tooltip("Seconds the engine takes to come back when the ship speeds up again.")]
    [Min(0.1f)] public float windUpTime = 1.5f;

    [Header("Live (read only)")]
    [SerializeField] private float speedNorm;
    [SerializeField] private string state = "idle";

    private ExperienceAudio audioSystem;
    private ExperienceAudio.Voice idle, low, high, sub, limiter;
    private bool started, arrived, wasSlow;
    private float ramp;     // 0 = engine layers silent, 1 = fully in
    private float zoneRamp = 1f; // 1 = engine running, 0 = wound down for a slow zone
    private float smoothed; // speed 0-1, smoothed a little more

    void Start()
    {
        audioSystem = ExperienceAudio.Instance;
        if (audioSystem == null)
        {
            Debug.LogError("EngineAudio: there is no ExperienceAudio in the scene.");
            enabled = false;
            return;
        }

        if (fullSpeed <= 0f && routeMover != null)
            foreach (var segment in routeMover.segments)
                fullSpeed = Mathf.Max(fullSpeed, segment.maxSpeed);
        if (fullSpeed <= 0f) fullSpeed = 60f;

        idle = EngineLoop(idleLoop, 1f);
        low = EngineLoop(lowLoop, 0f);
        high = EngineLoop(highLoop, 0f);
        sub = EngineLoop(subRumbleLoop, 0f);
        limiter = EngineLoop(limiterLoop, 0f);
    }

    private ExperienceAudio.Voice EngineLoop(AudioClip clip, float level)
    {
        if (clip == null) return null;
        var voice = audioSystem.Loop(clip, AudioCategory.Engine, level, ship, true, 3f, 40f);
        if (voice != null && ship != null) voice.Source.transform.localPosition = engineOffset;
        return voice;
    }

    private void PlayOnce(AudioClip clip, float level)
    {
        if (clip == null) return;
        audioSystem.Play3D(clip, AudioCategory.Engine, ship, engineOffset, level, 1f, 3f, 40f);
    }

    void Update()
    {
        bool rideOn = routeMover != null && routeMover.enabled;
        bool atEnd = routeMover != null && routeMover.RouteLength > 0f && routeMover.Distance >= routeMover.RouteLength - 0.5f;

        // First pedal: startup sound, idle fades away, engine layers fade in.
        if (rideOn && !started)
        {
            started = true;
            PlayOnce(startup, 1f);
            if (idle != null) idle.Stop(2f);
        }

        // Arrived: spool down (RouteAudio may already have asked for it a few seconds earlier).
        if (atEnd) SpoolDown();

        // Slow zone: the ship brakes and glides, so the engine winds down; when it speeds up again it starts up.
        bool slow = started && !arrived && focusStops != null && focusStops.SlowingOrGliding;
        if (slow && !wasSlow) PlayOnce(spooldown, 0.9f);
        else if (!slow && wasSlow && !arrived) PlayOnce(startup, 0.9f);
        wasSlow = slow;
        zoneRamp = Mathf.MoveTowards(zoneRamp, slow ? 0f : 1f, Time.deltaTime / (slow ? windDownTime : windUpTime));
        float zoneLevel = Mathf.Lerp(zoneEngineLevel, 1f, zoneRamp);
        float zonePitchMix = Mathf.Lerp(zonePitch, 1f, zoneRamp);

        float rampTarget = started && !arrived ? 1f : 0f;
        float rampSeconds = arrived && spooldown != null ? spooldown.length : startupFade;
        ramp = Mathf.MoveTowards(ramp, rampTarget, Time.deltaTime / rampSeconds);

        float speed = shipSpeed != null ? shipSpeed.CurrentSpeed : 0f;
        speedNorm = Mathf.Clamp01(speed / fullSpeed);
        smoothed = Mathf.Lerp(smoothed, speedNorm, 4f * Time.deltaTime);

        // Low and high crossfade (equal power), pitch and sub rumble follow the speed.
        float blend = Mathf.InverseLerp(0.25f, 0.85f, smoothed);
        float lowMix = Mathf.Cos(blend * Mathf.PI * 0.5f);
        float highMix = Mathf.Sin(blend * Mathf.PI * 0.5f);
        float loudness = Mathf.Lerp(crawlLevel, 1f, smoothed) * ramp * zoneLevel;
        float pitch = Mathf.Lerp(pitchRange.x, pitchRange.y, smoothed) * zonePitchMix;

        SetLayer(low, lowMix * loudness, pitch);
        SetLayer(high, highMix * loudness, pitch);
        SetLayer(sub, Mathf.Lerp(0.25f, 1f, smoothed) * ramp * zoneLevel, pitch);
        SetLayer(limiter, Mathf.InverseLerp(0.9f, 1f, smoothed) * limiterMaxLevel * ramp * zoneLevel, 1f);

        state = arrived ? "spooled down" : slow ? "wound down (slow zone)" : started ? "running" : "idle";
    }

    /// <summary>The engine winds down for good (the ship is arriving at the moon). Safe to call more than once.</summary>
    public void SpoolDown()
    {
        if (!started || arrived) return;
        arrived = true;
        PlayOnce(spooldown, 1f);
    }

    private static void SetLayer(ExperienceAudio.Voice voice, float level, float pitch)
    {
        if (voice == null || !voice.IsAlive) return;
        voice.Set(level);
        voice.Source.pitch = pitch;
    }
}
