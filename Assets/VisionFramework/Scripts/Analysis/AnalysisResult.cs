using System.Collections.Generic;

// Ergebnis einer interaktiven Analyse eines visuellen Effekts.
[System.Serializable]
public class AnalysisResult
{
    public VisualEffectType effectType;
    public float severity;      

    // Zusätzliche effektspezifische Parameter als Key-Value-Paare  
    public Dictionary<string, float> parameters = new Dictionary<string, float>();
}