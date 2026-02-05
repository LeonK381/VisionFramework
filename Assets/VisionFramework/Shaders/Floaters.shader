Shader "Vision/Filter/ProceduralFloaters"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _FloaterIntensity ("Intensity", Range(0, 1)) = 0.8
        _Density ("Density", Float) = 15.0
        _DriftSpeed ("Drift Speed", Float) = 0.03
        _FloaterSize ("Floater Size", Float) = 1.0
    }
    SubShader
    {
        // Notwendig für Image Effects in Unity
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

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Pseudo-Random Funktion für die Position der Punkte
            float hash(float2 p) {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            v2f vert (appdata_img v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float4 col = tex2D(_MainTex, i.uv);
                
                // 1. UVs animieren für die Drift-Bewegung
                // Wir nutzen unterschiedliche Geschwindigkeiten für X und Y für organische Bewegung
                float2 drift = float2(_Time.y * _DriftSpeed, _Time.y * _DriftSpeed * 0.6);
                float2 uv = i.uv + drift;
                
                // 2. Das Bild in Kacheln unterteilen
                float2 g = floor(uv * _Density); // Gitter-Zelle
                float2 f = frac(uv * _Density);  // Position innerhalb der Zelle
                
                // 3. Pro Kachel einen zufälligen Punkt generieren
                // Wir nutzen Hash-Werte für die X/Y Position innerhalb der Zelle
                float2 pointPos = float2(hash(g), hash(g + 0.5));
                
                // 4. Distanz zum Punkt berechnen
                float dist = distance(f, pointPos);
                
                // 5. Den Punkt zeichnen
                // Je nach _FloaterSize wird der Radius bestimmt
                float radius = 0.02 * _FloaterSize;
                // smoothstep erzeugt einen weichen Rand (Schatten-Look)
                float floater = smoothstep(radius, radius + 0.08, dist);
                
                // 6. Intensität anwenden (1.0 = kein Effekt, niedriger = dunkler Fleck)
                float finalMask = lerp(1.0, floater, _FloaterIntensity);

                // Ergebnis mit dem Originalbild multiplizieren
                return float4(col.rgb * finalMask, col.a);
            }
            ENDCG
        }
    }
}