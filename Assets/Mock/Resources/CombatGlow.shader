Shader "Katsuro/CombatGlow"
{
    Properties
    {
        _Tint("Tint", Color) = (0.2,0.9,1,1)
        _Ghost("Ghost", Float) = 0
        _GhostTime("Ghost time", Float) = 0
        _GhostSway("Ghost sway", Range(0,0.03)) = 0.009
        _GhostFlowSpeed("Ghost flow speed", Float) = 1.2
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
            float _Ghost;
            float _GhostTime, _GhostSway, _GhostFlowSpeed;
            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; float4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                if (_Ghost > .5)
                {
                    // キャラクターを基準に、穏やかな波を上方向へ流す。
                    float phase = input.positionOS.y * 13 - _GhostTime * _GhostFlowSpeed * 2;
                    input.positionOS.x += sin(phase + input.positionOS.z * 6) * _GhostSway;
                    input.positionOS.z += sin(phase * .73 + input.positionOS.x * 5) * _GhostSway * .65;
                }
                o.positionWS = TransformObjectToWorld(input.positionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.color = input.color;
                return o;
            }
            float4 Frag(Varyings input) : SV_Target
            {
                float rim = pow(1-saturate(abs(dot(normalize(input.normalWS), normalize(GetWorldSpaceViewDir(input.positionWS))))), 2);
                float4 color = _Tint;
                if (_Ghost > .5)
                {
                    float phase = input.positionWS.y * 16 - _GhostTime * _GhostFlowSpeed * 2;
                    float haze = .65 + .35 * sin(phase + sin(input.positionWS.x * 9 + _GhostTime * .6));
                    // 胴体を透過させ、輪郭を薄く途切れさせる。強い明滅は加えない。
                    color.rgb *= .65 + rim * .35;
                    color.a *= (.07 + rim * .48) * lerp(.35, 1, haze);
                }
                else if (_Ghost < -.5) color *= input.color;
                return color;
            }
            ENDHLSL
        }
    }
}
