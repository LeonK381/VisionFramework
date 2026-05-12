using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

// Analyzer für visuelle Artefakte
public class ArtifactsAnalyzer : InteractiveAnalyzer
{
    public override VisualEffectType TargetEffect => VisualEffectType.Artifacts;

    [Header("Platform UI")]
    public GameObject metaTestPanel;
    public GameObject appleTestPanel;

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;

    [Header("Pipeline")]
    public EffectPipelineManager pipeline;

    private bool testActive = false;
    private Action<AnalysisResult> onComplete;

    private GameObject ActivePanel => Application.platform == RuntimePlatform.Android
        ? metaTestPanel : appleTestPanel;

    // Startet die Analyse für visuelle Artefakte
    public override void StartAnalysis(Action<AnalysisResult> callback)
    {
        onComplete = callback;
        testActive = true;

        ActivePanel.SetActive(true);

        if (pipeline != null)
            pipeline.IsTestActive = true;

        if (instructionText != null)
            instructionText.text = "Siehst du schwebende Fäden\n"
                                 + "oder Punkte im Sichtfeld?\n\n"
                                 + "<b>X</b> = Ja       <b>Y</b> = Nein";

        progressText.gameObject.SetActive(false);

        BindButtons();
    }

    // Bindet UI-Button-Callbacks
    void BindButtons()
    {
        if (ActivePanel == null) return;

        var buttons = ActivePanel.GetComponentsInChildren<Button>();
        foreach (var btn in buttons)
        {
            btn.onClick.RemoveAllListeners();
            if (btn.name.Contains("Yes")) btn.onClick.AddListener(OnYes);
            if (btn.name.Contains("No")) btn.onClick.AddListener(OnNo);
        }
    }

    // Callback für "Ja" - Artefakte wahrgenommen
    void OnYes() { if (testActive) Finish(1.0f); }

    // Callback für "Nein" - Keine Artefakte wahrgenommen
    void OnNo() { if (testActive) Finish(0.0f); }

    // Beendet die Analyse und gibt das Ergebnis zurück
    void Finish(float severity)
    {
        testActive = false;
        ActivePanel.SetActive(false);

        if (pipeline != null)
            pipeline.IsTestActive = false;

        var result = new AnalysisResult
        {
            effectType = TargetEffect,
            severity = severity,
        };
        result.parameters["severity"] = severity;

        onComplete?.Invoke(result);
    }

    // Verarbeitet Controller-Eingaben 
    void Update()
    {
        if (!testActive) return;

#if UNITY_ANDROID
        if (OVRInput.GetDown(OVRInput.RawButton.X)) OnYes();
        if (OVRInput.GetDown(OVRInput.RawButton.Y)) OnNo();
#endif
    }

    // Räumt den Analyzer auf (wird beim Profilwechsel aufgerufen)
    public override void Cleanup()
    {
        testActive = false;

        if (pipeline != null)
            pipeline.IsTestActive = false;

        if (metaTestPanel != null) metaTestPanel.SetActive(false);
        if (appleTestPanel != null) appleTestPanel.SetActive(false);
    }
}