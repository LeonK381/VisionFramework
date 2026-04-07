using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CompensationPipelineManager : MonoBehaviour
{
    [Header("Render Target")]
    public RenderTexture compensatedRT;
    public RenderTexture compensatedRT2; // NEU – zweite RT

    [Header("Analyzers")]
    public List<InteractiveAnalyzer> analyzers;

    [Header("Unified Compensator")]
    public UnifiedCompensator unified;
    private Dictionary<VisualEffectType, InteractiveAnalyzer>  analyzerMap    = new();
    private Dictionary<VisualEffectType, AnalysisResult>       analysisResults = new();
    private List<VisualEffectType> activeEffectTypes = new();

    private bool compensationActive = false;
    private bool analysisRunning    = false;

// CompensationPipelineManager.cs

void Awake()
{
    Debug.Log($">>> CompensationManager Awake – Analyzers: {analyzers?.Count}");

    if (analyzers != null)
        foreach (var a in analyzers)
        {
            if (a == null) { Debug.LogWarning(">>> Analyzer ist null!"); continue; }
            analyzerMap[a.TargetEffect] = a;
            Debug.Log($">>> Analyzer registriert: {a.TargetEffect}");
            // Cleanup NICHT hier – Panels sind im Inspector deaktiviert
        }
}

  public void SetActiveEffects(List<VisualEffectType> types)
    {
        activeEffectTypes = types;
        Debug.Log($">>> SetActiveEffects: {string.Join(", ", types)}");
    }

public void ToggleCompensation()
    {
        Debug.Log($">>> ToggleCompensation – analysisRunning: {analysisRunning}, compensationActive: {compensationActive}, Results: {analysisResults.Count}");

        if (analysisRunning)
        {
            Debug.Log(">>> Analyse läuft bereits, ignoriere Toggle");
            return;
        }

        if (!compensationActive && analysisResults.Count == 0)
        {
            Debug.Log(">>> Starte neuen Analyse-Flow");
            StartCompensationFlow();
        }
        else
        {
            compensationActive = !compensationActive;
            Debug.Log($">>> Kompensation toggled: {(compensationActive ? "AN" : "AUS")}");
        }
    }

public void StartCompensationFlow()
    {
        Debug.Log($">>> StartCompensationFlow – activeEffectTypes: {string.Join(", ", activeEffectTypes)}");

        if (activeEffectTypes.Count == 0)
        {
            Debug.LogWarning(">>> Keine aktiven Effekte – wurde SetActiveEffects aufgerufen?");
            return;
        }

        var toAnalyze = activeEffectTypes
            .Where(t => analyzerMap.ContainsKey(t))
            .ToList();

        Debug.Log($">>> Zu analysierende Effekte: {string.Join(", ", toAnalyze)}");

        if (toAnalyze.Count == 0)
        {
            Debug.LogWarning(">>> Kein passender Analyzer gefunden für: "
                + string.Join(", ", activeEffectTypes));
            return;
        }

        analysisRunning = true;
        RunNextAnalysis(toAnalyze, 0);
    }

void RunNextAnalysis(List<VisualEffectType> queue, int index)
{
    Debug.Log($">>> RunNextAnalysis – Index: {index} / {queue.Count}");

    if (index >= queue.Count)
    {
        OnAllAnalysesDone();
        return;
    }

    VisualEffectType current     = queue[index];
    InteractiveAnalyzer analyzer = analyzerMap[current];

    Debug.Log($">>> Starte Analyzer für: {current}");
    // analyzer.Cleanup() ENTFERNT – war der Grund für unsichtbares Panel

    analyzer.StartAnalysis(result =>
    {
        Debug.Log($">>> Analyse fertig: {result.effectType} – Severity: {result.severity:F2}");
        analysisResults[result.effectType] = result;
        RunNextAnalysis(queue, index + 1);
    });
}

void OnAllAnalysesDone()
{
    Debug.Log(">>> Alle Analysen abgeschlossen");
    analysisRunning = false;

    // ApplyAll NUR EINMAL hier aufrufen
    unified.ApplyAll(analysisResults);

    compensationActive = true;
    Debug.Log(">>> Kompensation aktiv");
}

public void ResetCompensation()
    {
        Debug.Log(">>> ResetCompensation");
        compensationActive = false;
        analysisRunning    = false;
        analysisResults.Clear();

        foreach (var a in analyzers)
            a?.Cleanup();
    }

public RenderTexture Process(RenderTexture input)
{
    if (!compensationActive) return input;

    // ApplyAll NICHT hier – nur Process
    unified.Process(input, compensatedRT);

    return compensatedRT;
}
    // CompensationPipelineManager.cs – Hilfsmethode ergänzen
    public T GetAnalyzer<T>() where T : InteractiveAnalyzer
    {
        foreach (var a in analyzers)
            if (a is T typed) return typed;
        return null;
    }
}