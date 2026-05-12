using UnityEngine;


// Container für einen einzelnen visuellen Effekt innerhalb eines Profils.
// Kapselt die Parameter eines Effekts und ermöglicht die polymorphe Speicherung verschiedener Effekttypen durch [SerializeReference].
[System.Serializable]
public class VisualEffectEntry
{
    [SerializeReference]
    public VisualEffectParameters parameters;

    // Erstellt eine neue Parameter-Instanz basierend auf dem Effekttyp
    public void CreateParameters(VisualEffectType type)
    {
        switch (type)
        {
            case VisualEffectType.Blur:
                parameters = new BlurParameters();
                break;

            case VisualEffectType.Contrast:
                parameters = new ContrastParameters();
                break;

            
            case VisualEffectType.FieldOfView:
                parameters = new FieldOfViewParameters();
                break;

            case VisualEffectType.Distortion:
                parameters = new DistortionParameters();
                break;

            case VisualEffectType.ColorChange:
                parameters = new ColorChangeParameters();
                break;

            case VisualEffectType.Artifacts:
                parameters = new ArtifactsParameters();
                break;
        }
    }
}




