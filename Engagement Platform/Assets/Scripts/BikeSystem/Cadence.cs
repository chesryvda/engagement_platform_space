using System.Collections.Generic;
using UnityEngine;

// SCRIPT 2 — Cadence
// Taak: van "aantal omwentelingen" naar "hoe snel trapt iemand" (RPM = omwentelingen per minuut).
// Werkwijze: kijk naar de TIJD TUSSEN de omwentelingen.
//   1 omwenteling per seconde  = 60 RPM
//   2 omwentelingen per seconde = 120 RPM
// Stopt iemand, dan wordt de tijd sinds de laatste omwenteling langer -> RPM zakt vanzelf naar 0.
public class Cadence : MonoBehaviour
{
    public BikeInput bikeInput;

    [Header("Instellingen")]
    [Tooltip("Naar hoeveel seconden terug we kijken. Wat ouder is, telt niet meer.")]
    public float windowSeconds = 3f;
    [Tooltip("Hoe snel het RPM-getal meebeweegt. Groter = sneller reageren, kleiner = rustiger.")]
    public float smoothing = 4f;

    [Header("Live (alleen kijken)")]
    [SerializeField] private float rawRpm;
    [SerializeField] private float rpm;

    // Dit getal leest ShipSpeed.
    public float RPM => rpm;

    private readonly Queue<float> revolutionTimes = new Queue<float>();
    private float lastRevolutionTime;
    private int lastTotal;

    void Start()
    {
        lastTotal = bikeInput.TotalRevolutions;
    }

    void Update()
    {
        float now = Time.time;

        // 1. Nieuwe omwentelingen noteren (met het tijdstip).
        int total = bikeInput.TotalRevolutions;
        for (int i = lastTotal; i < total; i++)
        {
            revolutionTimes.Enqueue(now);
            lastRevolutionTime = now;
        }
        lastTotal = total;

        // 2. Te oude omwentelingen weggooien.
        while (revolutionTimes.Count > 0 && now - revolutionTimes.Peek() > windowSeconds)
            revolutionTimes.Dequeue();

        // 3. Gemiddelde tijd tussen omwentelingen -> RPM
        if (revolutionTimes.Count >= 2)
        {
            float oldest = revolutionTimes.Peek();
            float averageGap = (lastRevolutionTime - oldest) / (revolutionTimes.Count - 1);

            // Duurt het al langer dan normaal sinds de laatste duw? Dan vertraagt iemand.
            float sinceLast = now - lastRevolutionTime;
            float gap = Mathf.Max(averageGap, sinceLast);

            rawRpm = gap > 0.01f ? 60f / gap : 0f;
        }
        else
        {
            rawRpm = 0f; // te weinig omwentelingen = (bijna) niet trappen
        }

        // 4. Afvlakken, zodat het getal niet springt.
        rpm = Mathf.Lerp(rpm, rawRpm, smoothing * Time.deltaTime);
    }
}
