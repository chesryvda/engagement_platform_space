using UnityEngine;

// Laat het ruimtestof meegroeien met de snelheid van het schip.
// - In een scene (traag): stipjes, rustig.
// - In een tussenstuk (snel): meer stof, helderder en de stipjes rekken uit tot korte streepjes.
// - Ertussen groeit alles geleidelijk mee, zoals de snelheid zelf.
// - Tijdens een stop (FocusStops) verdwijnt het stof zacht en komt het daarna terug.
// Leest alleen ShipSpeed.CurrentSpeed en FocusStops.InStop; de bestaande scripts worden niet aangepast.
[RequireComponent(typeof(ParticleSystem))]
public class SpaceDustSpeedFx : MonoBehaviour
{
    [Header("Bron")]
    [Tooltip("Laat leeg: wordt automatisch gevonden.")]
    public ShipSpeed shipSpeed;
    [Tooltip("Laat leeg: wordt automatisch gevonden. Tijdens een stop verdwijnt het stof.")]
    public FocusStops focusStops;

    [Header("Stops")]
    [Tooltip("Hoe lang het stof erover doet om te verdwijnen bij het afremmen (seconden).")]
    [Min(0.05f)] public float stopFadeTime = 1.5f;
    [Tooltip("Hoe lang het stof erover doet om terug te komen zodra het schip weer optrekt (seconden).")]
    [Min(0.05f)] public float returnFadeTime = 0.5f;

    [Header("Wanneer begint het effect (m/s)")]
    [Tooltip("Op of onder deze snelheid: geen effect (de langzame scenes).")]
    public float slowSpeed = 16f;
    [Tooltip("Op of boven deze snelheid: volledig effect (de snelle tussenstukken).")]
    public float fastSpeed = 32f;
    [Tooltip("Hoe snel het effect meebeweegt. Groter = sneller, kleiner = rustiger.")]
    public float smoothing = 6f;

    [Header("Uitrekken")]
    [Tooltip("Hoeveel de stipjes uitrekken. 0 = nooit, hoger = langere streepjes.")]
    [Range(0f, 0.2f)] public float maxStretch = 0.1f;

    [Header("Hoeveelheid en helderheid")]
    [Tooltip("Extra hoeveelheid stof bij volledige snelheid (1 = niets extra).")]
    [Range(1f, 3f)] public float fastDensityMultiplier = 1.5f;
    [Tooltip("Helderheid (doorzichtigheid) in een scene.")]
    [Range(0f, 1f)] public float slowBrightness = 0.9f;
    [Tooltip("Helderheid bij volledige snelheid.")]
    [Range(0f, 1f)] public float fastBrightness = 1f;

    [Header("Live (alleen kijken)")]
    [SerializeField, Range(0f, 1f)] private float intensity;
    [SerializeField, Range(0f, 1f)] private float visible = 1f;

    private ParticleSystemRenderer rend;
    private SpaceDustSettings settings;
    private MaterialPropertyBlock block;
    private Color baseColor = Color.white;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        rend = GetComponent<ParticleSystemRenderer>();
        settings = GetComponent<SpaceDustSettings>();
        block = new MaterialPropertyBlock();
        if (rend.sharedMaterial != null && rend.sharedMaterial.HasProperty(BaseColorId))
            baseColor = rend.sharedMaterial.GetColor(BaseColorId);
        rend.velocityScale = 0f;
        rend.lengthScale = 1f;
    }

    void Start()
    {
        if (shipSpeed == null) shipSpeed = FindFirstObjectByType<ShipSpeed>();
        if (focusStops == null) focusStops = FindFirstObjectByType<FocusStops>();
    }

    void Update()
    {
        if (shipSpeed == null) return;

        float target = Mathf.InverseLerp(slowSpeed, Mathf.Max(slowSpeed + 0.01f, fastSpeed), shipSpeed.CurrentSpeed);
        intensity = Mathf.Lerp(intensity, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

        // Afremmen of glijden bij een stop? Dan zacht laten verdwijnen. Zodra het schip weer optrekt, snel terug.
        bool inStop = focusStops != null && focusStops.isActiveAndEnabled && focusStops.SlowingOrGliding;
        visible = Mathf.MoveTowards(visible, inStop ? 0f : 1f, Time.deltaTime / (inStop ? stopFadeTime : returnFadeTime));

        // 1. Uitrekken: gewone stip in een scene, uitgerekt streepje bij snelheid
        if (intensity > 0.02f)
        {
            if (rend.renderMode != ParticleSystemRenderMode.Stretch) rend.renderMode = ParticleSystemRenderMode.Stretch;
            rend.cameraVelocityScale = intensity * maxStretch;
        }
        else if (rend.renderMode != ParticleSystemRenderMode.Billboard)
        {
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.cameraVelocityScale = 0f;
        }

        // 2. Hoeveelheid (tijdens een stop komt er geen nieuw stof bij)
        if (settings != null) settings.SetSpeedFactor(Mathf.Lerp(1f, fastDensityMultiplier, intensity) * (inStop ? 0f : 1f));

        // 3. Helderheid
        rend.GetPropertyBlock(block);
        var c = baseColor;
        c.a = baseColor.a * Mathf.Lerp(slowBrightness, fastBrightness, intensity) * visible;
        block.SetColor(BaseColorId, c);
        rend.SetPropertyBlock(block);
    }
}
