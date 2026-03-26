Shader "Vision/Filter/Artifacts"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Intensity ("Intensity", Range(0,1)) = 0.8
        _Density ("Density", Float) = 6.0
        _DriftSpeed ("Drift Speed", Float) = 0.02
        _FloaterSize ("Floater Size", Float) = 1.0
        _Darkness ("Darkness", Range(0,1)) = 0.85
        _Transparency ("Transparency", Range(0,1)) = 0.5
        _ArtifactMode ("Artifact Mode", Float) = 0

        _PeripheryMask ("Periphery Mask", Range(0,1)) = 0
        _PeripheryRadius ("Periphery Radius", Float) = 0.4

        _Irregularity ("Irregularity", Range(0,0.05)) = 0.02
        _NoiseScale ("Shape Scale", Float) = 6.0
        _Softness ("Edge Softness", Float) = 0.05
    }

    SubShader
    {
        ZTest Always Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float _Intensity;
            float _Density;
            float _DriftSpeed;
            float _FloaterSize;
            float _Darkness;
            float _Transparency;
            float _ArtifactMode;

            float _PeripheryMask;
            float _PeripheryRadius;

            float _Irregularity;
            float _NoiseScale;
            float _Softness;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // ---------------- NOISE ----------------

            float hash(float2 p)
            {
                return frac(sin(dot(p,float2(127.1,311.7))) * 43758.5453);
            }

            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float a = hash(i);
                float b = hash(i + float2(1,0));
                float c = hash(i + float2(0,1));
                float d = hash(i + float2(1,1));

                float2 u = f*f*(3.0-2.0*f);

                return lerp(a,b,u.x)
                     + (c-a)*u.y*(1.0-u.x)
                     + (d-b)*u.x*u.y;
            }

            float fbm(float2 uv)
            {
                float f = 0.0;
                float amp = 0.5;

                for(int i = 0; i < 4; i++)
                {
                    f += noise(uv) * amp;
                    uv *= 2.0;
                    amp *= 0.5;
                }
                return f;
            }

float sdSegment(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a;
                float2 ba = b - a;

                float denom = max(dot(ba,ba), 1e-5);
                float h = saturate(dot(pa,ba) / denom);

                return length(pa - ba*h);
            }

            float sdFloater(float2 uv, float seed)
            {
                float2 center = float2(
                    frac(hash(float2(seed,1)) + seed*0.13),
                    frac(hash(float2(seed,2)) + seed*0.37)
                );

                float baseAngle = hash(float2(seed,3)) * 6.28318;

                float floaterLength = 0.04 + hash(float2(seed,4))*0.12;

                float2 rel = uv - center;

                float t = dot(rel, float2(cos(baseAngle), sin(baseAngle)));
                t = clamp(t, -floaterLength, floaterLength);

                float curve =
                    sin(t * 8.0 + seed) * 0.02 +
                    sin(t * 3.0 + seed * 2.0) * 0.015;

                float2 dir = float2(cos(baseAngle), sin(baseAngle));
                float2 normal = float2(-dir.y, dir.x);

                float2 closest = center + dir * t + normal * curve;

                float warp =
                    noise(closest*10.0 + seed)*0.015 +
                    noise(closest*20.0 + seed*2.0)*0.008;

                closest += normal * warp;

                return length(uv - closest);
            }


            v2f vert(appdata_img v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            // ---------------- FRAGMENT ----------------

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                float3 result = col.rgb;

                if(_ArtifactMode < 0.5)
                {
                    float field = 0.0;
                    int count = (int)_Density;

                    for(int k = 0; k < count; k++)
                    {
                        float seed = k * 13.37;

                        // ---------- HASH CACHE ----------
                        float h1 = hash(float2(seed,1));
                        float h2 = hash(float2(seed,2));
                        float h3 = hash(float2(seed,3));
                        float h4 = hash(float2(seed,11));

                        // ---------- ANGLE ----------
                        float angle = h1 * 6.28318;

                        float s = sin(angle);
                        float c = cos(angle);

                        float2 dir = float2(c, s);
                        float2 perp = float2(-s, c);

                        // ---------- CENTER ----------
                        float2 center;

                        if(_PeripheryMask > 0.5)
                        {
                            const float maxR = 0.35;
                            float r = lerp(_PeripheryRadius, maxR, h2);
                            center = float2(0.5,0.5) + dir * r;
                        }
                        else
                        {
                            float2 rnd = float2(h1, h2);
                            float cluster = noise(rnd * 3.0);
                            center = lerp(rnd, float2(0.5,0.5), cluster * 0.3);
                        }

                        float2 p = i.uv - center;
                        float2 pScaled = p * _NoiseScale;

                        // ---------- WARP ----------
                        float n1 = fbm(pScaled + seed);
                        float n2 = fbm(pScaled + seed * 2.0);

                        float2 warp = (float2(n1,n2) - 0.5) * _Irregularity * 3.0;
                        p += warp;

                        // ---------- ANISOTROPIC SHAPE ----------
                        float stretch = lerp(0.7, 1.6, h4);

                        float2 p2;
                        p2.x = dot(p, dir) / stretch;
                        p2.y = dot(p, perp) * stretch;

                        // ---------- DISTANCE ----------
                        float distSq = dot(p2, p2);

                        // ---------- SHAPE ----------
                        float shape = fbm(pScaled * 0.7 + seed);

                        float radius = (0.08 + h3 * 0.12) * _FloaterSize;
                        float distortedRadius = radius + (shape - 0.5) * _Irregularity * 2.0;

                        float r = distortedRadius;
                        float rSoft = distortedRadius + _Softness;

                        float blob = smoothstep(rSoft*rSoft, r*r, distSq);

                        field = max(field, blob);

                        // Early exit
                        if(field > 0.95)
                            break;
                    }

                    // ---------- CENTER MASK ----------
                    if(_PeripheryMask > 0.5)
                    {
                        float2 c = i.uv - 0.5;
                        float d = length(c);

                        float centerMask = smoothstep(
                            _PeripheryRadius * 0.8,
                            _PeripheryRadius + 0.2,
                            d
                        );

                        centerMask *= 0.9 + 0.1 * noise(i.uv * 3.0);

                        field *= centerMask;
                    }

                    float alpha = field * _Darkness * (1 - _Transparency);
                    result = lerp(col.rgb, float3(0,0,0), alpha);
                }
		else
{
    float2 uv = i.uv * 1.2 - 0.1;

                    float floaterField = 0;

                    int count = clamp((int)_Density, 3, 12);

                    for(int k=0;k<count;k++)
                    {
                        float seed = k * 19.17;

                        float t = _Time.y * (_DriftSpeed * 2.0);

                        float2 drift = float2(
                            sin(seed * 1.3 + t) + sin(seed * 2.1 + t * 0.5),
                            cos(seed * 1.7 + t * 0.8) + cos(seed * 2.3 + t * 0.3)
                        );

                        drift *= 0.03;

                        float d = sdFloater(uv + drift, seed);

                        float radius =
                            (0.006 +
                             noise(uv*10.0+seed)*0.006 +
                             hash(float2(seed,7))*0.004)
                            * _FloaterSize;

                        float core = smoothstep(radius, 0.0, d);

                        float fade =
                            smoothstep(radius*2.5, radius*1.2, d);

                        core *= fade;

                        float rim =
                            smoothstep(radius*1.2, radius, d) *
                            smoothstep(radius*2.5, radius*1.2, d);

                        float detail =
                            smoothstep(0.5, 0.8,
                                noise(uv*8.0 + seed));

                        core += detail * 0.15 * core;

                        floaterField += core*0.8 + rim*0.3;
                    }

                    floaterField /= count;
                    floaterField *= _Intensity;

                    float2 distortion =
                        float2(
                            noise(uv*6.0 + 1.0),
                            noise(uv*6.0 + 7.0)
                        ) - 0.5;

                    distortion *= floaterField * 0.015;

                    float2 uvSample = saturate(i.uv + distortion);

                    float3 baseCol =
                        tex2D(_MainTex, uvSample).rgb;

                    float strength = pow(floaterField, 0.6);

                    float absorption = strength * _Darkness * 1.5;
                    absorption *= (1.0 - _Transparency);

                    float transmission = 1.0 - absorption;
                    transmission = saturate(transmission);

                    result = baseCol * transmission;
}
                return float4(result, col.a);
            }
            ENDCG
        }
    }
}