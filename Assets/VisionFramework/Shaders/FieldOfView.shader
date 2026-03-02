Shader "Vision/Filter/FieldOfViewDefect"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}

        _Radius ("Radius", Float) = 0.3
        _Softness ("Softness", Float) = 0.1
        _Center ("Center", Vector) = (0.5,0.5,0,0)

        _Type ("Defect Type (0=Central,1=Peripheral,2=Arcuate)", Float) = 0

        _SensitivityLoss ("Sensitivity Loss", Range(0,1)) = 0.8
        _Irregularity ("Edge Irregularity", Range(0,0.05)) = 0
	_AngularIrregularity ("Angular Irregularity", Range(0,0.2)) = 0.05
	_NoiseScale ("Shape Scale", Float) = 6

        // NEW — arcuate scotoma
        _ArcCurvature ("Arc Curvature", Float) = 0.25
        _ArcThickness ("Arc Thickness", Float) = 0.05
        _ArcRotation ("Arc Rotation", Float) = 0
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

            float _Radius;
            float _Softness;
            float2 _Center;
            float _Type;

            float _SensitivityLoss;
            float _Irregularity;
	    float _AngularIrregularity;
	    float _NoiseScale;

            float _ArcCurvature;
            float _ArcThickness;
            float _ArcRotation;

            struct v2f {
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

float hash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 34.45);
    return frac(p.x * p.y);
}

            float noiseSmooth(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    float a = hash(i);
    float b = hash(i + float2(1,0));
    float c = hash(i + float2(0,1));
    float d = hash(i + float2(1,1));

    float2 u = f*f*(3.0-2.0*f);

    return lerp(a,b,u.x) +
           (c-a)*u.y*(1-u.x) +
           (d-b)*u.x*u.y;
}

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float4 col = tex2D(_MainTex, uv);

                float noise = noiseSmooth(uv * 20.0) * _Irregularity;

                float defectMask = 0;

                // CENTRAL
                // CENTRAL (Zentrales Skotom)
if (_Type < 0.5)
{
    float dist = distance(uv,_Center)+noise;
    float mask = smoothstep(_Radius,_Radius+_Softness,dist);

    // Zentrum dunkel
    defectMask = 1 - mask;
}
// PERIPHERAL (Tunnelblick)
else if (_Type < 1.5)
{
    float2 d = uv - _Center;

    float angle = atan2(d.y, d.x);
    float dist  = length(d);

    // Winkelabhängige Radius-Verformung
    float angularNoise =
        noiseSmooth(float2(angle * _NoiseScale, 0));

    float distortedRadius =
        _Radius +
        (angularNoise - 0.5) * _AngularIrregularity;

    defectMask = smoothstep(
        distortedRadius,
        distortedRadius + _Softness,
        dist);
}
                // ARCUATE SCOTOMA (Bjerrum)
                else
                {
                    float2 p = uv - _Center;

                    float rad = _ArcRotation * 0.0174533;
                    float2x2 rot = float2x2(cos(rad),-sin(rad),sin(rad),cos(rad));
                    p = mul(rot,p);

                    // curved arc distance
                    float curve = abs(length(p - float2(_ArcCurvature,0)) - _Radius);

                    defectMask = smoothstep(
                        _ArcThickness,
                        _ArcThickness + _Softness,
                        curve);
                }

                float sensitivity = lerp(1.0,1.0-_SensitivityLoss,defectMask);
                col.rgb *= sensitivity;

                return col;
            }
            ENDCG
        }
    }
}