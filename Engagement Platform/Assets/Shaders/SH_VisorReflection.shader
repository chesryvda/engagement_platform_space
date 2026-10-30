// Mirror visor for the astronaut's helmet: a dark glass that reflects a live cubemap of its surroundings
// (rendered by HelmetReflection from inside the helmet), plus a sun highlight and stronger reflection at
// grazing angles. Only the visor uses it, so nothing else in the scene is affected.
// URP, works with single-pass instanced VR. Supports the alpha fade used by DebrisFadeIn (_Surface/_BaseColor.a).
Shader "EngagementPlatform/VisorReflection"
{
    Properties
    {
        _BaseColor ("Glass Color", Color) = (0.02, 0.02, 0.025, 1)
        _Cube ("Reflection Cubemap", Cube) = "black" {}
        _ReflectionTint ("Reflection Tint", Color) = (0.85, 0.87, 0.9, 1)
        _ReflectionStrength ("Reflection Strength", Range(0, 3)) = 1.2
        _FresnelPower ("Edge Reflection Power", Range(0.5, 8)) = 3
        _FresnelBoost ("Edge Reflection Boost", Range(0, 2)) = 0.6
        _SunHighlight ("Sun Highlight", Range(0, 5)) = 1.5

        // Used by DebrisFadeIn to fade the visor in with the rest of the car
        [HideInInspector] _Surface ("Surface", Float) = 0
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite ("ZWrite", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Visor"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURECUBE(_Cube);
            SAMPLER(sampler_Cube);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ReflectionTint;
                half _ReflectionStrength;
                half _FresnelPower;
                half _FresnelBoost;
                half _SunHighlight;
                half _Surface;
                half _SrcBlend;
                half _DstBlend;
                half _ZWrite;
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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 r = reflect(-v, n);

                half3 reflection = SAMPLE_TEXTURECUBE(_Cube, sampler_Cube, r).rgb * _ReflectionTint.rgb;
                half fresnel = pow(1.0h - saturate(dot(n, v)), _FresnelPower);
                half strength = _ReflectionStrength * (1.0h + _FresnelBoost * fresnel);

                Light sun = GetMainLight();
                half highlight = pow(saturate(dot(r, sun.direction)), 300.0h) * _SunHighlight;

                half3 color = _BaseColor.rgb + reflection * strength + sun.color * highlight;
                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
