using UnityEngine;

[System.Serializable]
public class DistortionParameters : VisualEffectParameters
{
    // Stärke der Verzerrung
    [Range(0f, 0.1f)]
    public float strength = 0.02f;

    // 0 = Lokal/Wellig (Makula), >0 = Global/Gerichtet (Astigmatismus)
    // Wir nutzen dies, um zwischen Sinus-Wellen und einer Streckung zu wählen
    [Range(0f, 1f)]
    public float axisControl = 0f; 

    // Der Winkel der Verzerrung (Wichtig für Astigmatismus: 0°, 90°, 180°)
    [Range(0f, 360f)]
    public float rotation = 0f;

    public DistortionParameters() { effectType = VisualEffectType.Distortion; }
}
