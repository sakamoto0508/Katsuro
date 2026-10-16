Shader "Katsuro/JustAvoidCounterCut"
{
    Properties
    {
        [Header(White Silver Core)]
        [HDR] _Tint("White silver core color", Color) = (1.5,1.65,1.8,1)
        _StrikeMultiplier("Strike multiplier (this strike material)", Range(0,2)) = 1
        _CoreWidth("Core width (fraction of envelope height)", Range(.01,.45)) = .13
        _CoreLength("Core length (fraction of cut envelope)", Range(.3,1)) = .9
        _MinCorePixels("Minimum core radius in pixels", Range(.5,3)) = 1.3
        _CoreGain("Core intensity", Range(0,6)) = 1.45
        _ImpactBoost("Initial impact boost", Range(0,1.5)) = .4
        _TaperStrength("Pointed ends / taper strength", Range(.3,2.5)) = .8
        _BladeCurve("Blade silhouette curvature", Range(-.25,.25)) = .06
        _SweepDuration("Forward extension duration (seconds)", Range(.005,.12)) = .028
        _ForwardExtension("Forward extension amount", Range(0,.65)) = .25
        _LineHold("Core hold until (seconds)", Float) = .045
        _FadeDuration("Fade duration after core hold (seconds)", Float) = .095
        [Header(Silver Sheet)]
        [HDR] _SheetTint("Silver sheet color", Color) = (.92,.97,1.05,1)
        _SheetWidth("Silver sheet width", Range(.05,.7)) = .25
        _SheetLength("Silver sheet length (fraction of cut envelope)", Range(.3,1)) = .96
        _SheetIntensity("Silver sheet intensity", Range(0,2.5)) = .5
        _SheetSoftness("Silver sheet edge softness", Range(.02,.6)) = .2
        [Header(Outer Glow)]
        _GlowTint("Blue silver glow color", Color) = (.3,.52,.7,1)
        _GlowWidth("Glow width (fraction of envelope height)", Range(.05,.65)) = .27
        _GlowIntensity("Glow intensity", Range(0,.6)) = .22
        _ResidualGlowDuration("Residual glow duration after core fade (seconds)", Float) = .06
        _AirStrength("Fine directional filaments", Range(0,.3)) = .025
        _EdgeContrast("Dark shoulder on bright backgrounds", Range(0,1)) = .5
        [Header(Contact Flash)]
        _FlashStrength("Contact flare intensity", Range(0,5)) = 1.2
        _FlashSize("Contact flare size (X length / Y height fraction)", Vector) = (.12,.27,0,0)
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
            float4 _Tint, _GlowTint, _SheetTint, _FlashSize;
            float _Age, _CoreWidth, _CoreLength, _MinCorePixels, _CoreGain, _ImpactBoost;
            float _TaperStrength, _BladeCurve, _SweepDuration, _ForwardExtension, _LineHold, _FadeDuration;
            float _SheetWidth, _SheetLength, _SheetIntensity, _SheetSoftness, _StrikeMultiplier;
            float _GlowWidth, _GlowIntensity, _ResidualGlowDuration, _AirStrength, _EdgeContrast;
            float _FlashDuration, _FlashStrength, _FlashHold;
            CBUFFER_END
            struct A { float3 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            V Vert(A a) { V o; o.pos=TransformObjectToHClip(a.vertex); o.uv=a.uv; return o; }
            void BladeLens(float x, float tail, float tip, float taperStrength,
                out float segment, out float along, out float taper)
            {
                float aa=max(fwidth(x),.0001);
                segment=smoothstep(tail-aa,tail+aa,x)*(1-smoothstep(tip-aa,tip+aa,x));
                along=saturate((x-tail)/max(.001,tip-tail));
                taper=pow(saturate(4*along*(1-along)),max(.1,taperStrength));
            }
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
                float coreEnd=max(0,_LineHold)+max(.001,_FadeDuration);
                float withdrawal=smoothstep(max(0,_LineHold),coreEnd,age);
                // Peel the short heel forward as the incision expires, instead of fading a static bar.
                float tail=-.88+_ForwardExtension*.55*(1-travel)+withdrawal*.55;
                float tip=.98-_ForwardExtension*(1-travel);
                float aaY=max(fwidth(p.y),.0001);
                float segment,along,taper;
                BladeLens(x,tail,tip,_TaperStrength,segment,along,taper);
                // A narrow receding heel and a fuller forward shoulder, with two needle tips.
                float impact=1+_ImpactBoost*(1-smoothstep(0,sweep+.025,age));
                float width=_CoreWidth*taper*lerp(.55,1.25,along)*lerp(1.12,1,travel);
                // The pixel floor dies with the tips: it must not turn the cut into a white rod.
                width=max(width,aaY*_MinCorePixels*saturate(taper*5));
                float y=p.y-_BladeCurve*p.x*p.x;
                float ridge=1-smoothstep(width*.35,width+aaY,abs(y));
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
                // Independent crescent plate: broad curved upper face, a short sharp lower edge.
                // Its length and silhouette no longer collapse into the core's thin symmetric lens.
                float sheetX=p.x/max(.01,_SheetLength);
                float sheetSegment,sheetAlong,sheetTaper;
                BladeLens(sheetX,tail,tip,_TaperStrength*.8,sheetSegment,sheetAlong,sheetTaper);
                float sheetUpper=max(.001,_SheetWidth*sheetTaper*lerp(.4,1.25,sheetAlong));
                float sheetLower=max(.001,_SheetWidth*sheetTaper*lerp(.18,.42,sheetAlong));
                float sheetBound=y>=0?sheetUpper:sheetLower;
                float soft=max(aaY,sheetBound*_SheetSoftness);
                float sheetMask=1-smoothstep(max(0,sheetBound-soft),sheetBound+aaY,abs(y));
                float sheetPeak=lerp(.42,1.4,1-smoothstep(0,sweep+.03,age));
                float sheetLife=sheetPeak*(1-smoothstep(max(0,coreEnd-.03),glowEnd,age));
                float sheetHeel=lerp(1,smoothstep(0,.65,sheetAlong),withdrawal*.8);
                float sheet=sheetMask*sheetSegment*sheetTaper*sheetHeel*sheetLife*_SheetIntensity;
                float facet=1-smoothstep(aaY,aaY*2+sheetUpper*.14,abs(y-sheetUpper*.7));
                float silverGradient=saturate((y+sheetLower)/max(.001,sheetUpper+sheetLower));
                float3 silver=_SheetTint.rgb*(lerp(.35,.95,silverGradient)+facet*.6);
                // Reuse the same contact flare; no second flash object or particle system.
                float2 flashSize=max(abs(_FlashSize.xy),float2(.005,.005));
                float contact=exp(-pow(p.x/flashSize.x,2)-pow(p.y/flashSize.y,2));
                contact+=exp(-pow(p.x/(flashSize.x*.25),2)-pow(p.y/(flashSize.y*1.65),2))*.18;
                contact*=max(0,_FlashStrength)*(1-smoothstep(max(0,_FlashHold),
                    max(_FlashHold+.001,_FlashDuration),age));
                float alpha=saturate(core*.95+edge*_EdgeContrast+contact*.75+sheet*.32+glow+air);
                // Keep the localized contact brighter than the sustained full-length blade.
                float3 emission=_Tint.rgb*(core*_CoreGain*impact+head*.3+contact*(1+_CoreGain*.9)*.72)
                    +silver*sheet+_GlowTint.rgb*(glow+air);
                // HDRP's emissive path uses pre-exposure. Bloom/tonemapping remain camera-owned.
                return float4(emission*_StrikeMultiplier*GetCurrentExposureMultiplier(),alpha);
            }
            ENDHLSL
        }
    }
}
