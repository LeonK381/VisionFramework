using UnityEngine;

// Modus für tiefenbasierte Unschärfe
[System.Serializable]
public enum DefocusMode {Myopie = 1, Hyperopie = -1};

// Parameter für Unschärfe-Effekt
public class BlurParameters : VisualEffectParameters
{
    [Header("Blur")]
    [Range(0f, 5f)]
    public float blurStrength = 1.0f;

    [Range(3, 12)]
    public int sampleCount = 7;

    [Header("Astigmatism")]
    [Range(0f, 1f)]
    public float anisotropy = 0f;

    [Range(0f, 180f)]
    public float axisAngle = 0f;

    [Header("Focus (Depth-based Blur)")]
    public bool useDepth = false;

    [Range(0.1f, 10f)]
    public float focusDistance = 0.5f;

    public DefocusMode focusMode;

    [Header("Foveated Vision")]
    public bool useFovea = false;

    public bool invertFovea = false;

    [Range(0f, 1f)]
    public float foveaRadius = 0.25f;

    [Range(0.01f, 1f)]
    public float foveaSoftness = 0.15f;

    public Vector2 foveaCenter = new Vector2(0.5f, 0.5f);

    public BlurParameters()
    {
        effectType = VisualEffectType.Blur;
    }
}



