using UnityEngine;

// Typ der Farbveränderung
public enum ColorDefectType { None, Protanopia, CataractYellow }

// Parameter für Farbveränderungen-Effekt
[System.Serializable]
public class ColorChangeParameters : VisualEffectParameters
{
    [Header("Global Color Change Settings")]
    public ColorDefectType defectType = ColorDefectType.None;

    
    [Range(0f, 1f)]
    public float intensity = 1.0f; 

    public ColorChangeParameters() { effectType = VisualEffectType.ColorChange; }
}
