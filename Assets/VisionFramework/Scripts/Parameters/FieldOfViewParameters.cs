using UnityEngine;

[System.Serializable]
public class FieldOfViewParameters : VisualEffectParameters
{
    public enum DefectType { Central, Peripheral }
    public DefectType defectType = DefectType.Central;

    [Range(0f, 1f)]
    public float radius = 0.3f; // Größe des Ausfalls

    [Range(0.01f, 1f)]
    public float softness = 0.1f; // Wie weich der Rand ist

    public Vector2 center = new Vector2(0.5f, 0.5f); // Wo der Fleck ist

    public FieldOfViewParameters() { effectType = VisualEffectType.FieldOfView; }
}
