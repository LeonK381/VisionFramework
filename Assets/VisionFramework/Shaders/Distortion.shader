Shader "Vision/Filter/Distortion_Minimal"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Strength ("Strength", Float) = 0.02
        _AxisControl ("Axis Control", Float) = 0
        _Rotation ("Rotation", Float) = 0
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
            float _Strength;
            float _AxisControl;
            float _Rotation;

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
                float2 uv = i.uv;
                float rad = _Rotation * 0.0174533; // Grad in Radiant
                float2 dir = float2(cos(rad), sin(rad));

                if (_AxisControl > 0.5) {
                    // --- ASTIGMATISMUS (Direktionaler Blur) ---
                    // Wir nehmen zwei Proben in Richtung der Achse versetzt
                    float2 offset = dir * _Strength;
                    float4 col1 = tex2D(_MainTex, uv + offset);
                    float4 col2 = tex2D(_MainTex, uv - offset);
                    return (col1 + col2) * 0.5;
                } else {
                    // --- MAKULADEGENERATION (Wellen) ---
                    float dist = distance(uv, float2(0.5, 0.5));
                    // Stärkere Wellen-Mathematik
                    float wave = sin(dist * 60.0) * _Strength;
                    float mask = smoothstep(0.4, 0.0, dist);
                    uv += dir * wave * mask;
                    return tex2D(_MainTex, uv);
                }
            }
            ENDCG
        }
    }
}