Shader "Display/RenderTextureXR"
{
    Properties
    {
        _MainTex ("RenderTexture", 2D) = "white" {}
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque"
           "Queue"="Transparent"
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;

                o.positionHCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
            }

            ENDHLSL
        }
    }
}