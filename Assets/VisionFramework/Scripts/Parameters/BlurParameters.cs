using UnityEngine;

[System.Serializable]

public enum DefocusMode {Myopie = 1, Hyperopie = -1};
public class BlurParameters : VisualEffectParameters
{
    [Header("Global Blur Strength")]
    [Range(0f, 10f)]
    public float blurStrength = 1.0f;

    [Header("Anisotropic PSF (Astigmatism etc.)")]
    [Tooltip("0 = isotropic blur, 1 = strong anisotropy (Astigmatism)")]
    [Range(0f, 1f)]
    public float anisotropy = 0f;

    [Tooltip("Axis orientation in degrees (Cylinder Axis)")]
    [Range(0f, 180f)]
    public float axisAngle = 0f;

    [Tooltip("Sampling quality")]
    [Range(3, 15)]
    public int samples = 7;

    [Header("Depth / Myopia Settings")]
    public bool useDepthBlur = false;

    [Range(0.1f, 10f)]
    public float focusDistance = 0.5f;

    [Range(0.1f, 50f)]
    public float depthIntensityMultiplier = 1.0f;

    [Header("Refractive Error")]
    [Range(0f,10f)]
    public float maxDiopters = 6f;

    public DefocusMode defocusMode;

    [Header("Foveated Blur / Masking")]
    public bool useFovea = false;
    public bool invertFovea = false;

    [Range(0f, 1f)]
    public float foveaRadius = 0.25f;

    [Range(0.01f, 1f)]
    public float foveaSoftness = 0.15f;

    public Vector2 foveaCenter = new Vector2(0.5f, 0.5f);

    [Header("Optional Realism")]
    [Range(0f, 0.005f)]
    public float chromaticShift = 0f;

    [Range(0f, 1f)]
    public float anisotropicContrastLoss = 0f;

    [Header("Cataract Scatter (Veiling Glare)")]

    [Tooltip("0 = kein Streulicht, 1 = starke Katarakt")]
    [Range(0f, 1f)]
    public float scatterStrength = 0f;

    [Tooltip("Ab welcher Helligkeit beginnt Streuung?")]
    [Range(0f, 1f)]
    public float scatterThreshold = 0.6f;

    public BlurParameters()
    {
        effectType = VisualEffectType.Blur;
    }
}



