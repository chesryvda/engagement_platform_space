using UnityEngine;

// Start de rit pas bij de eerste omwenteling (1x SPATIE of 1 trap op de fiets).
// Tot dan staat RouteMover uit: het schip staat stil aan het begin van de route.
// Daarna start RouteMover op de minimumsnelheid van het eerste stuk.
public class StartOnFirstPedal : MonoBehaviour
{
    public BikeInput bikeInput;
    public RouteMover routeMover;

    void Awake()
    {
        routeMover.enabled = false;
    }

    void Update()
    {
        if (bikeInput.TotalRevolutions >= 1)
        {
            routeMover.enabled = true;
            enabled = false; // klaar, dit script hoeft niet meer te draaien
        }
    }
}
