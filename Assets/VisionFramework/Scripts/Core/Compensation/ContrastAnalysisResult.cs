using UnityEngine;

// ContrastAnalysisResult.cs
[System.Serializable]
public class ContrastAnalysisResult
{
    public float threshold;       // niedrigster erkannter Kontrastwert 0..1
    public float severity;        // 0 = kein Problem, 1 = schwer
    public float gammaEstimate;   // geschätzter Gamma-Wert für Korrektur
    public float channelBias;     // 0 = gleichmäßig, >0 = Blaukanal stärker betroffen (Katarakt)
}
