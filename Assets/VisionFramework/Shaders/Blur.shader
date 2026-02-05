Shader "Vision/Filter/GlobalBlur"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _BlurStrength ("Blur Strength", Float) = 0.002
        _UseFovea ("Use Fovea (0/1)", Float) = 0
        _FoveaRadius ("Fovea Radius", Float) = 0.25
        _FoveaSoftness ("Fovea Softness", Float) = 0.15
        _FoveaCenter ("Fovea Center", Vector) = (0.5, 0.5, 0, 0)
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
            float _BlurStrength;
            float _UseFovea;
            float _FoveaRadius;
            float _FoveaSoftness;
            float2 _FoveaCenter;

            v2f vert (appdata_img v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
    float currentBlur = _BlurStrength;

    // 9-Tap Box Filter für weicheren, starken Blur
    float4 col = 0;
    
    // Wir nehmen Proben in einem 3x3 Raster rund um den Pixel
    for (float x = -1; x <= 1; x++) {
        for (float y = -1; y <= 1; y++) {
            float2 offset = float2(x, y) * currentBlur;
            col += tex2D(_MainTex, i.uv + offset);
        }
    }
    
    return col / 9.0; // Durchschnitt der 9 Proben
}
            ENDCG
        }
    }
}