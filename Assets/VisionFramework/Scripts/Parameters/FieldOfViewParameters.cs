using UnityEngine;

[System.Serializable]
public class FieldOfViewParameters : VisualEffectParameters
{
    public enum DefectType
    {
        Central,
        Peripheral,
        Arcuate   
    }

    [Header("General")]
    public DefectType defectType = DefectType.Central;

    [Range(0f, 1f)]
    public float radius = 0.3f;

    [Range(0.01f, 1f)]
    public float softness = 0.1f;

    public Vector2 center = new Vector2(0.5f, 0.5f);

    [Range(0f,1f)]
    public float sensitivityLoss = 0.8f;

    [Range(0f,0.05f)]
    public float irregularity = 0f;

    public float angularIrregularity = 0.05f;

    public float noiseScale = 6f;

    [Header("Arcuate Scotoma (Glaucoma)")]
    public float arcCurvature = 0.25f;

    public float arcThickness = 0.05f;

    [Range(0f,360f)]
    public float arcRotation = 0f;

    public FieldOfViewParameters()
    {
        effectType = VisualEffectType.FieldOfView;
    }
}
