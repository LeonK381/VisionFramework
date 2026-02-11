using UnityEngine;

[System.Serializable]
public class BlurParameters : VisualEffectParameters
{
    [Header("Global Blur Settings")]
    [Range(0f, 10f)]
    public float blurStrength = 1.0f;

    [Tooltip("Directional factor: (1,1) is uniform, (1,0) is horizontal (Astigmatism).")]
    public Vector2 blurDirection = new Vector2(1f, 1f);

    [Header("Foveated Blur / Masking")]
    public bool useFovea = false;
    
    [Tooltip("If true, the center is blurred (AMD). If false, the periphery is blurred (Glaucoma).")]
    public bool invertFovea = false; 

    [Range(0f, 1f)]
    public float foveaRadius = 0.25f;

    [Range(0.01f, 1f)]
    public float foveaSoftness = 0.15f;

    [Tooltip("Normalized screen position (0–1). Default = center.")]
    public Vector2 foveaCenter = new Vector2(0.5f, 0.5f);

    public BlurParameters()
    {
        effectType = VisualEffectType.Blur;
    }
}



