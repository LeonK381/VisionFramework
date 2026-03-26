using UnityEngine;

public enum ColorDefectType { None, Protanopia, CataractYellow }

[System.Serializable]
public class ColorChangeParameters : VisualEffectParameters
{
    [Header("Global Color Change Settings")]
    public ColorDefectType defectType = ColorDefectType.None;

    
    [Range(0f, 1f)]
    public float intensity = 1.0f; 

    public ColorChangeParameters() { effectType = VisualEffectType.ColorChange; }
}
