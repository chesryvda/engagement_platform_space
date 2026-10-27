using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SpaceCockpit
{
    /// <summary>
    /// Keeps each leg's Two Bone IK foot target glued to its pedal, so the legs follow the crank.
    /// The offset between foot and pedal is taken from the pose at startup, so the rest pose doesn't change.
    /// </summary>
    [DefaultExecutionOrder(-40)] // after PedalCrank, before the Animator evaluates the rig
    public class PedalLegIK : MonoBehaviour
    {
        [System.Serializable]
        public class Leg
        {
            public TwoBoneIKConstraint constraint;
            [Tooltip("Pedal part the foot rests on (the platform).")]
            public Transform pedal;

            [System.NonSerialized] public Vector3 localOffset;
            [System.NonSerialized] public Quaternion localRotation;
            [System.NonSerialized] public bool ready;
        }

        public Leg left = new Leg();
        public Leg right = new Leg();

        [Range(0f, 1f)] public float ikWeight = 1f;

        void Start()
        {
            Capture(left);
            Capture(right);
        }

        /// <summary>Re-reads the foot-to-pedal offsets from the current pose.</summary>
        public void CaptureOffsets()
        {
            Capture(left);
            Capture(right);
        }

        public void SetIKWeight(float weight) => ikWeight = Mathf.Clamp01(weight);

        void Capture(Leg leg)
        {
            leg.ready = false;
            if (leg.constraint == null || leg.pedal == null || leg.constraint.data.target == null)
            {
                Debug.LogWarning($"{name}: a leg is missing its constraint, pedal or foot target.", this);
                return;
            }
            Transform target = leg.constraint.data.target;
            leg.localOffset = leg.pedal.InverseTransformPoint(target.position);
            leg.localRotation = Quaternion.Inverse(leg.pedal.rotation) * target.rotation;
            leg.ready = true;
        }

        void Update()
        {
            Follow(left);
            Follow(right);
        }

        void Follow(Leg leg)
        {
            if (!leg.ready) return;
            Transform target = leg.constraint.data.target;
            target.SetPositionAndRotation(
                leg.pedal.TransformPoint(leg.localOffset),
                leg.pedal.rotation * leg.localRotation);
            leg.constraint.weight = ikWeight;
        }
    }
}
