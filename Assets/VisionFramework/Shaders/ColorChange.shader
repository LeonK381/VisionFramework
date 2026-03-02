Shader "Vision/Filter/ColorChange"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Intensity ("Effect Intensity", Range(0,1)) = 1.0
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
            float4x4 _ColorMatrix; // Empfängt die Transformationsmatrix

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata_img v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);

                float3 changedColor =
                    mul(_ColorMatrix, float4(col.rgb,1)).rgb;

                changedColor = saturate(changedColor);

                return float4(
                    lerp(col.rgb, changedColor, _Intensity),
                    col.a
                );
            }
            ENDCG
        }
    }
}