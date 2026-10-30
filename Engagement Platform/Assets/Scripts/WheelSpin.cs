using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spins the Tesla's wheels around their axles, as if the car is driving forward. The car itself doesn't move.
/// Each wheel turns around its own sideways (X) axis; the brake calipers are separate objects and stay still.
/// </summary>
public class WheelSpin : MonoBehaviour
{
    [Tooltip("The wheel objects to spin (their pivot sits on the axle).")]
    public List<Transform> wheels = new List<Transform>();

    [Tooltip("How fast the wheels turn, in rotations per second. 1 = about 2 m/s for this wheel size.")]
    public float rotationsPerSecond = 1f;

    void Update()
    {
        float degrees = rotationsPerSecond * 360f * Time.deltaTime;
        foreach (var wheel in wheels)
            if (wheel != null)
                wheel.Rotate(Vector3.right, degrees, Space.Self); // positive = top of the wheel rolls forward
    }
}
