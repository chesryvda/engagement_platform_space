using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// FpsCounter — toont de performance tijdens het testen (editor én build.exe).
// Getekend met OnGUI: in VR enkel zichtbaar op het desktopvenster, niet in de headset.
//   F2  aan/uit
// - FPS: gemiddelde over een halve seconde.
// - ms:  tijd per frame.
// - laagste: de traagste frame van de laatste 5 seconden. Stotters (misselijkheid in VR!) zie je vooral hier.
// Kleur t.o.v. de refresh rate van de headset (of 60 op een gewone monitor):
//   groen = haalt het doel, oranje = iets eronder, rood = ver eronder.
public class FpsCounter : MonoBehaviour
{
    public bool visible = true;
    [Tooltip("Hoe ver terug 'laagste FPS' kijkt, in seconden.")]
    public float lowWindowSeconds = 5f;

    private float accumulated;
    private int frames;
    private float fps, frameMs;
    private float targetFps = 60f;
    private float refreshTimer;

    private readonly Queue<KeyValuePair<float, float>> recentFrames = new Queue<KeyValuePair<float, float>>();
    private GUIStyle style;

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame) visible = !visible;
#else
        if (Input.GetKeyDown(KeyCode.F2)) visible = !visible;
#endif

        float dt = Time.unscaledDeltaTime;
        float now = Time.unscaledTime;

        accumulated += dt;
        frames++;
        if (accumulated >= 0.5f)
        {
            fps = frames / accumulated;
            frameMs = accumulated / frames * 1000f;
            accumulated = 0f;
            frames = 0;
        }

        recentFrames.Enqueue(new KeyValuePair<float, float>(now, dt));
        while (recentFrames.Count > 0 && now - recentFrames.Peek().Key > lowWindowSeconds)
            recentFrames.Dequeue();

        refreshTimer -= dt;
        if (refreshTimer <= 0f)
        {
            targetFps = ReadTargetFps();
            refreshTimer = 2f;
        }
    }

    void OnGUI()
    {
        if (!visible) return;

        float worstDt = 0f;
        foreach (var f in recentFrames) worstDt = Mathf.Max(worstDt, f.Value);
        float lowFps = worstDt > 0f ? 1f / worstDt : 0f;

        float scale = Mathf.Max(0.75f, Screen.height / 1080f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        if (style == null) style = new GUIStyle(GUI.skin.box) { richText = true, alignment = TextAnchor.MiddleLeft, fontSize = 16 };

        string text = string.Format("<color={0}><b>{1:0} FPS</b></color>  {2:0.0} ms   laagste: <color={3}>{4:0}</color>   doel: {5:0}",
            ColorFor(fps), fps, frameMs, ColorFor(lowFps), lowFps, targetFps);

        float width = 380f;
        GUI.Box(new Rect(Screen.width / scale - width - 20f, 20f, width, 32f), text, style);
    }

    private string ColorFor(float value)
    {
        if (value >= targetFps * 0.95f) return "#5f5";
        if (value >= targetFps * 0.8f) return "#fb3";
        return "#f55";
    }

    private static readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

    private static float ReadTargetFps()
    {
        SubsystemManager.GetSubsystems(displays);
        foreach (var d in displays)
            if (d.running && d.TryGetDisplayRefreshRate(out float rate) && rate > 1f)
                return rate;

        double screenRate = Screen.currentResolution.refreshRateRatio.value;
        return screenRate > 1.0 ? (float)screenRate : 60f;
    }
}
