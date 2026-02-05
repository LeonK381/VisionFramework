Shader "Custom/AstigCompensation"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
        _AstigAxis ("Axis", Float) = 0
        _AstigStrength ("Strength", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _AstigAxis;
            float _AstigStrength;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // rotate UVs around center (0.5,0.5)
            float2 rotateUV(float2 uv, float angleDeg)
            {
                float angle = radians(angleDeg);
                float s = sin(angle), c = cos(angle);
                float2 p = uv - 0.5;
                float2 r;
                r.x = c * p.x - s * p.y;
                r.y = s * p.x + c * p.y;
                return r + 0.5;
            }

            // simple directional 1D blur (horizontal) sampling across N taps
            float3 dirBlur(float2 uv, float2 dir, float strength)
            {
                // strength: 0..1 maps to pixel offset range
                float2 px = float2(1.0/_ScreenParams.x, 1.0/_ScreenParams.y);
                float2 step = dir * (0.0035 * (0.5 + strength*4.0)); // adapt step by strength
                float3 acc = float3(0,0,0);
                acc += tex2D(_MainTex, uv - 3*step).rgb * 0.05;
                acc += tex2D(_MainTex, uv - 2*step).rgb * 0.09;
                acc += tex2D(_MainTex, uv - 1*step).rgb * 0.2;
                acc += tex2D(_MainTex, uv).rgb * 0.32;
                acc += tex2D(_MainTex, uv + 1*step).rgb * 0.2;
                acc += tex2D(_MainTex, uv + 2*step).rgb * 0.09;
                acc += tex2D(_MainTex, uv + 3*step).rgb * 0.05;
                return acc;
            }

            // simple directional sharpen: center + amount*(center - blurred)
            float3 directionalSharpen(float2 uv, float2 dir, float strength)
            {
                float3 center = tex2D(_MainTex, uv).rgb;
                float3 blurred = dirBlur(uv, dir, strength);
                float3 diff = center - blurred;
                return saturate(center + diff * (strength * 1.2));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // rotate UV so "axis" aligns with horizontal sampling
                float2 uvR = rotateUV(i.uv, _AstigAxis);

                // sample sharpen along rotated horizontal axis
                float2 dir = float2(1,0); // horizontal after rotation
                float3 compensated = directionalSharpen(uvR, dir, _AstigStrength);

                return fixed4(compensated, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
