Shader "Vision/Filter/UnifiedBlur"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        
        [Header(Global Settings)]
        _BlurStrength ("General Strength", Float) = 0.002
        _Direction ("Blur Direction (X, Y)", Vector) = (1, 1, 0, 0) // (1,1) = Uniform, (1,0) = Astigmatismus
        
        [Header(Fovea Settings)]
        _UseFovea ("Use Fovea Masking (0/1)", Float) = 0
        _FoveaRadius ("Fovea Radius", Float) = 0.25
        _FoveaSoftness ("Fovea Softness", Float) = 0.15
        _FoveaCenter ("Fovea Center", Vector) = (0.5, 0.5, 0, 0)
        _InvertFovea ("Invert Mask (AMD Mode)", Float) = 0 // 0 für Glaukom, 1 für AMD
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
            float _BlurStrength;
            float2 _Direction;
            float _UseFovea, _FoveaRadius, _FoveaSoftness, _InvertFovea;
            float2 _FoveaCenter;

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata_img v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float currentBlur = _BlurStrength;

                // 1. Fovea/Masken-Logik berechnen
                if (_UseFovea > 0.5) {
                    float dist = distance(i.uv, _FoveaCenter);
                    float mask = smoothstep(_FoveaRadius, _FoveaRadius - _FoveaSoftness, dist);
                    
                    if (_InvertFovea > 0.5) mask = 1.0 - mask; // AMD Modus: Zentrum unscharf
                    
                    currentBlur *= mask;
                }

                // 2. Anisotroper Sampling-Kernel
                float4 col = 0;
                float samples = 0;
                
                // Wir nutzen einen 5x5 oder 7x7 Kernel, den wir über _Direction verformen
                for (float x = -2; x <= 2; x++) {
                    for (float y = -2; y <= 2; y++) {
                        // Hier passiert die Magie: Die Richtung skaliert den Offset
                        float2 offset = float2(x * _Direction.x, y * _Direction.y) * currentBlur;
                        col += tex2D(_MainTex, i.uv + offset);
                        samples++;
                    }
                }
                
                return col / samples;
            }
            ENDCG
        }
    }
}