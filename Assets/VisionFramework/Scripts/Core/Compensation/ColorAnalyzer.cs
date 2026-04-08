using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColorAnalyzer : InteractiveAnalyzer
{
    public override VisualEffectType TargetEffect => VisualEffectType.ColorChange;

    [Header("Platform UI")]
    public GameObject metaTestPanel;
    public GameObject appleTestPanel;

    [Header("UI Elemente")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;
    public RawImage colorPlateDisplay;

    // Aktiver Defekttyp aus dem Profil
    // 0 = Protanopie, 1 = CataractYellow
    private int activeDefectType = 0;

    // Ishihara-inspirierte Tafeln
    // Wissenschaftliche Basis: Ishihara (1917)
    // Farbverwechslungslinien nach MacAdam (1942)
    // Jeder Eintrag: (Hintergrundfarbe, Figurfarbe, korrekte Antwort)
    private struct ColorPlate
    {
        public Color background;
        public Color figure;
        public string correctAnswer; // "Ja" = Figur erkennbar
    }

    private List<ColorPlate> plates     = new List<ColorPlate>();
    private int              currentPlate;
    private int              correctCount;
    private int              totalPlates;
    private bool             testActive  = false;

    private List<RenderTexture> plateRTs = new List<RenderTexture>();
    private Action<AnalysisResult> onComplete;

    private GameObject ActivePanel => Application.platform == RuntimePlatform.Android
        ? metaTestPanel : appleTestPanel;

    // Simulation auf Plates anwenden
    public Material colorSimulationMaterial;
    private int   activeDefectTypeFromProfile = 0;
    private float activeIntensity             = 1.0f;

    void Awake()
    {
        Debug.Log(">>> ColorAnalyzer bereit");
    }

 public void SetSimulationParams(ColorDefectType defectType, float intensity)
{
    activeIntensity  = intensity;

    switch (defectType)
    {
        case ColorDefectType.Protanopia:
            activeDefectType = 0;

            // Protanopie-Matrix auf Simulationsmaterial setzen
            if (colorSimulationMaterial != null)
            {
                Matrix4x4 m = Matrix4x4.identity;
                m.SetRow(0, new Vector4(0.152286f,  1.052583f, -0.204868f, 0));
                m.SetRow(1, new Vector4(0.114503f,  0.786281f,  0.099216f, 0));
                m.SetRow(2, new Vector4(-0.003882f, -0.048116f,  1.051998f, 0));
                m.SetRow(3, new Vector4(0, 0, 0, 1));
                colorSimulationMaterial.SetMatrix("_ColorMatrix", m);
                colorSimulationMaterial.SetFloat("_Intensity", intensity);
            }
            break;

        case ColorDefectType.CataractYellow:
            activeDefectType = 1;

            if (colorSimulationMaterial != null)
            {
                Matrix4x4 m = Matrix4x4.identity;
                m.SetRow(0, new Vector4(1.0f,  0.0f, 0.0f, 0));
                m.SetRow(1, new Vector4(0.0f,  0.95f, 0.0f, 0));
                m.SetRow(2, new Vector4(0.0f,  0.0f, 0.4f, 0));
                m.SetRow(3, new Vector4(0, 0, 0, 1));
                colorSimulationMaterial.SetMatrix("_ColorMatrix", m);
                colorSimulationMaterial.SetFloat("_Intensity", intensity);
            }
            break;
    }

    Debug.Log($">>> ColorAnalyzer: {defectType} → intern={activeDefectType}, "
            + $"intensity={intensity}");
}

    public override void StartAnalysis(Action<AnalysisResult> callback)
    {
        onComplete    = callback;
        currentPlate  = 0;
        correctCount  = 0;
        testActive    = true;

        // Tafeln je nach Defekttyp generieren
        GeneratePlates();
        totalPlates = plates.Count;

        if (metaTestPanel != null) metaTestPanel.SetActive(true);

        if (instructionText != null)
            instructionText.text = GetInstructionText();

        BindButtons();
        ShowPlate();
    }

    string GetInstructionText()
    {
        if (activeDefectType == 0)
            return "Siehst du eine Figur\nim Farbfeld?\n\n<b>X</b> = Ja       <b>Y</b> = Nein";
        else
            return "Erscheint das Bild\ngelblich verfärbt?\n\n<b>X</b> = Ja       <b>Y</b> = Nein";
    }

    // ── Tafeln generieren ────────────────────────────────────────────
    void GeneratePlates()
    {
        plates.Clear();

        if (activeDefectType == 0) // Protanopie
        {
            // Pseudoisochromatische Tafeln nach Ishihara-Prinzip (1917)
            // Verwechslungslinien nach MacAdam (1942)
            // Rot-Grün-Paare die bei Protanopie nicht unterscheidbar sind

            // Tafel 1: Rot auf Grün – stark verwechselbar
            plates.Add(new ColorPlate
            {
                background   = new Color(0.2f, 0.6f, 0.2f), // Grün
                figure       = new Color(0.8f, 0.2f, 0.2f), // Rot
                correctAnswer = "Ja"
            });

            // Tafel 2: Orange auf Grün – mäßig verwechselbar
            plates.Add(new ColorPlate
            {
                background   = new Color(0.3f, 0.6f, 0.1f),
                figure       = new Color(0.9f, 0.5f, 0.1f),
                correctAnswer = "Ja"
            });

            // Tafel 3: Blau auf Violett – kaum verwechselbar bei Protanopie
            plates.Add(new ColorPlate
            {
                background   = new Color(0.5f, 0.1f, 0.7f),
                figure       = new Color(0.1f, 0.3f, 0.9f),
                correctAnswer = "Ja"
            });

            // Tafel 4: Rot auf Braun – stark verwechselbar
            plates.Add(new ColorPlate
            {
                background   = new Color(0.5f, 0.3f, 0.1f),
                figure       = new Color(0.8f, 0.1f, 0.1f),
                correctAnswer = "Ja"
            });

            // Tafel 5: Grün auf Gelb – mäßig verwechselbar
            plates.Add(new ColorPlate
            {
                background   = new Color(0.8f, 0.8f, 0.1f),
                figure       = new Color(0.2f, 0.7f, 0.2f),
                correctAnswer = "Ja"
            });

            // Tafel 6: Kontrollplatte – Blau auf Weiß immer erkennbar
            plates.Add(new ColorPlate
            {
                background   = new Color(0.9f, 0.9f, 0.9f),
                figure       = new Color(0.1f, 0.1f, 0.8f),
                correctAnswer = "Ja"
            });
        }
        else // CataractYellow
        {
            // Test für Gelbfärbung durch Katarakt
            // Wooten & Hammond (1983): Blaukanal selektiv gedämpft
            // Tafeln zeigen blaue Elemente die bei Gelbfärbung verschwinden

            // Tafel 1: Blau auf Weiß – stark betroffen
            plates.Add(new ColorPlate
            {
                background   = new Color(0.9f, 0.9f, 0.9f),
                figure       = new Color(0.1f, 0.1f, 0.9f),
                correctAnswer = "Ja"
            });

            // Tafel 2: Hellblau auf Weiß – stark betroffen
            plates.Add(new ColorPlate
            {
                background   = new Color(0.95f, 0.95f, 0.95f),
                figure       = new Color(0.4f, 0.6f, 0.9f),
                correctAnswer = "Ja"
            });

            // Tafel 3: Rot auf Weiß – kaum betroffen (Kontrollplatte)
            plates.Add(new ColorPlate
            {
                background   = new Color(0.9f, 0.9f, 0.9f),
                figure       = new Color(0.9f, 0.1f, 0.1f),
                correctAnswer = "Ja"
            });

            // Tafel 4: Gelb auf Weiß – kaum betroffen
            plates.Add(new ColorPlate
            {
                background   = new Color(0.9f, 0.9f, 0.9f),
                figure       = new Color(0.9f, 0.9f, 0.1f),
                correctAnswer = "Ja"
            });

            // Tafel 5: Blau auf Gelb – stark betroffen
            plates.Add(new ColorPlate
            {
                background   = new Color(0.9f, 0.8f, 0.1f),
                figure       = new Color(0.1f, 0.2f, 0.8f),
                correctAnswer = "Ja"
            });

            // Tafel 6: Violett auf Weiß – mäßig betroffen
            plates.Add(new ColorPlate
            {
                background   = new Color(0.9f, 0.9f, 0.9f),
                figure       = new Color(0.6f, 0.1f, 0.8f),
                correctAnswer = "Ja"
            });
        }
    }

    // ── Tafel anzeigen ───────────────────────────────────────────────
    void ShowPlate()
    {
        if (currentPlate >= plates.Count) { Finish(); return; }

        ColorPlate plate = plates[currentPlate];
        Texture2D  tex   = GeneratePlateTex(plate);

        // Simulation auf Tafel anwenden
        RenderTexture rt = ApplySimulationToPlate(tex);
        plateRTs.Add(rt);
        colorPlateDisplay.texture = rt;
        Destroy(tex);

        if (progressText != null)
            progressText.text = $"Tafel {currentPlate + 1} / {totalPlates}";

        Debug.Log($">>> ColorAnalyzer: Tafel {currentPlate + 1}");
    }

    Texture2D GeneratePlateTex(ColorPlate plate)
    {
        int       size   = 256;
        Texture2D tex    = new Texture2D(size, size);
        Color[]   pixels = new Color[size * size];

        // Hintergrund füllen
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = plate.background;

        // Figur als Kreis in der Mitte
        int cx = size / 2;
        int cy = size / 2;
        int r  = size / 4;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            if (dist < r)
                pixels[y * size + x] = plate.figure;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    RenderTexture ApplySimulationToPlate(Texture2D baseTex)
    {
        var rt = new RenderTexture(256, 256, 0);

        if (colorSimulationMaterial != null && activeIntensity > 0f)
        {
            Graphics.Blit(baseTex, rt, colorSimulationMaterial);
            Debug.Log($">>> Plate simuliert: defectType={activeDefectTypeFromProfile}");
        }
        else
        {
            Graphics.Blit(baseTex, rt);
        }
        return rt;
    }

    // ── Buttons ──────────────────────────────────────────────────────
    void BindButtons()
    {
        if (ActivePanel == null) return;
        var buttons = ActivePanel.GetComponentsInChildren<Button>();
        foreach (var btn in buttons)
        {
            btn.onClick.RemoveAllListeners();
            if (btn.name.Contains("Yes")) btn.onClick.AddListener(OnYes);
            if (btn.name.Contains("No"))  btn.onClick.AddListener(OnNo);
        }
    }

    void OnYes()
    {
        // Figur erkannt
        ColorPlate plate = plates[currentPlate];
        if (plate.correctAnswer == "Ja") correctCount++;
        currentPlate++;
        ShowPlate();
    }

    void OnNo()
    {
        // Figur nicht erkannt
        ColorPlate plate = plates[currentPlate];
        if (plate.correctAnswer == "Nein") correctCount++;
        currentPlate++;
        ShowPlate();
    }

    // ── Ergebnis berechnen ───────────────────────────────────────────
    void Finish()
    {
        testActive = false;
        ActivePanel.SetActive(false);

        // Fehlerrate berechnen
        // Bei Protanopie: hohe Fehlerrate bei Rot-Grün-Tafeln
        // Bei Katarakt: hohe Fehlerrate bei Blau-Tafeln
        float errorRate = 1.0f - (float)correctCount / totalPlates;
        float severity  = Mathf.Clamp01(errorRate);

        // Spezifische Parameter je Defekttyp
        float redGreenAxis = 0f;
        float yellowBias   = 0f;

        if (activeDefectType == 0) // Protanopie
        {
            // Fehlerrate direkt als Rot-Grün-Achsen-Stärke
            // MacAdam (1942): Verwechslungslinien entlang Rot-Grün-Achse
            redGreenAxis = severity;
        }
        else // CataractYellow
        {
            // Fehlerrate bei Blau-Tafeln als Gelbfärbungsstärke
            // Wooten & Hammond (1983): selektive Blaudämpfung
            yellowBias = severity;
        }

        var result = new AnalysisResult
        {
            effectType = TargetEffect,
            severity   = severity,
            confidence = 0.85f
        };
        result.parameters["defectType"]   = (float)activeDefectType;
        result.parameters["severity"]     = severity;
        result.parameters["errorRate"]    = errorRate;
        result.parameters["redGreenAxis"] = redGreenAxis;
        result.parameters["yellowBias"]   = yellowBias;

        Debug.Log($">>> Color-Analyse: defectType={activeDefectType}, "
                + $"correct={correctCount}/{totalPlates}, "
                + $"errorRate={errorRate:F2}, severity={severity:F2}");

        onComplete?.Invoke(result);
    }

    void Update()
    {
        if (!testActive) return;
        if (OVRInput.GetDown(OVRInput.RawButton.X)) { Debug.Log(">>> X: Ja");   OnYes(); }
        if (OVRInput.GetDown(OVRInput.RawButton.Y)) { Debug.Log(">>> Y: Nein"); OnNo();  }
    }

    public override void Cleanup()
    {
        testActive = false;
        if (metaTestPanel  != null) metaTestPanel.SetActive(false);
        if (appleTestPanel != null) appleTestPanel.SetActive(false);
        foreach (var rt in plateRTs) if (rt != null) rt.Release();
        plateRTs.Clear();
    }
}