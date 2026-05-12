Shader "Vision/Compensation/FieldOfViewCompensation"
{
    Properties
{
    _MainTex        ("Input",           2D)    = "white" {}
    _OriginalTex    ("Original Kamera", 2D)    = "white" {}
    _FOVDefectType  ("Defect Type",     Float) = 0.0

    // Peripheral
    _TunnelRadius   ("Tunnel Radius",   Float) = 0.3
    _TunnelSoftness ("Tunnel Softness", Float) = 0.02

    // Central
    _ScotomRadius   ("Scotom Radius",   Float) = 0.15
    _EdgeWidth      ("Edge Width",      Float) = 0.05
    _EdgeStrength   ("Edge Strength",   Float) = 2.0
    _ContrastBoost  ("Contrast Boost",  Float) = 0.5

    _MainTex_TexelSize ("TexelSize",    Vector) = (0,0,0,0)
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
            sampler2D _OriginalTex;
	    float4 _MainTex_TexelSize;

            float _FOVDefectType;
            float _TunnelRadius;
            float _TunnelSoftness;
	    float _ScotomRadius;
    	    float _EdgeWidth; 
   	    float _EdgeStrength; 
    	    float _ContrastBoost; 
     

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                if (_FOVDefectType > 0.5)
                {
                    float2 d = uv - float2(0.5, 0.5);
                    float dist = length(d);

                    float edge = smoothstep(
                        _TunnelRadius,
                        _TunnelRadius - _TunnelSoftness,
                        dist);

                    if (edge <= 0.0)
                        return fixed4(0, 0, 0, 1);
             
                    float2 remapped = (d / _TunnelRadius) * 0.5 + 0.5;

                    return lerp(
                        fixed4(0, 0, 0, 1),
                        tex2D(_OriginalTex, remapped),
                        edge);
                }

else if (_FOVDefectType < 0.5)
{
    float2 d = uv - float2(0.5, 0.5);
    float  dist = length(d);

    float distToEdge = abs(dist - _ScotomRadius);

    float edgeMask = smoothstep(_EdgeWidth, 0.0, distToEdge);

    float2 texel = _MainTex_TexelSize.xy;
    float3 col = tex2D(_MainTex, uv).rgb;

    float3 blurred =
        tex2D(_MainTex, uv + float2( texel.x, 0)).rgb * 0.25 +
        tex2D(_MainTex, uv + float2(-texel.x, 0)).rgb * 0.25 +
        tex2D(_MainTex, uv + float2(0, texel.y)).rgb * 0.25 +
        tex2D(_MainTex, uv + float2(0, -texel.y)).rgb * 0.25;

    float3 highFreq = col - blurred;
    float3 sharpened = col + highFreq * _EdgeStrength * edgeMask;

    float outsideMask = smoothstep(
        _ScotomRadius,
        _ScotomRadius + 0.05,
        dist);

    float lum = dot(sharpened, float3(0.299, 0.587, 0.114));
    float3 contrasted = (sharpened - lum) * (1.0 + _ContrastBoost * outsideMask) + lum;

    return float4(saturate(contrasted), 1.0);
}

                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}