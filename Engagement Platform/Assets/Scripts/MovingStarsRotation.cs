using UnityEngine;

public class MovingStarsRotation : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;

    [SerializeField] private float rotationSpeed = 0.1f;

    private void Start()
    {
        if (targetCamera == null)
            return;

        // Fit the sphere (Unity's sphere mesh is 1 unit wide) just inside the camera's draw distance,
        // so the stars stay behind the moon and sun but never get clipped
        Camera cam = targetCamera.GetComponent<Camera>();
        if (cam != null)
            transform.localScale = Vector3.one * cam.farClipPlane * 1.9f;
    }

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
