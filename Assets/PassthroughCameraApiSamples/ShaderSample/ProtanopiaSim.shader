Shader "Custom/ProtanopiaPassthroughShader"
{
    Properties
    {
        _MainTex("Passthrough", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Farbtransformationsmatrix für Protanopie
            float3x3 ProtanopiaMatrix = float3x3(
                0.0, 1.05118294, -0.05116099,
                0.0, 1.0,         0.0,
                0.0, 0.0,         1.0
            );

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);

                // Transformiere RGB
                float3 prot = mul(ProtanopiaMatrix, col.rgb);

                return float4(prot, 1.0);
            }
            ENDCG
        }
    }
}
