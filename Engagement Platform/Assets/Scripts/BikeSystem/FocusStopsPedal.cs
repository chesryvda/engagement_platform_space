using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

// SCRIPT 5b — FocusStopsPedal  (VOORSTEL: alternatief voor FocusStops van chesry)
// FocusStops blijft ongewijzigd. Dit script neemt alleen zijn lijst met stops over (object, Keep In Front, Crawl Lead).
// Verschil met FocusStops: de stop is niet meer een klok van 15 s, maar heeft VASTE PLEKKEN op de route.
// De TIJD hangt dus af van hoe hard je trapt:
//   1. Afremmen  : begint 70 tot 154 m voor het glijden (hoe sneller je aankomt, hoe eerder), constante remming.
//   2. Glijden   : een vaste afstand (48 m) op een snelheid die met het trappen meebeweegt (3 tot 5 m/s).
//                  Niet trappen = ca. 16 s, voluit trappen = ca. 9,6 s. Dit is het stuk voor de stem van Lisa.
//   3. Optrekken : na het glijden naar de snelheid van het trappen, zelfde regel als afremmen.
// Terugschakelen naar FocusStops: vink dit component UIT (of verwijder het). Dan is FocusStops weer actief.
// Zet dit liefst aan/uit vóór je op Play drukt (niet tijdens de rit).
[DefaultExecutionOrder(-10)] // vóór RouteMover, net als FocusStops
public class FocusStopsPedal : MonoBehaviour
{
    [Header("Verwijzingen (leeg laten: wordt gezocht op RouteSystem)")]
    [Tooltip("Het originele FocusStops-script. Het wordt niet aangepast. Zolang dit component aan staat is het origineel uit; zet je dit component uit, dan werkt het origineel weer.")]
    public FocusStops original;
    public RouteMover routeMover;
    public ShipSpeed shipSpeed;

    [Header("Glijden (traag langs het object)")]
    [Tooltip("Lengte van het stuk waarop je traag glijdt, in meter. 48 m = ca. 16 s bij 3 m/s.")]
    [Min(1f)] public float glideLength = 48f;
    [Tooltip("Glijsnelheid als je niet trapt (m/s)")]
    public float glideSpeedMin = 3f;
    [Tooltip("Glijsnelheid als je voluit trapt (m/s)")]
    public float glideSpeedMax = 5f;

    [Header("Afremmen en optrekken")]
    [Tooltip("Zo kort mag het afremmen/optrekken zijn (m). Bij lage snelheid dus minstens deze afstand, zodat het zacht blijft.")]
    public float minBrakeDistance = 70f;
    [Tooltip("Hoogste remming/optrekking (m/s per seconde). Bij 61 m/s geeft 12 een remafstand van ca. 154 m.")]
    public float brakeRate = 12f;
    [Tooltip("Zoals bij FocusStops: bij 'Keep In Front' moet het glijden gedaan zijn voor het object verder dan deze hoek opzij ligt")]
    [Range(10f, 80f)] public float frontAngle = 35f;
    [Tooltip("Op het einde van de route zacht afremmen tot stilstand")]
    public bool stopAtEnd = true;

    [Header("Voor Lisa (de stem)")]
    [Tooltip("Het schip begint af te remmen voor een object (parameter = naam van de stop)")]
    public UnityEvent<string> onStopBegin;
    [Tooltip("Het traag glijden begint: HIER de stem starten. Met 3 tot 5 m/s duurt het glijden minimaal ca. 9,6 s.")]
    public UnityEvent<string> onGlideBegin;
    [Tooltip("Het traag glijden is klaar")]
    public UnityEvent<string> onGlideEnd;
    [Tooltip("Het optrekken is klaar, de stop is voorbij")]
    public UnityEvent<string> onStopEnd;

    [Header("Live (alleen kijken)")]
    [SerializeField] private string activeStop = "";
    [SerializeField] private string phaseName = "";
    [SerializeField] private float stopTime;
    [SerializeField] private float glideSecondsNow;

    public string ActiveStop => activeStop;
    public float GlideSecondsNow => glideSecondsNow;

    private class StopInfo
    {
        public string name;
        public float distanceOnRoute;
        public float sideDistance;
        public float glideStart;
        public float glideEnd;
    }

    private enum Phase { None, Brake, Glide, Accel }

    private readonly List<StopInfo> plan = new List<StopInfo>();
    private int next;
    private Phase phase = Phase.None;

    private float brakeStartDist, v0, brakeA;
    private float vHeld, vGlideAtStart;
    private float accelStartDist, accelA, accelV1, accelVg, accelDist;

    void Awake()
    {
        if (original == null) original = GetComponent<FocusStops>();
        if (routeMover == null) routeMover = GetComponent<RouteMover>();
        if (shipSpeed == null) shipSpeed = GetComponent<ShipSpeed>();
    }

    void OnEnable()
    {
        if (original != null) original.enabled = false; // het origineel doet nu niet mee
        ResyncNext();
    }

    void OnDisable()
    {
        if (phase != Phase.None && shipSpeed != null) shipSpeed.ReleaseSpeed();
        phase = Phase.None;
        activeStop = "";
        phaseName = "";
        if (original != null && Application.isPlaying) original.enabled = true; // terug naar chesry's script
    }

    void Start()
    {
        BuildPlan();
        ResyncNext();
    }

    // Waar ligt elk object op de route en waar moet het glijden plaatsvinden? (vaste plekken, onafhankelijk van je snelheid)
    private void BuildPlan()
    {
        plan.Clear();
        if (original == null || routeMover == null || routeMover.route == null) return;

        var spline = routeMover.route.Spline;
        float length = routeMover.route.CalculateLength();
        foreach (var s in original.stops)
        {
            if (s.target == null) continue;
            Vector3 center = CenterOf(s.target);
            float3 local = routeMover.route.transform.InverseTransformPoint(center);
            SplineUtility.GetNearestPoint(spline, local, out float3 nearest, out float t);

            var info = new StopInfo
            {
                name = s.name,
                distanceOnRoute = t * length,
                sideDistance = Vector3.Distance(routeMover.route.transform.TransformPoint(nearest), center)
            };
            if (s.keepInFront)
            {
                // Het glijden eindigt voor het object te ver opzij komt (zoals bij FocusStops)
                info.glideEnd = info.distanceOnRoute - info.sideDistance / Mathf.Tan(frontAngle * Mathf.Deg2Rad);
                info.glideStart = info.glideEnd - glideLength;
            }
            else
            {
                // Het glijden begint 'Crawl Lead' meter voor het object (negatief = pas erna)
                info.glideStart = info.distanceOnRoute - s.crawlLead;
                info.glideEnd = info.glideStart + glideLength;
            }
            plan.Add(info);
        }
        plan.Sort((a, b) => a.distanceOnRoute.CompareTo(b.distanceOnRoute));
    }

    private void ResyncNext()
    {
        next = 0;
        if (routeMover == null) return;
        while (next < plan.Count && routeMover.Distance >= plan[next].glideEnd) next++;
    }

    void Update()
    {
        if (routeMover == null || shipSpeed == null) return;
        if (!routeMover.enabled) return; // rit nog niet gestart

        float distance = routeMover.Distance;
        float speed = shipSpeed.CurrentSpeed;
        glideSecondsNow = glideLength / Mathf.Max(0.1f, GlideSpeedNow());

        if (phase != Phase.None)
        {
            RunStop(distance);
            return;
        }

        // Komt de volgende stop eraan? Dan op tijd afremmen: de remafstand hangt af van je snelheid.
        while (next < plan.Count && distance >= plan[next].glideEnd) next++;
        if (next < plan.Count)
        {
            StopInfo stop = plan[next];
            float vg = GlideSpeedNow();
            float brakeDistance = speed > vg ? Mathf.Max(minBrakeDistance, (speed * speed - vg * vg) / (2f * brakeRate)) : 0f;
            if (distance >= stop.glideStart - brakeDistance)
            {
                BeginStop(stop, distance, speed);
                return;
            }
        }

        // Einde van de route: zacht uitbollen tot stilstand
        if (stopAtEnd)
        {
            float remaining = Mathf.Max(0f, routeMover.RouteLength - distance);
            float stopSpeed = Mathf.Sqrt(2f * brakeRate * remaining);
            if (stopSpeed < speed)
                shipSpeed.HoldSpeed(Mathf.Max(1f, stopSpeed));
        }
    }

    private void BeginStop(StopInfo stop, float distance, float speed)
    {
        activeStop = stop.name;
        stopTime = 0f;
        v0 = speed;
        vGlideAtStart = GlideSpeedNow();
        brakeStartDist = distance;
        float remaining = Mathf.Max(1f, stop.glideStart - distance);
        brakeA = Mathf.Max(0f, (v0 * v0 - vGlideAtStart * vGlideAtStart) / (2f * remaining));
        brakeA = Mathf.Min(brakeA, brakeRate * 1.5f);
        vHeld = v0;
        phase = Phase.Brake;
        phaseName = "Afremmen";
        shipSpeed.HoldSpeed(v0);
        onStopBegin?.Invoke(stop.name);
    }

    // De snelheid hangt hier van de PLEK af (afstand), niet van een klok.
    private void RunStop(float distance)
    {
        StopInfo stop = plan[next];
        stopTime += Time.deltaTime;

        switch (phase)
        {
            case Phase.Brake:
            {
                float v = v0 > vGlideAtStart
                    ? Mathf.Sqrt(Mathf.Max(vGlideAtStart * vGlideAtStart, v0 * v0 - 2f * brakeA * (distance - brakeStartDist)))
                    : v0;
                vHeld = v;
                shipSpeed.HoldSpeed(v);
                if (distance >= stop.glideStart)
                {
                    phase = Phase.Glide;
                    phaseName = "Glijden";
                    onGlideBegin?.Invoke(stop.name);
                }
                break;
            }
            case Phase.Glide:
            {
                // De glijsnelheid volgt het trappen (3 tot 5 m/s), zacht bijgestuurd
                vHeld = Mathf.MoveTowards(vHeld, GlideSpeedNow(), 2f * Time.deltaTime);
                shipSpeed.HoldSpeed(vHeld);
                if (distance >= stop.glideEnd)
                {
                    onGlideEnd?.Invoke(stop.name);
                    accelVg = vHeld;
                    accelV1 = Mathf.Max(accelVg, shipSpeed.TargetSpeed);
                    accelDist = Mathf.Max(minBrakeDistance, (accelV1 * accelV1 - accelVg * accelVg) / (2f * brakeRate));
                    accelA = (accelV1 * accelV1 - accelVg * accelVg) / (2f * accelDist);
                    accelStartDist = distance;
                    phase = Phase.Accel;
                    phaseName = "Optrekken";
                }
                break;
            }
            case Phase.Accel:
            {
                float travelled = distance - accelStartDist;
                float v = Mathf.Min(accelV1, Mathf.Sqrt(accelVg * accelVg + 2f * accelA * travelled));
                vHeld = v;
                shipSpeed.HoldSpeed(v);
                if (travelled >= accelDist || v >= accelV1 - 0.01f)
                {
                    EndStop(stop);
                }
                break;
            }
        }
    }

    private void EndStop(StopInfo stop)
    {
        phase = Phase.None;
        phaseName = "";
        activeStop = "";
        next++;
        shipSpeed.ReleaseSpeed();
        onStopEnd?.Invoke(stop.name);
    }

    // Glijsnelheid volgens het trappen: p = 0 (niet trappen) -> min, p = 1 (voluit) -> max
    private float GlideSpeedNow()
    {
        return Mathf.Lerp(glideSpeedMin, glideSpeedMax, shipSpeed.P);
    }

    private static Vector3 CenterOf(Transform target)
    {
        var renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return target.position;
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        return bounds.center;
    }
}
