using UnityEngine;

/// <summary>
/// Slowly turns a cloud layer around the planet's own axis (west to east, like the real Earth).
/// Put it on the cloud object. The object's pivot must be the planet's center.
/// </summary>
public class EarthCloudRotation : MonoBehaviour
{
    [Tooltip("Degrees per second. 1 = one full turn in 6 minutes.")]
    [SerializeField] private float degreesPerSecond = 1f;

    [Tooltip("Pole axis in this object's local space (north). For the earthv2 clouds this is Z.")]
    [SerializeField] private Vector3 localPoleAxis = Vector3.forward;

    public float DegreesPerSecond
    {
        get => degreesPerSecond;
        set => degreesPerSecond = value;
    }

    private void Update()
    {
        transform.Rotate(localPoleAxis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
