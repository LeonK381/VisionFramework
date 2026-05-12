using UnityEngine;

// Parameter für Artefakte-Effekt
[System.Serializable]
public class ArtifactsParameters : VisualEffectParameters
{
    public ArtifactsParameters()
    {
        effectType = VisualEffectType.Artifacts;
    }
    [Header("Global")]

    [Range(0f, 1f)]
    public float intensity = 0.8f;
    [Range(0f, 1f)]
    public float darkness = 0.85f;
    [Range(0f, 1f)]
    public float transparency = 0.5f;
    [Header("Shared Appearance")]
    [Range(2f, 50f)]
    public float density = 15f;
    [Range(0.01f, 2f)]
    public float size = 1.0f;
    [Header("Floaters Movement")]
    [Range(0f, 0.2f)]
    public float driftSpeed = 0.03f;

}