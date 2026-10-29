using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Hides the space debris (the field you fly through and the belt around the Earth) when the game starts,
/// and fades it in once StartFade() is called. RouteMover calls StartFade() when the ship enters the
/// section after the satellite (set up in the route segment's "On Enter" event).
/// During the fade the renderers use temporary transparent copies of their materials; afterwards the
/// normal (opaque) materials are put back, so the debris costs nothing extra once it is visible.
/// </summary>
public class DebrisFadeIn : MonoBehaviour
{
    [Tooltip("Objects whose renderers are hidden at the start and faded in (e.g. Space_Debris and Space_Debris_Belt).")]
    public List<Transform> roots = new List<Transform>();

    [Tooltip("How long the fade-in takes, in seconds.")]
    [Min(0.1f)] public float fadeDuration = 4f;

    [Tooltip("Hide the debris when Play starts. Turn off to keep it visible from the start (for testing).")]
    public bool hideOnStart = true;

    Renderer[] renderers;
    Material[][] originalMaterials;
    bool started;

    void Awake()
    {
        var list = new List<Renderer>();
        foreach (var root in roots)
            if (root != null) list.AddRange(root.GetComponentsInChildren<Renderer>(true));
        renderers = list.ToArray();

        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;
            if (hideOnStart) renderers[i].enabled = false;
        }
    }

    /// <summary>Starts the fade-in. Safe to call more than once.</summary>
    public void StartFade()
    {
        if (started) return;
        started = true;
        StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        // One transparent copy per original material, shared by all renderers that use it.
        var copies = new Dictionary<Material, Material>();
        var baseColors = new Dictionary<Material, Color>();
        for (int i = 0; i < renderers.Length; i++)
        {
            var mats = originalMaterials[i];
            var faded = new Material[mats.Length];
            for (int m = 0; m < mats.Length; m++)
            {
                var src = mats[m];
                if (src == null) continue;
                if (!copies.TryGetValue(src, out var copy))
                {
                    copy = MakeTransparent(src);
                    copies[src] = copy;
                    baseColors[copy] = copy.HasProperty("_BaseColor") ? copy.GetColor("_BaseColor") : Color.white;
                }
                faded[m] = copy;
            }
            renderers[i].sharedMaterials = faded;
            renderers[i].enabled = true;
        }

        SetAlpha(copies.Values, baseColors, 0f);
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeDuration));
            SetAlpha(copies.Values, baseColors, a);
            yield return null;
        }

        // Back to the normal materials, then clean up the temporary copies.
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].sharedMaterials = originalMaterials[i];
        foreach (var copy in copies.Values) Destroy(copy);
    }

    static void SetAlpha(IEnumerable<Material> mats, Dictionary<Material, Color> baseColors, float alpha)
    {
        foreach (var m in mats)
        {
            if (!m.HasProperty("_BaseColor")) continue;
            var c = baseColors[m];
            c.a *= alpha;
            m.SetColor("_BaseColor", c);
        }
    }

    // Turns a URP Lit (or Simple Lit) material copy into an alpha-blended transparent one.
    static Material MakeTransparent(Material src)
    {
        var m = new Material(src) { name = src.name + " (fade)" };
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
        }
        return m;
    }
}
