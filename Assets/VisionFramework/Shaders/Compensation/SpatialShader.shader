Shader "Vision/Compensation/SpatialFilter"
{
    Properties
    {
        _MainTex              ("Input",                       2D)    = "white" {}
        _EdgeStrength         ("Edge Strength",               Float) = 0.0
        _LocalContrast        ("Local Contrast Strength",     Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        ZTest Always ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex   vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _EdgeStrength;
            float _LocalContrast;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy;
                float3 col = tex2D(_MainTex, i.uv).rgb;

                // ── Unsharp Masking
                float3 blurred =
                    tex2D(_MainTex, i.uv + float2(-1,-1) * t).rgb * 0.0625 +
                    tex2D(_MainTex, i.uv + float2( 0,-1) * t).rgb * 0.1250 +
                    tex2D(_MainTex, i.uv + float2( 1,-1) * t).rgb * 0.0625 +
                    tex2D(_MainTex, i.uv + float2(-1, 0) * t).rgb * 0.1250 +
                    col * 0.2500 +
                    tex2D(_MainTex, i.uv + float2( 1, 0) * t).rgb * 0.1250 +
                    tex2D(_MainTex, i.uv + float2(-1, 1) * t).rgb * 0.0625 +
                    tex2D(_MainTex, i.uv + float2( 0, 1) * t).rgb * 0.1250 +
                    tex2D(_MainTex, i.uv + float2( 1, 1) * t).rgb * 0.0625;

                float3 edge = col - blurred;

                // Kantenbetonung
                float3 result = col;
                if (_EdgeStrength > 0.01)
                {
                    result = col + edge * _EdgeStrength;
                }

                // Lokale Kontrastverbesserung
                if (_LocalContrast > 0.01)
                {
                    float localLum = dot(blurred, float3(0.299, 0.587, 0.114));
                    result = result * (1.0 + _LocalContrast * (result - localLum) / 				    max(localLum, 0.001));
                }

                return float4(saturate(result), 1.0);
            }
            ENDCG
        }
    }
}