using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

// Analyzer für Gesichtsfeldausfälle
public class FieldOfViewAnalyzer : InteractiveAnalyzer
{
    public override VisualEffectType TargetEffect => VisualEffectType.FieldOfView;

    [Header("Platform UI")]
    public GameObject metaTestPanel;
    public GameObject appleTestPanel;

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;

    [Header("Pipeline")]
    public EffectPipelineManager pipeline;

    private int activeDefectType = 0;
    private float activeRadius = 0.3f;
    private bool testActive = false;

    private Action<AnalysisResult> onComplete;

    // ── Sub-Analyzer ─────────────────────────────────────────────────
    // Central:    Humphrey Visual Field Test (Anderson 1992)
    // Peripheral: Goldmann Perimetrie (Goldmann 1945)
    // Scattered:  Binäre Symptombestätigung
    private CentralAnalyzer centralAnalyzer = new CentralAnalyzer();
    private PeripheralAnalyzer peripheralAnalyzer = new PeripheralAnalyzer();
    private ScatteredAnalyzer scatteredAnalyzer = new ScatteredAnalyzer();

    private GameObject ActivePanel => Application.platform == RuntimePlatform.Android
        ? metaTestPanel : appleTestPanel;

    // Setzt Simulationsparameter vom aktiven Profil
    public void SetSimulationParams(int defectType, float radius)
    {
        activeDefectType = defectType;
        activeRadius = radius;
    }

    // Startet den Gesichtsfeldausfall-Test basierend auf aktivem Defekttyp
    public override void StartAnalysis(Action<AnalysisResult> callback)
    {
        onComplete = callback;
        testActive = true;

        ActivePanel.SetActive(true);
        BindButtons();

        if (pipeline != null)
            pipeline.IsTestActive = true;

        switch (activeDefectType)
        {
            case 0:
                if (instructionText != null)
                    instructionText.text = "Siehst du den Punkt\nim Sichtfeld?\n\n"
                                        + "<b>X</b> = Ja       <b>Y</b> = Nein";

                centralAnalyzer.Start(pipeline, progressText, activeRadius,
                    result => FinishAnalysis(result));
                break;

            case 1:
                if (instructionText != null)
                    instructionText.text = "Siehst du den Punkt?\n\n"
                                        + "<b>X</b> = Ja       <b>Y</b> = Nein";

                peripheralAnalyzer.Start(pipeline, progressText, activeRadius,
                    result => FinishAnalysis(result));
                break;

            case 2:
                if (instructionText != null)
                    instructionText.text = "Schaue durch das Sichtfeld.\n\n"
                                        + "Siehst du schwarze Flecken?\n\n"
                                        + "<b>X</b> = Ja       <b>Y</b> = Nein";

                scatteredAnalyzer.Start(pipeline, progressText,
                    result => FinishAnalysis(result));
                break;
        }
    }

    // Wird von Sub-Analyzern aufgerufen wenn Test abgeschlossen ist
    void FinishAnalysis(AnalysisResult result)
    {
        testActive = false;

        ActivePanel.SetActive(false);

        if (pipeline != null)
            pipeline.IsTestActive = false;

        onComplete?.Invoke(result);
    }

    // Bindet UI-Button-Callbacks an aktiven Sub-Analyzer
    void BindButtons()
    {
        if (ActivePanel == null) return;

        var buttons = ActivePanel.GetComponentsInChildren<Button>();
        foreach (var btn in buttons)
        {
            btn.onClick.RemoveAllListeners();

            if (btn.name.Contains("Yes"))
                btn.onClick.AddListener(() =>
                {
                    if (activeDefectType == 0) centralAnalyzer.OnYes();
                    else if (activeDefectType == 1) peripheralAnalyzer.OnYes();
                    else scatteredAnalyzer.OnYes();
                });

            if (btn.name.Contains("No"))
                btn.onClick.AddListener(() =>
                {
                    if (activeDefectType == 0) centralAnalyzer.OnNo();
                    else if (activeDefectType == 1) peripheralAnalyzer.OnNo();
                    else scatteredAnalyzer.OnNo();
                });
        }
    }

    // Verarbeitet Controller-Eingaben 
    void Update()
    {
        if (!testActive) return;

#if UNITY_ANDROID
        if      (activeDefectType == 0) centralAnalyzer.HandleInput();
        else if (activeDefectType == 1) peripheralAnalyzer.HandleInput();
        else                            scatteredAnalyzer.HandleInput();
#endif
    }

    // Räumt den Analyzer auf
    public override void Cleanup()
    {
        testActive = false;

        if (pipeline != null)
            pipeline.IsTestActive = false;

        if (metaTestPanel != null) metaTestPanel.SetActive(false);
        if (appleTestPanel != null) appleTestPanel.SetActive(false);

        centralAnalyzer.Cleanup();
        peripheralAnalyzer.Cleanup();
        scatteredAnalyzer.Cleanup();
    }
}