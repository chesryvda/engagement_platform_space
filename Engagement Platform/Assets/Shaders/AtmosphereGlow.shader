// Soft atmosphere rim for a planet: additive glow that is strongest at the edge (fresnel)
// and brighter on the side facing the main light. URP, works with single-pass instanced VR.
Shader "EngagementPlatform/AtmosphereGlow"
{
    Properties
    {
        [HDR] _Color ("Glow Color", Color) = (0.35, 0.6, 1.0, 1.0)
        _Intensity ("Intensity", Range(0, 5)) = 1.5
        _Power ("Edge Sharpness", Range(0.5, 10)) = 3.0
        _NightSide ("Night Side Brightness", Range(0, 1)) = 0.15
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _Power;
                half _NightSide;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                half ndv = saturate(dot(n, viewDir));
                // strongest just inside the edge, fading to zero at the outer silhouette (no hard line)
                half rim = pow(1.0h - ndv, _Power) * saturate(ndv * 5.0h);
                half sun = saturate(dot(n, GetMainLight().direction) * 0.5h + 0.5h);
                half light = lerp(_NightSide, 1.0h, sun);
                return half4(_Color.rgb * rim * light * _Intensity, 1.0h);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
