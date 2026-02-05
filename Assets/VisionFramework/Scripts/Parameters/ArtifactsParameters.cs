using UnityEngine;

[System.Serializable]
public class ArtifactsParameters : VisualEffectParameters
{
    [Header("Floater Appearance")]
    [Range(0f, 1f)]
    public float floaterIntensity = 0.8f; // Wie dunkel/deckend sind die Punkte

    [Range(2f, 50f)]
    public float density = 15f;           // Wie viele Punkte (höher = mehr)

    [Range(0.01f, 2f)]
    public float size = 1.0f;             // Skalierung der Punkte

    [Header("Movement")]
    [Range(0f, 0.2f)]
    public float driftSpeed = 0.03f;      // Wie schnell sie durchs Bild schwimmen

    public ArtifactsParameters() 
    { 
        effectType = VisualEffectType.Artifacts; 
    }
}
