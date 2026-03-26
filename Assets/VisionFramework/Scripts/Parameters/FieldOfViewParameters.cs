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

    public FieldOfViewParameters()
    {
        effectType = VisualEffectType.FieldOfView;
    }

    [Header("General")]

    public DefectType defectType = DefectType.Central;

    [Range(0f, 1f)]
    public float sensitivityLoss = 0.8f;

    [Header("Shape")]

    [Range(0f, 1f)]
    public float radius = 0.3f;

    [Range(0.01f, 1f)]
    public float softness = 0.1f;

    [Header("Position")]

    [Tooltip("Normalized screen position (0–1)")]
    public Vector2 center = new Vector2(0.5f, 0.5f);

    [Header("Distortion")]

    [Range(0f, 0.05f)]
    public float irregularity = 0f;

    [Range(0f, 0.1f)]
    public float angularIrregularity = 0.05f;

    [Range(1f, 10f)] 
    public float noiseScale = 6f;
}