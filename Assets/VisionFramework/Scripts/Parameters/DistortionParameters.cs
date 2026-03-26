using UnityEngine;

[System.Serializable]
public class DistortionParameters : VisualEffectParameters
{
    public enum DistortionMode { AMD_Waves = 0, Astigmatism = 1 }

    [Header("Mode")]
    public DistortionMode mode = DistortionMode.Astigmatism;

    [Header("Global Distortion Settings")]
    [Range(0f, 0.05f)]
    public float strength = 0.01f;
    
    [Tooltip("Rotation in degrees (used for Astigmatism axis or Wave direction)")]
    [Range(0f, 360f)]
    public float rotation = 0f;

    [Header("AMD Wave Settings")]
    [Range(10f, 100f)]
    public float waveFrequency = 60.0f;
    [Range(0f, 1f)]
    public float centerRadius = 0.4f;

    public DistortionParameters()
    {
        effectType = VisualEffectType.Distortion;
    }
}
