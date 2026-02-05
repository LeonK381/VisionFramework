using UnityEngine;

[System.Serializable]
public class ContrastParameters : VisualEffectParameters
{
    [Header("Global Contrast Settings")]
    [Range(0f, 1f)]
    public float contrastFactor = 0.5f;

    public ContrastParameters()
    {
        effectType = VisualEffectType.Contrast;
    }
}
