using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// Extra snelheidsgevoel in één tussenstuk van de route (nu: tussen het ruimteafval en de Tesla).
// - Warp: het stof rekt verder uit tot lange streepjes (via SpaceDustSpeedFx.WarpBoost).
// - FOV-puls: een korte, kleine zoom bij het binnenkomen. Alleen op de PC-camera; in VR bepaalt
//   de headset de FOV en wordt er niets aangepast.
// - Trilling: de cockpit, schermen en mascotte trillen heel licht. De headsetcamera (XR Origin)
//   trilt niet mee. De trilling groeit met de snelheid en sterft weg richting het einde van het stuk.
// Leest alleen RouteMover (Distance en de stukken); de bestaande scripts worden niet aangepast.
[DefaultExecutionOrder(-1000)]
public class TussenstukWarp : MonoBehaviour
{
    [Header("Bron")]
    [Tooltip("Laat leeg: wordt automatisch gevonden.")]
    public RouteMover route;
    [Tooltip("Laat leeg: wordt automatisch gevonden.")]
    public SpaceDustSpeedFx dust;
    [Tooltip("Welk route-stuk? Het nummer in de lijst van RouteMover (0 = eerste). 4 = het tussenstuk tussen ruimteafval en Tesla.")]
    public int segmentIndex = 4;

    [Header("Warp-streepjes")]
    [Tooltip("Over hoeveel meter na het begin van het stuk het effect opbouwt.")]
    [Min(1f)] public float rampInMeters = 25f;
    [Tooltip("Over hoeveel meter voor het einde van het stuk het effect weer uitdooft.")]
    [Min(1f)] public float rampOutMeters = 80f;

    [Header("FOV-puls (alleen PC-camera)")]
    public Camera pcCamera;
    [Tooltip("Hoeveel graden de FOV even groter wordt. 0 = uit.")]
    [Range(0f, 10f)] public float fovPulse = 6f;
    [Tooltip("Hoe lang de puls duurt, in seconden.")]
    [Min(0.1f)] public float fovPulseDuration = 1.5f;

    [Header("Trilling (niet de headsetcamera)")]
    [Tooltip("Wat er trilt: cockpit, schermen, mascotte. Zet hier NIET de XR Origin of de camera in.")]
    public List<Transform> shakeTargets = new List<Transform>();
    [Tooltip("Sterkte in meter. 0 = uit.")]
    [Range(0f, 0.02f)] public float shakePosition = 0f;
    [Tooltip("Sterkte in graden. 0 = uit. Laat dit op 0: elk onderdeel draait om zijn eigen middelpunt, waardoor de schermen door de cockpit heen schuiven en flikkeren.")]
    [Range(0f, 1f)] public float shakeRotation = 0f;
    [Tooltip("Hoe snel het trilt (Hz, ongeveer).")]
    [Range(1f, 30f)] public float shakeSpeed = 9f;

    [Header("Live (alleen kijken)")]
    [SerializeField, Range(0f, 1f)] private float warp;
    [SerializeField, Range(0f, 1f)] private float shake;

    private Vector3 appliedPos;
    private Quaternion appliedRot = Quaternion.identity;
    private bool wasInside;
    private bool pulsing;
    private float pulseTime;
    private float baseFov;

    void Start()
    {
        if (route == null) route = FindFirstObjectByType<RouteMover>();
        if (dust == null) dust = FindFirstObjectByType<SpaceDustSpeedFx>();
        if (pcCamera == null)
        {
            var switcher = FindFirstObjectByType<CameraSwitcher>();
            if (switcher != null) pcCamera = switcher.pcCamera;
        }
    }

    // Eerst de vorige trilling weghalen, zodat andere scripts en animaties met de echte stand werken.
    void Update()
    {
        UndoShake();
    }

    void LateUpdate()
    {
        if (route == null || segmentIndex < 0 || segmentIndex >= route.segments.Count) return;

        float start = route.segments[segmentIndex].startDistance;
        float end = segmentIndex + 1 < route.segments.Count ? route.segments[segmentIndex + 1].startDistance : route.RouteLength;
        float d = route.Distance;
        bool inside = d >= start && d < end;

        // Opbouw aan het begin, uitdoven aan het einde
        float rampIn = inside ? SmoothStep01((d - start) / rampInMeters) : 0f;
        warp = inside ? rampIn * SmoothStep01((end - d) / rampOutMeters) : 0f;
        if (dust != null) dust.WarpBoost = warp;

        // Trilling: meeschalen met de snelheid, lineair wegsterven tot het einde van het stuk
        float intensity = dust != null ? dust.Intensity : 1f;
        float t = Mathf.InverseLerp(start, end, d);
        shake = inside ? rampIn * (1f - t) * intensity : 0f;
        ApplyShake();

        // FOV-puls bij het binnenkomen
        if (inside && !wasInside) StartPulse();
        wasInside = inside;
        UpdatePulse();
    }

    void OnDisable()
    {
        UndoShake();
        if (dust != null) dust.WarpBoost = 0f;
        if (pulsing && pcCamera != null) pcCamera.fieldOfView = baseFov;
        pulsing = false;
    }

    // ---- trilling ----

    private void ApplyShake()
    {
        if (shake <= 0.001f || shakeTargets.Count == 0) return;

        float time = Time.time * shakeSpeed;
        var pos = new Vector3(Noise(time, 0f), Noise(time, 10f), Noise(time, 20f)) * (shakePosition * shake);
        var rot = Quaternion.Euler(new Vector3(Noise(time, 30f), Noise(time, 40f), Noise(time, 50f)) * (shakeRotation * shake));

        foreach (var target in shakeTargets)
        {
            if (target == null) continue;
            target.localPosition += pos;
            target.localRotation = target.localRotation * rot;
        }
        appliedPos = pos;
        appliedRot = rot;
    }

    private void UndoShake()
    {
        if (appliedPos == Vector3.zero && appliedRot == Quaternion.identity) return;

        var inverse = Quaternion.Inverse(appliedRot);
        foreach (var target in shakeTargets)
        {
            if (target == null) continue;
            target.localPosition -= appliedPos;
            target.localRotation = target.localRotation * inverse;
        }
        appliedPos = Vector3.zero;
        appliedRot = Quaternion.identity;
    }

    // Rustige, onregelmatige waarde tussen -1 en 1 (twee lagen ruis)
    private static float Noise(float time, float seed)
    {
        float slow = Mathf.PerlinNoise(time * 0.5f + seed, seed + 0.5f) * 2f - 1f;
        float fast = Mathf.PerlinNoise(time * 1.7f + seed, seed + 3.5f) * 2f - 1f;
        return slow * 0.7f + fast * 0.3f;
    }

    // ---- FOV ----

    private bool CanPulse => fovPulse > 0f && pcCamera != null && pcCamera.enabled && !XRSettings.isDeviceActive;

    private void StartPulse()
    {
        if (!CanPulse) return;
        baseFov = pcCamera.fieldOfView;
        pulseTime = 0f;
        pulsing = true;
    }

    private void UpdatePulse()
    {
        if (!pulsing) return;
        if (pcCamera == null) { pulsing = false; return; }

        pulseTime += Time.deltaTime;
        float k = pulseTime / fovPulseDuration;
        if (k >= 1f)
        {
            pcCamera.fieldOfView = baseFov;
            pulsing = false;
            return;
        }
        pcCamera.fieldOfView = baseFov + fovPulse * Mathf.Sin(k * Mathf.PI);
    }

    private static float SmoothStep01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }
}
