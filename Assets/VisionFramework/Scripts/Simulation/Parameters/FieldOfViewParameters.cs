using UnityEngine;

// Parameter für Gesichtsfeldausfälle-Effekt
[System.Serializable]
public class FieldOfViewParameters : VisualEffectParameters
{
    // Typ des Gesichtsfeldausfalls
    public enum DefectType
    {
        Central,
        Peripheral,
        Scattered   
    }

    public FieldOfViewParameters()
    {
        effectType = VisualEffectType.FieldOfView;
    }

    [Header("General")]
    public DefectType defectType = DefectType.Central;

    [Range(0f, 1f)]
    public float sensitivityLoss = 0.8f;

    [Header("Shape (Central / Peripheral)")]
    [Range(0f, 1f)]
    public float radius = 0.3f;

    [Range(0.01f, 1f)]
    public float softness = 0.1f;

    [Header("Position (Central / Peripheral")]
    [Tooltip("Normalized screen position (0–1)")]
    public Vector2 center = new Vector2(0.5f, 0.5f);

    [Header("Distortion (Central / Peripheral)")]
    [Range(0f, 0.05f)]
    public float irregularity = 0f;

    [Range(0f, 0.1f)]
    public float angularIrregularity = 0.05f;

    [Range(1f, 10f)]
    public float noiseScale = 6f;

    [Header("Scattered – Black Spots")]
    [Range(2f, 30f)]
    public float density = 10f;

    [Range(0.01f, 0.3f)]
    public float spotSize = 0.06f;

    [Range(0f, 1f)]
    public float darkness = 0.9f;

    [Range(0f, 0.1f)]
    public float spotSoftness = 0.02f;

    [Range(0f, 1f)]
    public float asymmetry = 0.6f;

    public bool preferPeriphery = false;

    [Range(0.1f, 0.6f)]
    public float peripheryRadius = 0.3f;
}