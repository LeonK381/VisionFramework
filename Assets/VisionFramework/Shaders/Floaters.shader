Shader "Vision/Filter/ProceduralFloaters"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _FloaterIntensity ("Intensity", Range(0,1)) = 0.8
        _Density ("Density", Float) = 6.0
        _DriftSpeed ("Drift Speed", Float) = 0.02
        _FloaterSize ("Floater Size", Float) = 1.0
        _Darkness ("Darkness", Range(0,1)) = 0.85
        _Transparency ("Transparency", Range(0,1)) = 0.5
        _ArtifactMode ("Artifact Mode (0=Retinopathy,1=Floaters)", Float) = 0
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

            float _FloaterIntensity;
            float _Density;
            float _DriftSpeed;
            float _FloaterSize;
            float _Darkness;
            float _Transparency;
            float _ArtifactMode;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // ------------------------------------------------
            // NOISE
            // ------------------------------------------------

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

            v2f vert(appdata_img v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            // ------------------------------------------------
            // FLOATER CURVES
            // ------------------------------------------------

            float sdSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;

    float denom = max(dot(ba,ba), 1e-5); // ⭐ FIX

    float h = saturate(dot(pa,ba) / denom);

    return length(pa - ba*h);
}

float sdFloater(float2 uv, float seed)
{
    // Basisposition
    float2 center = float2(
        hash(float2(seed,1)),
        hash(float2(seed,2)));

    float angle = hash(float2(seed,3)) * 6.28318;

    float2 dir = float2(cos(angle), sin(angle));

    float segLength = 0.06 + hash(float2(seed,4))*0.08;

    // lokale Koordinaten
    float2 rel = uv - center;

    float t = dot(rel, dir);

    // entlang der Linie begrenzen
    t = clamp(t, -segLength, segLength);

    float2 closest = center + dir * t;

    // ⭐ ORGANISCHE VERFORMUNG
    float warp =
        noise(closest*8.0 + seed)*0.03 +
        noise(closest*16.0 + seed*2.0)*0.015;

    float2 normal = float2(-dir.y, dir.x);

    closest += normal * warp;

    return length(uv - closest);
}

            float floaterCurve(float2 uv, float seed)
            {
                float2 a=float2(hash(float2(seed,1)),hash(float2(seed,2)));
                float2 b=float2(hash(float2(seed,3)),hash(float2(seed,4)));

                float2 mid=(a+b)*0.5 +
                    float2(sin(seed*3.1),cos(seed*2.7))*0.15;

                float d1=sdSegment(uv,a,mid);
                float d2=sdSegment(uv,mid,b);

                return min(d1,d2);
            }

            // ------------------------------------------------
            // FRAGMENT
            // ------------------------------------------------

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);

                // ⭐ getrennte UVs
                float2 drift = float2(
                    _Time.y*_DriftSpeed,
                    _Time.y*_DriftSpeed*0.6);

                float2 uvStatic = i.uv;
                float2 uvDrift = frac(i.uv + drift);

                float3 result = col.rgb;

		float field = 0.0;

                // =====================================================
                // RETINOPATHY (STATIC)
                // =====================================================
                if(_ArtifactMode < 0.5)
                {
                    float sizeFactor = lerp(3.0,0.3,saturate(_FloaterSize));
                    float freq = _Density * sizeFactor;

                    float n1 = noise(uvStatic * freq);
                    float n2 = noise(uvStatic * freq * 0.5 + 13);

                    float field = n1*0.6 + n2*0.4;

                    float threshold = lerp(0.78,0.55,saturate(_FloaterSize));

                    float blob = smoothstep(threshold,threshold+0.08,field);

                    // harte Entfernung schwacher Werte
                    blob = smoothstep(0.35,0.65,blob);
                    blob *= blob;
                    blob = max(blob-0.15,0.0);
                    blob /= 0.85;
                    blob = saturate(blob);

                    float mask =
                        blob * _FloaterIntensity * (1-_Transparency);

                    // ⭐ physikalische Absorption (kein Halo)
                    float transmission =
                        exp(-mask * _Darkness * 4.0);

                    result = col.rgb * transmission;
                }

                // =====================================================
                // FLOATERS (DRIFTING)
                // =====================================================
               else
{
    float2 uv = i.uv;

    float floaterField = 0;

    int count = clamp((int)_Density, 3, 12);

    for(int k=0;k<count;k++)
    {
        float seed = k * 19.17;

        float d = sdFloater(uv, seed);

        // Dicke des Fadens
        float radius =
    (0.020 +
     noise(uv*6.0+seed)*0.020 +
     hash(float2(seed,7))*0.015)
    * _FloaterSize;

        // Kern
        float core =
            1.0 - smoothstep(0.0, radius, d);

// sanftes Auslaufen der Enden
float endFade =
    1.0 - smoothstep(radius*1.5, radius*3.5, d);

core *= endFade;

core *= endFade;

	float blobs =
    smoothstep(0.04, 0.0,
        noise(uv*6.0 + seed));

core += blobs * 0.35;

        // heller Rand (wie Referenzbild!)
        float rim =
            smoothstep(radius, radius*2.5, d);

        floaterField += core*0.8 + rim*0.2;
    }

    floaterField /= count;

    floaterField *= _FloaterIntensity;

    // weiche optische Absorption
    float transmission =
        exp(-floaterField * _Darkness * 3.0);

    result = col.rgb * transmission;
}
                return float4(result, col.a);
            }
            ENDCG
        }
    }
}