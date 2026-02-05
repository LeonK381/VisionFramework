Shader "Vision/Filter/ColorChange"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Intensity ("Intensity", Float) = 1.0
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
            float _Intensity;
            float4x4 _ColorMatrix; // Matrix wird per Skript übergeben

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
                float4 col = tex2D(_MainTex, i.uv);
                
                // Farbmatrix-Multiplikation
                float3 changedColor = mul(_ColorMatrix, float4(col.rgb, 1.0)).rgb;
                
                // Mischung zwischen Original und Effekt
                return float4(lerp(col.rgb, changedColor, _Intensity), col.a);
            }
            ENDCG
        }
    }
}