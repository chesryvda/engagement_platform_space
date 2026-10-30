using System.Reflection;
using UnityEngine;

/// <summary>
/// Live reflection in the astronaut's helmet visor.
/// - A small capture camera sits inside the helmet, attached to the head bone, so it moves and turns with the head.
///   It renders the surroundings (the passing ship, the Earth, the stars) into a cubemap.
/// - The visor uses the VisorReflection shader, which shows that cubemap as a mirror. The cubemap is handed to the
///   visor only (MaterialPropertyBlock), so no other object's look changes.
/// - At the start the cubemap is filled once; during the Tesla stop it is kept live by rendering one cube face per
///   frame (the whole reflection renews every 6 frames). The rest of the ride it costs nothing.
/// </summary>
public class HelmetReflection : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Disabled camera inside the helmet that captures the surroundings.")]
    public Camera captureCamera;
    [Tooltip("The astronaut's renderer and the material slot of the visor.")]
    public Renderer astronaut;
    public int visorMaterialIndex = 0;
    public FocusStops focusStops;
    [Tooltip("Name of the stop in FocusStops for the Tesla.")]
    public string stopName = "Tesla";

    [Header("Quality")]
    [Tooltip("Size of each cube face in pixels.")]
    public int resolution = 256;
    [Tooltip("Keep the reflection live all the time (for testing).")]
    public bool alwaysOn = false;

    [Header("Live (read only)")]
    [SerializeField] private bool active;

    RenderTexture cube;
    MaterialPropertyBlock block;
    FieldInfo activeStopField;
    int face;
    static readonly int CubeId = Shader.PropertyToID("_Cube");

    void Start()
    {
        activeStopField = typeof(FocusStops).GetField("activeStop", BindingFlags.NonPublic | BindingFlags.Instance);
        if (activeStopField == null)
            Debug.LogWarning("HelmetReflection: can't read the active stop from FocusStops; use 'Always On' instead.");
        if (captureCamera == null || astronaut == null) { enabled = false; return; }

        captureCamera.enabled = false;
        cube = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.ARGBHalf)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Cube,
            name = "HelmetReflectionCube"
        };
        cube.Create();

        block = new MaterialPropertyBlock();
        astronaut.GetPropertyBlock(block, visorMaterialIndex);
        block.SetTexture(CubeId, cube);
        astronaut.SetPropertyBlock(block, visorMaterialIndex);

        captureCamera.RenderToCubemap(cube, 63); // all six faces once
    }

    void LateUpdate()
    {
        bool want = alwaysOn;
        if (!want && activeStopField != null && focusStops != null)
            want = (string)activeStopField.GetValue(focusStops) == stopName;
        active = want;
        if (!active) return;

        captureCamera.RenderToCubemap(cube, 1 << face);
        face = (face + 1) % 6;
    }

    void OnDestroy()
    {
        if (cube != null) { cube.Release(); Destroy(cube); }
    }
}
