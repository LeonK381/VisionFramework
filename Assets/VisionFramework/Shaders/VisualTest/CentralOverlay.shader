Shader "Vision/Test/CentralOverlay"
{
    Properties
    {
        _MainTex    ("Kamerabild",   2D) = "white" {}
        _OverlayTex ("Testpunkt",    2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex   vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _OverlayTex;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 cam     = tex2D(_MainTex,    i.uv);
                fixed4 overlay = tex2D(_OverlayTex, i.uv);

                // Transparenter Bereich → nur Kamerabild
                if (overlay.a < 0.5)
                    return cam;

                // Blauer Punkt → direkt einblenden
                return overlay;
            }
            ENDCG
        }
    }
}