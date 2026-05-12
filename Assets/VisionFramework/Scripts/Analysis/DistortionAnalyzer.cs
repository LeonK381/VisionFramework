using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

// Analyzer für Verzerrungen-Effekt
public class DistortionAnalyzer : InteractiveAnalyzer
{
    public override VisualEffectType TargetEffect => VisualEffectType.Distortion;

    [Header("Platform UI")]
    public GameObject metaTestPanel;
    public GameObject appleTestPanel;

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;

    [Header("Pipeline")]
    public EffectPipelineManager pipeline;

    // M-Charts nach Matsumoto et al. (2003)
    // Metamorphopsie-Messung: horizontal + vertikal
    private static readonly float[] mValues = { 2.0f, 1.5f, 1.0f, 0.5f, 0.2f, 0.1f, 0.0f };

    private int currentStep = 0;
    private bool testHorizontal = true;
    private float lastSeenH = 2.0f;
    private float lastSeenV = 2.0f;
    private bool testActive = false;

    private Action<AnalysisResult> onComplete;

    private GameObject ActivePanel => Application.platform == RuntimePlatform.Android
        ? metaTestPanel : appleTestPanel;

    // Startet den Metamorphopsie-Test (M-Charts)
    public override void StartAnalysis(Action<AnalysisResult> callback)
    {
        onComplete = callback;
        currentStep = 0;
        testHorizontal = true;
        lastSeenH = mValues[0];
        lastSeenV = mValues[0];
        testActive = true;

        ActivePanel.SetActive(true);

        if (pipeline != null)
            pipeline.IsTestActive = true;

        ShowStep();
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

    // Zeigt den aktuellen Testschritt (M-Chart mit aktuellem M-Wert)
    void ShowStep()
    {
        if (currentStep >= mValues.Length)
        {
            if (testHorizontal)
            {
                // Horizontale Phase abgeschlossen → vertikale Phase
                testHorizontal = false;
                currentStep = 0;
                ShowStep();
                return;
            }
            Finish();
            return;
        }

        float mValue = mValues[currentStep];
        Texture2D tex = GenerateMChartTexture(mValue, testHorizontal);

        if (pipeline?.testOverlayManager != null)
            pipeline.SetTestOverlay(tex, pipeline.testOverlayManager.distortionOverlayMaterial);

        if (instructionText != null)
            instructionText.text = (testHorizontal ? "Horizontal" : "Vertikal")
                                 + ":\nErscheine die Punkte gerade?\n\n"
                                 + "<b>X</b> = Ja       <b>Y</b> = Nein";

        if (progressText != null)
            progressText.text = $"M-Wert: {mValue:F1} "
                              + $"({(testHorizontal ? "H" : "V")})";
    }

    // Callback für "Ja" - Punkte erscheinen gerade
    void OnYes()
    {
        if (!testActive) return;

        if (testHorizontal) lastSeenH = mValues[currentStep];
        else lastSeenV = mValues[currentStep];

        currentStep++;
        ShowStep();
    }

    // Callback für "Nein" - Punkte erscheinen verzerrt
    void OnNo()
    {
        if (!testActive) return;
        currentStep++;
        ShowStep();
    }

    // Beendet den Test und berechnet das Ergebnis
    void Finish()
    {
        testActive = false;
        ActivePanel.SetActive(false);

        if (pipeline != null)
        {
            pipeline.ClearTestOverlay();
            pipeline.IsTestActive = false;
        }

        // Severity aus M-Wert – Matsumoto et al. (2003)
        // M-Wert 0 = maximale Verzerrung, M-Wert 2 = keine Verzerrung
        float severityH = 1.0f - lastSeenH / 2.0f;
        float severityV = 1.0f - lastSeenV / 2.0f;
        float severity = Mathf.Max(severityH, severityV);

        var result = new AnalysisResult
        {
            effectType = TargetEffect,
            severity = severity,
        };
        result.parameters["severity"] = severity;
        result.parameters["severityH"] = severityH;
        result.parameters["severityV"] = severityV;
        result.parameters["mValueH"] = lastSeenH;
        result.parameters["mValueV"] = lastSeenV;

        onComplete?.Invoke(result);
    }

    // Generiert M-Chart Textur mit Punktreihe
    Texture2D GenerateMChartTexture(float mValue, bool horizontal)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        int cx = size / 2;
        int cy = size / 2;
        int dotR = 4;
        float spacing = Mathf.Max(8f, mValue * 20f);
        int count = 11;
        float offset = -(count / 2f) * spacing;

        for (int i = 0; i < count; i++)
        {
            int px = horizontal
                ? cx + Mathf.RoundToInt(offset + i * spacing)
                : cx;
            int py = horizontal
                ? cy
                : cy + Mathf.RoundToInt(offset + i * spacing);

            DrawDot(pixels, size, px, py, dotR);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // Zeichnet einen schwarzen Punkt in das Pixel-Array
    void DrawDot(Color[] pixels, int texSize, int cx, int cy, int radius)
    {
        for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= texSize || y < 0 || y >= texSize) continue;
                if (Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) <= radius)
                    pixels[y * texSize + x] = Color.black;
            }
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
}