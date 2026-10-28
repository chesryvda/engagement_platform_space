using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceCockpit
{
    /// <summary>
    /// Makes the astronaut pedal along with the player.
    /// With a Cadence assigned (the route system's Space taps or bike input), the pedals turn at the
    /// player's measured pace: tap faster and the legs go faster, stop and they coast to a stop.
    /// Without a Cadence, holding the key pedals and releasing it stops.
    /// </summary>
    public class PedalTestInput : MonoBehaviour
    {
        public PedalCrank crank;

        [Tooltip("Pedal pace from the route system (Space taps or the bike). When set, the key below is ignored.")]
        public Cadence cadence;

        [Tooltip("Below this many pedal turns per minute the astronaut stops pedaling.")]
        [Min(0f)] public float minimumRpm = 5f;

        [Tooltip("Fastest the astronaut pedals, in turns per minute, so the legs never look frantic.")]
        [Min(1f)] public float maximumRpm = 120f;

        [Tooltip("Only used when no Cadence is assigned: hold this key to pedal.")]
        public Key key = Key.Space;

        void Update()
        {
            if (crank == null) return;

            if (cadence != null)
            {
                float rpm = cadence.RPM;
                if (rpm >= minimumRpm)
                {
                    crank.SetPedalSpeed(Mathf.Min(rpm, maximumRpm));
                    crank.StartPedaling();
                }
                else
                {
                    crank.StopPedaling();
                }
                return;
            }

            if (Keyboard.current == null) return;
            var control = Keyboard.current[key];
            if (control.wasPressedThisFrame) crank.StartPedaling();
            else if (control.wasReleasedThisFrame) crank.StopPedaling();
        }

        void OnDisable()
        {
            if (crank != null) crank.StopPedaling();
        }
    }
}
