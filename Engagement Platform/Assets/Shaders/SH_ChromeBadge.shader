// Polished chrome for the Technopolis badge on the Tesla. The badge reflects a cubemap captured from the
// Tesla's nose (red hood, windshield, space), so it reads as real chrome even though the nose is in the
// shadow of the sun. Only the badge uses it, so the paint and the rest of the scene are not affected.
// Adds a stronger reflection at grazing angles, a soft bright glint band and a sharp sun highlight.
// URP, works with single-pass instanced VR. Supports the alpha fade used by DebrisFadeIn (_Surface/_BaseColor.a).
Shader "EngagementPlatform/ChromeBadge"
{
    Properties
    {
        _BaseColor ("Chrome Tint", Color) = (0.92, 0.93, 0.95, 1)
        _Cube ("Reflection Cubemap", Cube) = "grey" {}
        _ReflectionStrength ("Reflection Strength", Range(0, 3)) = 1.4
        _Blur ("Reflection Blur", Range(0, 1)) = 0.12
        _FresnelPower ("Edge Reflection Power", Range(0.5, 8)) = 4
        _FresnelBoost ("Edge Reflection Boost", Range(0, 2)) = 0.5
        _Ambient ("Minimum Brightness", Range(0, 0.5)) = 0.06
        _SunHighlight ("Sun Highlight", Range(0, 5)) = 2
        _BandColor ("Glint Band Color", Color) = (0.85, 0.9, 1, 1)
        _BandStrength ("Glint Band Strength", Range(0, 2)) = 0.7
        _BandHeight ("Glint Band Height", Range(-1, 1)) = 0.25
        _BandWidth ("Glint Band Width", Range(0.02, 1)) = 0.18

        // Used by DebrisFadeIn to fade the badge in with the rest of the car
        [HideInInspector] _Surface ("Surface", Float) = 0
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite ("ZWrite", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _ReflectionStrength;
            half _Blur;
            half _FresnelPower;
            half _FresnelBoost;
            half _Ambient;
            half _SunHighlight;
            half4 _BandColor;
            half _BandStrength;
            half _BandHeight;
            half _BandWidth;
            half _Surface;
            half _SrcBlend;
            half _DstBlend;
            half _ZWrite;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ChromeBadge"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURECUBE(_Cube);
            SAMPLER(sampler_Cube);
            float4 _Cube_HDR;

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

                half mip = _Blur * 6.0h;
                half3 env = SAMPLE_TEXTURECUBE_LOD(_Cube, sampler_Cube, r, mip).rgb;

                // Soft bright horizon band around the car (like a studio light), so the bevels catch a moving
                // glint as you pass by. Measured against the car's own up axis.
                float3 up = normalize(TransformObjectToWorldDir(float3(0, 1, 0)));
                half h = dot(r, up) - _BandHeight;
                env += _BandColor.rgb * _BandStrength * exp(-h * h / max(_BandWidth * _BandWidth, 1e-4h));

                // Chrome: the reflection is tinted by the metal, and gets stronger toward grazing angles.
                half fresnel = pow(1.0h - saturate(dot(n, v)), _FresnelPower);
                half3 f = lerp(_BaseColor.rgb, half3(1, 1, 1), fresnel * _FresnelBoost);
                half3 color = env * f * _ReflectionStrength + _BaseColor.rgb * _Ambient;

                // Sharp sun highlight on the faces and bevels that catch the sun.
                Light sun = GetMainLight();
                half spec = pow(saturate(dot(r, sun.direction)), 400.0h) * _SunHighlight;
                color += sun.color * spec * saturate(dot(n, sun.direction) * 4.0h);

                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
