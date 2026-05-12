Shader "Vision/Compensation/ColorCorrection"
{
    Properties
    {
        _MainTex      ("Input",                  2D)    = "white" {}
        _DefectType   ("Defect Type",            Float) = 0.0
        _RedGreenComp ("Red-Green Compensation", Float) = 0.0
        _YellowBias   ("Yellow Bias Correction", Float) = 0.0
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
            float     _DefectType;
            float     _RedGreenComp;
            float     _YellowBias;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 result = tex2D(_MainTex, i.uv).rgb;

                // Daltonisierung Protanopie
                // Machado et al. (2009)
                // MacAdam (1942): Verwechslungslinien
                if (_DefectType < 0.5 && _RedGreenComp > 0.0)
                {
                    float3 original = result;
                    float3 compensated;
                    compensated.r =  1.00000f * original.r
                                  +  0.00000f * original.g
                                  +  0.00000f * original.b;
                    compensated.g =  0.49421f * original.r
                                  +  0.00000f * original.g
                                  +  1.24827f * original.b;
                    compensated.b = -0.01242f * original.r
                                  +  0.00000f * original.g
                                  +  1.01242f * original.b;
                    result = saturate(lerp(original, compensated,
                                          _RedGreenComp));
                }

                // Gelbfärbungskorrektur Katarakt
                // Wooten & Hammond (1983)
                if (_DefectType > 0.5 && _YellowBias > 0.0)
                {
                    result.b = saturate(result.b * (1.0 + _YellowBias * 2.0));
                    result.r = saturate(result.r * (1.0 - _YellowBias * 0.3));
                    result.g = saturate(result.g * (1.0 - _YellowBias * 0.1));
                }

                return float4(saturate(result), 1.0);
            }
            ENDCG
        }
    }
}