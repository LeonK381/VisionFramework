// AnalysisResult.cs
using System.Collections.Generic;

[System.Serializable]
public class AnalysisResult
{
    public VisualEffectType effectType;
    public float severity;      
    public float confidence;   
    public Dictionary<string, float> parameters = new Dictionary<string, float>();
}