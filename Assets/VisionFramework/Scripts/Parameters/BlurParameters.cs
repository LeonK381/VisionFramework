using UnityEngine;



[System.Serializable]
public class BlurParameters : VisualEffectParameters
{
    [Header("Global Blur Settings")]
    [Range(0f, 10f)]
    public float blurStrength = 1.0f;

    [Header("Foveated Blur (Eye-Center)")]
    public bool useFovea = false;

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



