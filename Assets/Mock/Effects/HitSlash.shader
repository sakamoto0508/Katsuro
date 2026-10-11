Shader "Katsuro/HitSlash"
{
    Properties { _Tint("白銀の斬撃線", Color) = (.9,.95,1,1) }
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
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            CBUFFER_END
            struct A { float3 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            V Vert(A a) { V o; o.positionCS=TransformObjectToHClip(a.positionOS); o.uv=a.uv; o.color=a.color*_Tint; return o; }
            float4 Frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float taper=pow(saturate(1-p.y*p.y),.65);
                float core=exp(-pow(p.x/max(.06,.38*taper),2));
                float4 c=i.color;
                c.a*=core*taper;
                // 画面上の白銀色を保つ。露出済みの色へ照明用の露出倍率を重ねない。
                return c;
            }
            ENDHLSL
        }
    }
}
