using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceCockpit
{
    /// <summary>
    /// Test controller: hold a key (W by default) to pedal, release it to coast to a stop.
    /// Uses the Input System directly, so it needs no changes to the project's input actions.
    /// Only reacts to press/release, so other scripts can still call StartPedaling/StopPedaling.
    /// </summary>
    public class PedalTestInput : MonoBehaviour
    {
        public PedalCrank crank;
        public Key key = Key.W;

        void Update()
        {
            if (crank == null || Keyboard.current == null) return;

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
