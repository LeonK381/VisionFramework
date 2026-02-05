Shader "Unlit/TunnelVision"
{
    Properties
    {
        _MainTex ("Camera Feed", 2D) = "white" {}
        _Radius ("Inner Radius", Range(0.0, 1.0)) = 0.4
        _Feather ("Feather Width", Range(0.0, 1.0)) = 0.25
        _OutsideDarkness ("Outside Darkness", Range(0.0, 1.0)) = 1.0
        _CenterOffset ("Center Offset (X,Y)", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Radius;
            float _Feather;
            float _OutsideDarkness;
            float4 _CenterOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float2 center = float2(0.5, 0.5) + _CenterOffset.xy;

                // Unity stellt _ScreenParams automatisch bereit
                float screenAspect = _ScreenParams.x / _ScreenParams.y;

                float2 uvC = uv - center;
                uvC.x *= screenAspect;

                float dist = length(uvC);

                float inner = _Radius - _Feather * 0.5;
                float outer = _Radius + _Feather * 0.5;

                float mask = smoothstep(outer, inner, dist);

                float3 col = tex2D(_MainTex, uv).rgb;
                float3 outside = lerp(col, float3(0,0,0), _OutsideDarkness);

                float3 finalColor = lerp(outside, col, mask);

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
