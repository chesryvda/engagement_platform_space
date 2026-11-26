using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// End of the experience: arriving at the Moon you fall asleep again. Where the white end fade used to start,
/// the eyes get heavy: they blink a few times (closing longer and opening less far each time, the view
/// getting blurrier) and then stay shut. On the black, a white Technopolis logo fades in.
/// - While blinking, the eyes are drawn by EyesEffectFeature (URP renderer feature) on the finished image,
///   so everything blurs the same way. Once shut, a black sphere around the camera takes over (both eyes).
/// - The logo hangs in front of you in the direction you look when the eyes close (not stuck to your head).
/// - Starts at the same point as EndFade's white fade (its "Fade Length" before the end of the route), so the
///   end sounds of RouteAudio (which follow EndFade's distances) stay in place. EndFade itself should be off.
/// </summary>
public class FallAsleepOutro : MonoBehaviour
{
    [Header("References")]
    public RouteMover routeMover;
    [Tooltip("Only used for its distances (where the white fade used to start). Leave EndFade itself switched off.")]
    public EndFade endFade;
    [Tooltip("Material with the shader EngagementPlatform/ScreenFade (M_ScreenFade), used to draw the logo on top")]
    public Material logoMaterial;
    [Tooltip("White Technopolis logo with a transparent background")]
    public Texture2D logo;

    [Header("When")]
    [Tooltip("Metres before the end of the route where falling asleep starts. 0 = take EndFade's Fade Length.")]
    public float startBeforeEnd = 0f;

    [Header("Falling asleep (seconds)")]
    [Tooltip("The heavy blinks: how long closing takes, how long the eyes stay shut, how far they open again (0 = stay shut, the last one), how long opening takes, how long they stay open, and how blurry it gets.")]
    public List<Droop> droops = new List<Droop>
    {
        new Droop { closing = 0.45f, shut = 0.25f, reopen = 0.7f, opening = 0.6f, hold = 0.7f, blur = 0.15f },
        new Droop { closing = 0.6f,  shut = 0.5f,  reopen = 0.4f, opening = 0.8f, hold = 0.6f, blur = 0.45f },
        new Droop { closing = 1.0f,  shut = 0f,    reopen = 0f,   opening = 0f,   hold = 0f,   blur = 0.8f },
    };

    [System.Serializable]
    public class Droop
    {
        public float closing = 0.5f, shut = 0.3f;
        [Range(0f, 1f)] public float reopen = 0.5f;
        public float opening = 0.6f, hold = 0.5f;
        [Range(0f, 1f)] public float blur = 0.3f;
    }

    [Header("Logo")]
    [Tooltip("Seconds after the eyes are shut before the logo starts to appear.")]
    public float logoDelay = 0.6f;
    [Tooltip("How long the logo takes to fade in (seconds).")]
    public float logoFadeTime = 1.5f;
    [Tooltip("How far in front of you the logo hangs (metres).")]
    public float logoDistance = 2.5f;
    [Tooltip("Width of the logo (metres). 1.6 m at 2.5 m is about a third of your view.")]
    public float logoWidth = 1.6f;

    [Header("Live (read only)")]
    [SerializeField, Range(0f, 1f)] private float openAmount = 1f;
    [SerializeField, Range(0f, 1f)] private float blurAmount;
    [SerializeField, Range(0f, 1f)] private float logoAmount;
    [SerializeField] private bool started, closed;

    private Transform sphere, logoQuad;
    private Material blackMaterial, logoMat;
    private float time, closedAt;
    private Vector3 logoForward = Vector3.forward;

    void Start()
    {
        // While blinking, the eyes are drawn by EyesEffectFeature on the finished image (everything blurs the
        // same way). Once the eyes are shut, this plain black sphere takes over, so the logo can be drawn on top.
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "FallAsleepBlack";
        Destroy(go.GetComponent<Collider>());
        go.transform.localScale = Vector3.one * 0.2f;
        var r = go.GetComponent<MeshRenderer>();
        blackMaterial = new Material(logoMaterial);
        blackMaterial.SetColor("_Color", Color.black);
        blackMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
        blackMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay + 90;
        r.sharedMaterial = blackMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        sphere = go.transform;
        go.SetActive(false);

        if (logo != null && logoMaterial != null)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "SleepLogo";
            Destroy(q.GetComponent<Collider>());
            q.transform.localScale = new Vector3(logoWidth, logoWidth * logo.height / logo.width, 1f);
            var qr = q.GetComponent<MeshRenderer>();
            logoMat = new Material(logoMaterial);
            logoMat.SetTexture("_MainTex", logo);
            logoMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay + 100; // over the closed eyes
            qr.sharedMaterial = logoMat;
            qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            qr.receiveShadows = false;
            logoQuad = q.transform;
            SetLogo(0f);
        }
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (!started)
        {
            if (routeMover == null || !routeMover.enabled || routeMover.RouteLength <= 0f) return;
            float startAt = startBeforeEnd > 0f ? startBeforeEnd : (endFade != null ? endFade.fadeLength : 200f);
            if (routeMover.RouteLength - routeMover.Distance > startAt) return;
            started = true;
            time = 0f;
        }

        if (cam != null) sphere.position = cam.transform.position;

        time += Time.deltaTime;
        bool shutForGood = closed;
        if (!closed)
        {
            float open = OpenAt(time, out float blur, out shutForGood);
            Apply(open, blur);
        }

        if (shutForGood && !closed)
        {
            closed = true;
            closedAt = time;
            PlaceLogo(cam);
            sphere.gameObject.SetActive(true); // plain black from now on, with the logo on top
            EyesEffect.Active = false;
        }

        if (closed && logoQuad != null)
        {
            if (cam != null) logoQuad.position = cam.transform.position + logoForward * logoDistance; // travels along while the ship glides out
            if (logoAmount < 1f) SetLogo((time - closedAt - logoDelay) / Mathf.Max(0.01f, logoFadeTime));
        }
    }

    // How open the eyes are at time t (and how blurry), and whether they have closed for good.
    private float OpenAt(float t, out float blur, out bool shutForGood)
    {
        shutForGood = false;
        float s = 0f, from = 1f, prevBlur = 0f;
        for (int i = 0; i < droops.Count; i++)
        {
            var d = droops[i];
            if (t < s + d.closing)
            {
                float k = Ease((t - s) / Mathf.Max(0.01f, d.closing));
                blur = Mathf.Lerp(prevBlur, d.blur, k);
                return from * (1f - k);
            }
            s += d.closing;
            blur = d.blur;
            if (d.reopen <= 0f) { shutForGood = true; return 0f; }
            if (t < s + d.shut) return 0f;
            s += d.shut;
            if (t < s + d.opening) return d.reopen * Ease((t - s) / Mathf.Max(0.01f, d.opening));
            s += d.opening;
            if (t < s + d.hold) return d.reopen;
            s += d.hold;
            from = d.reopen;
            prevBlur = d.blur;
        }
        blur = droops.Count > 0 ? droops[droops.Count - 1].blur : 0f;
        shutForGood = true;
        return 0f;
    }

    private static float Ease(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * x * (x * (x * 6f - 15f) + 10f);
    }

    private void Apply(float open, float blur)
    {
        openAmount = open;
        blurAmount = blur;
        EyesEffect.Active = true;
        EyesEffect.Open = open;
        EyesEffect.Blur = blur;
    }

    // In front of you, in the direction you look at that moment (level), not stuck to your head.
    private void PlaceLogo(Camera cam)
    {
        if (logoQuad == null || cam == null) return;
        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        logoForward = forward;
        logoQuad.position = cam.transform.position + forward * logoDistance;
        logoQuad.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    private void SetLogo(float a)
    {
        logoAmount = Mathf.Clamp01(a);
        logoMat.SetColor("_Color", new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, logoAmount)));
        logoQuad.gameObject.SetActive(logoAmount > 0.001f);
    }

    void OnDisable()
    {
        if (started && !closed) EyesEffect.Active = false;
    }
}
