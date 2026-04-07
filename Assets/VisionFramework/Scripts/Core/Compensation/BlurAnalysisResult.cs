using UnityEngine;

[System.Serializable]
public class BlurAnalysisResult
{
    public float lastSeenSize;   // kleinste lesbare Größe
    public float logMAR;         // Sehschärfe in logMAR
    public float severity;       // 0..1

    public float estimatedBlur;  // 🔥 NEU: entscheidend für Kompensation
}