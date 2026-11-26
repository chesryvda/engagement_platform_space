// Waking-up eyes for VR: drawn on a small sphere around the camera, over everything, in both eyes.
// - Eyelids: an upper and a lower lid with a curved shape and soft edges. _Open = 0 is fully closed (black),
//   1 is fully open (the lids are then outside the view).
// - Blur: the view behind the lids is blurred by sampling the camera's opaque image (_CameraOpaqueTexture)
//   around each pixel. _Blur = 0 shows the normal, sharp view.
// URP, works with single-pass instanced VR. Needs the camera's opaque texture while _Blur > 0
// (WakeUpIntro switches it on for the active camera during the intro only).
Shader "EngagementPlatform/WakeUpEyes"
{
    Properties
    {
        _Open ("Eyes Open", Range(0, 1)) = 0
        _Blur ("Blur", Range(0, 1)) = 1
        _BlurRadius ("Max Blur Radius (part of the screen)", Range(0, 0.08)) = 0.035
        _LidColor ("Eyelid Color", Color) = (0, 0, 0, 1)
        _OpenHeight ("Opening Height When Open (radians)", Range(0.2, 1.6)) = 1.15
        _OpenWidth ("Eye Width (radians)", Range(0.5, 3)) = 1.6
        _LidSoftness ("Lid Edge Softness", Range(0.005, 0.4)) = 0.06
    }
    SubShader
    {
        // Drawn last, before the end fade (Overlay+100)
        Tags { "RenderType" = "Transparent" "Queue" = "Overlay+90" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "WakeUpEyes"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Open;
                half _Blur;
                half _BlurRadius;
                half4 _LidColor;
                half _OpenHeight;
                half _OpenWidth;
                half _LidSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionVS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionVS = p.positionVS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Where this pixel looks, as angles left/right (x) and up/down (y). The camera looks down -z.
                float3 v = i.positionVS;
                float2 ang = float2(atan2(v.x, -v.z), atan2(v.y, -v.z));
                bool behind = v.z > 0;

                // Lid shape: a soft arc that is highest in the middle. The lower lid moves less than the upper one.
                float arc = saturate(1.0 - (ang.x * ang.x) / (_OpenWidth * _OpenWidth));
                float h = _Open * _OpenHeight;
                float top = h * arc + 0.04 * _Open;
                float bottom = -h * 0.6 * arc - 0.02 * _Open;
                float soft = _LidSoftness * (1.0 + 2.0 * _Blur);
                float lid = saturate(smoothstep(top - soft, top + soft, ang.y) + (1.0 - smoothstep(bottom - soft, bottom + soft, ang.y)));
                lid = max(lid, 1.0 - saturate(_Open * 25.0)); // fully shut: no thin line left
                if (behind) lid = 1.0;

                // Darker towards the lids, like looking through half-open eyes.
                float edge = min(top - ang.y, ang.y - bottom);
                float shade = lerp(0.35, 1.0, saturate(edge / max(0.001, 0.25 * h + 0.05)));

                // Blurred view: samples on a spiral around the pixel, from the camera's opaque image.
                float2 uv = GetNormalizedScreenSpaceUV(i.positionCS);
                half3 col = 0;
                half alphaBlur = saturate(_Blur * 3.0);
                if (_Blur > 0.001)
                {
                    float r = _Blur * _BlurRadius;
                    float aspect = _ScreenParams.y / _ScreenParams.x;
                    const int N = 16;
                    half3 sum = 0;
                    [unroll] for (int k = 0; k < N; k++)
                    {
                        float a = k * 2.39996; // golden angle
                        float d = sqrt((k + 0.5) / N) * r;
                        float2 o = float2(cos(a) * aspect, sin(a)) * d;
                        sum += SampleSceneColor(uv + o);
                    }
                    col = sum / N;
                }

                half3 rgb = lerp(col * shade, _LidColor.rgb, lid);
                half alpha = lerp(max(alphaBlur, 1.0 - shade), 1.0, lid);
                if (_Blur <= 0.001) rgb = lerp(half3(0, 0, 0), _LidColor.rgb, lid);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
