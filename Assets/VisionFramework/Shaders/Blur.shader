Shader "Vision/Filter/UnifiedBlurPSF"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}

        _BlurStrength ("Blur Strength", Float) = 0.002

        _Anisotropy ("Anisotropy", Range(0,1)) = 0
        _AxisAngle ("Axis Angle", Float) = 0
        _Samples ("Samples", Range(3,15)) = 7

        _CameraDepthTexture ("Depth", 2D) = "black" {}
        _UseDepth ("Use Depth", Float) = 0
        _FocusDistance ("Focus Distance", Float) = 0.5
	_DefocusSign ("Defocus Sign", Float) = 1

        _UseFovea ("Use Fovea", Float) = 0
        _InvertFovea ("Invert Fovea", Float) = 0
        _FoveaCenter ("Fovea Center", Vector) = (0.5,0.5,0,0)
        _FoveaRadius ("Fovea Radius", Float) = 0.25
        _FoveaSoftness ("Fovea Softness", Float) = 0.15
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

            float _BlurStrength;
            float _Anisotropy;
            float _AxisAngle;
            int _Samples;

            float _UseDepth;
            float _FocusDistance;
	    float _DefocusSign;

            float _UseFovea;
            float _InvertFovea;
            float2 _FoveaCenter;
            float _FoveaRadius;
            float _FoveaSoftness;

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

		if(_BlurStrength < 0.0001)
			return original;

                float blurStrength = _BlurStrength;

                if (_UseDepth > 0.5)
                {
                     float depth = tex2D(_CameraDepthTexture, i.uv).r;
depth = pow(depth, 1.3);

float defocus = 0;

if (_DefocusSign > 0)
    defocus = max(0, depth - _FocusDistance);
else 
    defocus = max(0, _FocusDistance - depth);

float blurFactor = pow(defocus, 1.3);

// kleine Verstärkung für Wahrnehmbarkeit
blurFactor *= 2.0;

// optional clamp
blurFactor = saturate(blurFactor);

blurStrength *= blurFactor;
                }

                int samples = _Samples;

                float4 col = 0;
                float weightSum = 0;
            
                if (_Anisotropy < 0.05)
                {
                    float goldenAngle = 2.39996323;
		    int sampleCount = samples * 4;

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
int sampleCount = samples * 4;

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

    float2 offset =
    majorAxis * disk.x * major +
    minorAxis * disk.y * minor;

float distortion = _Anisotropy * 0.3 * blurStrength;

offset += majorAxis * distortion * r;

    float4 sampleCol = tex2D(_MainTex, i.uv + offset);

    col += sampleCol;
    weightSum += 1.0;

                    }
                }

                float4 finalColor=col/weightSum;

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