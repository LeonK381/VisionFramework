using UnityEngine;
using System;
using TMPro;

// Sub-Analyzer für verstreute Gesichtsfeldausfälle 
[System.Serializable]
public class ScatteredAnalyzer
{
    private TextMeshProUGUI progressText;
    private Action<AnalysisResult> onComplete;
    private bool isRunning = false;

    // Startet den Scattered-Defekt-Test
    public void Start(
        EffectPipelineManager pipeline,
        TextMeshProUGUI progressText,
        Action<AnalysisResult> callback)
    {
        this.progressText = progressText;
        this.onComplete = callback;
        isRunning = true;

        progressText.gameObject.SetActive(false);
    }

    // Verarbeitet Controller-Eingaben
    public void HandleInput()
    {
        if (!isRunning) return;

#if UNITY_ANDROID
        if (OVRInput.GetDown(OVRInput.RawButton.X)) OnYes();
        if (OVRInput.GetDown(OVRInput.RawButton.Y)) OnNo();
#endif
    }

    // Callback für "Ja" - Flecken wahrgenommen
    public void OnYes() { if (isRunning) Finish(1.0f); }

    // Callback für "Nein" - Keine Flecken wahrgenommen
    public void OnNo() { if (isRunning) Finish(0.0f); }

    // Beendet den Test mit binärem Ergebnis
    void Finish(float severity)
    {
        isRunning = false;

        var result = new AnalysisResult
        {
            effectType = VisualEffectType.FieldOfView,
            severity = severity,
        };
        result.parameters["defectType"] = 2f;
        result.parameters["severity"] = severity;

        onComplete?.Invoke(result);
    }

    // Räumt den Analyzer auf
    public void Cleanup()
    {
        isRunning = false;
    }
}