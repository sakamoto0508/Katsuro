Shader "Katsuro/JustAvoidCounterCut"
{
    Properties
    {
        [Header(White Silver Core)]
        [HDR] _Tint("White silver core color", Color) = (1.5,1.65,1.8,1)
        _CoreWidth("Core width (fraction of envelope height)", Range(.01,.3)) = .13
        _CoreLength("Core length (fraction of cut envelope)", Range(.3,1)) = .9
        _MinCorePixels("Minimum core radius in pixels", Range(.5,3)) = 1.3
        _CoreGain("Core intensity", Range(0,4)) = 1.45
        _ImpactBoost("Initial impact boost", Range(0,1.5)) = .4
        _TaperStrength("Pointed ends / taper strength", Range(.3,2.5)) = .8
        _BladeCurve("Blade silhouette curvature", Range(-.25,.25)) = .06
        _SweepDuration("Forward extension duration (seconds)", Range(.005,.12)) = .028
        _LineHold("Core hold until (seconds)", Float) = .045
        _FadeDuration("Fade duration after core hold (seconds)", Float) = .095
        [Header(Silver Sheet)]
        [HDR] _SheetTint("Silver sheet color", Color) = (.92,.97,1.05,1)
        _SheetWidth("Silver sheet width", Range(.05,.7)) = .25
        _SheetIntensity("Silver sheet intensity", Range(0,2.5)) = .5
        [Header(Outer Glow)]
        _GlowTint("Blue silver glow color", Color) = (.3,.52,.7,1)
        _GlowWidth("Glow width (fraction of envelope height)", Range(.05,.65)) = .27
        _GlowIntensity("Glow intensity", Range(0,.6)) = .22
        _ResidualGlowDuration("Residual glow duration after core fade (seconds)", Float) = .06
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
            float4 _Tint, _GlowTint, _SheetTint;
            float _Age, _CoreWidth, _CoreLength, _MinCorePixels, _CoreGain, _ImpactBoost;
            float _TaperStrength, _BladeCurve, _SweepDuration, _LineHold, _FadeDuration;
            float _SheetWidth, _SheetIntensity;
            float _GlowWidth, _GlowIntensity, _ResidualGlowDuration, _AirStrength, _EdgeContrast;
            float _FlashDuration, _FlashStrength, _FlashHold;
            CBUFFER_END
            struct A { float3 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            V Vert(A a) { V o; o.pos=TransformObjectToHClip(a.vertex); o.uv=a.uv; return o; }
            float4 Frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float x=p.x/max(.01,_CoreLength);
                float age=max(0,_Age);
                float sweep=max(.001,_SweepDuration);
                float travel=saturate(age/sweep);
                travel=1-pow(1-travel,3); // fast attack, then settle into the incision
                // Most of the silver silhouette is present on the very first contact frame.
                // Extension supplies the forward snap, rather than delaying the impact peak.
                float tail=lerp(-.6,-.88,travel);
                float tip=lerp(.68,.98,travel);
                float aaX=max(fwidth(x),.0001);
                float aaY=max(fwidth(p.y),.0001);
                float segment=smoothstep(tail-aaX,tail+aaX,x)
                    *(1-smoothstep(tip-aaX,tip+aaX,x));
                float along=saturate((x-tail)/max(.001,tip-tail));
                float lens=saturate(4*along*(1-along));
                float taper=pow(lens,max(.1,_TaperStrength));
                // A narrow receding heel and a fuller forward shoulder, with two needle tips.
                float impact=1+_ImpactBoost*(1-smoothstep(0,sweep+.025,age));
                float width=_CoreWidth*taper*lerp(.55,1.25,along)*lerp(1.12,1,travel);
                // The pixel floor dies with the tips: it must not turn the cut into a white rod.
                width=max(width,aaY*_MinCorePixels*saturate(taper*5));
                float y=p.y-_BladeCurve*p.x*p.x;
                float ridge=1-smoothstep(width*.35,width+aaY,abs(y));
                float coreEnd=max(0,_LineHold)+max(.001,_FadeDuration);
                float coreLife=1-smoothstep(max(0,_LineHold),coreEnd,age);
                float recovery=saturate((age-sweep)/max(.001,coreEnd-sweep));
                float heelFade=lerp(1,smoothstep(0,.65,along),recovery*.7);
                float core=ridge*segment*coreLife*heelFade;
                float edge=(1-smoothstep(width+aaY,width+aaY*3+.035,abs(y)))
                    *segment*coreLife*heelFade;
                float head=exp(-pow((x-tip+.1)/.13,2))*ridge*segment
                    *(1-smoothstep(sweep,sweep+.025,age));
                float glowEnd=coreEnd+max(.001,_ResidualGlowDuration);
                float glowLife=1-smoothstep(sweep,max(sweep+.001,glowEnd),age);
                float glow=exp(-pow(y/max(.01,_GlowWidth),2))*segment*taper
                    *_GlowIntensity*glowLife;
                float air=exp(-pow((y-.22-p.x*.07)/.025,2));
                air+=exp(-pow((y+.18-p.x*.04)/.02,2))*.5;
                air*=_AirStrength*segment*taper*glowLife;
                // A cut-shaped silver surface, not a wider Gaussian or a second bloom halo.
                // The forward shoulder is fuller; the heel sheds light first during recovery.
                float sheetWidth=max(.001,_SheetWidth*taper*lerp(.5,1.15,along));
                float sheetMask=1-smoothstep(sheetWidth*.68,sheetWidth+aaY,abs(y));
                float sheetPeak=lerp(.32,1.25,1-smoothstep(0,sweep+.035,age));
                float sheetLife=sheetPeak*coreLife;
                sheetLife+=.13*(1-coreLife)*glowLife;
                float sheet=sheetMask*segment*taper*heelFade*sheetLife*_SheetIntensity;
                // A narrow hard facet inside the surface gives it a silver blade-like edge.
                float facet=1-smoothstep(aaY,aaY*2+.035,abs(y-sheetWidth*.35));
                float3 silver=_SheetTint.rgb*lerp(.6,1.1,facet);
                // Reuse the same contact flare; no second flash object or particle system.
                float contact=exp(-pow(p.x/.14,2)-pow(p.y/.31,2));
                contact+=exp(-pow(p.x/.035,2)-pow(p.y/.63,2))*.23;
                contact*=max(0,_FlashStrength)*(1-smoothstep(max(0,_FlashHold),
                    max(_FlashHold+.001,_FlashDuration),age));
                float alpha=saturate(core*.95+edge*_EdgeContrast+contact*.75+sheet*.48+glow+air);
                float3 emission=_Tint.rgb*(core*_CoreGain*impact+head*.3+contact*.72)
                    +silver*sheet+_GlowTint.rgb*(glow+air);
                // HDRP's emissive path uses pre-exposure. Bloom/tonemapping remain camera-owned.
                return float4(emission*GetCurrentExposureMultiplier(),alpha);
            }
            ENDHLSL
        }
    }
}
