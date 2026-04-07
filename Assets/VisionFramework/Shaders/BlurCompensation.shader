Shader "Custom/BlurCompensation"
{
    Properties
    {
        _MainTex        ("Input",                        2D)    = "white" {}
        _USMStrength    ("Unsharp Masking Strength",     Float) = 1.0
        _ContrastBoost  ("Contrast Boost",               Float) = 1.0
        _Saturation     ("Saturation",                   Float) = 1.0
        _Severity       ("Severity",                     Float) = 0.0
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
            float _USMStrength;
            float _ContrastBoost;
            float _Saturation;
            float _Severity;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 col  = tex2D(_MainTex, i.uv);
                float2 texel = _MainTex_TexelSize.xy;

                // ── 1. Erweitertes Unsharp Masking ───────────────────
                // Polesel, Ramponi & Mathews (2000)
                // 2-Pixel-Kernel für stärkere Hochfrequenzbetonung
                // Formel: Output = Input + Stärke * (Input - Blur(Input))
                float3 blurWide =
                    tex2D(_MainTex, i.uv + float2(-2*texel.x,        0)).rgb * 0.125 +
                    tex2D(_MainTex, i.uv + float2( 2*texel.x,        0)).rgb * 0.125 +
                    tex2D(_MainTex, i.uv + float2(       0, -2*texel.y)).rgb * 0.125 +
                    tex2D(_MainTex, i.uv + float2(       0,  2*texel.y)).rgb * 0.125 +
                    col.rgb * 0.5;

                float3 highFreq = col.rgb - blurWide;
                // Clamp verhindert Artefakte bei starken Kanten
                highFreq = clamp(highFreq, -0.2, 0.2);
                col.rgb = col.rgb + highFreq * _USMStrength;
                col.rgb = saturate(col.rgb);

                // ── 2. Lokale Kontrastverstärkung ────────────────────
                // Gonzalez & Woods (2002) – Local Contrast Enhancement
                // Formel: Output = (Input - Luminanz) * Boost + Luminanz
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = (col.rgb - lum) * _ContrastBoost + lum;
                col.rgb = saturate(col.rgb);

                // ── 3. Sättigung ─────────────────────────────────────
                // Regan & Mollon (1997)
                // ITU-R BT.601 Luminanzgewichtung
                lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(float3(lum, lum, lum), col.rgb, _Saturation);
                col.rgb = saturate(col.rgb);

                return col;
            }
            ENDCG
        }
    }
}