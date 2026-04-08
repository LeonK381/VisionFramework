// InteractiveAnalyzer.cs
using System;
using UnityEngine;

public abstract class InteractiveAnalyzer : MonoBehaviour
{
    public abstract VisualEffectType TargetEffect { get; }
    public abstract void StartAnalysis(Action<AnalysisResult> onComplete);

    public virtual void Cleanup() { }
}
