using UnityEngine;

public enum ColorDefectType { None, Protanopia, CataractYellow }

[System.Serializable]
public class ColorChangeParameters : VisualEffectParameters
{
    public ColorDefectType defectType = ColorDefectType.None;

    [Range(0f, 1f)]
    public float intensity = 1.0f; // Wie stark der Effekt ist (Lerp zum Original)

    public ColorChangeParameters() { effectType = VisualEffectType.ColorChange; }
}
