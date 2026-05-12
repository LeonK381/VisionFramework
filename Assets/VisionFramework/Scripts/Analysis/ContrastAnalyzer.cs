using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

// Analyzer für Kontrastverlust-Effekt
public class ContrastAnalyzer : InteractiveAnalyzer
{
    public override VisualEffectType TargetEffect => VisualEffectType.Contrast;

    [Header("Platform UI")]
    public GameObject metaTestPanel;
    public GameObject appleTestPanel;

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;

    [Header("Pipeline")]
    public EffectPipelineManager pipeline;

    private float[] contrastLevels;
    private int currentStep;
    private int consecutiveFails;
    private float lastSeenContrast;
    private bool testActive;
    private float activeContrastFactor = 1.0f;

    private Action<AnalysisResult> onComplete;

    private GameObject ActivePanel => Application.platform == RuntimePlatform.Android
        ? metaTestPanel : appleTestPanel;

    void Awake()
    {
        // Logarithmische Kontrastabstufung nach Pelli & Robson (1988)
        contrastLevels = GenerateLogSteps(0.0f, -1.65f, 0.15f);
    }

    // Setzt Simulationsparameter für adaptive Testanpassung
    public void SetSimulationParams(float contrastFactor)
    {
        activeContrastFactor = contrastFactor;
        GenerateLevelsForSimulation();
    }

    // Startet den Kontrast-Sensitivitätstest
    public override void StartAnalysis(Action<AnalysisResult> callback)
    {
        onComplete = callback;
        currentStep = 0;
        consecutiveFails = 0;
        lastSeenContrast = 1.0f;
        testActive = true;

        ActivePanel.SetActive(true);

        if (pipeline != null)
            pipeline.IsTestActive = true;

        if (instructionText != null)
            instructionText.text = "Siehst du einen Unterschied\n"
                                 + "zwischen den zwei Feldern?\n\n"
                                 + "<b>X</b> = Ja       <b>Y</b> = Nein";

        BindButtons();
        ShowStep();
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

    // Zeigt den aktuellen Testschritt (zwei Graupatches mit Kontrastlevel)
    void ShowStep()
    {
        if (currentStep >= contrastLevels.Length) { Finish(); return; }

        float c = contrastLevels[currentStep];
        Texture2D tex = GeneratePatchTexture(c);

        if (pipeline?.testOverlayManager != null)
            pipeline.SetTestOverlay(tex, pipeline.testOverlayManager.contrastOverlayMaterial);

        if (progressText != null)
            progressText.text = $"Schritt {currentStep + 1} / {contrastLevels.Length}";
    }

    // Callback für "Ja" - Unterschied erkannt
    void OnYes()
    {
        if (!testActive) return;
        lastSeenContrast = contrastLevels[currentStep];
        consecutiveFails = 0;
        currentStep++;
        ShowStep();
    }

    // Callback für "Nein" - Kein Unterschied erkannt
    void OnNo()
    {
        if (!testActive) return;
        consecutiveFails++;
        if (consecutiveFails >= 2) { Finish(); return; }
        currentStep++;
        ShowStep();
    }

    // Beendet den Test und berechnet das Ergebnis
    void Finish()
    {
        testActive = false;

        if (pipeline != null)
        {
            pipeline.ClearTestOverlay();
            pipeline.IsTestActive = false;
        }

        int lastSeenIndex = System.Array.IndexOf(contrastLevels, lastSeenContrast);
        if (lastSeenIndex < 0) lastSeenIndex = currentStep - 1;

        float severity = 1.0f - Mathf.Clamp01(
            (float)lastSeenIndex / (contrastLevels.Length - 1));
        float gammaEstimate = Mathf.Lerp(1.0f, 2.5f, severity);
        float logThreshold = Mathf.Log10(lastSeenContrast);

        var result = new AnalysisResult
        {
            effectType = TargetEffect,
            severity = severity,
        };
        result.parameters["threshold"] = lastSeenContrast;
        result.parameters["logThreshold"] = logThreshold;
        result.parameters["gammaEstimate"] = gammaEstimate;
        result.parameters["channelBias"] = 0f;

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

    // Räumt den Analyzer auf 
    public override void Cleanup()
    {
        testActive = false;

        if (pipeline != null)
        {
            pipeline.ClearTestOverlay();
            pipeline.IsTestActive = false;
        }

        if (metaTestPanel != null) metaTestPanel.SetActive(false);
        if (appleTestPanel != null) appleTestPanel.SetActive(false);
    }

    // Generiert Textur mit zwei Graupatches für Kontrastvergleich
    // Schwellenwertmessung nach Pelli & Robson (1988)
    Texture2D GeneratePatchTexture(float contrastLevel)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        int patchW = size / 4;
        int patchH = size / 3;
        int centerX = size / 2;
        int centerY = size / 2;

        // Linker Patch – Referenzgrau
        DrawPatch(pixels, size,
                  centerX - patchW, centerY - patchH / 2,
                  patchW, patchH,
                  new Color(0.3f, 0.3f, 0.3f, 1f));

        // Rechter Patch – variabel je nach Kontraststufe
        float brightness = 0.3f + contrastLevel * 0.7f;
        DrawPatch(pixels, size,
                  centerX, centerY - patchH / 2,
                  patchW, patchH,
                  new Color(brightness, brightness, brightness, 1f));

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // Zeichnet ein rechteckiges Patch in das Pixel-Array
    void DrawPatch(Color[] pixels, int texSize,
                   int startX, int startY,
                   int width, int height, Color color)
    {
        for (int y = startY; y < startY + height; y++)
            for (int x = startX; x < startX + width; x++)
            {
                if (x < 0 || x >= texSize || y < 0 || y >= texSize) continue;
                pixels[y * texSize + x] = color;
            }
    }

    // Generiert adaptive Kontrastlevel basierend auf Simulationsstärke
    void GenerateLevelsForSimulation()
    {
        if (activeContrastFactor >= 1.0f)
        {
            contrastLevels = GenerateLogSteps(0.0f, -1.65f, 0.15f);
            return;
        }

        float stepSize = Mathf.Clamp(0.15f / activeContrastFactor, 0.15f, 0.80f);
        float endLog = -(stepSize * 11f);
        contrastLevels = GenerateLogSteps(0.0f, endLog, stepSize);
    }

    // Generiert logarithmische Abstufung nach Weber-Fechner Gesetz
    static float[] GenerateLogSteps(float startLog, float endLog, float stepSize)
    {
        var steps = new List<float>();
        for (float log = startLog; log >= endLog; log -= stepSize)
            steps.Add(Mathf.Round(Mathf.Pow(10f, log) * 1000f) / 1000f);
        return steps.ToArray();
    }
}