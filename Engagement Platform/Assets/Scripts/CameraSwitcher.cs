using System.Collections;
using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Wisselt tussen de VR-camera (voor de headset) en een PC-camera (om te testen op een gewoon scherm).
// - Zonder headset in de Editor (of een development build) start automatisch de PC-camera.
// - Met headset blijft de VR-camera actief.
// - Tijdens Play wissel je met de toets C (Editor / development build).
// De VR-camera zelf wordt niet verplaatst of aangepast.
public class CameraSwitcher : MonoBehaviour
{
    [Header("Camera's")]
    [Tooltip("De camera van de XR Origin (Main Camera). Wordt niet aangepast, alleen aan/uit gezet.")]
    public Camera vrCamera;
    [Tooltip("De camera voor PC-tests, kind van Ship.")]
    public Camera pcCamera;

    [Header("Gedrag")]
    [Tooltip("Zonder headset automatisch de PC-camera gebruiken (alleen Editor / development build).")]
    public bool autoPcCameraWithoutHeadset = true;
#if ENABLE_INPUT_SYSTEM
    public Key switchKey = Key.C;
#endif

    [Header("Live (alleen kijken)")]
    [SerializeField] private bool usingPcCamera;

    private bool AllowPcCamera => Application.isEditor || Debug.isDebugBuild;

    IEnumerator Start()
    {
        Apply(false);
        yield return null; // XR krijgt even de tijd om te starten
        if (autoPcCameraWithoutHeadset && AllowPcCamera && !XRSettings.isDeviceActive)
            Apply(true);
    }

    void Update()
    {
        if (!AllowPcCamera) return;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current[switchKey].wasPressedThisFrame) Toggle();
#else
        if (Input.GetKeyDown(KeyCode.C)) Toggle();
#endif
    }

    public void Toggle()
    {
        Apply(!usingPcCamera);
    }

    private void Apply(bool pc)
    {
        if (pc && (pcCamera == null || !AllowPcCamera)) pc = false;
        usingPcCamera = pc;

        SetCamera(pcCamera, pc);
        SetCamera(vrCamera, !pc);
    }

    // Camera + AudioListener aan/uit en de MainCamera-tag meegeven, zodat Camera.main altijd klopt.
    private static void SetCamera(Camera cam, bool on)
    {
        if (cam == null) return;
        cam.enabled = on;
        var listener = cam.GetComponent<AudioListener>();
        if (listener != null) listener.enabled = on;
        cam.tag = on ? "MainCamera" : "Untagged";
    }
}
