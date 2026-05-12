using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// Manager für die Kompensations-Pipeline.
// 1. Analyse der Sehprobleme durch interaktive Tests
// 2. Sammlung der Analyseergebnisse
// 3. Anwendung der Kompensationsstrategien
public class CompensationPipelineManager : MonoBehaviour
{
    [Header("Render Target")]
    public RenderTexture compensatedRT;

    [Header("Analyzers")]
    public List<InteractiveAnalyzer> analyzers;

    [Header("Unified Compensator")]
    public UnifiedCompensator unified;

    private Dictionary<VisualEffectType, InteractiveAnalyzer> analyzerMap = new();
    private Dictionary<VisualEffectType, AnalysisResult> analysisResults = new();
    private List<VisualEffectType> activeEffectTypes = new();

    private bool compensationActive = false;
    private bool analysisRunning = false;

    void Awake()
    {
        if (analyzers == null)
        {
            Debug.LogWarning("No analyzers assigned!");
            return;
        }

        foreach (var analyzer in analyzers)
        {
            if (analyzer == null)
            {
                Debug.LogWarning("Null analyzer in list!");
                continue;
            }

            analyzerMap[analyzer.TargetEffect] = analyzer;
        }
    }

    // Setzt die Original-Textur für die Kompensation
    public void SetOriginalTexture(Texture tex)
    {
        unified.SetOriginalTexture(tex);
    }

    // Setzt die aktuell aktiven Effekttypen aus dem gewählten Sehprofil
    public void SetActiveEffects(List<VisualEffectType> types)
    {
        activeEffectTypes = types;
    }

    // Gibt zurück, ob gerade eine Analyse läuft
    public bool IsAnalysisRunning()
    {
        return analysisRunning;
    }

    // Wechselt zwischen simuliertem und kompensiertem Bild oder startet neue Analyse
    public void ToggleCompensation()
    {
        if (analysisRunning)
        {
            return;
        }

        if (!compensationActive && analysisResults.Count == 0)
            StartCompensationFlow();
        else
        {
            compensationActive = !compensationActive;
        }
    }

    // Setzt die Kompensation zurück
    public void ResetCompensation()
    {
        Debug.Log("[COMPENSATION] ResetCompensation called");

        compensationActive = false;
        analysisRunning = false;
        analysisResults.Clear();

        foreach (var a in analyzers)
            a?.Cleanup();

        // FOV Material deaktivieren
        if (unified != null && unified.fovMaterial != null)
            unified.fovMaterial.SetFloat("_FOVDefectType", -1f);
    }

    // Verarbeitet das Eingabebild und gibt kompensierter oder Original zurück
    public RenderTexture Process(RenderTexture input)
    {
        if (!compensationActive) return input;
        unified.Process(input, compensatedRT);
        return compensatedRT;
    }

    // Gibt einen spezifischen Analyzer zurück
    public T GetAnalyzer<T>() where T : InteractiveAnalyzer
    {
        foreach (var a in analyzers)
            if (a is T typed) return typed;
        return null;
    }

    // Startet den Kompensationsablauf
    void StartCompensationFlow()
    {
        if (activeEffectTypes.Count == 0)
        {
            Debug.LogWarning("No active effects to analyze!");
            return;
        }

        var toAnalyze = activeEffectTypes
            .Where(t => analyzerMap.ContainsKey(t))
            .ToList();

        foreach (var effectType in toAnalyze)
        {
            var analyzer = analyzerMap[effectType];
        }

        analysisRunning = true;
        RunNextAnalysis(toAnalyze, 0);
    }

    // Führt sequentiell alle Analysen durch
    void RunNextAnalysis(List<VisualEffectType> queue, int index)
    {
        if (index >= queue.Count)
        {
            OnAllAnalysesDone();
            return;
        }

        var effectType = queue[index];
        var analyzer = analyzerMap[effectType];


        analyzer.StartAnalysis(result =>
        {
            analysisResults[result.effectType] = result;
            RunNextAnalysis(queue, index + 1);
        });
    }

    // Schließt den Analyseprozess ab
    void OnAllAnalysesDone()
    {
        analysisRunning = false;
        unified.ApplyAll(analysisResults);
        compensationActive = true;
    }
}