using UnityEngine;
using UnityEngine.Events;

// SCRIPT 6 — EndFade
// Taak: het einde van de ervaring. Dichter bij de maan wordt het beeld steeds helderder,
// tot het helemaal wit is op het einde van de route. Daarna blijft het wit.
// - Werkt in VR: een klein bolletje rond de camera wordt over alles heen getekend (beide ogen).
// - Volgt altijd de actieve camera (VR-camera of de PC-testcamera na C).
// - Op het einde gaat 'On Fully White' af: daar kan later bv. een geluid of een herstart aan.
// - Daarna komt het logo (Technopolis) zacht in beeld, in het midden, recht voor je. Dat is het eindscherm.
public class EndFade : MonoBehaviour
{
    [Header("Verwijzingen")]
    public RouteMover routeMover;
    [Tooltip("Materiaal met de shader EngagementPlatform/ScreenFade (M_ScreenFade)")]
    public Material fadeMaterial;

    [Header("Fade")]
    [Tooltip("Hoeveel meter voor het einde van de route het beeld begint op te lichten")]
    public float fadeLength = 200f;
    [Tooltip("Hoeveel meter voor het einde het beeld al helemaal wit is")]
    public float whiteBefore = 5f;
    [Tooltip("Vorm van de fade: 1 = gelijkmatig, hoger = eerst zacht, op het einde snel wit")]
    [Range(1f, 4f)] public float curve = 2f;
    public Color fadeColor = Color.white;

    [Header("Einde")]
    public UnityEvent onFullyWhite;

    [Header("Logo op het eindscherm")]
    [Tooltip("Het logo dat verschijnt als het beeld wit is (leeg = geen logo)")]
    public Texture2D logo;
    [Tooltip("Hoeveel seconden het wit eerst alleen blijft")]
    public float logoDelay = 0.5f;
    [Tooltip("Hoe lang het logo erover doet om in beeld te komen (seconden)")]
    public float logoFadeTime = 2f;
    [Tooltip("Hoe ver voor je het logo hangt (meter)")]
    public float logoDistance = 2.5f;
    [Tooltip("Hoe breed het logo is (meter). 1.6 m op 2.5 m = ongeveer een derde van je zicht")]
    public float logoWidth = 1.6f;

    [Header("Live (alleen kijken)")]
    [SerializeField, Range(0f, 1f)] private float amount;
    [SerializeField, Range(0f, 1f)] private float logoAmount;

    private Transform sphere;
    private Material material;
    private Transform logoQuad;
    private Material logoMaterial;
    private bool done;
    private float whiteTime;
    private Vector3 logoForward = Vector3.forward;

    void Start()
    {
        // Klein bolletje rond de ogen (verder dan de near clip van de camera, dichter dan alles in de cockpit)
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "EndFadeSphere";
        Destroy(go.GetComponent<Collider>());
        go.transform.localScale = Vector3.one * 0.2f;
        var r = go.GetComponent<MeshRenderer>();
        material = new Material(fadeMaterial);
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        sphere = go.transform;
        SetAmount(0f);

        // Het logo: een vlak met dezelfde shader, één stap later getekend dan het wit
        if (logo != null)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "EndLogo";
            Destroy(q.GetComponent<Collider>());
            q.transform.localScale = new Vector3(logoWidth, logoWidth * logo.height / logo.width, 1f);
            var qr = q.GetComponent<MeshRenderer>();
            logoMaterial = new Material(fadeMaterial);
            logoMaterial.SetTexture("_MainTex", logo);
            logoMaterial.renderQueue = fadeMaterial.renderQueue + 1;
            qr.sharedMaterial = logoMaterial;
            qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            qr.receiveShadows = false;
            logoQuad = q.transform;
            SetLogo(0f);
        }
    }

    void LateUpdate()
    {
        if (sphere == null) return;

        // Altijd rond de camera waar je nu door kijkt
        Camera cam = Camera.main;
        if (cam != null) sphere.position = cam.transform.position;

        if (!done && routeMover != null && routeMover.enabled && routeMover.RouteLength > 0f)
        {
            float remaining = routeMover.RouteLength - routeMover.Distance;
            float t = Mathf.InverseLerp(fadeLength, whiteBefore, remaining);
            SetAmount(Mathf.Max(amount, Mathf.Pow(t, curve)));
            if (amount >= 1f)
            {
                done = true;
                whiteTime = Time.time;
                PlaceLogo(cam);
                onFullyWhite?.Invoke();
            }
        }

        // Eindscherm: het logo komt zacht in beeld
        if (done && logoQuad != null)
        {
            // het schip glijdt nog even uit: het logo reist mee op dezelfde afstand (de richting blijft vast)
            if (cam != null) logoQuad.position = cam.transform.position + logoForward * logoDistance;
            if (logoAmount < 1f) SetLogo((Time.time - whiteTime - logoDelay) / Mathf.Max(0.01f, logoFadeTime));
        }
    }

    // Recht voor je, in de richting waarin je op dat moment kijkt (daarna niet aan je hoofd vast,
    // zodat je er in VR rustig naar kunt kijken)
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
        logoMaterial.SetColor("_Color", new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, logoAmount)));
        logoQuad.gameObject.SetActive(logoAmount > 0.001f);
    }

    private void SetAmount(float a)
    {
        amount = Mathf.Clamp01(a);
        var c = fadeColor;
        c.a = amount;
        material.SetColor("_Color", c);
        sphere.gameObject.SetActive(amount > 0.001f);
    }
}
