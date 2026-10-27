using UnityEngine;

public class MovingStarsRotation : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;

    [SerializeField] private float rotationSpeed = 0.1f;

    private void LateUpdate()
    {
        if (targetCamera == null)
            return;

        // Keep the star sphere centered on the camera
        transform.position = targetCamera.position;

        // Slowly rotate the stars around the vertical axis
        transform.Rotate(
            Vector3.up,
            rotationSpeed * Time.deltaTime,
            Space.Self
        );
    }
}
