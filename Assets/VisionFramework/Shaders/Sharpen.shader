Shader "Vision/Compensation/BlurCompensation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _EdgeStrength ("Edge Strength", Range(0,5)) = 1.5
        _Contrast ("Contrast", Range(0.5,3)) = 1.2
        _DetailBoost ("Detail Boost", Range(0,3)) = 1.0
    }

    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _EdgeStrength;
            float _Contrast;
            float _DetailBoost;

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy;

                float3 center = tex2D(_MainTex, i.uv).rgb;

                // --- EDGE DETECTION ---
                float3 left  = tex2D(_MainTex, i.uv + float2(-t.x, 0)).rgb;
                float3 right = tex2D(_MainTex, i.uv + float2(t.x, 0)).rgb;
                float3 up    = tex2D(_MainTex, i.uv + float2(0, t.y)).rgb;
                float3 down  = tex2D(_MainTex, i.uv + float2(0, -t.y)).rgb;

                float3 edge = abs(left - right) + abs(up - down);

                // --- SIMPLE BLUR (für High-Pass) ---
                float3 blur =
                    tex2D(_MainTex, i.uv + t * float2(-1,-1)).rgb +
                    tex2D(_MainTex, i.uv + t * float2( 1,-1)).rgb +
                    tex2D(_MainTex, i.uv + t * float2(-1, 1)).rgb +
                    tex2D(_MainTex, i.uv + t * float2( 1, 1)).rgb;

                blur *= 0.25;

                float3 highPass = center - blur;

                // --- APPLY EFFECTS ---
                float3 result = center;

                // Edge Boost
                result += edge * _EdgeStrength;

                // Detail Boost
                result += highPass * _DetailBoost;

                // Contrast
                result = (result - 0.5) * _Contrast + 0.5;

                return float4(saturate(result), 1);
            }
            ENDCG
        }
    }
}