Shader "Katsuro/CounterImpactStroke"
{
    Properties
    {
        [HDR] _WhiteTint("Tip and thin core white", Color) = (1.3,1.4,1.55,1)
        [HDR] _SteelTint("Transparent silver blue face", Color) = (.12,.35,.65,1)
        _CoreWidth("Thin white core width", Range(.01,.2)) = .045
        _MinCorePixels("Minimum core radius in pixels", Range(.5,1.5)) = .8
        _CoreIntensity("Thin core intensity", Range(0,5)) = 2
        _FrontIntensity("Forward tip and edge intensity", Range(0,4)) = 1.6
        _SurfaceIntensity("Blue face intensity", Range(0,2)) = .7
        _FacetContrast("Steel facet contrast", Range(0,1)) = .75
        _EdgeSoftness("Outer transparency falloff", Range(.02,.4)) = .16
        _ExtensionDuration("Forward reveal duration (seconds)", Range(.005,.06)) = .018
        _ExposureWeight("Scene exposure influence", Range(0,1)) = 0
        [HideInInspector] _StrokeAge("Age", Float) = 0
        [HideInInspector] _CoreDuration("Core duration", Float) = .055
        [HideInInspector] _Duration("Total duration", Float) = .14
        [HideInInspector] _StrokeIntensity("Strike intensity", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Transparent" "Queue"="Transparent+11" }
        Pass
        {
            Tags { "LightMode"="ForwardOnly" }
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
            float4 _WhiteTint, _SteelTint;
            float _CoreWidth, _MinCorePixels, _CoreIntensity, _FrontIntensity, _SurfaceIntensity, _FacetContrast;
            float _EdgeSoftness, _ExtensionDuration, _ExposureWeight;
            float _StrokeAge, _CoreDuration, _Duration, _StrokeIntensity;
            CBUFFER_END
            struct A { float3 position:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
            V Vert(A a) { V o; o.position=TransformObjectToHClip(a.position); o.uv=a.uv; return o; }
            float Gaussian(float x) { return exp(-x*x); }
            float4 Frag(V i):SV_Target
            {
                float x=saturate(i.uv.x), y=i.uv.y*2-1;
                float age=max(0,_StrokeAge);
                float coreLife=1-smoothstep(.01,max(.033,_CoreDuration),age);
                float faceLife=1-smoothstep(.015,max(.05,_Duration),age);
                float residual=smoothstep(.02,max(.033,_CoreDuration),age)*faceLife;
                float front=lerp(.8,1,smoothstep(0,max(.005,_ExtensionDuration),age));
                float longitudinal=smoothstep(0,.12,x)*(1-smoothstep(front-.08,front,x));
                float border=1-smoothstep(1-max(.02,_EdgeSoftness),1,abs(y));
                float heel=smoothstep(0,.4,x);
                float pixel=max(.0001,fwidth(y));
                float core=Gaussian(y/max(max(.01,_CoreWidth),pixel*_MinCorePixels))*_CoreIntensity*coreLife*lerp(.3,1,heel);
                float brightEdge=Gaussian((y-.68)/max(.055,pixel*.65))*smoothstep(.4,.85,x);
                float tip=Gaussian((x-(front-.1))/.085)*Gaussian(y/.28);
                float frontWhite=(brightEdge+tip)*_FrontIntensity*coreLife;
                float facet=lerp(1,lerp(.18,.8,smoothstep(-.75,.7,y))+Gaussian((y-.4)/.08)*.25,_FacetContrast);
                float face=Gaussian(y/.68)*_SurfaceIntensity*facet*(faceLife*.65+residual*.12);
                float3 rgb=(_WhiteTint.rgb*(core+frontWhite)+_SteelTint.rgb*face)*longitudinal*border*_StrokeIntensity;
                rgb*=lerp(1,GetCurrentExposureMultiplier(),saturate(_ExposureWeight));
                // Emissive transparency: unlit pixels contribute no RGB and no occluding alpha.
                return float4(max(0,rgb),0);
            }
            ENDHLSL
        }
    }
}
