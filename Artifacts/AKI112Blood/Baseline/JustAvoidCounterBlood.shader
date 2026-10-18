Shader "Katsuro/JustAvoidCounterBlood"
{
    Properties
    {
        _Tint("Blood tint", Color) = (1,1,1,1)
        _RimColor("Dark separating rim", Color) = (.065,.003,.012,1)
        _RimWidth("Rim width", Range(.05,.4)) = .2
        _VeinColor("Crimson leading vein", Color) = (.95,.065,.095,1)
        _VeinStrength("Vein reflection (non additive)", Range(0,.6)) = .3
        _BloodGain("Crimson luminance", Range(.1,1)) = .4
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Contact depth test", Float) = 4
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="ForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint, _RimColor, _VeinColor;
            float _RimWidth, _VeinStrength, _BloodGain;
            CBUFFER_END
            struct A { float3 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            V Vert(A a) { V o; o.pos=TransformObjectToHClip(a.vertex); o.uv=a.uv; o.color=a.color*_Tint; return o; }
            float4 Frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                // A pointed tail and asymmetric wet head, shared by threads and droplets.
                float width=lerp(.23,.86,smoothstep(-1,.65,p.y));
                float wobble=.045*sin(p.y*15)+.025*sin(p.y*28);
                float edge=pow((p.x+wobble)/max(.05,width),2)+p.y*p.y;
                float aa=max(.025,fwidth(edge));
                float coverage=1-smoothstep(1-aa,1+aa,edge);
                float rim=smoothstep(1-_RimWidth-aa,1-_RimWidth*.25,edge);
                float vein=exp(-pow((p.x+.1)/max(.08,width*.25),2))*smoothstep(-.6,.65,p.y);
                float3 body=lerp(i.color.rgb,_VeinColor.rgb,vein*_VeinStrength);
                float3 rgb=lerp(body,_RimColor.rgb,rim*.92);
                return float4(saturate(rgb*_BloodGain),i.color.a*coverage);
            }
            ENDHLSL
        }
    }
}
