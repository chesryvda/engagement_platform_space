// Star field for the inside of the MovingStars sphere: additive, so the black of the texture
// stays invisible, with each star twinkling on its own rhythm. URP, works with single-pass instanced VR.
Shader "EngagementPlatform/TwinklingStars"
{
    Properties
    {
        _BaseMap ("Star Texture", 2D) = "black" {}
        [HDR] _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _Brightness ("Brightness", Range(0, 5)) = 1.0
        _TwinkleSpeed ("Twinkle Speed", Range(0, 10)) = 2.0
        _TwinkleAmount ("Twinkle Amount", Range(0, 1)) = 0.7
        _TwinkleCells ("Twinkle Cells Per Tile", Float) = 128
    }
    SubShader
    {
        // Drawn first among transparents so planet glows etc. layer on top of the stars
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-499" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Stars"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Brightness;
                half _TwinkleSpeed;
                half _TwinkleAmount;
                float _TwinkleCells;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 Hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                // Tiling on the material makes the stars smaller (and more of them)
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 stars = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;

                // Every small cell of the texture gets its own phase and speed, so stars twinkle independently
                float2 rnd = Hash22(floor(i.uv * _TwinkleCells));
                float wave = sin(_Time.y * _TwinkleSpeed * (0.5 + rnd.y) + rnd.x * TWO_PI) * 0.5 + 0.5;
                half twinkle = 1.0h - _TwinkleAmount * (half)(wave * wave);

                return half4(stars * _BaseColor.rgb * _Brightness * twinkle, 1.0h);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
