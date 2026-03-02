Shader "Hidden/AppleARColorFix"
{
    Properties {
        _YTex ("Y", 2D) = "white" {}
        _UVTex ("UV", 2D) = "white" {}
    }
    SubShader {
        Pass {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _YTex; 
            sampler2D _UVTex;

            v2f vert (appdata_img v) {
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    
    // UV-Koordinaten korrigieren:
    // Wir nehmen die Standard-UVs (0 bis 1)
    o.uv = v.texcoord;
    
    // Da das Bild auf dem Kopf steht, invertieren wir Y:
    // 0 wird zu 1, 1 wird zu 0.
    o.uv.y = 1.0 - o.uv.y;
    
    return o;
}

            fixed4 frag (v2f i) : SV_Target {
    // 1. YUV-Werte abgreifen
    float y = tex2D(_YTex, i.uv).r;
    float2 uv = tex2D(_UVTex, i.uv).rg - float2(0.5, 0.5);

    // 2. Präzisere Konvertierungsmatrix (BT.709) für natürlichere Farben
    float r = y + 1.5748 * uv.y;
    float g = y - 0.1873 * uv.x - 0.4681 * uv.y;
    float b = y + 1.8556 * uv.x;

    float3 rgb = float3(r, g, b);

    // 3. SÄTTIGUNG VERSTÄRKEN (Damit es nicht mehr blass wirkt)
    float luminance = dot(rgb, float3(0.299, 0.587, 0.114));
    float saturation = 1.3; // 1.0 ist normal, 1.3 ist kräftiger
    rgb = lerp(float3(luminance, luminance, luminance), rgb, saturation);

    // 4. LEICHTE KONTRAST-ANHEBUNG
    // Zieht die Schwarzwerte etwas tiefer für mehr "Tiefe"
    rgb = saturate((rgb - 0.05) * 1.1); 

    return float4(rgb, 1.0);
}
            ENDCG
        }
    }
}