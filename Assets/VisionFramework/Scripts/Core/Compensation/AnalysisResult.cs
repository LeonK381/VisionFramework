// AnalysisResult.cs
using System.Collections.Generic;

[System.Serializable]
public class AnalysisResult
{
    public VisualEffectType effectType;
    public float severity;      // 0..1
    public float confidence;    // 0..1
    public Dictionary<string, float> parameters = new Dictionary<string, float>();
}