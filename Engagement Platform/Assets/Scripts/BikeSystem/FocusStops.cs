using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

// SCRIPT 5 — FocusStops
// Taak: bij elk object (ISS, ruimteafval, Tesla, asteroïden) het schip bijna laten stilvallen,
// altijd even lang (bv. 15 s), hoe snel je ook trapt. Zo blijft de focus op het object.
// - Vlak voor het object remt het schip af, glijdt traag langs het object en trekt daarna weer op.
// - Wie snel aankomt, begint vroeger te remmen. Zo duurt elke stop precies even lang.
// - Tijdens een stop telt het trappen niet mee (de benen bewegen wel gewoon).
// - Op het einde van de route remt het schip zacht af tot stilstand voor de maan.
[DefaultExecutionOrder(-10)] // vóór RouteMover, zodat die meteen de juiste snelheid gebruikt
public class FocusStops : MonoBehaviour
{
    [Serializable]
    public class Stop
    {
        public string name = "ISS";
        [Tooltip("Het object waar het schip langs glijdt")]
        public Transform target;
        [Tooltip("Hoe lang de hele stop duurt: afremmen + traag langs het object + optrekken")]
        public float duration = 15f;
        [Tooltip("Aan = traag glijden terwijl het object nog vóór je ligt (niet opzij kijken). Uit = traag glijden vanaf vlak voor het object, er middenin (bv. asteroïden).")]
        public bool keepInFront = true;
        [Tooltip("Zonder 'Keep In Front': hoeveel meter vóór het object het schip al bijna stilstaat (negatief = pas erna)")]
        public float crawlLead = 10f;
        [Tooltip("Live (alleen kijken): waar op de route het object ligt, in meter")]
        public float distanceOnRoute;
        [Tooltip("Live (alleen kijken): hoe ver het object naast de route ligt, in meter")]
        public float sideDistance;
    }

    [Header("Verwijzingen")]
    public RouteMover routeMover;
    public ShipSpeed shipSpeed;

    [Header("Stops (volgorde maakt niet uit)")]
    public List<Stop> stops = new List<Stop>();

    [Header("Gevoel")]
    [Tooltip("Snelheid waarmee het schip langs het object glijdt (bijna stilstaan)")]
    public float crawlSpeed = 3f;
    [Tooltip("Hoe hard het schip remt en optrekt tijdens een stop (m/s per seconde)")]
    public float brakeRate = 12f;
    [Tooltip("Stops met 'Keep In Front': het traag glijden stopt voor het object verder dan deze hoek opzij ligt (0 = recht vooruit)")]
    [Range(10f, 80f)] public float frontAngle = 35f;
    [Tooltip("Op het einde van de route zacht afremmen tot stilstand")]
    public bool stopAtEnd = true;

    [Header("Live (alleen kijken)")]
    [SerializeField] private string activeStop = "";
    [SerializeField] private float stopTime;

    private int next;          // de volgende stop die nog moet komen
    private bool inStop;
    private float v0, vc, v1, rate, tBrake, tCrawl;

    void Start()
    {
        // Waar ligt elk object op de route? (midden van zijn meshes, niet zijn pivot)
        var spline = routeMover.route.Spline;
        float length = routeMover.route.CalculateLength();
        foreach (var stop in stops)
        {
            if (stop.target == null) continue;
            Vector3 center = CenterOf(stop.target);
            float3 local = routeMover.route.transform.InverseTransformPoint(center);
            SplineUtility.GetNearestPoint(spline, local, out float3 nearest, out float t);
            stop.distanceOnRoute = t * length;
            stop.sideDistance = Vector3.Distance(routeMover.route.transform.TransformPoint(nearest), center);
        }
        stops.Sort((a, b) => a.distanceOnRoute.CompareTo(b.distanceOnRoute));
    }

    void Update()
    {
        if (!routeMover.enabled) return; // rit nog niet gestart

        float distance = routeMover.Distance;
        float speed = shipSpeed.CurrentSpeed;

        // 1. Bezig met een stop? Dan bepaalt de klok de snelheid.
        if (inStop)
        {
            stopTime += Time.deltaTime;
            if (stopTime < stops[next].duration)
            {
                shipSpeed.HoldSpeed(SpeedAt(stopTime));
                return;
            }
            inStop = false;
            activeStop = "";
            next++;
            shipSpeed.ReleaseSpeed();
        }

        // 2. Komt de volgende stop eraan? Op tijd remmen, zodat we traag glijden waar we het object goed zien.
        if (next < stops.Count)
        {
            Stop stop = stops[next];
            float crawlStart;
            if (stop.keepInFront)
            {
                // Het traag glijden moet gedaan zijn voor het object te ver opzij komt.
                float glideEnd = stop.distanceOnRoute - stop.sideDistance / Mathf.Tan(frontAngle * Mathf.Deg2Rad);
                crawlStart = glideEnd - Mathf.Min(crawlSpeed, speed) * PlanCrawlTime(stop, speed);
            }
            else
            {
                crawlStart = stop.distanceOnRoute - stop.crawlLead;
            }
            float brakeDistance = Mathf.Max(0f, (speed * speed - crawlSpeed * crawlSpeed) / (2f * brakeRate));
            if (distance >= crawlStart - brakeDistance)
            {
                Begin(stop, speed);
                shipSpeed.HoldSpeed(SpeedAt(0f));
                return;
            }
        }

        // 3. Einde van de route: zacht uitbollen tot stilstand
        if (stopAtEnd)
        {
            float remaining = Mathf.Max(0f, routeMover.RouteLength - distance);
            float stopSpeed = Mathf.Sqrt(2f * brakeRate * remaining);
            if (stopSpeed < speed)
                shipSpeed.HoldSpeed(Mathf.Max(1f, stopSpeed));
        }
    }

    // Plant de stop: afremmen -> traag glijden -> optrekken naar de snelheid van het trappen.
    private void Begin(Stop stop, float speed)
    {
        inStop = true;
        stopTime = 0f;
        activeStop = stop.name;

        v0 = speed;
        vc = Mathf.Min(crawlSpeed, v0);
        v1 = Mathf.Max(vc, shipSpeed.TargetSpeed);
        rate = brakeRate;

        float change = (v0 - vc) + (v1 - vc);
        tCrawl = PlanCrawlTime(stop, speed);
        if (tCrawl <= 0f)
        {
            // Te weinig tijd om rustig te remmen en op te trekken: dan iets steviger.
            rate = change / stop.duration;
        }
        tBrake = (v0 - vc) / rate;
    }

    // Hoe lang het schip traag glijdt als de stop nu met deze snelheid begint.
    private float PlanCrawlTime(Stop stop, float speed)
    {
        float low = Mathf.Min(crawlSpeed, speed);
        float back = Mathf.Max(low, shipSpeed.TargetSpeed);
        float change = (speed - low) + (back - low);
        return Mathf.Max(0f, stop.duration - change / brakeRate);
    }

    private float SpeedAt(float t)
    {
        if (t < tBrake) return v0 - rate * t;
        if (t < tBrake + tCrawl) return vc;
        return Mathf.Min(v1, vc + rate * (t - tBrake - tCrawl));
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
