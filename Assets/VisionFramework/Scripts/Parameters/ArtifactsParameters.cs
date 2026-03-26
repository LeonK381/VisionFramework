using UnityEngine;

[System.Serializable]
public enum ArtifactMode 
{ 
    Retinopathy = 0, 
    Floater = 1 
}

public class ArtifactsParameters : VisualEffectParameters
{
    public ArtifactsParameters() 
    { 
        effectType = VisualEffectType.Artifacts; 
    }

    [Header("Global")]

    public ArtifactMode artifactMode = ArtifactMode.Retinopathy;

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

    [Header("Retinopathy Shape")]

    [Range(0f, 0.05f)]
    public float irregularity = 0.02f;

    [Range(1f, 10f)]
    public float noiseScale = 6.0f;

    [Range(0.001f, 0.1f)]
    public float softness = 0.05f;

    [Header("Glaucoma (Optional)")]

    public bool avoidCenter = false;

    [Range(0.1f, 0.8f)]
    public float centerRadius = 0.4f;
}