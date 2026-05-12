using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

// Analyzer für Unschärfe-Effekt
public class BlurAnalyzer : InteractiveAnalyzer
{
    public override VisualEffectType TargetEffect => VisualEffectType.Blur;

    [Header("Platform UI")]
    public GameObject metaTestPanel;
    public GameObject appleTestPanel;

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;

    [Header("Pipeline")]
    public EffectPipelineManager pipeline;

    private float[] sizeLevels;
    private int currentStep;
    private int consecutiveFails;
    private float lastSeenSize;
    private bool testActive = false;

    private Action<AnalysisResult> onComplete;

    private GameObject ActivePanel => Application.platform == RuntimePlatform.Android
        ? metaTestPanel : appleTestPanel;

    void Awake()
    {
        // Logarithmische Größenabstufung nach Bailey & Lovie (1976)
        sizeLevels = GenerateLogSizes(1.0f, 0.05f, 0.1f);
    }

    // Public API für Pipeline um Simulationsparameter zu setzen
    public void SetSimulationParams(float blurStrength) { }

    // Startet den Blur-Sehschärfetest
    public override void StartAnalysis(Action<AnalysisResult> callback)
    {
        onComplete = callback;
        currentStep = 0;
        consecutiveFails = 0;
        lastSeenSize = sizeLevels[0];
        testActive = true;

        ActivePanel.SetActive(true);

        if (pipeline != null)
            pipeline.IsTestActive = true;

        if (instructionText != null)
            instructionText.text = "Kannst du die Öffnung des Rings erkennen?\n\n"
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

    // Zeigt den aktuellen Testschritt (Landolt-C in aktueller Größe)
    void ShowStep()
    {
        if (currentStep >= sizeLevels.Length) { Finish(); return; }

        float size = sizeLevels[currentStep];
        int dir = UnityEngine.Random.Range(0, 4);
        Texture2D tex = GenerateLandoltC(size, dir);

        if (progressText != null)
            progressText.text = $"Schritt {currentStep + 1} / {sizeLevels.Length}";

        if (pipeline?.testOverlayManager != null)
        {
            var mat = pipeline.testOverlayManager.blurOverlayMaterial;
            mat?.SetFloat("_OverlaySize", 0.12f);
            pipeline.SetTestOverlay(tex, mat);
        }
    }

    // Callback für "Ja" - Öffnung erkannt
    void OnYes()
    {
        if (!testActive) return;
        lastSeenSize = sizeLevels[currentStep];
        consecutiveFails = 0;
        currentStep++;
        ShowStep();
    }

    // Callback für "Nein" - Öffnung nicht erkannt
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

        // Stevens Power Law (1957): Potenzgesetz für Wahrnehmungsstärke
        float severity = 1.0f - Mathf.Clamp01(
            (lastSeenSize - 0.05f) / (1.0f - 0.05f));
        float compensationStrength = MapSizeToCompensation(lastSeenSize);

        var result = new AnalysisResult
        {
            effectType = TargetEffect,
            severity = severity,
        };
        result.parameters["lastSeenSize"] = lastSeenSize;
        result.parameters["severity"] = severity;
        result.parameters["compensationStrength"] = compensationStrength;

        onComplete?.Invoke(result);
    }

    // Mappt die letzte erkannte Größe auf Kompensationsstärke
    // Verwendet Weber-Fechner Gesetz: Wahrnehmung skaliert logarithmisch
    float MapSizeToCompensation(float size)
    {
        float normalized = Mathf.Clamp01((size - 0.05f) / (1.0f - 0.05f));
        return Mathf.Lerp(0f, 2.5f, Mathf.Pow(normalized, 0.5f));
    }

    // Logarithmische Größenabstufung nach Bailey & Lovie (1976)
    static float[] GenerateLogSizes(float start, float end, float step)
    {
        var steps = new List<float>();
        float logStart = Mathf.Log10(start);
        float logEnd = Mathf.Log10(end);
        for (float log = logStart; log >= logEnd; log -= step)
            steps.Add(Mathf.Round(Mathf.Pow(10f, log) * 1000f) / 1000f);
        return steps.ToArray();
    }

    // Erstellt Landolt-C Optotyp nach ISO 8596
    Texture2D GenerateLandoltC(float relativeSize, int openingDir)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        int cx = size / 2;
        int cy = size / 2;
        float outerR = size * 0.35f * relativeSize;
        float innerR = outerR * 0.6f;
        float gapAngle = 45f;

        float openAngle = openingDir switch
        {
            0 => 90f,
            1 => 0f,
            2 => 270f,
            _ => 180f
        };

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist < innerR || dist > outerR) continue;

                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                float diff = Mathf.Abs(Mathf.DeltaAngle(angle, openAngle));

                if (diff < gapAngle * 0.5f) continue;

                pixels[y * size + x] = Color.black;
            }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
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
        {
            pipeline.ClearTestOverlay();
            pipeline.IsTestActive = false;
        }

        if (metaTestPanel != null) metaTestPanel.SetActive(false);
        if (appleTestPanel != null) appleTestPanel.SetActive(false);
    }
}