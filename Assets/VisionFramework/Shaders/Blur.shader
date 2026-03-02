Shader "Vision/Filter/UnifiedBlurPSF"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}

        _EnableBlur ("Enable Blur", Float) = 1
        _BaseStrength ("Base Strength", Float) = 0.002

        _Anisotropy ("Anisotropy", Range(0,1)) = 0
        _AxisAngle ("Axis Angle", Float) = 0
        _Samples ("Samples", Range(3,15)) = 7

        _CameraDepthTexture ("Depth", 2D) = "black" {}
        _UseDepth ("Use Depth", Float) = 0
        _FocusDistance ("Focus Distance", Float) = 0.5
        _DepthMultiplier ("Depth Multiplier", Float) = 1
	_DefocusSign ("Defocus Sign", Float) = 1

        _UseFovea ("Use Fovea", Float) = 0
        _InvertFovea ("Invert Fovea", Float) = 0
        _FoveaCenter ("Fovea Center", Vector) = (0.5,0.5,0,0)
        _FoveaRadius ("Fovea Radius", Float) = 0.25
        _FoveaSoftness ("Fovea Softness", Float) = 0.15

        _ChromaticShift ("Chromatic Shift", Float) = 0
        _AnisoContrastLoss ("Aniso Contrast Loss", Float) = 0

        _ScatterStrength ("Scatter Strength", Range(0,1)) = 0
        _ScatterThreshold ("Scatter Threshold", Range(0,1)) = 0.6
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
            sampler2D _CameraDepthTexture;

            float _EnableBlur;
            float _BaseStrength;
            float _Anisotropy;
            float _AxisAngle;
            int _Samples;

            float _UseDepth;
            float _FocusDistance;
            float _DepthMultiplier;
	    float _DefocusSign;

            float _UseFovea;
            float _InvertFovea;
            float2 _FoveaCenter;
            float _FoveaRadius;
            float _FoveaSoftness;

            float _ChromaticShift;
            float _AnisoContrastLoss;

            float _ScatterStrength;
            float _ScatterThreshold;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
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
                float4 original = tex2D(_MainTex, i.uv);

                if (_EnableBlur < 0.5)
                    return original;

                float blurStrength = _BaseStrength;

                if (_UseDepth > 0.5)
                {
                    float depth = tex2D(_CameraDepthTexture, i.uv).r;
                    float diff = (depth - _FocusDistance) * _DefocusSign;
		    diff = max(0, diff);
                    blurStrength *= diff * diff * _DepthMultiplier;
                }

                int samples = _Samples;

                float4 col = 0;
                float weightSum = 0;
                float4 scatterAccum = 0;

                // ===== PSF Blur =====

                if (_Anisotropy < 0.05)
                {
                    float goldenAngle = 2.39996323;
int sampleCount = samples * samples;

for(int s = 0; s < sampleCount; s++)
{
    float r = sqrt((s + 0.5) / sampleCount);
    float theta = s * goldenAngle;

    float2 dir = float2(cos(theta), sin(theta));
    float2 offset = dir * r * blurStrength;

    float4 sampleCol = tex2D(_MainTex, i.uv + offset);

    col += sampleCol;
    weightSum += 1.0;

                        
                    }
                }
                else
                {
                    float goldenAngle = 2.39996323;
int sampleCount = samples * samples;

float rad = _AxisAngle * 0.0174533;
float2 majorAxis = float2(cos(rad), sin(rad));
float2 minorAxis = float2(-majorAxis.y, majorAxis.x);

float major = blurStrength;
float minor = blurStrength * (1 - _Anisotropy);

for(int s = 0; s < sampleCount; s++)
{
    float r = sqrt((s + 0.5) / sampleCount);
    float theta = s * goldenAngle;

    float2 disk = float2(cos(theta), sin(theta));

    // ⭐ elliptische Skalierung
    float2 offset =
        majorAxis * disk.x * major +
        minorAxis * disk.y * minor;

    float4 sampleCol = tex2D(_MainTex, i.uv + offset);

    col += sampleCol;
    weightSum += 1.0;

                    }
                }

                float4 finalColor=col/weightSum;

                // ===== Cataract Scatter =====
                if(_ScatterStrength>0)
                {
                    float4 scatter=scatterAccum/weightSum;
                    finalColor=lerp(finalColor,finalColor+scatter,_ScatterStrength);
                }

                // ===== Fovea Mask =====
                if(_UseFovea>0.5)
                {
                    float dist=distance(i.uv,_FoveaCenter);
                    float mask=smoothstep(_FoveaRadius,_FoveaRadius-_FoveaSoftness,dist);
                    if(_InvertFovea>0.5) mask=1-mask;

                    finalColor=lerp(finalColor,original,mask);
                }

                return finalColor;
            }

            ENDCG
        }
    }
}