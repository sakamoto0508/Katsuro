Shader "Katsuro/JustAvoidScreenDistortion"
{
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Just Avoid Distortion"
            ZWrite Off ZTest Always Cull Off Blend Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment FullScreenPass
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
            TEXTURE2D_X(_DistortionSource);
            float4 _SourceInfo; // RT scale xy, reciprocal viewport zw
            float4 _CenterRadius; // viewport center xy, radius z, aspect w
            float4 _Ring; // width, strength, radius noise, progress
            float4 _NoiseTint; // angular frequency, tint strength
            float4 _EdgeTint;

            float4 FullScreenPass(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.positionCS.xy * _SourceInfo.zw;
                float2 center = _CenterRadius.xy;
                // WorldToViewportPointは下端原点。D3DのSV_Positionは上端原点。
                #if UNITY_UV_STARTS_AT_TOP
                    center.y = 1 - center.y;
                #endif
                float2 delta = (uv - center) * float2(_CenterRadius.w, 1);
                float distance = length(delta);
                float angle = atan2(delta.y, delta.x);
                float noise = sin(angle * _NoiseTint.x + _Ring.w * 2) *
                    sin(angle * 7 - _Ring.w) * _Ring.z;
                float mask = 1 - smoothstep(_Ring.x * .35, _Ring.x, abs(distance - _CenterRadius.z + noise));
                mask *= smoothstep(0, _Ring.x, distance);
                float2 direction = delta / max(distance, .00001);
                direction.x /= _CenterRadius.w;
                float2 sampleUV = uv + direction * mask * _Ring.y;
                sampleUV = clamp(sampleUV, _SourceInfo.zw * .5, 1 - _SourceInfo.zw * .5);
                float4 color = SAMPLE_TEXTURE2D_X_LOD(_DistortionSource, s_linear_clamp_sampler, sampleUV * _SourceInfo.xy, 0);
                color.rgb = lerp(color.rgb, _EdgeTint.rgb, mask * _NoiseTint.y);
                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
