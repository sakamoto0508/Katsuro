Shader "Katsuro/JustAvoidCounterCut"
{
    Properties
    {
        [Header(White Silver Core)]
        [HDR] _Tint("White silver core color", Color) = (1.5,1.65,1.8,1)
        _CoreWidth("Core width (fraction of envelope height)", Range(.01,.3)) = .13
        _MinCorePixels("Minimum core radius in pixels", Range(.5,3)) = 1.3
        _CoreGain("Core intensity", Range(0,4)) = 1.45
        _ImpactBoost("Initial impact boost", Range(0,1.5)) = .4
        _TaperStrength("Pointed ends / taper strength", Range(.3,2.5)) = .8
        _BladeCurve("Blade silhouette curvature", Range(-.25,.25)) = .06
        _SweepDuration("Forward extension duration (seconds)", Range(.005,.12)) = .028
        _LineHold("Core hold until (seconds)", Float) = .045
        _LineDuration("Core fade ends at (seconds)", Float) = .17
        [Header(Outer Glow)]
        _GlowTint("Blue silver glow color", Color) = (.3,.52,.7,1)
        _GlowWidth("Glow width (fraction of envelope height)", Range(.05,.65)) = .27
        _GlowIntensity("Glow intensity", Range(0,.6)) = .22
        _GlowDuration("Residual glow ends at (seconds)", Float) = .235
        _AirStrength("Fine directional filaments", Range(0,.3)) = .025
        _EdgeContrast("Dark shoulder on bright backgrounds", Range(0,1)) = .5
        [Header(Contact Flash)]
        _FlashStrength("Contact flare intensity", Range(0,3)) = 1.2
        _FlashHold("Contact flare hold until (seconds)", Float) = .018
        _FlashDuration("Contact flare ends at (seconds)", Float) = .07
        [Header(Depth)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Cut depth test (LEqual recommended)", Float) = 4
        [HideInInspector] _Age("Age seconds (pool clock)", Float) = 0
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
            float _Age, _CoreWidth, _MinCorePixels, _CoreGain, _ImpactBoost;
            float _TaperStrength, _BladeCurve, _SweepDuration, _LineHold, _LineDuration;
            float _GlowWidth, _GlowIntensity, _GlowDuration, _AirStrength, _EdgeContrast;
            float _FlashDuration, _FlashStrength, _FlashHold;
            CBUFFER_END
            struct A { float3 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            V Vert(A a) { V o; o.pos=TransformObjectToHClip(a.vertex); o.uv=a.uv; return o; }
            float4 Frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float age=max(0,_Age);
                float sweep=max(.001,_SweepDuration);
                float travel=saturate(age/sweep);
                travel=1-pow(1-travel,3); // fast attack, then settle into the incision
                float tail=lerp(-.28,-.88,travel);
                float tip=lerp(.06,.98,travel);
                float aaX=max(fwidth(p.x),.0001);
                float aaY=max(fwidth(p.y),.0001);
                float segment=smoothstep(tail-aaX,tail+aaX,p.x)
                    *(1-smoothstep(tip-aaX,tip+aaX,p.x));
                float along=saturate((p.x-tail)/max(.001,tip-tail));
                float lens=saturate(4*along*(1-along));
                float taper=pow(lens,max(.1,_TaperStrength));
                // A narrow receding heel and a fuller forward shoulder, with two needle tips.
                float impact=1+_ImpactBoost*(1-smoothstep(0,sweep+.025,age));
                float width=_CoreWidth*taper*lerp(.55,1.25,along)*lerp(1.12,1,travel);
                // The pixel floor dies with the tips: it must not turn the cut into a white rod.
                width=max(width,aaY*_MinCorePixels*saturate(taper*5));
                float y=p.y-_BladeCurve*p.x*p.x;
                float ridge=1-smoothstep(width*.35,width+aaY,abs(y));
                float coreEnd=max(_LineHold+.001,_LineDuration);
                float coreLife=1-smoothstep(max(0,_LineHold),coreEnd,age);
                float recovery=saturate((age-sweep)/max(.001,coreEnd-sweep));
                float heelFade=lerp(1,smoothstep(0,.65,along),recovery*.7);
                float core=ridge*segment*coreLife*heelFade;
                float edge=(1-smoothstep(width+aaY,width+aaY*3+.035,abs(y)))
                    *segment*coreLife*heelFade;
                float head=exp(-pow((p.x-tip+.1)/.13,2))*ridge*segment
                    *(1-smoothstep(sweep,sweep+.025,age));
                float glowLife=1-smoothstep(sweep,max(sweep+.001,_GlowDuration),age);
                float glow=exp(-pow(y/max(.01,_GlowWidth),2))*segment*taper
                    *_GlowIntensity*glowLife;
                float air=exp(-pow((y-.22-p.x*.07)/.025,2));
                air+=exp(-pow((y+.18-p.x*.04)/.02,2))*.5;
                air*=_AirStrength*segment*taper*glowLife;
                // Reuse the same contact flare; no second flash object or particle system.
                float contact=exp(-pow(p.x/.14,2)-pow(p.y/.31,2));
                contact+=exp(-pow(p.x/.035,2)-pow(p.y/.63,2))*.23;
                contact*=max(0,_FlashStrength)*(1-smoothstep(max(0,_FlashHold),
                    max(_FlashHold+.001,_FlashDuration),age));
                float alpha=saturate(core*.95+edge*_EdgeContrast+contact*.75+glow+air);
                float3 emission=_Tint.rgb*(core*_CoreGain*impact+head*.3+contact*.72)
                    +_GlowTint.rgb*(glow+air);
                // HDRP's emissive path uses pre-exposure. Bloom/tonemapping remain camera-owned.
                return float4(emission*GetCurrentExposureMultiplier(),alpha);
            }
            ENDHLSL
        }
    }
}
