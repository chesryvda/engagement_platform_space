using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

// SCRIPT 4 — RouteMover
// Taak: het schip over de route bewegen en scenes starten.
// - De route is een Spline (lijn) in je scene.
// - De route is opgedeeld in STUKKEN (tussenstuk, satelliet, ...), elk met een eigen min/max.
// - Vlak voor een nieuw stuk gaat de band geleidelijk over (= remzone / optrekzone).
// Nodig: package "Splines" (Window > Package Manager > Unity Registry > Splines).
public class RouteMover : MonoBehaviour
{
    [Serializable]
    public class RouteSegment
    {
        public string name = "Tussenstuk";
        [Tooltip("Op welke afstand (meter langs de route) begint dit stuk?")]
        public float startDistance;
        public float minSpeed = 15f;
        public float maxSpeed = 25f;
        [Tooltip("Wat moet er gebeuren als het schip hier binnenkomt? (Lisa praten, Tesla draaien, ...)")]
        public UnityEvent onEnter;
    }

    [Header("Verwijzingen")]
    public SplineContainer route;
    [Tooltip("Het ruimteschip (met de XR Origin / camera erin)")]
    public Transform ship;
    public ShipSpeed shipSpeed;

    [Header("Route-stukken (van begin naar einde, eerste op afstand 0)")]
    public List<RouteSegment> segments = new List<RouteSegment>();

    [Tooltip("Hoeveel meter VOOR een nieuw stuk de snelheid al begint over te gaan")]
    public float blendDistance = 30f;

    [Header("Einde")]
    public UnityEvent onRouteFinished;

    [Header("Live (alleen kijken)")]
    [SerializeField] private float distance;
    [SerializeField] private float routeLength;
    [SerializeField] private string currentSegmentName;
    [SerializeField] private float elapsedTime;

    private int currentIndex = -1;
    private bool finished;

    void Start()
    {
        routeLength = route.CalculateLength();
        Debug.Log("Lengte van de route: " + routeLength.ToString("0") + " m");
        UpdateBand();      // eerste band doorgeven
        PlaceShip();
    }

    void Update()
    {
        if (finished) return;

        UpdateBand();

        // Afstand optellen: snelheid x tijd
        distance += shipSpeed.CurrentSpeed * Time.deltaTime;
        elapsedTime += Time.deltaTime;

        if (distance >= routeLength)
        {
            distance = routeLength;
            finished = true;
            Debug.Log("Route klaar in " + elapsedTime.ToString("0.0") + " seconden");
            onRouteFinished?.Invoke();
        }

        PlaceShip();
    }

    // Zoekt in welk stuk we zitten, start de scene als het nieuw is,
    // en geeft de (eventueel overvloeiende) band door aan ShipSpeed.
    private void UpdateBand()
    {
        if (segments.Count == 0) return;

        // In welk stuk zitten we?
        int index = 0;
        for (int i = 0; i < segments.Count; i++)
            if (distance >= segments[i].startDistance) index = i;

        // Nieuw stuk? -> gebeurtenis starten
        if (index != currentIndex)
        {
            currentIndex = index;
            currentSegmentName = segments[index].name;
            segments[index].onEnter?.Invoke();
        }

        RouteSegment current = segments[index];
        float min = current.minSpeed;
        float max = current.maxSpeed;

        // Remzone: komt het volgende stuk eraan, dan al geleidelijk overgaan
        if (index + 1 < segments.Count && blendDistance > 0f)
        {
            RouteSegment next = segments[index + 1];
            float blendStart = next.startDistance - blendDistance;
            if (distance > blendStart)
            {
                float t = Mathf.InverseLerp(blendStart, next.startDistance, distance);
                min = Mathf.Lerp(current.minSpeed, next.minSpeed, t);
                max = Mathf.Lerp(current.maxSpeed, next.maxSpeed, t);
            }
        }

        shipSpeed.SetBand(min, max);
    }

    // Zet het schip op de juiste plek op de lijn en laat het vooruit kijken.
    private void PlaceShip()
    {
        float t = routeLength > 0f ? distance / routeLength : 0f;
        route.Evaluate(t, out float3 position, out float3 tangent, out float3 up);

        ship.position = position;
        if (math.lengthsq(tangent) > 0.0001f)
            ship.rotation = Quaternion.LookRotation(tangent, up);
    }
}
