Shader "Custom/SimpleSharpen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Strength ("Strength", Range(0, 3)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            ZWrite Off
            Cull Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Strength;
            float4 _MainTex_TexelSize;

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
                float2 texel = _MainTex_TexelSize.xy;

                fixed4 center = tex2D(_MainTex, i.uv);

                fixed4 blur =
                    tex2D(_MainTex, i.uv + texel * float2(-1, -1)) +
                    tex2D(_MainTex, i.uv + texel * float2( 0, -1)) +
                    tex2D(_MainTex, i.uv + texel * float2( 1, -1)) +
                    tex2D(_MainTex, i.uv + texel * float2(-1,  0)) +
                    tex2D(_MainTex, i.uv + texel * float2( 0,  0)) +
                    tex2D(_MainTex, i.uv + texel * float2( 1,  0)) +
                    tex2D(_MainTex, i.uv + texel * float2(-1,  1)) +
                    tex2D(_MainTex, i.uv + texel * float2( 0,  1)) +
                    tex2D(_MainTex, i.uv + texel * float2( 1,  1));

                blur /= 9.0;

                fixed4 sharpened = center + (center - blur) * _Strength;

                return saturate(sharpened);
            }
            ENDCG
        }
    }
}