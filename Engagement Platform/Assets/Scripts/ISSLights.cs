using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small lights that make the ISS feel alive.
/// - Beacons: slow, calm blinking like the lights on a control tower. Two groups (red and amber) that take turns.
/// - Windows: warm light in a few windows with a soft flicker; now and then one dims for a moment,
///   as if someone floats past it.
/// - Sun glint: the solar panels flash briefly when they reflect the sun toward the camera
///   (this happens during the tilt in ISSFlyover).
/// All glow is switched with a MaterialPropertyBlock (same approach as TeslaHeadlights), so the materials stay untouched.
/// Put the beacon and window objects under the ISS so ISSFlyover moves them along.
/// Setup: fill in Target, then right-click this component → "Create placeholder lights" to get a starting layout.
/// </summary>
public class ISSLights : MonoBehaviour
{
    [System.Serializable]
    public class BeaconGroup
    {
        public string name = "Red";
        public List<Renderer> lights = new List<Renderer>();
        [ColorUsage(false, true)] public Color color = new Color(1f, 0.05f, 0.02f) * 6f;
        [Tooltip("One full blink (on + off), in seconds.")]
        [Min(0.2f)] public float cycle = 2f;
        [Tooltip("Part of the cycle the light is on.")]
        [Range(0.05f, 0.95f)] public float onPart = 0.5f;
        [Tooltip("How long it takes to glow up and fade out, in seconds. 0 = hard on/off.")]
        [Min(0f)] public float fade = 0.25f;
        [Tooltip("Shift in the cycle (0–1). 0.5 = this group is on while a group at 0 is off.")]
        [Range(0f, 1f)] public float offset = 0f;
        [Tooltip("How much glow is left when off (0 = dark).")]
        [Range(0f, 0.3f)] public float offGlow = 0.02f;
    }

    [Header("References")]
    [Tooltip("The ISS object (the one ISSFlyover moves).")]
    public Transform target;

    [Header("Beacons")]
    public List<BeaconGroup> beacons = new List<BeaconGroup>
    {
        new BeaconGroup { name = "Wing edges", color = new Color(1f, 0.55f, 0.05f) * 5f, offset = 0f },
        new BeaconGroup { name = "Amber", color = new Color(1f, 0.2f, 0f) * 4.5f, offset = 0.5f },
    };

    [Header("Windows")]
    public List<Renderer> windows = new List<Renderer>();
    [ColorUsage(false, true)] public Color windowColor = new Color(1f, 0.45f, 0.15f) * 2f;
    [Tooltip("How strong the soft flicker is (0 = steady).")]
    [Range(0f, 0.5f)] public float flicker = 0.12f;
    [Tooltip("How fast the flicker moves.")]
    [Min(0f)] public float flickerSpeed = 1.5f;
    [Tooltip("On average every this many seconds, one window dims for a moment (someone floats past).")]
    [Min(1f)] public float dimEvery = 6f;

    [Header("Sun glint on the solar panels")]
    [Tooltip("Renderers of the solar panels. 'Create placeholder lights' fills this with every renderer that uses the panel material.")]
    public List<Renderer> solarPanels = new List<Renderer>();
    [Tooltip("Names of the solar panel materials (the big brown wings are M_Issbso / M_Issbso2).")]
    public List<string> solarMaterials = new List<string> { "M_Issbso", "M_Issbso2", "M_Isssolar" };
    [Tooltip("The sun. Empty = the scene's sun light (Lighting settings).")]
    public Light sun;
    [ColorUsage(false, true)] public Color glintColor = new Color(1f, 0.95f, 0.85f) * 3f;
    [Tooltip("How exactly the panel must mirror the sun into the camera before it flashes. Higher = rarer, shorter flash.")]
    [Range(0.9f, 0.9999f)] public float glintSharpness = 0.985f;

    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    MaterialPropertyBlock block;
    readonly List<int> panelSlots = new List<int>();
    readonly List<Vector3> panelNormals = new List<Vector3>();
    float[] windowSeeds;
    int dimWindow = -1;
    float dimTime, nextDim;
    Camera cam;

    void Start()
    {
        block = new MaterialPropertyBlock();
        if (sun == null) sun = RenderSettings.sun;
        cam = Camera.main;

        windowSeeds = new float[windows.Count];
        for (int i = 0; i < windowSeeds.Length; i++) windowSeeds[i] = Random.value * 100f;
        nextDim = Random.Range(dimEvery * 0.5f, dimEvery * 1.5f);

        // Each panel: which material slot to light, and which way its flat side faces
        // (the thinnest side of its mesh).
        foreach (var r in solarPanels)
        {
            int slot = -1;
            var mats = r != null ? r.sharedMaterials : new Material[0];
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && solarMaterials.Contains(mats[i].name)) { slot = i; break; }
            panelSlots.Add(slot);
            panelNormals.Add(FlatSide(r));
        }
    }

    void Update()
    {
        float t = Time.time;

        foreach (var g in beacons)
        {
            float level = Blink(g, t);
            Color glow = g.color * Mathf.Lerp(g.offGlow, 1f, level);
            glow.a = 1f;
            // Beacons are soft glow sprites (additive): their colour IS the light, so off = invisible.
            foreach (var r in g.lights)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColor, glow);
                r.SetPropertyBlock(block);
            }
        }

        UpdateWindows(t);
        UpdateGlint();
    }

    // The glow sprites always face the camera, so you only ever see a round point of light.
    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        Vector3 eye = cam.transform.position;
        foreach (var g in beacons)
            foreach (var r in g.lights)
                if (r != null)
                    // A quad is seen from its -Z side, so point +Z away from the camera.
                    r.transform.rotation = Quaternion.LookRotation(r.transform.position - eye, cam.transform.up);
    }

    // 0..1: how far a beacon is on at time t. Soft edges when fade > 0.
    static float Blink(BeaconGroup g, float t)
    {
        float phase = Mathf.Repeat(t / g.cycle + g.offset, 1f) * g.cycle; // seconds into this cycle
        float onTime = g.cycle * g.onPart;
        if (g.fade <= 0f) return phase < onTime ? 1f : 0f;
        float f = Mathf.Min(g.fade, onTime * 0.5f);
        float up = Mathf.Clamp01(phase / f);
        float down = Mathf.Clamp01((onTime - phase) / f);
        return Mathf.SmoothStep(0f, 1f, Mathf.Min(up, down));
    }

    void UpdateWindows(float t)
    {
        if (windows.Count == 0) return;

        nextDim -= Time.deltaTime;
        if (nextDim <= 0f)
        {
            dimWindow = Random.Range(0, windows.Count);
            dimTime = 0f;
            nextDim = Random.Range(dimEvery * 0.5f, dimEvery * 1.5f);
        }
        dimTime += Time.deltaTime;

        for (int i = 0; i < windows.Count; i++)
        {
            float noise = Mathf.PerlinNoise(windowSeeds[i], t * flickerSpeed);
            float level = 1f - flicker + flicker * noise;
            if (i == dimWindow && dimTime < 1.2f)
                level *= 1f - 0.6f * Mathf.Sin(dimTime / 1.2f * Mathf.PI); // dips and comes back
            SetGlow(windows[i], -1, windowColor * level, null);
        }
    }

    void UpdateGlint()
    {
        if (sun == null || cam == null) return;
        Vector3 sunDir = sun.transform.forward; // direction the sunlight travels
        for (int i = 0; i < solarPanels.Count; i++)
        {
            var r = solarPanels[i];
            if (r == null || panelSlots[i] < 0) continue;
            Vector3 n = r.transform.TransformDirection(panelNormals[i]).normalized;
            Vector3 toCam = (cam.transform.position - r.bounds.center).normalized;
            // Panels have two sides: use whichever faces the sun.
            if (Vector3.Dot(n, -sunDir) < 0f) n = -n;
            float mirror = Vector3.Dot(Vector3.Reflect(sunDir, n), toCam);
            float strength = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(glintSharpness, 1f, mirror));
            SetGlow(r, panelSlots[i], glintColor * strength, null);
        }
    }

    void SetGlow(Renderer r, int slot, Color emission, Color? baseColor)
    {
        if (r == null) return;
        if (slot >= 0) r.GetPropertyBlock(block, slot); else r.GetPropertyBlock(block);
        block.SetColor(EmissionColor, emission);
        if (baseColor.HasValue) block.SetColor(BaseColor, baseColor.Value * 0.3f);
        if (slot >= 0) r.SetPropertyBlock(block, slot); else r.SetPropertyBlock(block);
    }

    // Which way a panel faces (local). Uses the direction saved by "Create placeholder lights"
    // (worked out from the mesh normals in the editor); otherwise the thinnest side of its mesh.
    Vector3 FlatSide(Renderer r)
    {
        int i = solarPanels.IndexOf(r);
        if (i >= 0 && i < panelFacing.Count && panelFacing[i] != Vector3.zero) return panelFacing[i];
        var mf = r != null ? r.GetComponent<MeshFilter>() : null;
        if (mf == null || mf.sharedMesh == null) return Vector3.up;
        Vector3 s = mf.sharedMesh.bounds.size;
        if (s.x <= s.y && s.x <= s.z) return Vector3.right;
        if (s.y <= s.z) return Vector3.up;
        return Vector3.forward;
    }

    [SerializeField, HideInInspector] List<Vector3> panelFacing = new List<Vector3>();

    [Header("Placeholder layout (editor only)")]
    [Tooltip("Material for the beacon glow sprites (M_ISSBeaconGlow). Windows get M_ISSWindow.")]
    public Material placeholderMaterial;
    [Tooltip("Size of the beacon glow as a part of the ISS length (the soft halo; the bright core is much smaller).")]
    [Range(0.002f, 0.05f)] public float beaconSize = 0.025f;

#if UNITY_EDITOR
    /// <summary>
    /// Makes a starting layout under the ISS: red beacons on both ends of its longest side,
    /// amber beacons near the middle, a row of windows on the underside, and finds the solar panels.
    /// Everything can be moved afterwards. Undo works.
    /// </summary>
    [ContextMenu("Create placeholder lights")]
    void CreatePlaceholders()
    {
        if (target == null) { Debug.LogWarning("ISSLights: fill in Target first."); return; }

        // Size of the ISS in its own space.
        Bounds b = new Bounds();
        bool first = true;
        foreach (var mf in target.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            Matrix4x4 m = target.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            Bounds mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mb.center + Vector3.Scale(mb.extents, new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1) * 2f - Vector3.one);
                Vector3 p = m.MultiplyPoint3x4(corner);
                if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
            }
        }
        if (first) { Debug.LogWarning("ISSLights: no meshes found under Target."); return; }

        // Longest side = the truss with the panels; shortest side = top/bottom.
        Vector3 size = b.size;
        int longAxis = size.x >= size.y && size.x >= size.z ? 0 : (size.y >= size.z ? 1 : 2);
        int shortAxis = size.x <= size.y && size.x <= size.z ? 0 : (size.y <= size.z ? 1 : 2);
        int midAxis = 3 - longAxis - shortAxis;
        Vector3 L = Axis(longAxis), S = Axis(shortAxis), M = Axis(midAxis);
        float length = size[longAxis];
        float d = length * beaconSize;

        UnityEditor.Undo.RecordObject(this, "Create ISS lights");
        var root = new GameObject("ISS_Lights").transform;
        UnityEditor.Undo.RegisterCreatedObjectUndo(root.gameObject, "Create ISS lights");
        root.SetParent(target, false);

        var red = beacons.Count > 0 ? beacons[0] : null;
        var amber = beacons.Count > 1 ? beacons[1] : null;
        if (red != null) red.lights.Clear();
        if (amber != null) amber.lights.Clear();
        windows.Clear();

        Vector3 c = b.center;
        Vector3 top = S * (size[shortAxis] * 0.5f);
        red?.lights.Add(Make(root, PrimitiveType.Quad, "Beacon_Red_L", c - L * (length * 0.49f) + top * 0.3f, d));
        red?.lights.Add(Make(root, PrimitiveType.Quad, "Beacon_Red_R", c + L * (length * 0.49f) + top * 0.3f, d));
        amber?.lights.Add(Make(root, PrimitiveType.Quad, "Beacon_Amber_Front", c + M * (size[midAxis] * 0.3f) + top * 0.5f, d * 0.8f));
        amber?.lights.Add(Make(root, PrimitiveType.Quad, "Beacon_Amber_Back", c - M * (size[midAxis] * 0.3f) + top * 0.5f, d * 0.8f));
        amber?.lights.Add(Make(root, PrimitiveType.Quad, "Beacon_Amber_Bottom", c - top * 0.6f, d * 0.8f));

        // Windows: a short row along the middle, on the underside (the side the ship looks at).
        for (int i = 0; i < 5; i++)
        {
            Vector3 p = c - top * 0.35f + M * (size[midAxis] * (-0.2f + 0.1f * i));
            var w = Make(root, PrimitiveType.Quad, "Window_" + (i + 1), p, d * 0.3f);
            // A Unity quad is visible from its -Z side; pointing +Z up makes it face down, toward the ship.
            w.transform.localRotation = Quaternion.LookRotation(S, M);
            windows.Add(w);
        }

        // Solar panels: every renderer that uses the panel material.
        solarPanels.Clear();
        foreach (var r in target.GetComponentsInChildren<Renderer>())
            foreach (var m in r.sharedMaterials)
                if (m != null && solarMaterials.Contains(m.name)) { solarPanels.Add(r); break; }

        // Save which way each panel faces: the most common normal direction of its mesh.
        panelFacing.Clear();
        foreach (var r in solarPanels)
        {
            var mf = r.GetComponent<MeshFilter>();
            var ns = mf != null && mf.sharedMesh != null ? mf.sharedMesh.normals : new Vector3[0];
            Vector3 best = Vector3.zero; int bestCount = 0;
            for (int k = 0; k < ns.Length; k += Mathf.Max(1, ns.Length / 64))
            {
                Vector3 sum = Vector3.zero; int count = 0;
                foreach (var n in ns) { float dot = Vector3.Dot(n, ns[k]); if (Mathf.Abs(dot) > 0.95f) { count++; sum += dot > 0 ? n : -n; } }
                if (count > bestCount) { bestCount = count; best = sum.normalized; }
            }
            panelFacing.Add(best);
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"ISSLights: made {beacons.Count} beacon groups, {windows.Count} windows, found {solarPanels.Count} solar panel renderers. Move them where they look right.");
    }

    Renderer Make(Transform parent, PrimitiveType type, string name, Vector3 localPos, float size)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * size;
        var r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (placeholderMaterial != null) r.sharedMaterial = placeholderMaterial;
        return r;
    }

    static Vector3 Axis(int i) => i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward;
#endif
}
