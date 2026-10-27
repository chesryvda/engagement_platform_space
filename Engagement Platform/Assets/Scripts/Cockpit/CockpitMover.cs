using UnityEngine;

namespace SpaceCockpit
{
    /// <summary>
    /// Moves the cockpit forward while the pedals turn. Speed follows the crank, so the cockpit
    /// speeds up and coasts to a stop together with the pedals. Optionally flies toward a
    /// destination (the Moon), turns to face it, and stops at a set distance from its surface.
    /// </summary>
    [DefaultExecutionOrder(-45)] // after PedalCrank (reads its speed), before PedalLegIK
    public class CockpitMover : MonoBehaviour
    {
        [Tooltip("The object that moves (the cockpit root). Everything under it moves along, including the camera.")]
        public Transform vehicle;

        [Tooltip("Pedals that drive the movement.")]
        public PedalCrank crank;

        [Header("Speed")]
        [Tooltip("Forward speed in meters per second when pedaling at full speed.")]
        [Min(0f)] public float forwardSpeed = 8f;

        [Header("Destination (optional)")]
        [Tooltip("Fly toward this object. Leave empty to fly straight ahead.")]
        public Transform destination;

        [Tooltip("Stop this many meters before the destination's surface.")]
        [Min(0f)] public float stopDistance = 40f;

        [Tooltip("Turn the cockpit to face the destination while moving.")]
        public bool faceDestination = true;

        [Tooltip("How fast the cockpit turns toward the destination, in degrees per second.")]
        [Min(0f)] public float turnSpeed = 5f;

        [Header("Cockpit orientation")]
        [Tooltip("The cockpit's nose direction in the vehicle's local space. This cockpit faces -Z.")]
        public Vector3 localForward = Vector3.back;

        public float CurrentSpeed { get; private set; }
        public bool HasArrived { get; private set; }

        /// <summary>Sets the forward speed (meters per second at full pedaling speed).</summary>
        public void SetForwardSpeed(float metersPerSecond) => forwardSpeed = Mathf.Max(0f, metersPerSecond);

        /// <summary>Allows moving again after the destination was reached (for example after picking a new one).</summary>
        public void ResetArrival() => HasArrived = false;

        void Reset()
        {
            vehicle = transform.parent;
            crank = GetComponent<PedalCrank>();
        }

        void Update()
        {
            if (vehicle == null || crank == null) return;

            // Same smooth speed-up and slow-down as the pedals.
            float pedal = crank.pedalSpeed > 0f ? Mathf.Clamp01(crank.CurrentRpm / crank.pedalSpeed) : 0f;
            CurrentSpeed = HasArrived ? 0f : forwardSpeed * pedal;
            if (CurrentSpeed <= 0f) return;

            float dt = Time.deltaTime;
            Vector3 nose = vehicle.TransformDirection(localForward).normalized;
            Vector3 direction = nose;
            float maxStep = CurrentSpeed * dt;

            if (destination != null)
            {
                GetDestination(out Vector3 center, out float radius);
                Vector3 toTarget = center - vehicle.position;
                float remaining = toTarget.magnitude - radius - stopDistance;
                if (remaining <= 0f)
                {
                    HasArrived = true;
                    CurrentSpeed = 0f;
                    return;
                }
                direction = toTarget.normalized;
                maxStep = Mathf.Min(maxStep, remaining);

                if (faceDestination && turnSpeed > 0f)
                {
                    Quaternion facing = Quaternion.FromToRotation(nose, direction) * vehicle.rotation;
                    vehicle.rotation = Quaternion.RotateTowards(vehicle.rotation, facing, turnSpeed * dt);
                }
            }

            vehicle.position += direction * maxStep;
        }

        void GetDestination(out Vector3 center, out float radius)
        {
            var r = destination.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                center = r.bounds.center;
                radius = Mathf.Min(r.bounds.extents.x, Mathf.Min(r.bounds.extents.y, r.bounds.extents.z));
            }
            else
            {
                center = destination.position;
                radius = 0f;
            }
        }
    }
}
