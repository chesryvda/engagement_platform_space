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

        [Tooltip("How fast the cockpit turns toward the destination at full speed, in degrees per second. Eases in with the pedals.")]
        [Min(0f)] public float turnSpeed = 5f;

        [Tooltip("Point the cockpit straight at the destination when Play starts, so it doesn't turn when you start pedaling.")]
        public bool alignOnStart = true;

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

        void Start()
        {
            if (alignOnStart && faceDestination && vehicle != null && destination != null)
            {
                GetDestination(out Vector3 center, out _);
                Vector3 toTarget = center - vehicle.position;
                if (toTarget.sqrMagnitude > 0.0001f) vehicle.rotation = Facing(toTarget.normalized);
            }
        }

        /// <summary>Rotation that points the cockpit's nose along a direction while staying upright (no roll).</summary>
        Quaternion Facing(Vector3 direction)
        {
            Vector3 up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f ? vehicle.up : Vector3.up;
            Vector3 flatForward = new Vector3(localForward.x, 0f, localForward.z);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;
            return Quaternion.LookRotation(direction, up) * Quaternion.Inverse(Quaternion.LookRotation(flatForward.normalized, Vector3.up));
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
                    // turn rate follows the pedal speed, so it eases in instead of snapping
                    vehicle.rotation = Quaternion.RotateTowards(vehicle.rotation, Facing(direction), turnSpeed * pedal * dt);
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
