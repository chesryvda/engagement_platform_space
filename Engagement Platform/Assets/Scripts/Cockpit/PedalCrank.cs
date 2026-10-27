using System.Collections.Generic;
using UnityEngine;

namespace SpaceCockpit
{
    /// <summary>
    /// Turns a bicycle-style crank around its axle. Crank parts (axle, arms, spindles)
    /// rotate with the crank; pedal platforms orbit with it but stay level, like real pedals.
    /// Control it with StartPedaling(), StopPedaling() and SetPedalSpeed().
    /// </summary>
    [DefaultExecutionOrder(-50)] // before PedalLegIK, so feet follow this frame's pedal position
    public class PedalCrank : MonoBehaviour
    {
        [Header("Crank")]
        [Tooltip("The crank axle. Its position is the pivot the pedals turn around.")]
        public Transform axle;

        [Tooltip("Axis of the axle mesh, in the axle's local space, that the crank turns around.")]
        public Vector3 axleLocalAxis = Vector3.forward;

        [Tooltip("Parts that rotate with the crank: axle, crank arms, spindles, bearing caps.")]
        public List<Transform> rotatingParts = new List<Transform>();

        /// <summary>One pedal: parts that move together with a spindle but stay level.</summary>
        [System.Serializable]
        public class LevelPedal
        {
            [Tooltip("Spindle the pedal turns on (one of the rotating parts). The pedal follows its position.")]
            public Transform spindle;
            [Tooltip("Pedal parts that stay level: platform and cage rails.")]
            public List<Transform> parts = new List<Transform>();
        }

        [Tooltip("Pedals that orbit with the crank but keep their orientation, each as one solid piece.")]
        public List<LevelPedal> pedals = new List<LevelPedal>();

        [Header("Direction")]
        [Tooltip("Object whose forward direction decides which way is 'pedaling forward'.")]
        public Transform forwardReference;

        [Tooltip("Forward direction in forwardReference's local space. The top of the crank moves this way.")]
        public Vector3 localForward = Vector3.back;

        [Header("Speed (revolutions per minute)")]
        [Min(0f)] public float pedalSpeed = 60f;
        [Min(0f)] public float acceleration = 120f;
        [Min(0f)] public float deceleration = 90f;

        public bool IsPedaling { get; private set; }
        public float CurrentRpm { get; private set; }
        public float CrankAngle { get; private set; }

        struct RestPose
        {
            public Transform part;
            public Vector3 offset;       // from pivot, in the axle parent's space
            public Quaternion rotation;  // in the axle parent's space
            public bool rotates;
            public Vector3 anchor;       // level parts: spindle offset from pivot they move with
        }

        readonly List<RestPose> rest = new List<RestPose>();
        Transform space;
        Vector3 pivot;
        Vector3 axis;
        float direction = 1f;

        void Awake()
        {
            CaptureRestPose();
        }

        /// <summary>Records the current pose as angle 0. Call again if the pedal parts are moved.</summary>
        public void CaptureRestPose()
        {
            rest.Clear();
            if (axle == null)
            {
                Debug.LogWarning($"{name}: PedalCrank has no axle assigned.", this);
                return;
            }

            space = axle.parent;
            pivot = ToSpacePoint(axle.position);
            axis = ToSpaceDir(axle.TransformDirection(axleLocalAxis)).normalized;

            // Pick the spin direction so the top of the crank moves forward.
            if (forwardReference != null)
            {
                Vector3 forward = ToSpaceDir(forwardReference.TransformDirection(localForward));
                Vector3 up = ToSpaceDir(forwardReference.up);
                direction = Vector3.Dot(Vector3.Cross(axis, up), forward) >= 0f ? 1f : -1f;
            }

            foreach (var part in rotatingParts) Add(part, true, Vector3.zero);
            foreach (var pedal in pedals)
            {
                if (pedal == null || pedal.spindle == null) continue;
                Vector3 anchor = ToSpacePoint(pedal.spindle.position) - pivot;
                foreach (var part in pedal.parts) Add(part, false, anchor);
            }
            CrankAngle = 0f;
        }

        void Add(Transform part, bool rotates, Vector3 anchor)
        {
            if (part == null) return;
            rest.Add(new RestPose
            {
                part = part,
                offset = ToSpacePoint(part.position) - pivot,
                rotation = ToSpaceRot(part.rotation),
                rotates = rotates,
                anchor = anchor
            });
        }

        public void StartPedaling() => IsPedaling = true;

        public void StopPedaling() => IsPedaling = false;

        /// <summary>Sets the pedaling speed in revolutions per minute.</summary>
        public void SetPedalSpeed(float rpm) => pedalSpeed = Mathf.Max(0f, rpm);

        void Update()
        {
            float target = IsPedaling ? pedalSpeed : 0f;
            float rate = IsPedaling ? acceleration : deceleration;
            CurrentRpm = Mathf.MoveTowards(CurrentRpm, target, rate * Time.deltaTime);
            if (CurrentRpm <= 0f) return;

            CrankAngle = Mathf.Repeat(CrankAngle + CurrentRpm * 6f * Time.deltaTime, 360f); // rpm -> degrees/second
            Apply();
        }

        void Apply()
        {
            Quaternion spin = Quaternion.AngleAxis(CrankAngle * direction, axis);
            foreach (var r in rest)
            {
                if (r.part == null) continue;
                if (r.rotates)
                {
                    r.part.position = FromSpacePoint(pivot + spin * r.offset);
                    r.part.rotation = FromSpaceRot(spin * r.rotation);
                }
                else
                {
                    // the whole pedal shifts by however far its spindle moved, so it stays one solid piece
                    Vector3 shift = spin * r.anchor - r.anchor;
                    r.part.position = FromSpacePoint(pivot + r.offset + shift);
                    r.part.rotation = FromSpaceRot(r.rotation);
                }
            }
        }

        Vector3 ToSpacePoint(Vector3 p) => space ? space.InverseTransformPoint(p) : p;
        Vector3 ToSpaceDir(Vector3 d) => space ? space.InverseTransformDirection(d) : d;
        Quaternion ToSpaceRot(Quaternion q) => space ? Quaternion.Inverse(space.rotation) * q : q;
        Vector3 FromSpacePoint(Vector3 p) => space ? space.TransformPoint(p) : p;
        Quaternion FromSpaceRot(Quaternion q) => space ? space.rotation * q : q;
    }
}
