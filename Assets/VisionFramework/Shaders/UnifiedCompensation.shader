Shader "Custom/UnifiedVisionCompensation"
{
    Properties
    {
        _MainTex        ("Input",           2D)    = "white" {}

        // Blur-Test Parameter
        _BlurStrength   ("Blur Strength",   Float) = 0.0
        _EdgeStrength   ("Edge Strength",   Float) = 0.0
        _LocalContrast  ("Local Contrast",  Float) = 0.0

        // Kontrast-Test Parameter
        _GammaContrast  ("Gamma Contrast",  Float) = 1.0
        _GlobalContrast ("Global Contrast", Float) = 1.0
        _Saturation     ("Saturation",      Float) = 1.0
        _BlueBoost      ("Blue Boost",      Float) = 1.0
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
            float _BlurStrength;
            float _EdgeStrength;
            float _LocalContrast;
            float _GammaContrast;
            float _GlobalContrast;
            float _Saturation;
            float _BlueBoost;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                float3 col   = tex2D(_MainTex, i.uv).rgb;

                // ── 1. Unsharp Masking ───────────────────────────────
                // Polesel, Ramponi & Mathews (2000)
                // Kernel skaliert mit _BlurStrength (aus compensationStrength)
                // Stärkere Unschärfe → breiterer Kernel
                float2 t = texel * (1.0 + _BlurStrength * 2.0);
                float3 blurred =
                    tex2D(_MainTex, i.uv + float2(-t.x,    0)).rgb * 0.25 +
                    tex2D(_MainTex, i.uv + float2( t.x,    0)).rgb * 0.25 +
                    tex2D(_MainTex, i.uv + float2(    0, -t.y)).rgb * 0.25 +
                    tex2D(_MainTex, i.uv + float2(    0,  t.y)).rgb * 0.25;

                float3 highFreq  = clamp(col - blurred, -0.3, 0.3);
                float3 sharpened = col + highFreq * _EdgeStrength;

                // ── 2. Lokale Kontrastverstärkung ────────────────────
                // Gonzalez & Woods (2002) – CLAHE-Approximation
                // Kontrast relativ zur lokalen Helligkeit (blurred)
                // _LocalContrast aus compensationStrength (Blur-Test)
                float localLum = dot(blurred, float3(0.299, 0.587, 0.114));
                sharpened = sharpened + (sharpened - localLum) * _LocalContrast;
                sharpened = saturate(sharpened);

                // ── 3. Adaptives Gamma ───────────────────────────────
                // Huang et al. (2013) – AGCWD
                // _GammaContrast = 1/gammaEstimate (Kontrast-Test)
                // Elliott et al. (1990): Inverse der Gammakurvenverschiebung
                float lum = dot(sharpened, float3(0.299, 0.587, 0.114));
                float adaptiveGamma   = lerp(_GammaContrast, 1.0, lum);
                float3 gammaCorrected = pow(max(sharpened, 0.001), adaptiveGamma);

                // ── 4. Globale Kontrastverstärkung ───────────────────
                // Gonzalez & Woods (2002) – Contrast Stretching
                // _GlobalContrast aus severity (Kontrast-Test)
                float lum2        = dot(gammaCorrected, float3(0.299, 0.587, 0.114));
                float3 contrasted = (gammaCorrected - lum2) * _GlobalContrast + lum2;
                contrasted        = saturate(contrasted);

                // ── 5. Blaukanal-Boost ───────────────────────────────
                // Wooten & Hammond (1983)
                // _BlueBoost aus channelBias (Kontrast-Test)
                // Katarakt dämpft kurzwelliges Licht (380-500nm) selektiv
                contrasted.b = saturate(contrasted.b * _BlueBoost);

                // ── 6. Sättigungserhöhung ────────────────────────────
                // Regan & Mollon (1997)
                // _Saturation aus severity (Kontrast-Test)
                // ITU-R BT.601 Luminanzgewichtung
                float lum3    = dot(contrasted, float3(0.299, 0.587, 0.114));
                float3 result = lerp(float3(lum3, lum3, lum3), contrasted, _Saturation);

                return float4(saturate(result), 1.0);
            }
            ENDCG
        }
    }
}