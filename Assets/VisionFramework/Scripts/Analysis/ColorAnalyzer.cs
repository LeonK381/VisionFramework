using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

// Analyzer für Farbveränderungs-Effekt
public class ColorAnalyzer : InteractiveAnalyzer
{
    public override VisualEffectType TargetEffect => VisualEffectType.ColorChange;

    [Header("Platform UI")]
    public GameObject metaTestPanel;
    public GameObject appleTestPanel;

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;

    [Header("Pipeline")]
    public EffectPipelineManager pipeline;

    private int activeDefectType = 0;

    // Struktur für eine Farbtafel
    private struct ColorPlate
    {
        public Color background;
        public Color figure;
        public string correctAnswer;
        public bool isControl;
    }

    private List<ColorPlate> plates = new List<ColorPlate>();
    private List<bool> plateResults = new List<bool>();
    private int currentPlate;
    private int totalPlates;
    private bool testActive = false;

    private Action<AnalysisResult> onComplete;

    private GameObject ActivePanel => Application.platform == RuntimePlatform.Android
        ? metaTestPanel : appleTestPanel;

    // Setzt den aktiven Defekttyp für die Platten-Generierung
    public void SetSimulationParams(ColorDefectType defectType, float intensity)
    {
        activeDefectType = defectType == ColorDefectType.Protanopia ? 0 : 1;
    }

    // Startet den Farbwahrnehmungstest
    public override void StartAnalysis(Action<AnalysisResult> callback)
    {
        onComplete = callback;
        currentPlate = 0;
        testActive = true;

        GeneratePlates();
        totalPlates = plates.Count;

        ActivePanel.SetActive(true);

        if (pipeline != null)
            pipeline.IsTestActive = true;

        if (instructionText != null)
            instructionText.text = "Siehst du eine Figur\nim Farbfeld?\n\n"
                                 + "<b>X</b> = Ja       <b>Y</b> = Nein";

        BindButtons();
        ShowPlate();
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

    // Zeigt die aktuelle Farbtafel
    void ShowPlate()
    {
        if (currentPlate >= plates.Count) { Finish(); return; }

        Texture2D tex = GeneratePlateTex(plates[currentPlate]);

        if (pipeline?.testOverlayManager != null)
            pipeline.SetTestOverlay(tex, pipeline.testOverlayManager.colorOverlayMaterial);

        if (progressText != null)
            progressText.text = $"Tafel {currentPlate + 1} / {totalPlates}";
    }

    // Callback für "Ja" - Figur sichtbar
    void OnYes()
    {
        if (!testActive) return;
        bool correct = plates[currentPlate].correctAnswer == "Ja";
        plateResults.Add(correct);
        currentPlate++;
        ShowPlate();
    }

    // Callback für "Nein" - Figur nicht sichtbar
    void OnNo()
    {
        if (!testActive) return;
        bool correct = plates[currentPlate].correctAnswer == "Nein";
        plateResults.Add(correct);
        currentPlate++;
        ShowPlate();
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

        // Testplatten und Kontrollplatten separat auswerten
        int controlErrors = 0, controlCount = 0;
        int testErrors = 0, testCount = 0;

        for (int i = 0; i < plates.Count; i++)
        {
            if (plates[i].isControl)
            {
                controlCount++;
                if (i < plateResults.Count && !plateResults[i]) controlErrors++;
            }
            else
            {
                testCount++;
                if (i < plateResults.Count && !plateResults[i]) testErrors++;
            }
        }

        float errorRate = testCount > 0 ? (float)testErrors / testCount : 0f;
        float severity = Mathf.Clamp01(errorRate);
        float redGreenAxis = activeDefectType == 0
            ? (severity > 0.3f ? 1.0f : 0.0f)
            : 0f;
        float yellowBias = activeDefectType == 1 ? severity : 0f;
        float channelBias = activeDefectType == 1 ? severity * 0.8f : 0f;

        var result = new AnalysisResult
        {
            effectType = TargetEffect,
            severity = severity,
        };
        result.parameters["defectType"] = activeDefectType;
        result.parameters["severity"] = severity;
        result.parameters["errorRate"] = errorRate;
        result.parameters["redGreenAxis"] = redGreenAxis;
        result.parameters["yellowBias"] = yellowBias;
        result.parameters["channelBias"] = channelBias;
        result.parameters["controlErrors"] = controlErrors;

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

    // Generiert alle Farbtafeln basierend auf aktivem Defekttyp
    void GeneratePlates()
    {
        plates.Clear();
        plateResults.Clear();

        if (activeDefectType == 0)
            GenerateProtanopiaPlates();
        else
            GenerateCataractPlates();
    }

    // Generiert Protanopie-Testplatten
    void GenerateProtanopiaPlates()
    {
        // Testplatten – Rot-Grün Verwechslungslinien nach MacAdam (1942)
        // Luminanz nach Protanopie-Simulation angepasst
        var basePlates = new (Color bg, Color fig)[]
        {
            (new Color(0.05f, 0.55f, 0.05f), new Color(0.90f, 0.05f, 0.05f)),
            (new Color(0.05f, 0.80f, 0.05f), new Color(0.90f, 0.45f, 0.00f)),
            (new Color(0.50f, 0.30f, 0.05f), new Color(0.90f, 0.10f, 0.00f)),
            (new Color(0.80f, 0.80f, 0.00f), new Color(0.10f, 0.90f, 0.00f)),
            (new Color(0.20f, 0.40f, 0.05f), new Color(0.75f, 0.15f, 0.05f)),
            (new Color(0.10f, 0.65f, 0.10f), new Color(0.85f, 0.35f, 0.00f)),
            (new Color(0.05f, 0.45f, 0.05f), new Color(0.80f, 0.05f, 0.05f)),
            (new Color(0.10f, 0.60f, 0.10f), new Color(0.70f, 0.25f, 0.05f)),
        };

        foreach (var (bg, fig) in basePlates)
            plates.Add(MakePlate(bg, AdjustFigureLuminance(bg, fig), false));

        plates.Add(MakePlate(new Color(0.40f, 0.10f, 0.70f), new Color(0.10f, 0.20f, 0.90f), true));
        plates.Add(MakePlate(new Color(0.90f, 0.90f, 0.90f), new Color(0.10f, 0.10f, 0.90f), true));
        plates.Add(MakePlate(new Color(0.85f, 0.85f, 0.85f), new Color(0.20f, 0.20f, 0.80f), true));
        plates.Add(MakePlate(new Color(0.10f, 0.10f, 0.80f), new Color(0.50f, 0.10f, 0.90f), true));
    }

    // Generiert Katarakt-Testplatten
    void GenerateCataractPlates()
    {
        // Testplatten – Blaukanal betroffen (Wooten & Hammond 1983)
        // Luminanz nach Katarakt-Simulation angepasst
        var basePlates = new (Color bg, Color fig)[]
        {
            (new Color(0.9f,  0.9f,  0.9f),  new Color(0.1f, 0.1f,  0.9f)),
            (new Color(0.95f, 0.95f, 0.95f), new Color(0.4f, 0.6f,  0.9f)),
            (new Color(0.9f,  0.8f,  0.1f),  new Color(0.1f, 0.2f,  0.8f)),
            (new Color(0.9f,  0.9f,  0.9f),  new Color(0.6f, 0.1f,  0.8f)),
            (new Color(0.8f,  0.8f,  0.8f),  new Color(0.2f, 0.3f,  0.9f)),
            (new Color(0.9f,  0.85f, 0.7f),  new Color(0.2f, 0.4f,  0.9f)),
            (new Color(0.7f,  0.7f,  0.7f),  new Color(0.1f, 0.1f,  0.8f)),
            (new Color(0.9f,  0.9f,  0.8f),  new Color(0.3f, 0.5f,  0.95f)),
        };

        foreach (var (bg, fig) in basePlates)
            plates.Add(MakePlate(bg, AdjustFigureLuminanceCataract(bg, fig), false));

        // Kontrollplatten – Rot/Gelb kaum betroffen
        var controlPlates = new (Color bg, Color fig)[]
        {
            (new Color(0.9f, 0.9f, 0.9f), new Color(0.9f, 0.1f, 0.1f)),
            (new Color(0.9f, 0.9f, 0.9f), new Color(0.9f, 0.9f, 0.1f)),
            (new Color(0.8f, 0.8f, 0.8f), new Color(0.9f, 0.4f, 0.1f)),
            (new Color(0.9f, 0.9f, 0.9f), new Color(0.8f, 0.2f, 0.2f)),
        };

        foreach (var (bg, fig) in controlPlates)
            plates.Add(MakePlate(bg, AdjustFigureLuminanceCataract(bg, fig), true));
    }

    // Erstellt eine ColorPlate-Struktur
    ColorPlate MakePlate(Color bg, Color fig, bool isControl) => new ColorPlate
    {
        background = bg,
        figure = fig,
        correctAnswer = "Ja",
        isControl = isControl
    };

    // Generiert Textur für eine Farbtafel
    Texture2D GeneratePlateTex(ColorPlate plate)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        int plateSize = size / 4;
        int startX = size / 2 - plateSize / 2;
        int startY = size / 2 - plateSize / 2;

        // Hintergrund
        for (int y = startY; y < startY + plateSize; y++)
            for (int x = startX; x < startX + plateSize; x++)
            {
                if (x < 0 || x >= size || y < 0 || y >= size) continue;
                pixels[y * size + x] = new Color(
                    plate.background.r, plate.background.g, plate.background.b, 1f);
            }

        // Figur als Kreis
        int cx = size / 2;
        int cy = size / 2;
        int r = plateSize / 5;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) < r)
                    pixels[y * size + x] = new Color(
                        plate.figure.r, plate.figure.g, plate.figure.b, 1f);
            }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // Simuliert Protanopie nach Machado et al. (2009)
    Color SimulateProtanopia(Color c) => new Color(
        0.56667f * c.r + 0.43333f * c.g,
        0.55833f * c.r + 0.44167f * c.g,
        0.24167f * c.g + 0.75833f * c.b);

    // Simuliert Katarakt-Gelbstich nach Wooten & Hammond (1983)
    Color SimulateCataract(Color c) => new Color(c.r, c.g * 0.95f, c.b * 0.40f);

    // Passt Figur-Luminanz für Protanopie an
    Color AdjustFigureLuminance(Color bg, Color fig)
        => AdjustLuminance(SimulateProtanopia(bg), SimulateProtanopia(fig), fig);

    // Passt Figur-Luminanz für Katarakt an
    Color AdjustFigureLuminanceCataract(Color bg, Color fig)
        => AdjustLuminance(SimulateCataract(bg), SimulateCataract(fig), fig);

    // Skaliert Originalfarbe so dass simulierte Luminanz dem simulierten Hintergrund entspricht
    Color AdjustLuminance(Color simBg, Color simFig, Color originalFig)
    {
        float lumBg = Luminance(simBg);
        float lumFig = Luminance(simFig);
        float scale = lumBg / Mathf.Max(lumFig, 0.001f);
        return new Color(
            Mathf.Clamp01(originalFig.r * scale),
            Mathf.Clamp01(originalFig.g * scale),
            Mathf.Clamp01(originalFig.b * scale));
    }

    // Berechnet relative Luminanz einer Farbe
    float Luminance(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
}