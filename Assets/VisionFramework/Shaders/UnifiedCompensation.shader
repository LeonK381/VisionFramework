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

	_RedGreenComp  ("Red-Green Compensation", Float) = 0.0
	_YellowBias    ("Yellow Bias Correction", Float) = 0.0
	_DefectType    ("Defect Type",            Float) = 0.0
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
	    float _RedGreenComp;
	    float _YellowBias;
	    float _DefectType;

            fixed4 frag(v2f_img i) : SV_Target
{
    float2 texel = _MainTex_TexelSize.xy;
    float3 col   = tex2D(_MainTex, i.uv).rgb;

    // ── 1. Unsharp Masking (Blur-Kompensation) ───────────────────
    // Polesel et al. (2000)
    // Kernel skaliert mit _BlurStrength
    float2 t = texel * (1.0 + _BlurStrength * 2.0);
    float3 blurred =
        tex2D(_MainTex, i.uv + float2(-t.x,    0)).rgb * 0.25 +
        tex2D(_MainTex, i.uv + float2( t.x,    0)).rgb * 0.25 +
        tex2D(_MainTex, i.uv + float2(    0, -t.y)).rgb * 0.25 +
        tex2D(_MainTex, i.uv + float2(    0,  t.y)).rgb * 0.25;

    float3 highFreq  = clamp(col - blurred, -0.3, 0.3);
    float3 sharpened = col + highFreq * _EdgeStrength;

    // ── 2. Lokale Kontrastverstärkung (Blur + Kontrast) ──────────
    // Gonzalez & Woods (2002) – CLAHE-Approximation
    // _LocalContrast aus Blur-Test
    // edgeBoost aus Kontrast-severity (GlobalContrast-1)
    float localLum  = dot(blurred, float3(0.299, 0.587, 0.114));
    float edgeBoost = (_GlobalContrast - 1.0) * 0.4;
    sharpened = sharpened
              + (sharpened - localLum) * _LocalContrast
              + highFreq * edgeBoost;
    sharpened = saturate(sharpened);

    // ── 3. Adaptives Gamma ───────────────────────────────────────
    // Huang et al. (2013) – AGCWD
    // _GammaContrast = 1/gammaEstimate (Kontrast-Test)
    float lum = dot(sharpened, float3(0.299, 0.587, 0.114));
    float adaptiveGamma   = lerp(_GammaContrast, 1.0, lum);
    float3 gammaCorrected = pow(max(sharpened, 0.001), adaptiveGamma);

    // ── 4. Histogramm-Stretching ─────────────────────────────────
    // Gonzalez & Woods (2002)
    // Spreitet den genutzten Helligkeitsbereich
    // Effektiver als S-Kurve bei globalem Kontrastverlust
    float minBright = 0.5 - 0.5 / max(_GlobalContrast, 1.001);
    float maxBright = 0.5 + 0.5 / max(_GlobalContrast, 1.001);
    float range     = max(maxBright - minBright, 0.001);
    float3 stretched = (gammaCorrected - minBright) / range;
    stretched = saturate(stretched);

    // ── 5. Globale Kontrastverstärkung ───────────────────────────
    // Gonzalez & Woods (2002) – Contrast Stretching
    // Zusätzliche S-Kurve nach Histogramm-Stretching
    float lum2        = dot(stretched, float3(0.299, 0.587, 0.114));
    float3 contrasted = (stretched - lum2) * _GlobalContrast + lum2;
    contrasted        = saturate(contrasted);

    // ── 6. Blaukanal-Boost ───────────────────────────────────────
    // Wooten & Hammond (1983)
    contrasted.b = saturate(contrasted.b * _BlueBoost);

    // ── 7. Sättigungserhöhung ────────────────────────────────────
    // Regan & Mollon (1997)
    float lum3    = dot(contrasted, float3(0.299, 0.587, 0.114));
    float3 result = lerp(float3(lum3, lum3, lum3), contrasted, _Saturation);



    return float4(saturate(result), 1.0);
}
            ENDCG
        }
    }
}