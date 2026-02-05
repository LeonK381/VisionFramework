Shader "Effects/ContrastLoss"
{
    Properties
    {
        _MainTex ("Input Texture", 2D) = "white" {}
        _ContrastFactor ("Contrast Factor", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _ContrastFactor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Kontrastreduktion um Mittelgrau (0.5)
                col.rgb = (col.rgb - 0.5) * _ContrastFactor + 0.5;

                return col;
            }
            ENDCG
        }
    }
}

