using UnityEngine;

/// <summary>
/// Keeps a far-away object (the sun) at a fixed offset from the ship, like a skybox.
/// Without this the sun is a normal world object, so flying towards the moon makes it
/// slide across the sky and grow, which is not what a star millions of km away does.
/// Put it on the root of the sun and assign the ship as target.
/// </summary>
public class SkyAnchor : MonoBehaviour
{
    [Tooltip("The ship (the object RouteMover moves). The sun follows its position, not its rotation.")]
    [SerializeField] private Transform target;

    [Tooltip("Sun position minus target position. Fixed, so the sun keeps the same direction and size.")]
    [SerializeField] private Vector3 offset;

    [Tooltip("Directional light that is the sun's light. It is aimed from the sun at the ship, so lighting and shadows always match where the sun is.")]
    [SerializeField] private Light sunLight;

    [ContextMenu("Capture offset from current positions")]
    private void CaptureOffset()
    {
        if (target != null) offset = transform.position - target.position;
    }

    // After RouteMover.Update has moved the ship, so there is no one-frame lag.
    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;

        if (sunLight != null && offset != Vector3.zero)
            sunLight.transform.rotation = Quaternion.LookRotation(-offset);
    }
}
