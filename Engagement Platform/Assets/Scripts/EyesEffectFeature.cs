using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>
/// What the eyes do right now (set every frame by WakeUpIntro and FallAsleepOutro).
/// While Active is false the renderer feature does nothing at all.
/// </summary>
public static class EyesEffect
{
    public static bool Active;
    [Range(0f, 1f)] public static float Open = 1f;
    [Range(0f, 1f)] public static float Blur;
}

/// <summary>
/// Draws the waking-up / falling-asleep eyes (blur + eyelids) on the finished image, after all transparent
/// objects, so the scene, the cockpit glass and the text on the screens are all blurred the same way.
/// Only for the camera you look through (tagged MainCamera), and only while EyesEffect.Active is on, so it
/// costs nothing during the rest of the ride. Add it once to the URP renderer (PC_Renderer).
/// </summary>
public class EyesEffectFeature : ScriptableRendererFeature
{
    [Tooltip("Shader Hidden/EngagementPlatform/EyesFullscreen")]
    public Shader shader;
    [Tooltip("Largest blur radius, as a part of the screen (same strength as before).")]
    [Range(0f, 0.08f)] public float blurRadius = 0.035f;
    [Tooltip("Opening height when the eyes are open (radians).")]
    public float openHeight = 1.15f;
    [Tooltip("Eye width (radians).")]
    public float openWidth = 1.6f;
    public float lidSoftness = 0.06f;

    class EyesPass : ScriptableRenderPass
    {
        public Material material;
        public EyesEffectFeature settings;
        static readonly int OpenId = Shader.PropertyToID("_Open"), BlurId = Shader.PropertyToID("_Blur"),
            RadiusId = Shader.PropertyToID("_BlurRadius"), HeightId = Shader.PropertyToID("_OpenHeight"),
            WidthId = Shader.PropertyToID("_OpenWidth"), SoftId = Shader.PropertyToID("_LidSoftness"),
            FovId = Shader.PropertyToID("_TanHalfFov");

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            if (resources.isActiveTargetBackBuffer) return;

            var cam = cameraData.camera;
            float vFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float tanV = Mathf.Tan(vFov), tanH = tanV * cam.aspect;
            material.SetFloat(OpenId, EyesEffect.Open);
            material.SetFloat(BlurId, EyesEffect.Blur);
            material.SetFloat(RadiusId, settings.blurRadius);
            material.SetFloat(HeightId, settings.openHeight);
            material.SetFloat(WidthId, settings.openWidth);
            material.SetFloat(SoftId, settings.lidSoftness);
            material.SetVector(FovId, new Vector4(tanH, tanV, 0f, 0f));

            TextureHandle source = resources.activeColorTexture;
            var desc = renderGraph.GetTextureDesc(source);
            desc.name = "_EyesEffect";
            desc.clearBuffer = false;
            desc.depthBufferBits = 0;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0), "Eyes (blur + lids)");
            resources.cameraColor = destination;
        }
    }

    private EyesPass pass;
    private Material material;

    public override void Create()
    {
        if (shader == null) shader = Shader.Find("Hidden/EngagementPlatform/EyesFullscreen");
        if (shader == null) return;
        material = CoreUtils.CreateEngineMaterial(shader);
        pass = new EyesPass { material = material, settings = this, renderPassEvent = RenderPassEvent.AfterRenderingTransparents };
        // Without post-processing URP may draw straight into the screen; this pass needs an intermediate image
        // to read from (only while the eyes are active, so the rest of the ride is unaffected).
        pass.requiresIntermediateTexture = true;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null || !EyesEffect.Active) return;
        var cam = renderingData.cameraData.camera;
        if (renderingData.cameraData.cameraType != CameraType.Game || !cam.CompareTag("MainCamera")) return;
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(material);
}
