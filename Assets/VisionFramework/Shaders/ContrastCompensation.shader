Shader "Custom/ContrastCompensation"
{
    Properties
    {
        _MainTex       ("Input",        2D)    = "white" {}
        _Severity      ("Severity",     Float) = 0.0
        _Saturation    ("Saturation",   Float) = 1.0
        _BlueBoost     ("Blue Boost",   Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;
            float _Severity;
            float _Saturation;
            float _BlueBoost;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).rgb;

                float s = saturate(_Severity);

                // ── 1. Adaptives Gamma ───────────────────────────────
                // Huang et al. (2013) – AGCWD
                // Dunkle Pixel stärker anheben als helle
                // Keine Simulationsparameter nötig – nur Pixelhelligkeit
                float lum = dot(col, float3(0.299, 0.587, 0.114));
                float adaptiveGamma = lerp(1.0, 0.85, s * (1.0 - lum));
                col = pow(max(col, 0.001), adaptiveGamma);
                col = saturate(col);

                // ── 2. Blaukanal-Boost (Katarakt-spezifisch) ────────
                // Wooten & Hammond (1983)
                col.b = saturate(col.b * _BlueBoost);

                // ── 3. Sättigung ─────────────────────────────────────
                // Regan & Mollon (1997)
                lum = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(float3(lum, lum, lum), col, _Saturation);
                col = saturate(col);

                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}