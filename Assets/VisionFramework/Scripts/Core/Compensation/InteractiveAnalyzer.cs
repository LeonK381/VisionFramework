// InteractiveAnalyzer.cs
using System;
using UnityEngine;

public abstract class InteractiveAnalyzer : MonoBehaviour
{
    public abstract VisualEffectType TargetEffect { get; }

    // Blendet Test-UI ein, ruft onComplete wenn fertig
    public abstract void StartAnalysis(Action<AnalysisResult> onComplete);

    // Aufräumen falls nötig
    public virtual void Cleanup() { }
}
