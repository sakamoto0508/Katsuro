Shader "Katsuro/BladeTrail"
{
    Properties
    {
        _Tint("Trail Color", Color)=(.92,.96,1,1)
        _CoreWidth("Core Width", Range(.01,.08))=.035
        _CoreBrightness("Core Brightness", Range(1,4))=2.8
        _BodyAlpha("Body Alpha", Range(0,.5))=.24
        _BodyWidth("Body Width", Range(.1,.35))=.22
        _AfterGlowAlpha("After Glow Alpha", Range(0,.15))=.055
        _EdgeFade("Edge Fade", Range(.04,.25))=.12
        _TailFade("Tail Fade Start", Range(.2,.85))=.5
        _TipBrightness("Tip Brightness", Range(1,1.5))=1.2
        _Brightness("Overall Brightness", Range(.5,2))=1.05
        _FadePower("Fade Power", Range(.5,3))=1.2
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="ForwardOnly" }
            Blend One OneMinusSrcAlpha
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
            float _CoreWidth, _CoreBrightness, _BodyAlpha, _BodyWidth, _AfterGlowAlpha;
            float _EdgeFade, _TailFade, _TipBrightness, _Brightness, _FadePower;
            CBUFFER_END
            struct A { float3 positionOS:POSITION; float2 uv:TEXCOORD0; float2 dynamics:TEXCOORD1; float4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 dynamics:TEXCOORD1; float4 color:COLOR; };
            V Vert(A a) { V o; o.positionCS=TransformObjectToHClip(a.positionOS); o.uv=a.uv; o.dynamics=a.dynamics; o.color=a.color*_Tint; return o; }
            float4 Frag(V i):SV_Target
            {
                // UV.x is per-sample age; UV.y runs from blade root to tip.
                float age = saturate(i.uv.x), blade = saturate(i.uv.y);
                float edge = smoothstep(0, _EdgeFade, blade) * (1-smoothstep(1-_EdgeFade*.5,1,blade));
                float tail = pow(saturate(1-age),_FadePower) * (1-smoothstep(_TailFade,1,age));
                float tip = lerp(.7,_TipBrightness,smoothstep(.12,.9,blade));
                float width = max(.02,_BodyWidth*i.dynamics.y*lerp(.75,1,saturate(i.dynamics.x)));
                float bodyShape = exp(-pow((blade-.62)/width,2));
                // Leave a translucent hollow rather than filling a uniform sheet.
                bodyShape *= 1-.35*exp(-pow((blade-.51)/.09,2));
                float bodyA = i.color.a * _BodyAlpha * bodyShape * edge * tail;
                float coreShape = exp(-pow((blade-.85)/max(.005,_CoreWidth),2));
                float coreA = i.color.a * .82 * coreShape * edge * (1-smoothstep(.18,.55,age));
                float glowShape = exp(-pow((blade-.64)/(width*1.35),2));
                float glowA = i.color.a * _AfterGlowAlpha * glowShape * edge * tail * smoothstep(.08,.3,age);
                float3 steel = i.color.rgb * _Brightness;
                float3 coreColor = lerp(steel,float3(1,1.015,1.035)*dot(steel,float3(.2126,.7152,.0722)),.8);
                float3 rgb = coreColor*_CoreBrightness*coreA + steel*tip*bodyA + steel*float3(.8,.85,.92)*glowA;
                // Premultiplied transparency retains the fine HDR core without an opaque plate.
                return float4(rgb,saturate(coreA+bodyA+glowA));
            }
            ENDHLSL
        }
    }
}
