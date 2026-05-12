Shader "Vision/Test/BlurOverlay"
{
    Properties
    {
        _MainTex    ("Kamerabild",   2D) = "white" {}
        _OverlayTex ("Buchstabe",    2D) = "white" {}
        _OverlaySize("Overlay Size", Float) = 0.4
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
            float     _OverlaySize;

            fixed4 frag(v2f_img i) : SV_Target
{
    fixed4 cam = tex2D(_MainTex, i.uv);

    // Weißes Rechteck nur in der Mitte
    // _OverlaySize steuert wie groß der Bereich ist
    float2 d = abs(i.uv - float2(0.5, 0.5));
    if (d.x > _OverlaySize || d.y > _OverlaySize)
        return cam; // außerhalb → nur Kamerabild

    // UV für Overlay remappen damit es die gesamte RT abdeckt
    float2 overlayUV = (i.uv - (0.5 - _OverlaySize))
                     / (2.0 * _OverlaySize);

    return tex2D(_OverlayTex, overlayUV);
}
            ENDCG
        }
    }
}