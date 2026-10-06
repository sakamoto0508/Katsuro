Shader "Katsuro/ContactFlash"
{
    Properties { _Tint("色",Color)=(.86,.95,1,.6) }
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
            struct A { float3 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            V Vert(A a) { V o; o.positionCS=TransformObjectToHClip(a.positionOS); o.uv=a.uv; return o; }
            float4 Frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float slash=exp(-pow((p.y-p.x)*22,2))*exp(-pow((p.x+p.y)*1.8,2));
                float cross=exp(-pow((p.x+p.y)*32,2))*exp(-pow((p.x-p.y)*3.5,2))*.5;
                float4 color=_Tint; color.a*=saturate(slash+cross); return color;
            }
            ENDHLSL
        }
    }
}
