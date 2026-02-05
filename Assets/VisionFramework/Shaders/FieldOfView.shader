Shader "Vision/Filter/FieldOfViewDefect"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Radius ("Radius", Float) = 0.3
        _Softness ("Softness", Float) = 0.1
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        [KeywordEnum(Central, Peripheral)] _Type ("Defect Type", Float) = 0
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

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _Radius;
            float _Softness;
            float2 _Center;
            float _Type; // 0 = Central, 1 = Peripheral

            v2f vert (appdata_img v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
    float4 col = tex2D(_MainTex, i.uv);
    float dist = distance(i.uv, _Center);

    // Maske: 0 innen, 1 außen
    float mask = smoothstep(_Radius, _Radius + _Softness, dist);

    float finalMask = 0;
    if (_Type < 0.5) {
        // Central Defect: Innen weg (0), außen da (1)
        finalMask = mask;
    } else {
        // Peripheral (Tunnel): Innen da (1), außen weg (0)
        finalMask = 1.0 - mask;
    }

    // WICHTIG: Wir multiplizieren RGB mit der Maske. 
    // Wenn finalMask 0 ist, wird es absolut schwarz.
    return float4(col.rgb * finalMask, col.a);
}
            ENDCG
        }
    }
}