using UnityEngine;

[System.Serializable]
public class VisualEffectEntry
{
    [SerializeReference]
    public VisualEffectParameters parameters;

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




