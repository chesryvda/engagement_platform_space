using UnityEngine;

/// <summary>
/// Keeps the ship pointing in one fixed direction (from the start of the route towards its end, the Moon),
/// so it doesn't turn along with the gentle bends of the route. The route itself is not changed: the ship
/// still follows exactly the same path, it just no longer yaws with every bend (at most ~4.4°, so you only
/// slide very slightly sideways at times).
/// RouteMover sets the rotation every frame; this script runs after it and puts the fixed heading back.
/// RouteMover itself is not modified.
/// </summary>
[DefaultExecutionOrder(50)] // after RouteMover (0) and FocusStops (-10)
public class ShipHeadingLock : MonoBehaviour
{
    public RouteMover routeMover;
    [Tooltip("Off = the ship turns with the route again, as before.")]
    public bool lockHeading = true;

    private Quaternion fixedRotation;
    private bool ready;

    void Start()
    {
        if (routeMover == null || routeMover.route == null || routeMover.ship == null) return;
        var route = routeMover.route;
        Vector3 start = route.EvaluatePosition(0f);
        Vector3 end = route.EvaluatePosition(1f);
        Vector3 up = route.EvaluateUpVector(0f);
        fixedRotation = Quaternion.LookRotation((end - start).normalized, up);
        ready = true;
        Apply();
    }

    void Update() => Apply();
    void LateUpdate() => Apply();

    private void Apply()
    {
        if (ready && lockHeading) routeMover.ship.rotation = fixedRotation;
    }
}
