Shader "Katsuro/BladeTrail"
{
    Properties { _Tint("色", Color)=(1,1,1,1) }
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
            struct A { float3 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            V Vert(A a) { V o; o.positionCS=TransformObjectToHClip(a.positionOS); o.uv=a.uv; o.color=a.color*_Tint; return o; }
            float4 Frag(V i):SV_Target
            {
                // 根元と切先の縁をぼかし、刀身中央に控えめな芯を残す。
                float edge = smoothstep(0,.12,i.uv.y) * (1-smoothstep(.8,1,i.uv.y));
                float core = exp(-pow((i.uv.y-.55)*4,2));
                i.color.rgb *= .65 + .35*core;
                i.color.a *= edge;
                return i.color;
            }
            ENDHLSL
        }
    }
}
