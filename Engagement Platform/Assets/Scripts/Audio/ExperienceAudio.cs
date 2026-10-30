using System.Collections.Generic;
using UnityEngine;

/// <summary>Which part of the mix a sound belongs to. Each one has its own level on ExperienceAudio.</summary>
public enum AudioCategory { Music, Ambience, Effects, Interface, Engine }

/// <summary>
/// The one place for sound in the ride: all volume sliders, Lisa ducking, and small helpers to play
/// loops, 2D and 3D sounds and to crossfade music.
/// Put it once in the scene. EngineAudio, CockpitAmbience and RouteAudio all use ExperienceAudio.Instance.
/// Every sound is played through a Voice, so its final volume is always
/// (its own level) x (its category level) x (master) x (ducking, for music and ambience).
/// </summary>
[DefaultExecutionOrder(-100)] // before every other audio script, so Instance exists in their Start
public class ExperienceAudio : MonoBehaviour
{
    public static ExperienceAudio Instance { get; private set; }

    [Header("Levels (the whole mix in one place)")]
    [Range(0f, 1f)] public float master = 1f;
    [Range(0f, 1f)] public float music = 0.55f;
    [Range(0f, 1f)] public float ambience = 0.45f;
    [Range(0f, 1f)] public float effects = 0.8f;
    [Range(0f, 1f)] public float interfaceSounds = 0.6f;
    [Range(0f, 1f)] public float engine = 0.5f;

    [Header("Lisa ducking")]
    [Tooltip("While Lisa speaks, music and ambience drop to this fraction (0.4 = about -8 dB).")]
    [Range(0f, 1f)] public float duckLevel = 0.4f;
    [Tooltip("How long the music and ambience take to move down or back up, in seconds.")]
    [Min(0.01f)] public float duckFadeTime = 0.4f;
    [Tooltip("Live: tick this to hear the ducking. Lisa's voice will call SetLisaSpeaking.")]
    [SerializeField] private bool lisaSpeaking;

    /// <summary>One playing sound. Use FadeTo / Stop to change it; never set Source.volume yourself.</summary>
    public sealed class Voice
    {
        public AudioSource Source { get; internal set; }
        public AudioCategory Category { get; internal set; }
        public float Level { get; internal set; }
        public bool IsAlive => Source != null;

        internal float target;
        internal float rate = 1000f;
        internal bool stopWhenSilent;
        internal float killAt = -1f;
        internal bool hasPath;
        internal Vector3 pathFrom, pathTo;
        internal float pathStart, pathDuration;

        /// <summary>Fade this sound's own level to a value (0-1) over some seconds.</summary>
        public void FadeTo(float level, float seconds, bool stopWhenSilent = false)
        {
            target = Mathf.Clamp01(level);
            rate = seconds > 0.001f ? 1f / seconds : 1000f;
            this.stopWhenSilent = stopWhenSilent;
        }

        /// <summary>Set this sound's own level right now.</summary>
        public void Set(float level)
        {
            Level = target = Mathf.Clamp01(level);
        }

        /// <summary>Fade out and remove the sound.</summary>
        public void Stop(float fadeSeconds = 0.5f) => FadeTo(0f, fadeSeconds, true);
    }

    private readonly List<Voice> voices = new List<Voice>();
    private Voice musicVoice;
    private AudioListener listener;
    private float duck = 1f;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>The active listener (the VR camera, or the PC test camera).</summary>
    public Transform ListenerTransform
    {
        get
        {
            if (listener == null || !listener.isActiveAndEnabled)
            {
                listener = null;
                foreach (var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                    if (l.isActiveAndEnabled) { listener = l; break; }
            }
            return listener != null ? listener.transform : null;
        }
    }

    /// <summary>Lisa starts or stops talking: music and ambience duck while she does.</summary>
    public void SetLisaSpeaking(bool speaking) => lisaSpeaking = speaking;

    // ---------- Playing sounds ----------

    /// <summary>A flat sound (UI, music, stingers). It is heard the same everywhere.</summary>
    public Voice Play2D(AudioClip clip, AudioCategory category, float level = 1f, float pitch = 1f)
    {
        if (clip == null) return null;
        return Create(clip.name, clip, category, level, pitch, false, null, Vector3.zero, false, 1f, 2f, 0f);
    }

    /// <summary>A sound in the world, attached to an object so it moves with it.</summary>
    public Voice Play3D(AudioClip clip, AudioCategory category, Transform parent, Vector3 localPosition,
                        float level = 1f, float pitch = 1f, float minDistance = 2f, float maxDistance = 40f)
    {
        if (clip == null) return null;
        return Create(clip.name, clip, category, level, pitch, false, parent, localPosition, true, minDistance, maxDistance, 0f);
    }

    /// <summary>A looping sound. Spatial = a 3D sound on 'parent', otherwise flat. Starts at 'level' (use 0 to fade in).</summary>
    public Voice Loop(AudioClip clip, AudioCategory category, float level = 1f, Transform parent = null,
                      bool spatial = false, float minDistance = 5f, float maxDistance = 100f, float pitch = 1f)
    {
        if (clip == null) return null;
        return Create(clip.name, clip, category, level, pitch, true, parent, Vector3.zero, spatial, minDistance, maxDistance, 0f);
    }

    /// <summary>
    /// A 3D sound at a spot around the player. 'offset' is in the frame's axes (frame = the ship):
    /// x = right, y = up, z = ahead, measured from the player's head.
    /// </summary>
    public Voice PlayAround(AudioClip clip, AudioCategory category, Transform frame, Vector3 offset,
                            float level = 1f, float pitch = 1f, float minDistance = 1f, float maxDistance = 15f)
    {
        if (clip == null) return null;
        var head = ListenerTransform;
        if (frame == null) frame = head;
        if (frame == null) return Play2D(clip, category, level, pitch);

        Vector3 world = (head != null ? head.position : frame.position) + frame.rotation * offset;
        return Create(clip.name, clip, category, level, pitch, false, frame, frame.InverseTransformPoint(world),
                      true, minDistance, maxDistance, 0f);
    }

    /// <summary>
    /// A sound that travels past the player in a straight line, with Doppler. 'from' and 'to' are offsets
    /// in the frame's axes from the head, and it takes as long as the clip lasts to get there.
    /// </summary>
    public Voice Flyby(AudioClip clip, AudioCategory category, Transform frame, Vector3 from, Vector3 to,
                       float level = 1f, float pitch = 1f, float minDistance = 2f, float maxDistance = 100f)
    {
        if (clip == null) return null;
        var head = ListenerTransform;
        if (frame == null) frame = head;
        if (frame == null) return Play2D(clip, category, level, pitch);

        Vector3 origin = head != null ? head.position : frame.position;
        Vector3 localFrom = frame.InverseTransformPoint(origin + frame.rotation * from);
        Vector3 localTo = frame.InverseTransformPoint(origin + frame.rotation * to);

        var voice = Create(clip.name, clip, category, level, pitch, false, frame, localFrom, true, minDistance, maxDistance, 1f);
        voice.hasPath = true;
        voice.pathFrom = localFrom;
        voice.pathTo = localTo;
        voice.pathStart = Time.time;
        voice.pathDuration = Mathf.Max(0.1f, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)));
        return voice;
    }

    /// <summary>Fade the current music out and the new music in. Pass null to fade the music out.</summary>
    public void CrossfadeMusic(AudioClip clip, float seconds, float level = 1f)
    {
        if (musicVoice != null && musicVoice.IsAlive) musicVoice.Stop(seconds);
        musicVoice = null;
        if (clip == null) return;

        musicVoice = Create(clip.name, clip, AudioCategory.Music, 0f, 1f, true, null, Vector3.zero, false, 1f, 2f, 0f);
        musicVoice.FadeTo(level, seconds);
    }

    /// <summary>
    /// An empty child of 'target' placed in the middle of its meshes, so a loop sounds like it comes
    /// from the object's centre (not from its pivot). It moves with the object.
    /// </summary>
    public static Transform CreateEmitterAtCenter(Transform target, string name)
    {
        if (target == null) return null;
        var renderers = target.GetComponentsInChildren<Renderer>();
        Vector3 center = target.position;
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            center = bounds.center;
        }
        var emitter = new GameObject(name).transform;
        emitter.SetParent(target, true);
        emitter.position = center;
        return emitter;
    }

    // ---------- Internals ----------

    private Voice Create(string name, AudioClip clip, AudioCategory category, float level, float pitch, bool loop,
                         Transform parent, Vector3 localPosition, bool spatial, float minDistance, float maxDistance, float doppler)
    {
        var go = new GameObject("Audio_" + name);
        go.transform.SetParent(parent != null ? parent : transform, false);
        go.transform.localPosition = localPosition;

        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.pitch = pitch;
        source.playOnAwake = false;
        source.spatialBlend = spatial ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = Mathf.Max(maxDistance, minDistance + 0.1f);
        source.dopplerLevel = doppler;

        var voice = new Voice { Source = source, Category = category };
        voice.Set(level);
        if (!loop) voice.killAt = Time.time + clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)) + 0.25f;
        voices.Add(voice);

        ApplyVolume(voice);
        source.Play();
        return voice;
    }

    private float CategoryLevel(AudioCategory category)
    {
        switch (category)
        {
            case AudioCategory.Music: return music;
            case AudioCategory.Ambience: return ambience;
            case AudioCategory.Effects: return effects;
            case AudioCategory.Interface: return interfaceSounds;
            case AudioCategory.Engine: return engine;
            default: return 1f;
        }
    }

    private void ApplyVolume(Voice voice)
    {
        bool ducks = voice.Category == AudioCategory.Music || voice.Category == AudioCategory.Ambience;
        voice.Source.volume = voice.Level * CategoryLevel(voice.Category) * master * (ducks ? duck : 1f);
    }

    // After everything else has moved this frame, so 3D sounds on moving objects are placed correctly.
    void LateUpdate()
    {
        float dt = Time.deltaTime;
        duck = Mathf.MoveTowards(duck, lisaSpeaking ? duckLevel : 1f, (1f - duckLevel) / duckFadeTime * dt);

        for (int i = voices.Count - 1; i >= 0; i--)
        {
            Voice voice = voices[i];
            if (voice.Source == null) { voices.RemoveAt(i); continue; }

            voice.Level = Mathf.MoveTowards(voice.Level, voice.target, voice.rate * dt);

            bool silent = voice.stopWhenSilent && voice.Level <= 0f && voice.target <= 0f;
            bool finished = voice.killAt >= 0f && Time.time >= voice.killAt;
            if (silent || finished)
            {
                Destroy(voice.Source.gameObject);
                voices.RemoveAt(i);
                continue;
            }

            if (voice.hasPath)
            {
                float t = Mathf.Clamp01((Time.time - voice.pathStart) / voice.pathDuration);
                voice.Source.transform.localPosition = Vector3.Lerp(voice.pathFrom, voice.pathTo, t);
            }
            ApplyVolume(voice);
        }
    }
}
