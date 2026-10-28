using UnityEngine;

// SCRIPT 3 — ShipSpeed
// Taak: van RPM naar de snelheid van het ruimteschip.
// 1. RPM -> p (een getal van 0 tot 1)
// 2. doelsnelheid = min + p * (max - min)     (min en max krijgt hij van RouteMover)
// 3. Rustig naar die doelsnelheid toe bewegen, nooit springen.
public class ShipSpeed : MonoBehaviour
{
    public Cadence cadence;

    [Header("Trapbereik (MEET DIT)")]
    [Tooltip("Op of onder deze RPM: p = 0 -> minimumsnelheid")]
    public float rpmForMin = 20f;
    [Tooltip("Op of boven deze RPM: p = 1 -> maximumsnelheid")]
    public float rpmForMax = 80f;

    [Header("Gevoel")]
    [Tooltip("Hoeveel de snelheid per seconde mag veranderen. Kleiner = zachter (minder misselijk).")]
    public float acceleration = 4f;

    [Header("Live (alleen kijken)")]
    [SerializeField] private float p;
    [SerializeField] private float minSpeed = 5f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float targetSpeed;
    [SerializeField] private float currentSpeed;

    // Dit lezen RouteMover en later het dashboard.
    public float CurrentSpeed => currentSpeed;
    public float TargetSpeed => targetSpeed;
    public float P => p;

    private bool started;
    private bool held;
    private float heldSpeed;

    // FocusStops neemt de snelheid tijdelijk over (stop bij een object, einde van de route).
    // Zolang dat zo is, telt het trappen niet mee.
    public void HoldSpeed(float speed)
    {
        held = true;
        heldSpeed = speed;
        currentSpeed = speed;
    }

    public void ReleaseSpeed()
    {
        held = false;
    }

    // RouteMover roept dit elk frame aan met de band van het huidige stuk.
    public void SetBand(float min, float max)
    {
        minSpeed = min;
        maxSpeed = max;

        // Allereerste keer: vanuit stilstand vertrekken en rustig optrekken (niet springen).
        if (!started)
        {
            currentSpeed = 0f;
            started = true;
        }
    }

    void Update()
    {
        // 1. RPM -> p (InverseLerp begrenst automatisch tussen 0 en 1)
        p = Mathf.InverseLerp(rpmForMin, rpmForMax, cadence.RPM);

        // 2. Doelsnelheid binnen de band
        targetSpeed = minSpeed + p * (maxSpeed - minSpeed);

        if (held)
        {
            currentSpeed = heldSpeed;
            return;
        }

        // 3. Rustig ernaartoe
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
    }
}
