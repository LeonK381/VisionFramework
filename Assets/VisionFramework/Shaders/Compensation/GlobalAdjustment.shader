Shader "Vision/Compensation/GlobalAdjustment"
{
    Properties
    {
        _MainTex        ("Input",              2D)    = "white" {}
        _GammaContrast  ("Gamma Correction",   Float) = 1.0
        _GlobalContrast ("Global Contrast",    Float) = 1.0
        _Saturation     ("Saturation",         Float) = 1.0
        _BlueBoost      ("Blue Channel Boost", Float) = 1.0
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

            float _GammaContrast;
            float _GlobalContrast;
            float _Saturation;
            float _BlueBoost;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).rgb;

                //Gamma-Korrektur
                float3 gammaCorrected = pow(max(col, 0.001), _GammaContrast);

                // Histogramm-Stretching
                float3 contrasted = (gammaCorrected - 0.5) * _GlobalContrast + 0.5;

                // Saettigungsanpassung
                float  lum = dot(contrasted, float3(0.299, 0.587, 0.114));
                float3 saturated = lerp(float3(lum, lum, lum), contrasted, _Saturation);

                return float4(saturate(saturated), 1.0);
            }
            ENDCG
        }
    }
}
