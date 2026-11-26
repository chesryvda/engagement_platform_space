using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Start of the experience: you wake up as the astronaut in the cockpit. The view starts black (eyes closed),
/// the eyes blink open a few times (a bit further each time) and the view is blurry at first. Once the eyes
/// are fully open the blur fades away and the view is sharp.
/// - Drawn by EyesEffectFeature (URP renderer feature) on the finished image of the camera you look through,
///   after all transparent objects: the scene, the cockpit glass and the screen texts blur the same way.
///   Works in VR (both eyes). Costs nothing once the intro is over.
/// </summary>
public class WakeUpIntro : MonoBehaviour
{
    public bool playOnStart = true;
    [Tooltip("Keep the eyes closed (black) until the ride starts: the first pedal stroke (or SPACE), when StartOnFirstPedal switches RouteMover on.")]
    public bool waitForFirstPedal = true;
    public RouteMover routeMover;

    [Header("Timing (seconds)")]
    [Tooltip("How long it stays black after the first pedal stroke, before the first blink.")]
    public float startBlack = 0.6f;
    [Tooltip("The blinks, from slow to fast: how far the eyes open (0-1), how blurry the view is during that blink (0-1), how long opening takes, how long they stay open, how long closing takes, how long they stay shut. The last one stays open.")]
    public List<Blink> blinks = new List<Blink>
    {
        new Blink { open = 0.3f, blur = 1f,    opening = 0.7f,  hold = 0.45f, closing = 0.35f, shut = 0.35f },
        new Blink { open = 0.6f, blur = 0.3f,  opening = 0.45f, hold = 0.25f, closing = 0.22f, shut = 0.18f },
        new Blink { open = 1f,   blur = 0.08f, opening = 0.8f,  hold = 0f,    closing = 0f,    shut = 0f },
    };
    [Tooltip("How long after the eyes are fully open the last bit of blur is gone.")]
    public float sharpenTime = 0.9f;

    [System.Serializable]
    public class Blink
    {
        [Range(0f, 1f)] public float open = 0.5f;
        [Tooltip("Blur during this blink: like real eyes, the view gets sharper with every blink.")]
        [Range(0f, 1f)] public float blur = 0.5f;
        public float opening = 0.5f, hold = 0.5f, closing = 0.25f, shut = 0.5f;
    }

    [Header("Live (read only)")]
    [SerializeField, Range(0f, 1f)] private float openAmount;
    [SerializeField, Range(0f, 1f)] private float blurAmount;
    [SerializeField] private bool playing;

    private float time;
    private float totalBlinkTime;

    void Start()
    {
        totalBlinkTime = startBlack;
        foreach (var b in blinks) totalBlinkTime += b.opening + b.hold + b.closing + b.shut;

        if (playOnStart) Play();
        else Finish();
    }

    /// <summary>Plays the wake-up from the start (black, blinks, blur fading away).</summary>
    public void Play()
    {
        time = 0f;
        playing = true;
        Apply(0f, blinks.Count > 0 ? blinks[0].blur : 0f);
    }

    void LateUpdate()
    {
        if (!playing) return;

        // Eyes stay shut until the ride starts (first pedal stroke / SPACE switches RouteMover on).
        if (waitForFirstPedal && routeMover != null && !routeMover.enabled)
        {
            time = 0f;
            Apply(0f, blinks.Count > 0 ? blinks[0].blur : 0f);
            return;
        }

        time += Time.deltaTime;
        float open = OpenAt(time, out float openedAt);

        // Blur: every blink has its own blur (strong, then much less, then almost none), shifting smoothly
        // while the eyes open; the last bit fades away once the eyes are fully open.
        float blur = BlurAt(time);
        if (openedAt >= 0f)
        {
            float atOpen = blinks.Count > 0 ? blinks[blinks.Count - 1].blur : 0f;
            blur = atOpen * (1f - Mathf.SmoothStep(0f, 1f, (time - openedAt) / Mathf.Max(0.01f, sharpenTime)));
        }

        Apply(open, blur);
        if (openedAt >= 0f && time - openedAt >= sharpenTime) Finish();
    }

    // How far the eyes are open at time t; openedAt = when the last (staying open) blink is fully open, or -1.
    private float OpenAt(float t, out float openedAt)
    {
        openedAt = -1f;
        if (t < startBlack) return 0f;
        float s = startBlack;
        for (int i = 0; i < blinks.Count; i++)
        {
            var b = blinks[i];
            bool last = i == blinks.Count - 1;
            if (t < s + b.opening) return b.open * Ease((t - s) / Mathf.Max(0.01f, b.opening));
            s += b.opening;
            if (last) { openedAt = s; return b.open; }
            if (t < s + b.hold) return b.open;
            s += b.hold;
            if (t < s + b.closing) return b.open * (1f - Ease((t - s) / Mathf.Max(0.01f, b.closing)));
            s += b.closing;
            if (t < s + b.shut) return 0f;
            s += b.shut;
        }
        return 1f;
    }

    // Blur at time t: the blur of the current blink, moving from the previous blink's blur while the eyes open.
    private float BlurAt(float t)
    {
        if (blinks.Count == 0) return 0f;
        float previous = blinks[0].blur;
        float s = startBlack;
        for (int i = 0; i < blinks.Count; i++)
        {
            var b = blinks[i];
            if (t < s + b.opening) return Mathf.Lerp(previous, b.blur, Ease((t - s) / Mathf.Max(0.01f, b.opening)));
            s += b.opening + b.hold + b.closing + b.shut;
            if (t < s || i == blinks.Count - 1) return b.blur;
            previous = b.blur;
        }
        return blinks[blinks.Count - 1].blur;
    }

    // Smootherstep: starts and stops even more gently than SmoothStep, so the lids glide instead of snap.
    private static float Ease(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * x * (x * (x * 6f - 15f) + 10f);
    }

    // The eyes are drawn by EyesEffectFeature (URP renderer) on the finished image.
    private void Apply(float open, float blur)
    {
        openAmount = open;
        blurAmount = blur;
        EyesEffect.Active = true;
        EyesEffect.Open = open;
        EyesEffect.Blur = blur;
    }

    private void Finish()
    {
        playing = false;
        openAmount = 1f;
        blurAmount = 0f;
        EyesEffect.Active = false;
        EyesEffect.Open = 1f;
        EyesEffect.Blur = 0f;
    }

    void OnDisable()
    {
        if (playing) EyesEffect.Active = false;
    }
}
