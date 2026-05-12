using System;
using UnityEngine;

// Abstrakte Basis-Klasse für alle interaktiven Analyzer
public abstract class InteractiveAnalyzer : MonoBehaviour
{
    public abstract VisualEffectType TargetEffect { get; }

    // Startet die interaktive Analyse
    public abstract void StartAnalysis(Action<AnalysisResult> onComplete);

    // Räumt den Analyzer auf
    public virtual void Cleanup() { }
}
