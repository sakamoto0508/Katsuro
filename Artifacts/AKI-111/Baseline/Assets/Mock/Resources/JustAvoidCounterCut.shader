Shader "Katsuro/JustAvoidCounterCut"
{
    Properties
    {
        [HDR] _Tint("Silver core", Color) = (1.55,1.7,1.85,1)
        _GlowTint("Blue steel afterimage", Color) = (.32,.54,.72,1)
        _Age("Age seconds", Float) = 0
        _CoreWidth("Core width", Float) = .045
        _MinCorePixels("Minimum core radius in pixels", Range(.5,3)) = 1.3
        _CoreGain("Silver core presence", Range(.5,1.5)) = 1.1
        _SweepDuration("Blade crossing time", Float) = .045
        _LineHold("Core hold", Float) = .065
        _LineDuration("Cut duration", Float) = .18
        _FlashDuration("Contact duration", Float) = .085
        _FlashHold("Contact hold", Float) = .033
        _FlashStrength("Contact strength", Float) = 1
        _GlowDuration("Afterimage duration", Float) = .25
        _AirStrength("Air filaments", Float) = .1
        _EdgeContrast("Dark edge on bright backgrounds", Float) = .35
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Contact depth test", Float) = 4
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="ForwardOnly" }
            Blend One OneMinusSrcAlpha
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
            float4 _Tint, _GlowTint;
            float _Age, _CoreWidth, _SweepDuration, _LineHold, _LineDuration;
            float _FlashDuration, _FlashStrength, _GlowDuration, _AirStrength, _EdgeContrast;
            float _MinCorePixels, _CoreGain, _FlashHold;
            CBUFFER_END
            struct A { float3 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            V Vert(A a) { V o; o.pos=TransformObjectToHClip(a.vertex); o.uv=a.uv; return o; }
            float4 Frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float travel=saturate(_Age/max(.001,_SweepDuration));
                // The cut grows out of contact toward the actual sweep, with pointed ends.
                float tip=lerp(-.32,.98,travel);
                float segment=smoothstep(-.72,-.42,p.x)*(1-smoothstep(tip-.20,tip,p.x));
                // Broad longitudinal shoulder, pointed ends: readable as a blade incision,
                // not a long uniformly glowing bar. Anti-aliasing keeps the core at distance.
                float taper=pow(saturate(1-pow(abs(p.x),2.8)),.6);
                float width=max(_CoreWidth*lerp(.15,1,taper),fwidth(p.y)*_MinCorePixels);
                float ridge=exp(-pow(p.y/width,2));
                float life=1-smoothstep(_LineHold,_LineDuration,_Age);
                float core=ridge*segment*taper*life;
                float edge=exp(-pow(p.y/(width*2.6),2))*segment*taper*life;
                float head=exp(-pow((p.x-tip+.08)/.13,2))*ridge*(1-smoothstep(_SweepDuration,_SweepDuration+.035,_Age));
                // A narrow contact blade, not a circular white burst.
                float contact=exp(-pow(p.x*6.5,2)-pow(p.y*5,2));
                contact+=(exp(-pow(p.x*18,2)-pow(p.y*2.5,2))*.25);
                contact*=(1-smoothstep(_FlashHold,max(_FlashHold+.001,_FlashDuration),_Age))*_FlashStrength;
                float glow=exp(-pow(p.y/.16,2))*segment*taper;
                glow*=.16*(1-smoothstep(.045,_GlowDuration,_Age));
                float air=exp(-pow((p.y-.20-p.x*.065)/.024,2));
                air+=exp(-pow((p.y+.16-p.x*.035)/.018,2))*.55;
                air*=_AirStrength*segment*taper*(1-smoothstep(.06,_GlowDuration,_Age));
                float alpha=saturate(core*.9+contact*.72+glow+air+edge*_EdgeContrast);
                // Keep luminance bounded. A dark shoulder gives the silver cut contrast in daylight.
                float3 rgb=_Tint.rgb*(core*.85*_CoreGain+head*.25+contact*.65)+_GlowTint.rgb*(glow+air);
                return float4(rgb,alpha);
            }
            ENDHLSL
        }
    }
}
