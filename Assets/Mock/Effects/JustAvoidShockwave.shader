Shader "Katsuro/JustAvoidShockwave"
{
    Properties
    {
        _Tint("色", Color) = (0.8,0.94,1,1)
        _Width("輪の太さ", Range(0.005,0.15)) = 0.025
        _Softness("輪のぼかし", Range(0.001,0.1)) = 0.015
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="ForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            float4 _Tint;
            float _Width, _Softness;
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * _Tint;
                return output;
            }
            float4 Frag(Varyings input) : SV_Target
            {
                // 中央を透明に保ち、輪の縁だけを柔らかく描画する。
                float radius = length(input.uv * 2 - 1);
                float edge = abs(radius - .86);
                float4 color = input.color;
                color.a *= 1 - smoothstep(_Width, _Width + _Softness, edge);
                return color;
            }
            ENDHLSL
        }
    }
}
