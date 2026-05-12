Shader "Vision/Filter/FieldOfViewDefect"
{
    Properties
    {
        _MainTex             ("Source",           2D)          = "white" {}
        _Radius              ("Radius",            Float)       = 0.3
        _Softness            ("Softness",          Float)       = 0.1
        _Center              ("Center",            Vector)      = (0.5,0.5,0,0)
        _Type                ("Defect Type",       Float)       = 0
        _SensitivityLoss     ("Sensitivity Loss",  Range(0,1))  = 0.8
        _Irregularity        ("Edge Irregularity", Range(0,0.05)) = 0
        _AngularIrregularity ("Angular Irreg.",    Range(0,0.2)) = 0.05
        _NoiseScale          ("Shape Scale",       Float)       = 6

        _Density         ("Spot Density",      Float)      = 10.0
        _SpotSize        ("Spot Size",         Float)      = 0.06
        _Darkness        ("Spot Darkness",     Range(0,1)) = 0.9
        _SpotSoftness    ("Spot Softness",     Float)      = 0.02
        _Asymmetry       ("Asymmetry",         Range(0,1)) = 0.6
        _PreferPeriphery ("Prefer Periphery",  Float)      = 0.0
        _PeripheryRadius ("Periphery Radius",  Float)      = 0.3
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
            float _Density;
            float _SpotSize;
            float _Darkness;
            float _SpotSoftness;
            float _Asymmetry;
            float _PreferPeriphery;
            float _PeripheryRadius;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.texcoord;
                return o;
            }

            float hash(float2 p)
            {
                p  = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.45);
                return frac(p.x * p.y);
            }

            float noiseSmooth(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a  = hash(i);
                float b  = hash(i + float2(1, 0));
                float c  = hash(i + float2(0, 1));
                float d  = hash(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x)
                     + (c - a) * u.y * (1 - u.x)
                     + (d - b) * u.x * u.y;
            }

            float fbm(float2 uv)
            {
                float f = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 3; i++)
                {
                    f += noiseSmooth(uv) * amp;
                    uv *= 2.1;
                    amp *= 0.5;
                }
                return f;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float4 col = tex2D(_MainTex, uv);
                float3 result = col.rgb;

                float noiseVal = noiseSmooth(uv * 20.0) * _Irregularity;
                float defectMask = 0;

                if (_Type < 0.5)
                {
                    float dist = distance(uv, _Center) + noiseVal;
                    float mask = smoothstep(_Radius, _Radius + _Softness, dist);
                    defectMask = 1 - mask;
                    float sensitivity = lerp(1.0, 1.0 - _SensitivityLoss, defectMask);
                    result = col.rgb * sensitivity;
                }
                else if (_Type < 1.5)
                {
                    float2 d = uv - _Center;
                    float angle = atan2(d.y, d.x);
                    float dist = length(d);

                    float angularNoise = noiseSmooth(float2(angle * _NoiseScale, 0));
                    float distortedRadius = _Radius + (angularNoise - 0.5) * _AngularIrregularity;

                    defectMask = smoothstep(distortedRadius, distortedRadius + _Softness, dist);

                    float sensitivity = lerp(1.0, 1.0 - _SensitivityLoss, defectMask);
                    result = col.rgb * sensitivity;
                }
                else
                {
                    float totalDarkness = 0.0;
                    int count = (int)_Density;

                    for (int k = 0; k < count; k++)
                    {
                        float seed = float(k) * 17.31 + 3.7;

                        float2 spotCenter;
                        float h1 = hash(float2(seed, 1.0));
                        float h2 = hash(float2(seed, 2.0));

                        if (_PreferPeriphery > 0.5)
                        {
                            float angle = h1 * 6.28318;
                            float rMin = _PeripheryRadius;
                            float rMax = 0.5;
                            float r = rMin + h2 * (rMax - rMin);
                            spotCenter = float2(0.5, 0.5) + float2(cos(angle), sin(angle)) * r;
                        }
                        else
                        {
                            spotCenter = float2(h1, h2);
                        }

                        float2 p = uv - spotCenter;
                        float spotAngle = hash(float2(seed, 3.0)) * 6.28318;
                        float2 dir = float2(cos(spotAngle), sin(spotAngle));
                        float2 perp = float2(-dir.y, dir.x);

                        float stretch = 1.0 + hash(float2(seed, 4.0)) * _Asymmetry * 2.0;

                        float2 p2;
                        p2.x = dot(p, dir) * stretch;
                        p2.y = dot(p, perp) / stretch;

                        float2 warpUV = (uv + seed) * 4.0;
                        float warpX = fbm(warpUV);
                        float warpY = fbm(warpUV + float2(1.7, 9.2));
                        float2 warp = float2(warpX, warpY) - 0.5;
                        p2 += warp * _SpotSize * _Asymmetry;

                        float dist = length(p2);
                        float r = _SpotSize + noiseSmooth(uv * 8.0 + seed) * _SpotSize * 0.5;

                        float spot = smoothstep(r + _SpotSoftness, r, dist);

                        totalDarkness = max(totalDarkness, spot);
                    }

                    result = lerp(col.rgb, float3(0.0, 0.0, 0.0), totalDarkness * _Darkness);
                }

                return float4(result, col.a);
            }
            ENDCG
        }
    }
}