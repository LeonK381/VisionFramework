Shader "Vision/Filter/Distortion"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Strength ("Strength", Float) = 0.01
        _Rotation ("Rotation (Deg)", Float) = 0
        _Mode ("Mode (0:AMD, 1:Astig)", Float) = 1
        
        _WaveFreq ("Wave Frequency", Float) = 60.0
        _CenterRad ("Center Radius", Float) = 0.4
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
	    float _Rotation; 
	    float _Mode;
            float _WaveFreq;
	    float  _CenterRad;

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
                
                float rad = _Rotation * 0.0174533;
                float2 dir = float2(cos(rad), sin(rad));

                if (_Mode > 0.5) {
                    float2 offset = dir * _Strength;
                    float4 col1 = tex2D(_MainTex, uv + offset);
                    float4 col2 = tex2D(_MainTex, uv - offset);
                    return (col1 + col2) * 0.5;
                } 
                else {
                    float dist = distance(uv, float2(0.5, 0.5));
                    float wave = sin(dist * _WaveFreq) * _Strength;
                    float mask = smoothstep(_CenterRad, 0.0, dist);
                    
                    uv += dir * wave * mask;
                    return tex2D(_MainTex, uv);
                }
            }
            ENDCG
        }
    }
}