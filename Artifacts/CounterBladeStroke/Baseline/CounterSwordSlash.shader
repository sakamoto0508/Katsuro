Shader "Katsuro/CounterSwordSlash"
{
    Properties
    {
        [HDR] _CoreTint("White silver core", Color) = (1.5,1.65,1.9,1)
        [HDR] _SheetTint("Cold silver surface", Color) = (.55,.72,1,1)
        _CoreWidth("Core width across blade", Range(.02,.3)) = .12
        _CorePosition("Core position along blade", Range(.4,.95)) = .72
        _SheetIntensity("Silver surface intensity", Range(0,3)) = .8
        _SheetSoftness("Surface edge softness", Range(.02,.3)) = .12
        _EdgeIntensity("Bright blade edge intensity", Range(0,5)) = 2.2
        _EdgeWidth("Bright blade edge width", Range(.01,.2)) = .06
        _AfterglowIntensity("Directional afterglow intensity", Range(0,2)) = .45
        _ImpactGain("Short impact brightness boost", Range(0,3)) = .7
        _ContactRadius("Local contact boost radius (m)", Range(.05,1)) = .3
        _ExposureWeight("Scene exposure influence (0 = compensated VFX)", Range(0,1)) = 0
        _Intensity("Overall slash intensity", Range(0,4)) = 1
        [HideInInspector] _ContactPoint("Contact point", Vector) = (0,0,0,1)
        [HideInInspector] _Impact("Impact envelope", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Transparent" "Queue"="Transparent+10" }
        Pass
        {
            Tags { "LightMode"="ForwardOnly" }
            // A luminous ribbon does not absorb the background. RGB=0 must leave it untouched.
            Blend One One
            ColorMask RGB
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _CoreTint, _SheetTint, _ContactPoint;
            float _CoreWidth, _CorePosition, _SheetIntensity, _SheetSoftness;
            float _EdgeIntensity, _EdgeWidth, _AfterglowIntensity, _ImpactGain;
            float _ContactRadius, _ExposureWeight, _Intensity, _Impact;
            CBUFFER_END
            struct A { float3 positionOS:POSITION; float2 uv:TEXCOORD0; float2 dynamics:TEXCOORD1; float4 highlight:TEXCOORD2; float4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 dynamics:TEXCOORD1; float4 highlight:TEXCOORD2; float4 color:COLOR; float3 world:TEXCOORD3; };
            V Vert(A a)
            {
                V o; o.positionCS=TransformObjectToHClip(a.positionOS);
                o.world=GetAbsolutePositionWS(TransformObjectToWorld(a.positionOS));
                o.uv=a.uv; o.dynamics=a.dynamics; o.highlight=a.highlight; o.color=a.color;
                return o;
            }
            float Gaussian(float x) { return exp(-x*x); }
            float4 Frag(V i):SV_Target
            {
                float age=saturate(i.uv.x), blade=saturate(i.uv.y), along=saturate(i.dynamics.y);
                float endMask=smoothstep(0,.14,along)*(1-smoothstep(.93,1,along));
                float border=smoothstep(0,max(.02,_SheetSoftness),blade)*(1-smoothstep(.98,1,blade));
                // One temporal fade; core leaves before the silver surface and trailing glow.
                float coreLife=1-smoothstep(.25,1,saturate(i.highlight.z));
                float surfaceLife=1-smoothstep(.3,1,age);
                float residual=smoothstep(.15,.6,age)*(1-smoothstep(.65,1,age));
                float core=Gaussian((blade-_CorePosition)/max(.01,_CoreWidth))*max(0,i.highlight.x)*coreLife;
                float edge=Gaussian((blade-.955)/max(.01,_EdgeWidth))*_EdgeIntensity*coreLife;
                float surface=sin(blade*PI)*_SheetIntensity*surfaceLife;
                float wake=Gaussian((blade-.7)/.28)*_AfterglowIntensity*max(0,i.highlight.y)*residual*lerp(.4,1,along);
                float3 distanceToContact=(i.world-_ContactPoint.xyz)/max(.05,_ContactRadius);
                float localContact=exp(-dot(distanceToContact,distanceToContact));
                float impact=1+max(0,_Impact)*_ImpactGain*(.3+localContact);
                float directional=lerp(.55,1,smoothstep(0,.85,along));
                float3 rgb=(_CoreTint.rgb*(core+edge)+_SheetTint.rgb*(surface+wake))*max(0,i.color.rgb);
                rgb*=endMask*border*directional*impact*_Intensity;
                // Exposure-compensated unlit VFX by default, with optional scene exposure influence.
                rgb*=lerp(1,GetCurrentExposureMultiplier(),saturate(_ExposureWeight));
                return float4(max(0,rgb),0);
            }
            ENDHLSL
        }
    }
}
