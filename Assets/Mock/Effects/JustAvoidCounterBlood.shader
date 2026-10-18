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
        _Opacity("Blood opacity", Range(0,1)) = 1
        _ContourVariation("Wet contour variation", Range(0,1)) = .65
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
            float _RimWidth, _VeinStrength, _BloodGain, _Opacity, _ContourVariation;
            CBUFFER_END
            struct A { float3 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            V Vert(A a) { V o; o.pos=TransformObjectToHClip(a.vertex); o.uv=a.uv; o.color=a.color*_Tint; return o; }
            float4 Frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float t=saturate(i.uv.y);
                // Both ends close to zero; the broad wet body has bounded necks, not a uniform bar.
                float taper=pow(max(0,sin(t*PI)),.65)*lerp(.68,1,smoothstep(0,.48,t));
                float variation=saturate(_ContourVariation);
                float width=taper*(.82+variation*(.09*sin(t*19)+.055*sin(t*41+.7)));
                float wobble=variation*.065*sin(t*17)*taper;
                float edge=abs(p.x+wobble)/max(.001,width);
                float aa=clamp(fwidth(edge),.015,.18);
                float ends=smoothstep(0,.055,t)*(1-smoothstep(.94,1,t));
                float coverage=(1-smoothstep(1-aa,1+aa,edge))*ends*(1-smoothstep(.94,1,abs(p.x)));
                float rim=smoothstep(1-_RimWidth-aa,1-_RimWidth*.25,edge);
                float vein=exp(-pow((p.x+wobble+.07)/max(.045,width*.23),2))*smoothstep(.08,.6,t);
                float wetShade=lerp(.72,1,smoothstep(.05,.48,t))*(1-variation*.13*pow(sin(t*23),2));
                float3 body=lerp(i.color.rgb*wetShade,_VeinColor.rgb,vein*_VeinStrength);
                float3 rgb=lerp(body,_RimColor.rgb,rim*.92);
                // Straight-alpha RGB: multiply coverage only into alpha, never into RGB a second time.
                return float4(saturate(rgb*_BloodGain),saturate(i.color.a*_Opacity)*coverage);
            }
            ENDHLSL
        }
    }
}
