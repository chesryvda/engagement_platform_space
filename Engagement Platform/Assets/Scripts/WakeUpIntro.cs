using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Start of the experience: you wake up as the astronaut in the cockpit. The view starts black (eyes closed),
/// the eyes blink open a few times (a bit further each time) and the view is blurry at first. Once the eyes
/// are fully open the blur fades away and the view is sharp.
/// - Works in VR: a small sphere around the camera is drawn over everything (both eyes), like EndFade.
/// - Always follows the camera you are looking through (VR camera or the PC test camera).
/// - For the blur, the active camera's opaque image is switched on during the intro only, then set back.
/// </summary>
public class WakeUpIntro : MonoBehaviour
{
    [Tooltip("Material with the shader EngagementPlatform/WakeUpEyes (M_WakeUpEyes)")]
    public Material eyesMaterial;
    public bool playOnStart = true;

    [Header("Timing (seconds)")]
    [Tooltip("How long it stays black before the first blink.")]
    public float startBlack = 2f;
    [Tooltip("The blinks: how far the eyes open (0-1), how long opening takes, how long they stay open, how long closing takes, how long they stay shut. The last one stays open.")]
    public List<Blink> blinks = new List<Blink>
    {
        new Blink { open = 0.25f, opening = 0.7f, hold = 0.5f, closing = 0.25f, shut = 0.8f },
        new Blink { open = 0.5f,  opening = 0.6f, hold = 0.7f, closing = 0.22f, shut = 0.5f },
        new Blink { open = 1f,    opening = 1.2f, hold = 0f,   closing = 0f,    shut = 0f },
    };
    [Tooltip("How long after the eyes are fully open the view becomes sharp.")]
    public float sharpenTime = 2.5f;

    [Header("Blur")]
    [Tooltip("Blur at the start (0-1).")]
    [Range(0f, 1f)] public float startBlur = 1f;

    [System.Serializable]
    public class Blink
    {
        [Range(0f, 1f)] public float open = 0.5f;
        public float opening = 0.5f, hold = 0.5f, closing = 0.25f, shut = 0.5f;
    }

    [Header("Live (read only)")]
    [SerializeField, Range(0f, 1f)] private float openAmount;
    [SerializeField, Range(0f, 1f)] private float blurAmount;
    [SerializeField] private bool playing;

    private Transform sphere;
    private Material material;
    private float time;
    private float totalBlinkTime;
    private UniversalAdditionalCameraData colorCamera;
    private CameraOverrideOption previousColorOption;

    void Start()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "WakeUpEyes";
        Destroy(go.GetComponent<Collider>());
        go.transform.localScale = Vector3.one * 0.2f; // around the eyes, closer than anything in the cockpit
        var r = go.GetComponent<MeshRenderer>();
        material = new Material(eyesMaterial);
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        sphere = go.transform;

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
        sphere.gameObject.SetActive(true);
        Apply(0f, startBlur);
    }

    void LateUpdate()
    {
        if (!playing) return;
        Camera cam = Camera.main;
        if (cam != null)
        {
            sphere.position = cam.transform.position;
            UseOpaqueImage(cam);
        }

        time += Time.deltaTime;
        float open = OpenAt(time, out float openedAt);

        // Blur: strong while blinking, fading away once the eyes are fully open.
        float blur = startBlur;
        if (openedAt >= 0f) blur = startBlur * (1f - Mathf.SmoothStep(0f, 1f, (time - openedAt) / Mathf.Max(0.01f, sharpenTime)));
        else blur = startBlur * Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(time / Mathf.Max(0.01f, totalBlinkTime)));

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

    private static float Ease(float x) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));

    private void Apply(float open, float blur)
    {
        openAmount = open;
        blurAmount = blur;
        material.SetFloat("_Open", open);
        material.SetFloat("_Blur", blur);
    }

    private void Finish()
    {
        playing = false;
        Apply(1f, 0f);
        if (sphere != null) sphere.gameObject.SetActive(false);
        RestoreOpaqueImage();
    }

    // The blur reads the camera's opaque image; switch it on for the camera in use, only during the intro.
    private void UseOpaqueImage(Camera cam)
    {
        var data = cam.GetUniversalAdditionalCameraData();
        if (data == colorCamera) return;
        RestoreOpaqueImage();
        colorCamera = data;
        previousColorOption = data.requiresColorOption;
        data.requiresColorOption = CameraOverrideOption.On;
    }

    private void RestoreOpaqueImage()
    {
        if (colorCamera != null) colorCamera.requiresColorOption = previousColorOption;
        colorCamera = null;
    }

    void OnDisable() => RestoreOpaqueImage();
}
