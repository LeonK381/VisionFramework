using UnityEngine;

public class EffectLibrary : MonoBehaviour
{
    public Material blurMaterial;
    public Material contrastLossMaterial;
    public Material fieldOfViewMaterial;
    public Material distortionMaterial;
    public Material colorChangeMaterial;
    public Material artifactsMaterial;

    public Material GetMaterial(VisualEffectType type)
    {
        switch (type)
        {
            case VisualEffectType.Blur:
                return blurMaterial;
            case VisualEffectType.Contrast:
                return contrastLossMaterial;
            case VisualEffectType.FieldOfView:
                return fieldOfViewMaterial;
            case VisualEffectType.Distortion:
                return distortionMaterial;
            case VisualEffectType.ColorChange:
                return colorChangeMaterial;
            case VisualEffectType.Artifacts:
                return artifactsMaterial;
            default:
                return null;
        }
    }
}

