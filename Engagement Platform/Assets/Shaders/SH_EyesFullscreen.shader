// Waking up / falling asleep, as a full-screen pass on the finished image (EyesEffectFeature).
// Because it runs after all transparent objects, everything is blurred the same way: the scene, the cockpit
// glass and the text on the screens. On top of the blur come the eyelids (upper and lower lid, soft edges).
// URP, works in VR (single-pass instanced).
Shader "Hidden/EngagementPlatform/EyesFullscreen"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Eyes"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Open;          // 0 = closed (black), 1 = fully open
            float _Blur;          // 0 = sharp
            float _BlurRadius;    // largest blur radius, as a part of the screen
            float _OpenHeight;    // opening height when open (radians)
            float _OpenWidth;     // eye width (radians)
            float _LidSoftness;
            float2 _TanHalfFov;   // tan(half field of view) horizontally / vertically

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                // Blur: samples on a spiral, with a different rotation per pixel (a soft haze, no rings).
                half3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                if (_Blur > 0.001)
                {
                    float r = _Blur * _BlurRadius;
                    float aspect = _ScreenParams.y / _ScreenParams.x;
                    float rnd = frac(sin(dot(uv * _ScreenParams.xy, float2(12.9898, 78.233))) * 43758.5453) * 6.28318;
                    const int N = 24;
                    half3 sum = 0;
                    [unroll] for (int k = 0; k < N; k++)
                    {
                        float a = k * 2.39996 + rnd;
                        float d = sqrt((k + 0.5) / N) * r;
                        sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(cos(a) * aspect, sin(a)) * d).rgb;
                    }
                    half3 blurred = sum / N;
                    col = lerp(col, blurred, saturate(_Blur * 3.0));
                }

                // Where this pixel looks, as angles (left/right, up/down) from the middle of the view.
                float2 ang = atan((uv * 2.0 - 1.0) * _TanHalfFov);

                // Lids: a soft arc, highest in the middle; the lower lid moves less. Near fully open they slide
                // all the way out of view.
                float arc = saturate(1.0 - (ang.x * ang.x) / (_OpenWidth * _OpenWidth));
                float h = _Open * _OpenHeight;
                float clear = smoothstep(0.85, 1.0, _Open) * 3.0;
                float top = h * arc + 0.04 * _Open + clear;
                float bottom = -h * 0.6 * arc - 0.02 * _Open - clear;
                float soft = _LidSoftness * (1.0 + 2.0 * _Blur);
                float lid = saturate(smoothstep(top - soft, top + soft, ang.y) + (1.0 - smoothstep(bottom - soft, bottom + soft, ang.y)));
                lid = max(lid, 1.0 - saturate(_Open * 25.0));

                // Darker towards the lids, like looking through half-open eyes.
                float edge = min(top - ang.y, ang.y - bottom);
                float shade = lerp(0.35, 1.0, saturate(edge / max(0.001, 0.25 * h + 0.05)));

                col *= shade * (1.0 - lid);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
