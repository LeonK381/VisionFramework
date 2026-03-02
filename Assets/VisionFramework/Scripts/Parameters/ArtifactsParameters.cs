using UnityEngine;

[System.Serializable]

public enum ArtifactMode {Retinopathy = 0, Floater = 1}
public class ArtifactsParameters : VisualEffectParameters
{
    [Header("Floater Appearance")]
    [Range(0f, 1f)]
    public float floaterIntensity = 0.8f; 

    [Range(2f, 50f)]
    public float density = 15f;   

    [Range(0.01f, 2f)]
    public float size = 1.0f;

    [Header("Movement")]
    [Range(0f, 0.2f)]
    public float driftSpeed = 0.03f; 
    
    public float darkness = 0.85f;
    
    public float transparency = 0.5f; 

    public ArtifactMode artifactMode = ArtifactMode.Retinopathy;

    public ArtifactsParameters() 
    { 
        effectType = VisualEffectType.Artifacts; 
    }
}
