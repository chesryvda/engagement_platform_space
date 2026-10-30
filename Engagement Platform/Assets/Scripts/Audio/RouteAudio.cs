using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays the right sounds at the right place on the route. It only READS RouteMover, FocusStops and
/// ShipSpeed; it changes nothing about the route.
/// The ride is split into 6 scenes, in the order of the named route segments (the "Tussenstuk" segments
/// are the transits in between and belong to the scene before them):
///   1 above Earth, 2 the ISS, 3 space debris, 4 the Tesla, 5 asteroids, 6 the moon.
/// - Music changes per scene (crossfade). A swell builds up just before a new scene and a whoosh plays when it
///   starts, but only for a scene that does not start at an object: there the slow zone is the cue (EngineAudio
///   lets the engine wind down and back up for it).
/// - Radar ping when the ISS, the Tesla and the asteroids come into range.
/// - The short signature sounds (ping, swell, whoosh, alarm) never pile up: they get a little room, and the
///   important ones wait their turn.
/// - ISS: satellite beep, hum and data crackle come from the ISS itself (3D).
/// - Space debris: warning alarm, things ticking on the hull, debris whooshing past, two heavy clanks,
///   tumbling panels creaking.
/// - Tesla: the radio tunes in and the car radio plays from the car.
/// - Asteroids: big and small asteroids flying past, hull groans, rock scraping.
/// - Moon: riser on the way in, boom on arrival, then the finale music.
/// Works from the ship's position on the route, so it keeps working when you jump with SkipToNextStop.
/// Needs an ExperienceAudio in the scene.
/// </summary>
public class RouteAudio : MonoBehaviour
{
    [Header("References")]
    public RouteMover routeMover;
    public FocusStops focusStops;
    public ShipSpeed shipSpeed;
    [Tooltip("The ship. Sounds around the player are placed in this object's axes.")]
    public Transform ship;

    [Header("Stop names (as in FocusStops)")]
    public string issStop = "ISS";
    public string debrisStop = "Ruimteafval";
    public string teslaStop = "Tesla";
    public string asteroidStop = "Asteroïden";

    [Header("Music")]
    public AudioClip calmMusic;
    public AudioClip wonderMusic;
    public AudioClip tenseMusic;
    public AudioClip climaxMusic;
    public AudioClip finaleMusic;
    [Tooltip("Seconds it takes to crossfade from one track to the next.")]
    public float musicFade = 3f;

    [Header("Between scenes")]
    public AudioClip[] whooshes;
    public AudioClip swell;

    [Header("Slow zones and radar")]
    public AudioClip radarPing;
    [Tooltip("Radar ping when the ship is this many metres (along the route) from the ISS, the Tesla or the asteroids.")]
    public float radarDistance = 300f;
    [Tooltip("Seconds a signature sound (ping, swell, whoosh, alarm) keeps the room to itself.")]
    [Min(0f)] public float cueSpacing = 2.5f;

    [Header("Scene 1 - above Earth")]
    public AudioClip missionControlRadio;

    [Header("Scene 2 - ISS (3D, on the ISS)")]
    public AudioClip satelliteBeep;
    public AudioClip satelliteHum;
    public AudioClip dataCrackle;

    [Header("Scene 3 - space debris")]
    public AudioClip warningAlarm;
    public AudioClip[] heavyClanks;
    [Tooltip("Looping creak, put on this many tumbling panels in the debris field (3D).")]
    public AudioClip panelCreak;
    [Min(0)] public int creakingPanels = 2;
    public SoundScatter debrisTicks = new SoundScatter
    {
        interval = new Vector2(1.2f, 3.5f),
        level = new Vector2(0.35f, 0.7f),
        distance = new Vector2(1.2f, 2.5f),
        height = new Vector2(-0.3f, 1f),
        hearingRange = 12f
    };
    public SoundScatter debrisWhooshes = new SoundScatter
    {
        placement = ScatterPlacement.Flyby,
        interval = new Vector2(4.5f, 8f),
        level = new Vector2(0.6f, 1f),
        distance = new Vector2(1.5f, 4f),
        height = new Vector2(-0.5f, 1.5f),
        flybyHalfLength = 25f,
        hearingRange = 50f
    };

    [Header("Scene 4 - Tesla")]
    public AudioClip radioTuneIn;
    public AudioClip carRadio;
    [Tooltip("The radio tunes in when the ship is this many metres (along the route) from the Tesla.")]
    public float radioDistance = 350f;
    [Tooltip("One honk from the Tesla at the moment the astronaut starts waving (the slow part of the Tesla stop).")]
    public AudioClip hornRadio;

    [Header("Scene 5 - asteroids")]
    public SoundScatter smallFlybys = new SoundScatter
    {
        placement = ScatterPlacement.Flyby,
        interval = new Vector2(2.5f, 5f),
        level = new Vector2(0.6f, 0.9f),
        distance = new Vector2(6f, 18f),
        height = new Vector2(-4f, 6f),
        flybyHalfLength = 40f,
        hearingRange = 90f
    };
    public SoundScatter largeFlybys = new SoundScatter
    {
        placement = ScatterPlacement.Flyby,
        interval = new Vector2(9f, 15f),
        level = new Vector2(0.8f, 1f),
        pitch = new Vector2(0.95f, 1.05f),
        distance = new Vector2(20f, 45f),
        height = new Vector2(-8f, 10f),
        flybyHalfLength = 90f,
        hearingRange = 180f
    };
    public SoundScatter hullGroans = new SoundScatter
    {
        interval = new Vector2(9f, 16f),
        level = new Vector2(0.5f, 0.8f),
        pitch = new Vector2(0.9f, 1.05f),
        distance = new Vector2(1.5f, 3f),
        height = new Vector2(-0.5f, 1f),
        hearingRange = 15f
    };
    public SoundScatter rockScrape = new SoundScatter
    {
        interval = new Vector2(14f, 24f),
        level = new Vector2(0.6f, 0.9f),
        distance = new Vector2(1.5f, 2.5f),
        height = new Vector2(-0.3f, 0.8f),
        hearingRange = 12f
    };

    [Header("Scene 6 - moon")]
    [Tooltip("Starts so that its peak lands on the moment the ship arrives.")]
    public AudioClip riser;
    public AudioClip moonBoom;
    [Tooltip("Volume of everything at the end together (riser, shimmer, boom, finale, logo). Lower it if the white fade is too loud.")]
    [Range(0f, 1f)] public float endLevel = 0.7f;
    [Tooltip("The engine winds down this many seconds before the ship arrives at the moon.")]
    [Min(0f)] public float engineSpooldownLead = 4f;
    public EngineAudio engineAudio;

    [Header("Ending screen (follows EndFade: the white fade and the logo)")]
    [Tooltip("Leave empty to not play the two sounds below by themselves.")]
    public EndFade endFade;
    [Tooltip("Plays when the picture starts to turn white.")]
    public AudioClip whiteoutShimmer;
    [Tooltip("Plays when the Technopolis logo starts to appear.")]
    public AudioClip sonicLogo;

    [Header("Not played by itself - call these from a UnityEvent when the visual exists")]
    public AudioClip lisaRadioOn;
    public AudioClip lisaRadioOff;
    public AudioClip lisaWavesAppear;

    [Header("Live (read only)")]
    [SerializeField] private int scene;
    [SerializeField] private bool inObjectZone;

    private ExperienceAudio audioSystem;
    private ExperienceAudio.Voice missionRadio, carRadioVoice;
    private Transform teslaEmitter;
    private FocusStops.Stop[] radarStops;
    private bool[] radarDone;
    private FocusStops.Stop teslaStopData;
    private AudioClip currentMusic;
    private bool musicStarted;
    private bool teslaTuned;
    private float carRadioAt = -1f;
    private bool hornPlayed;
    private enum CueKind { Interface, Effect }
    private struct Cue { public AudioClip clip; public float level; public CueKind kind; public float since; }
    private readonly Queue<Cue> cueQueue = new Queue<Cue>();
    private float cueFreeAt;
    private System.Reflection.FieldInfo activeStopField, stopTimeField, brakeTimeField;
    private bool clankOne, clankTwo;
    private bool riserPlayed, boomPlayed, shimmerPlayed, logoSoundPending, spoolCalled;
    private float logoSoundAt;
    private float swellForBoundary = -1f;
    private int whooshIndex;

    void Start()
    {
        audioSystem = ExperienceAudio.Instance;
        if (audioSystem == null || routeMover == null || focusStops == null)
        {
            Debug.LogError("RouteAudio: needs an ExperienceAudio in the scene and the RouteMover and FocusStops assigned.");
            enabled = false;
            return;
        }

        // Scene 1: faint radio chatter from Earth.
        missionRadio = audioSystem.Loop(missionControlRadio, AudioCategory.Ambience, 0f);

        // Scene 2: the ISS makes sounds of its own, from the middle of the station.
        Transform iss = ExperienceAudio.CreateEmitterAtCenter(StopOf(issStop), "ISS audio");
        audioSystem.Loop(satelliteBeep, AudioCategory.Effects, 0.55f, iss, true, 15f, 250f);
        audioSystem.Loop(satelliteHum, AudioCategory.Effects, 0.45f, iss, true, 10f, 150f);
        audioSystem.Loop(dataCrackle, AudioCategory.Effects, 0.35f, iss, true, 10f, 150f);

        // Scene 3: a few tumbling panels creak.
        var debris = StopOf(debrisStop);
        if (debris != null && debris.parent != null && panelCreak != null)
        {
            int found = 0;
            foreach (Transform piece in debris.parent)
            {
                if (found >= creakingPanels) break;
                if (!piece.name.Contains("Panel")) continue;
                var emitter = ExperienceAudio.CreateEmitterAtCenter(piece, "Panel audio");
                audioSystem.Loop(panelCreak, AudioCategory.Effects, 0.5f, emitter, true, 4f, 60f, Random.Range(0.9f, 1.1f));
                found++;
            }
        }

        // Scene 4: the Tesla carries the car radio.
        teslaEmitter = ExperienceAudio.CreateEmitterAtCenter(StopOf(teslaStop), "Tesla audio");
        teslaStopData = FindStop(teslaStop);

        // The honk follows the stop's timeline, which FocusStops keeps private (AstronautWave reads it the same way).
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        activeStopField = typeof(FocusStops).GetField("activeStop", flags);
        stopTimeField = typeof(FocusStops).GetField("stopTime", flags);
        brakeTimeField = typeof(FocusStops).GetField("tBrake", flags);

        // Radar pings when something comes into range.
        radarStops = new[] { FindStop(issStop), teslaStopData, FindStop(asteroidStop) };
        radarDone = new bool[radarStops.Length];
    }

    // ---------- helpers to find things on the route ----------

    private FocusStops.Stop FindStop(string stopName)
    {
        return focusStops.stops.Find(s => s.name == stopName);
    }

    private Transform StopOf(string stopName)
    {
        var stop = FindStop(stopName);
        return stop != null ? stop.target : null;
    }

    private static bool IsTransit(RouteMover.RouteSegment segment)
    {
        return segment.name.StartsWith("Tussen");
    }

    // Which scene (1-6) is this distance in, and is it in a transit between two scenes?
    private int SceneAt(float distance, out bool transit)
    {
        int count = 0;
        transit = false;
        foreach (var segment in routeMover.segments)
        {
            if (distance < segment.startDistance) break;
            transit = IsTransit(segment);
            if (!transit) count++;
        }
        return Mathf.Max(1, count);
    }

    // Start of scene number 'number' and where it ends (the start of the segment after it).
    private bool SceneRange(int number, out float start, out float end)
    {
        start = end = 0f;
        int count = 0;
        var segments = routeMover.segments;
        for (int i = 0; i < segments.Count; i++)
        {
            if (IsTransit(segments[i])) continue;
            if (++count != number) continue;
            start = segments[i].startDistance;
            end = i + 1 < segments.Count ? segments[i + 1].startDistance : routeMover.RouteLength;
            return true;
        }
        return false;
    }

    // Start of the first scene after this distance, or -1.
    private float NextSceneStart(float distance)
    {
        foreach (var segment in routeMover.segments)
            if (!IsTransit(segment) && segment.startDistance > distance) return segment.startDistance;
        return -1f;
    }

    // ---------- every frame ----------

    void Update()
    {
        float distance = routeMover.Distance;
        int now = SceneAt(distance, out bool transit);
        inObjectZone = !transit;

        if (now != scene)
        {
            OnSceneChanged(scene, now);
            scene = now;
        }

        FlushCues();
        UpdateMusic();
        UpdateRadar(distance);
        UpdateTeslaRadio(distance);
        UpdateTeslaHorn(distance);
        UpdateSwell(distance);
        UpdateSceneSounds(distance);
        UpdateMoon(distance);
    }

    private void OnSceneChanged(int from, int to)
    {
        if (missionRadio != null) missionRadio.FadeTo(to == 1 ? 0.25f : 0f, 4f);

        if (from > 0 && to > from)
        {
            // A scene that starts at an object already has its cue: the slow zone. Only the others get a whoosh.
            if (SceneRange(to, out float start, out _) && !StartsAtObject(start) && whooshes != null && whooshes.Length > 0)
                PlayCue(whooshes[whooshIndex++ % whooshes.Length], 0.6f, CueKind.Effect, false);
            if (to == 3) PlayCue(warningAlarm, 0.8f, CueKind.Effect, true); // waits if another signature sound is playing
        }

        if (to < 3) clankOne = clankTwo = false;
    }

    // ---------- signature sounds: one at a time ----------

    // Does a scene that begins here start at an object (ISS, debris, Tesla, asteroids)? Then the slow zone
    // that comes with the object is the cue for the new scene, so the swell and whoosh stay out of its way.
    private bool StartsAtObject(float sceneStart)
    {
        foreach (var stop in focusStops.stops)
            if (stop != null && sceneStart >= stop.distanceOnRoute - 150f && sceneStart <= stop.distanceOnRoute + 20f)
                return true;
        return false;
    }

    // Plays a short signature sound if the room is free. An important one that arrives too early waits its
    // turn; an unimportant one is dropped.
    private void PlayCue(AudioClip clip, float level, CueKind kind, bool important)
    {
        if (clip == null) return;

        if (Time.time >= cueFreeAt && cueQueue.Count == 0)
        {
            StartCue(clip, level, kind);
            return;
        }
        if (important) cueQueue.Enqueue(new Cue { clip = clip, level = level, kind = kind, since = Time.time });
    }

    private void FlushCues()
    {
        while (cueQueue.Count > 0 && Time.time >= cueFreeAt)
        {
            Cue cue = cueQueue.Dequeue();
            if (Time.time - cue.since > 6f) continue; // too late to still mean something
            StartCue(cue.clip, cue.level, cue.kind);
        }
    }

    private void StartCue(AudioClip clip, float level, CueKind kind)
    {
        cueFreeAt = Time.time + Mathf.Min(clip.length, cueSpacing);
#if UNITY_EDITOR
        Debug.Log("RouteAudio cue: " + clip.name + " at " + routeMover.Distance.ToString("0") + " m");
#endif
        audioSystem.Play2D(clip, kind == CueKind.Interface ? AudioCategory.Interface : AudioCategory.Effects, level);
    }

    private void UpdateMusic()
    {
        AudioClip wanted = null;
        switch (scene)
        {
            case 1: wanted = calmMusic; break;
            case 2: wanted = wonderMusic; break;
            case 3: wanted = teslaTuned ? null : tenseMusic; break; // the Tesla's radio takes over
            case 5: wanted = climaxMusic; break;
        }

        if (musicStarted && wanted == currentMusic) return;
        musicStarted = true;
        currentMusic = wanted;
        audioSystem.CrossfadeMusic(wanted, musicFade);
    }

    private void UpdateRadar(float distance)
    {
        for (int i = 0; i < radarStops.Length; i++)
        {
            var stop = radarStops[i];
            if (stop == null) continue;

            float trigger = stop.distanceOnRoute - radarDistance;
            if (!radarDone[i] && distance >= trigger)
            {
                radarDone[i] = true;
                PlayCue(radarPing, 0.6f, CueKind.Interface, false);
            }
            else if (radarDone[i] && distance < trigger - 100f)
            {
                radarDone[i] = false; // jumped back: ping again next time
            }
        }
    }

    private void UpdateTeslaRadio(float distance)
    {
        if (teslaStopData == null || teslaEmitter == null) return;

        float trigger = teslaStopData.distanceOnRoute - radioDistance;
        if (!teslaTuned && distance >= trigger)
        {
            teslaTuned = true;
            if (radioTuneIn != null)
            {
                audioSystem.Play3D(radioTuneIn, AudioCategory.Effects, teslaEmitter, Vector3.zero, 0.9f, 1f, 20f, 250f);
                carRadioAt = Time.time + Mathf.Max(0f, radioTuneIn.length - 0.4f);
            }
            else carRadioAt = Time.time;
        }

        // When the tuning is done the music from the car fades in.
        if (teslaTuned && carRadioVoice == null && carRadioAt >= 0f && Time.time >= carRadioAt)
        {
            carRadioVoice = audioSystem.Loop(carRadio, AudioCategory.Music, 0f, teslaEmitter, true, 12f, 200f);
            if (carRadioVoice != null) carRadioVoice.FadeTo(0.8f, 1.5f);
        }
    }

    // One honk when the astronaut starts waving: the Tesla stop has finished braking and the slow part begins.
    private void UpdateTeslaHorn(float distance)
    {
        if (teslaStopData == null || teslaEmitter == null || hornRadio == null) return;
        if (activeStopField == null || stopTimeField == null || brakeTimeField == null) return;

        if (hornPlayed)
        {
            if (distance < teslaStopData.distanceOnRoute - radioDistance - 100f) hornPlayed = false; // jumped back
            return;
        }

        bool inTeslaStop = (string)activeStopField.GetValue(focusStops) == teslaStop;
        if (inTeslaStop && (float)stopTimeField.GetValue(focusStops) >= (float)brakeTimeField.GetValue(focusStops))
        {
            hornPlayed = true;
            audioSystem.Play3D(hornRadio, AudioCategory.Effects, teslaEmitter, Vector3.zero, 0.8f, 1f, 15f, 200f);
        }
    }

    // The swell builds up just before the start of the next scene.
    private void UpdateSwell(float distance)
    {
        if (swell == null) return;
        float next = NextSceneStart(distance);
        if (next < 0f || Mathf.Approximately(next, swellForBoundary)) return;

        float speed = Mathf.Max(10f, shipSpeed != null ? shipSpeed.CurrentSpeed : 10f);
        float secondsLeft = (next - distance) / speed;
        if (secondsLeft > swell.length) return;

        // No swell for a scene that starts at an object (the slow zone is its cue), and none when it is too late.
        if (StartsAtObject(next) || secondsLeft < 1f)
        {
            swellForBoundary = next;
            return;
        }

        // Another signature sound is still playing? Then try again next frame.
        if (Time.time >= cueFreeAt && cueQueue.Count == 0)
        {
            swellForBoundary = next;
            PlayCue(swell, 0.6f, CueKind.Effect, false);
        }
    }

    private void UpdateSceneSounds(float distance)
    {
        bool debrisZone = scene == 3 && inObjectZone;
        bool asteroidZone = scene == 5 && inObjectZone;

        // Debris: ticks on the hull, things whooshing past, two heavy clanks while crossing the field.
        debrisTicks.Tick(debrisZone, audioSystem, ship);
        debrisWhooshes.Tick(debrisZone, audioSystem, ship);
        if (debrisZone && SceneRange(3, out float start, out float end) && end > start)
        {
            float progress = (distance - start) / (end - start);
            if (!clankOne && progress >= 0.3f) { clankOne = true; PlayClank(0); }
            if (!clankTwo && progress >= 0.7f) { clankTwo = true; PlayClank(1); }
        }

        // Asteroids: flybys, groans and scraping.
        smallFlybys.Tick(asteroidZone, audioSystem, ship);
        largeFlybys.Tick(asteroidZone, audioSystem, ship);
        hullGroans.Tick(asteroidZone, audioSystem, ship);
        rockScrape.Tick(asteroidZone, audioSystem, ship);
    }

    private void PlayClank(int index)
    {
        if (heavyClanks == null || heavyClanks.Length == 0) return;
        float side = Random.value < 0.5f ? -1f : 1f;
        var offset = new Vector3(side * 1.8f, 0.4f, 1.2f);
        audioSystem.PlayAround(heavyClanks[index % heavyClanks.Length], AudioCategory.Effects, ship, offset, 1f, 1f, 1f, 15f);
    }

    // Moon: the riser peaks as the ship arrives, then the boom and the finale music.
    private void UpdateMoon(float distance)
    {
        if (!routeMover.enabled || routeMover.RouteLength <= 0f) return;

        float length = routeMover.RouteLength;

        // The engine starts winding down a little before arrival.
        if (!spoolCalled && engineAudio != null && SecondsToEnd(distance, length) <= engineSpooldownLead)
        {
            spoolCalled = true;
            engineAudio.SpoolDown();
        }

        if (!riserPlayed && riser != null && SecondsToEnd(distance, length) <= riser.length)
        {
            riserPlayed = true;
            var voice = audioSystem.Play2D(riser, AudioCategory.Effects, 0.9f * endLevel);
            // Started late (for example after a jump)? Skip ahead so the peak still lands on arrival.
            if (voice != null)
                voice.Source.time = Mathf.Clamp(riser.length - SecondsToEnd(distance, length), 0f, riser.length - 0.1f);
        }

        if (!boomPlayed && distance >= length - 0.5f)
        {
            boomPlayed = true;
            audioSystem.Play2D(moonBoom, AudioCategory.Effects, endLevel);
            audioSystem.Play2D(finaleMusic, AudioCategory.Music, endLevel);
        }

        UpdateEndScreen(length - distance);
    }

    // The picture turns white (shimmer), and the logo appears a moment after it is fully white (sonic logo).
    private void UpdateEndScreen(float remaining)
    {
        if (endFade == null) return;

        if (!shimmerPlayed && remaining <= endFade.fadeLength)
        {
            shimmerPlayed = true;
            PlayWhiteoutShimmer();
        }

        if (!logoSoundPending && remaining <= endFade.whiteBefore)
        {
            logoSoundPending = true;
            logoSoundAt = Time.time + endFade.logoDelay;
        }
        if (logoSoundPending && logoSoundAt >= 0f && Time.time >= logoSoundAt)
        {
            logoSoundAt = -1f;
            PlaySonicLogo();
        }
    }

    // How many seconds until the ship reaches the end of the route, following FocusStops' braking.
    private float SecondsToEnd(float distance, float length)
    {
        float remaining = Mathf.Max(0f, length - distance);
        float speed = Mathf.Max(1f, shipSpeed != null ? shipSpeed.CurrentSpeed : 1f);
        if (!focusStops.stopAtEnd) return remaining / speed;

        float brake = Mathf.Max(0.1f, focusStops.brakeRate);
        float brakeDistance = speed * speed / (2f * brake);
        if (remaining <= brakeDistance) return Mathf.Sqrt(2f * remaining / brake);       // already braking
        return (remaining - brakeDistance) / speed + speed / brake;                       // cruise, then brake
    }

    // ---------- to call from a UnityEvent when the matching visual exists ----------

    /// <summary>The click and hiss just before Lisa speaks.</summary>
    public void PlayLisaRadioOn() => audioSystem.Play2D(lisaRadioOn, AudioCategory.Interface, 0.8f);

    /// <summary>The click after Lisa's sentence.</summary>
    public void PlayLisaRadioOff() => audioSystem.Play2D(lisaRadioOff, AudioCategory.Interface, 0.8f);

    /// <summary>Lisa's audio waves appear on the dashboard.</summary>
    public void PlayLisaWavesAppear() => audioSystem.Play2D(lisaWavesAppear, AudioCategory.Interface, 0.8f);

    /// <summary>The fade to white at the moon.</summary>
    public void PlayWhiteoutShimmer() => audioSystem.Play2D(whiteoutShimmer, AudioCategory.Effects, 0.9f * endLevel);

    /// <summary>The Technopolis logo appears.</summary>
    public void PlaySonicLogo() => audioSystem.Play2D(sonicLogo, AudioCategory.Effects, 0.9f * endLevel);
}
