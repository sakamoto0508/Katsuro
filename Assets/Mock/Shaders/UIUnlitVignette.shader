Shader "UI/UnlitVignette"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0,0,0,0.6)
        _InnerRadius ("Inner Radius", Range(0,1)) = 0.3
        _OuterRadius ("Outer Radius", Range(0,1)) = 0.9
        _Smoothness ("Smoothness", Range(0.001,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _InnerRadius;
            float _OuterRadius;
            float _Smoothness;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // uv in 0..1
                float2 uv = i.uv;
                float2 center = float2(0.5, 0.5);
                float2 diff = uv - center;
                float dist = length(diff);
                float t = smoothstep(_InnerRadius, _OuterRadius, dist);
                float alphaMask = saturate(t);
                alphaMask = pow(alphaMask, _Smoothness);

                fixed4 tex = tex2D(_MainTex, i.uv);
                // multiply texture by color, then apply mask to alpha
                fixed4 col = tex * _Color;
                col.a *= alphaMask;
                return col;
            }
            ENDCG
        }
    }
}
