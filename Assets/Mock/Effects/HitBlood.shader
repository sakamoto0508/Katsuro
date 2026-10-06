Shader "Katsuro/HitBlood"
{
    Properties
    {
        _Tint("色",Color)=(1,1,1,1)
        _Mist("薄い霧",Range(0,1))=0
        _EdgeTint("血の縁の深紅",Color)=(.55,.045,.025,1)
        _EdgeStrength("縁のコントラスト",Range(0,1))=.35
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="ForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            float4 _Tint;
            float _Mist;
            float4 _EdgeTint;
            float _EdgeStrength;
            struct A { float3 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            V Vert(A a) { V o; o.positionCS=TransformObjectToHClip(a.positionOS); o.uv=a.uv; o.color=a.color*_Tint; return o; }
            float4 Frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float4 color=i.color;
                // 非対称の滴：太い先端と細い尾。円形の血玉にしない。
                float width=lerp(.38,.9,saturate(i.uv.y));
                float wobble=.055*sin(p.y*13)+.03*sin(p.y*27);
                float body=abs((p.x+wobble)/width);
                float drop=1-smoothstep(.45,1,body*body+p.y*p.y);
                float mist=1-smoothstep(.05,1,dot(p,p)*(1+.12*sin(p.x*11+p.y*7)));
                float coverage=lerp(drop,mist,_Mist);
                color.rgb*=lerp(.82,1,saturate(abs(p.x)/width));
                // 暗い芯を残し、滴の先端・縁にだけ深紅の色差を付ける。
                // 透明ブレンドを維持し、加算発光にはしない。
                float edge=smoothstep(.3,.8,body)*smoothstep(.35,.7,i.uv.y);
                color.rgb=lerp(color.rgb,_EdgeTint.rgb,edge*_EdgeStrength*(1-_Mist));
                color.a*=coverage;
                // HDRPの透明パスへ非発光の色として渡す。
                color.rgb*=GetCurrentExposureMultiplier();
                return color;
            }
            ENDHLSL
        }
    }
}
