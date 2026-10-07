Shader "Katsuro/BladeTrail"
{
    Properties
    {
        _Tint("Trail Color", Color)=(.92,.96,1,1)
        _CoreWidth("Core Width", Range(.01,.08))=.022
        _CoreBrightness("Core Brightness", Range(1,4))=2.8
        _BodyAlpha("Body Alpha", Range(0,.5))=.14
        _BodyWidth("Body Width", Range(.1,.35))=.12
        _AfterGlowAlpha("After Glow Alpha", Range(0,.15))=.03
        _EdgeFade("Edge Fade", Range(.04,.25))=.12
        _TailFade("Tail Fade Start", Range(.2,.85))=.4
        _TipBrightness("Tip Brightness", Range(1,1.5))=1.35
        _Brightness("Overall Brightness", Range(.5,2))=1.05
        _FadePower("Fade Power", Range(.5,3))=1.8
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
                // UV.y spans only the outer blade segment selected by SwordTrail.
                float age = saturate(i.uv.x), blade = saturate(i.uv.y);
                float edge = smoothstep(0, _EdgeFade, blade) * (1-smoothstep(1-_EdgeFade*.5,1,blade));
                edge *= smoothstep(.15,.55,blade);
                float tail = pow(saturate(1-age),_FadePower) * (1-smoothstep(_TailFade,1,age));
                float tip = lerp(.2,_TipBrightness,smoothstep(.3,.9,blade));
                float width = max(.02,_BodyWidth*i.dynamics.y*lerp(.75,1,saturate(i.dynamics.x)));
                float bodyShape = exp(-pow((blade-.75)/width,2));
                float bodyA = i.color.a * _BodyAlpha * bodyShape * edge * tail;
                float coreShape = exp(-pow((blade-.9)/max(.005,_CoreWidth),2));
                // Only the freshest quarter retains the bright core; older samples
                // use the faint body/glow and the compounded vertex + shader fade.
                float coreA = i.color.a * .82 * coreShape * edge * (1-smoothstep(.1,.25,age));
                float glowShape = exp(-pow((blade-.75)/(width*1.1),2));
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
