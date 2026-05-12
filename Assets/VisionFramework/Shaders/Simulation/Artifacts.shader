Shader "Vision/Filter/Artifacts"
{
    Properties
    {
        _MainTex      ("Source",       2D)          = "white" {}
        _Intensity    ("Intensity",    Range(0,1))  = 0.8
        _Density      ("Density",      Float)       = 6.0
        _DriftSpeed   ("Drift Speed",  Float)       = 0.02
        _FloaterSize  ("Floater Size", Float)       = 1.0
        _Darkness     ("Darkness",     Range(0,1))  = 0.85
        _Transparency ("Transparency", Range(0,1))  = 0.5
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

            // Texture
            sampler2D _MainTex;
            
            // Parameters
            float _Intensity;
            float _Density;
            float _DriftSpeed;
            float _FloaterSize;
            float _Darkness;
            float _Transparency;

            /// <summary>
            /// Varyings (Vertex → Fragment)
            /// </summary>
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            /// <summary>
            /// Hash-Funktion für Pseudozufallszahlen
            /// </summary>
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            /// <summary>
            /// Smooth Noise (Perlin-Style)
            /// </summary>
            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(a, b, u.x)
                     + (c - a) * u.y * (1.0 - u.x)
                     + (d - b) * u.x * u.y;
            }

            /// <summary>
            /// Signed Distance Field für einen Glaskörper-Floater
            /// Generiert organische, gekrümmte Formen mit Noise
            /// </summary>
            float sdFloater(float2 uv, float seed)
            {
                // Zufällige Position für diesen Floater
                float2 center = float2(
                    frac(hash(float2(seed, 1)) + seed * 0.13),
                    frac(hash(float2(seed, 2)) + seed * 0.37)
                );

                // Zufällige Rotation und Länge
                float baseAngle = hash(float2(seed, 3)) * 6.28318;
                float floaterLength = 0.04 + hash(float2(seed, 4)) * 0.12;

                // Projektion auf Floater-Achse
                float2 rel = uv - center;
                float t = dot(rel, float2(cos(baseAngle), sin(baseAngle)));
                t = clamp(t, -floaterLength, floaterLength);

                // Organische Krümmung
                float curve =
                    sin(t * 8.0 + seed) * 0.02 +
                    sin(t * 3.0 + seed * 2.0) * 0.015;

                float2 dir = float2(cos(baseAngle), sin(baseAngle));
                float2 normal = float2(-dir.y, dir.x);

                float2 closest = center + dir * t + normal * curve;

                // Noise-basierte Verzerrung für organisches Aussehen
                float warp =
                    noise(closest * 10.0 + seed) * 0.015 +
                    noise(closest * 20.0 + seed * 2.0) * 0.008;

                closest += normal * warp;

                return length(uv - closest);
            }

            /// <summary>
            /// Vertex Shader
            /// </summary>
            v2f vert(appdata_img v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }  // ← HIER WAR DIE FEHLENDE KLAMMER!

            /// <summary>
            /// Fragment Shader - Mouches Volantes (Glaskörpertrübungen)
            /// 
            /// Wissenschaftliche Basis:
            /// - Simuliert Kollagenfasern im Glaskörper
            /// - Drifting mit Augenbewegung
            /// - Lichtstreuung und Absorption
            /// </summary>
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv * 1.2 - 0.1;
                float4 col = tex2D(_MainTex, i.uv);

                float floaterField = 0;
                int count = clamp((int)_Density, 3, 12);

                // Generiere mehrere Floater
                for (int k = 0; k < count; k++)
                {
                    float seed = k * 19.17;
                    float t = _Time.y * (_DriftSpeed * 2.0);

                    // Langsame Drift-Bewegung
                    float2 drift = float2(
                        sin(seed * 1.3 + t) + sin(seed * 2.1 + t * 0.5),
                        cos(seed * 1.7 + t * 0.8) + cos(seed * 2.3 + t * 0.3)
                    ) * 0.03;

                    // Distanz zum Floater
                    float d = sdFloater(uv + drift, seed);

                    // Floater-Radius mit Noise-Variation
                    float radius =
                        (0.006 +
                         noise(uv * 10.0 + seed) * 0.006 +
                         hash(float2(seed, 7)) * 0.004)
                        * _FloaterSize;

                    // Kern des Floaters (dunkel)
                    float core = smoothstep(radius, 0.0, d);
                    float fade = smoothstep(radius * 2.5, radius * 1.2, d);
                    core *= fade;

                    // Rand des Floaters (leichter)
                    float rim =
                        smoothstep(radius * 1.2, radius, d) *
                        smoothstep(radius * 2.5, radius * 1.2, d);

                    // Details hinzufügen
                    float detail = smoothstep(0.5, 0.8, noise(uv * 8.0 + seed));
                    core += detail * 0.15 * core;

                    floaterField += core * 0.8 + rim * 0.3;
                }

                // Normalisieren und Intensität anwenden
                floaterField /= count;
                floaterField *= _Intensity;

                // Leichte Verzerrung durch Lichtbrechung
                float2 distortion =
                    (float2(noise(uv * 6.0 + 1.0),
                            noise(uv * 6.0 + 7.0)) - 0.5)
                    * floaterField * 0.015;

                // Sample mit Verzerrung
                float3 baseCol = tex2D(_MainTex, saturate(i.uv + distortion)).rgb;
                
                // Lichtabsorption durch Floater
                float strength = pow(floaterField, 0.6);
                float absorption = strength * _Darkness * 1.5 * (1.0 - _Transparency);
                float transmission = saturate(1.0 - absorption);

                return float4(baseCol * transmission, col.a);
            }
            ENDCG
        }
    }
    
    // Fallback für ältere Hardware
    Fallback Off
}