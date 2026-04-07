using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ContrastAnalyzer : InteractiveAnalyzer
{
    public override VisualEffectType TargetEffect => VisualEffectType.Contrast;

    [Header("Platform UI")]
    public GameObject metaTestPanel;
    public GameObject appleTestPanel;

    [Header("UI Elemente")]
    public TextMeshProUGUI instructionText;
    public RawImage leftPatch;
    public RawImage rightPatch;
    public TextMeshProUGUI progressText;

    [Header("Simulation auf Patches")]
    public Material contrastSimulationMaterial;

    // ── Plattform-Helper ─────────────────────────────────────────────
    private GameObject ActivePanel => Application.platform == RuntimePlatform.Android
        ? metaTestPanel : appleTestPanel;

    // ── Test-Zustand ─────────────────────────────────────────────────
    private float[] contrastLevels;
    private int   currentStep;
    private int   consecutiveFails;
    private float lastSeenContrast;
    private bool  testActive;
    private float activeContrastFactor = 1.0f;

    private List<RenderTexture> patchRTs = new List<RenderTexture>();
    private Action<AnalysisResult> onComplete;

    // ── Awake ────────────────────────────────────────────────────────
    void Awake()
    {
        // Pelli et al. (1988): 0.15 log-Einheiten pro Schritt
        // Bereich 0.0 bis -1.65 log → 12 Schritte
        contrastLevels = GenerateLogSteps(0.0f, -1.65f, 0.15f);
        Debug.Log($">>> ContrastLevels generiert: {contrastLevels.Length} Schritte");
    }

    static float[] GenerateLogSteps(float startLog, float endLog, float stepSize)
    {
        var steps = new List<float>();
        for (float log = startLog; log >= endLog; log -= stepSize)
        {
            float contrast = Mathf.Pow(10f, log);
            steps.Add(Mathf.Round(contrast * 1000f) / 1000f);
        }
        return steps.ToArray();
    }

void GenerateLevelsForSimulation()
{
    if (activeContrastFactor >= 1.0f)
    {
        // Keine Simulation: Standard Pelli-Robson 0.15
        contrastLevels = GenerateLogSteps(0.0f, -1.65f, 0.15f);
        return;
    }

    // Mit Simulation: größere Schrittweite damit Unterschiede
    // schneller unter die Wahrnehmungsschwelle fallen
    // Bei cf=0.25 soll Nutzer bei Schritt 4-6 aufhören
    //
    // Schrittweite basiert auf contrastFactor:
    // Starker Verlust (cf niedrig) → große Schritte → früh aufhören
    // Schwacher Verlust (cf hoch)  → kleine Schritte → spät aufhören
    //
    // Formel: stepSize = 0.15 / contrastFactor
    // cf=1.0  → 0.15 (Standard)
    // cf=0.5  → 0.30
    // cf=0.25 → 0.60
    float stepSize = Mathf.Clamp(0.15f / activeContrastFactor, 0.15f, 0.80f);

    // 12 Schritte fest – Endpunkt ergibt sich aus Schrittweite
    float endLog = -(stepSize * 11f);

    contrastLevels = GenerateLogSteps(0.0f, endLog, stepSize);

    Debug.Log($">>> ContrastLevels: {contrastLevels.Length} Schritte, "
            + $"stepSize={stepSize:F3}, endLog={endLog:F2}, "
            + $"contrastFactor={activeContrastFactor}");
}

   public void SetSimulationParams(float contrastFactor)
{
    activeContrastFactor = contrastFactor;
    GenerateLevelsForSimulation(); // Levels neu berechnen
    Debug.Log($">>> ContrastAnalyzer: contrastFactor={contrastFactor}, "
            + $"Levels={contrastLevels.Length}");
}

    // ── Einstiegspunkt ───────────────────────────────────────────────
    public override void StartAnalysis(Action<AnalysisResult> callback)
{
    Debug.Log($">>> ContrastAnalyzer.StartAnalysis – currentStep VOR Reset: {currentStep}");
    
    onComplete       = callback;
    currentStep      = 0;  // ← wird das wirklich ausgeführt?
    consecutiveFails = 0;
    lastSeenContrast = 1.0f;
    testActive       = true;

    Debug.Log($">>> ContrastAnalyzer.StartAnalysis – currentStep NACH Reset: {currentStep}");

    // DEBUG
    Debug.Log(">>> ContrastAnalyzer.StartAnalysis aufgerufen");
    Debug.Log($">>> metaTestPanel: {metaTestPanel}");
    Debug.Log($">>> Plattform: {Application.platform}");
    Debug.Log($">>> Ist Android: {Application.platform == RuntimePlatform.Android}");

    if (metaTestPanel != null)
    {
        metaTestPanel.SetActive(true);
        Debug.Log($">>> metaTestPanel activeSelf: {metaTestPanel.activeSelf}");
        Debug.Log($">>> metaTestPanel activeInHierarchy: {metaTestPanel.activeInHierarchy}");
    }
    else
        Debug.LogWarning(">>> metaTestPanel ist nicht zugewiesen!");

   

    if (instructionText != null)
        instructionText.text = "Siehst du einen Unterschied\n"
                             + "zwischen den zwei Feldern?\n\n";
    BindButtons();
    ShowStep();

    // NEU – nach allem anderen:
    Debug.Log($">>> Nach ShowStep – Panel Status:");
    Debug.Log($">>> metaTestPanel activeSelf: {metaTestPanel?.activeSelf}");
    Debug.Log($">>> metaTestPanel activeInHierarchy: {metaTestPanel?.activeInHierarchy}");
    Debug.Log($">>> leftPatch: {leftPatch}");
    Debug.Log($">>> rightPatch: {rightPatch}");
    Debug.Log($">>> leftPatch texture: {leftPatch?.texture}");
    Debug.Log($">>> rightPatch texture: {rightPatch?.texture}");
    Debug.Log($">>> leftPatch activeInHierarchy: {leftPatch?.gameObject.activeInHierarchy}");
}

    // ── Buttons binden ───────────────────────────────────────────────
    void BindButtons()
    {
        if (ActivePanel == null)
        {
            Debug.LogWarning(">>> ActivePanel ist null!");
            return;
        }

        var buttons = ActivePanel.GetComponentsInChildren<Button>();
        Debug.Log($">>> BindButtons – {buttons.Length} Buttons in {ActivePanel.name}");

        foreach (var btn in buttons)
        {
            btn.onClick.RemoveAllListeners();
            if (btn.name.Contains("Yes")) btn.onClick.AddListener(OnYes);
            if (btn.name.Contains("No"))  btn.onClick.AddListener(OnNo);
        }
    }

    // ── Schritt anzeigen ─────────────────────────────────────────────
void ShowStep()
{
    if (currentStep >= contrastLevels.Length) { Finish(); return; }

    float c = contrastLevels[currentStep];

    // Kein adjustedC – Patches werden direkt durch Simulation gedämpft
    // Das ist der wissenschaftlich korrekte Ansatz:
    // Der Test misst die Wahrnehmungsschwelle UNTER der Simulation
    Texture2D baseLeft  = MakePatch(new Color(0.5f, 0.5f, 0.5f));
    Texture2D baseRight = MakePatch(new Color(
        0.5f + c * 0.5f,
        0.5f + c * 0.5f,
        0.5f + c * 0.5f));

    if (leftPatch  != null) leftPatch.texture  = ApplySimulationToPatch(baseLeft);
    if (rightPatch != null) rightPatch.texture = ApplySimulationToPatch(baseRight);

    if (progressText != null)
        progressText.text = $"Schritt {currentStep + 1} / {contrastLevels.Length}";

    Destroy(baseLeft);
    Destroy(baseRight);
}

    // ── Patch-Hilfsmethoden ──────────────────────────────────────────
    Texture2D MakePatch(Color color)
    {
        var tex = new Texture2D(128, 128);
        var px  = new Color[128 * 128];
        for (int i = 0; i < px.Length; i++) px[i] = color;
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    RenderTexture ApplySimulationToPatch(Texture2D baseTex)
{
    var rt = new RenderTexture(128, 128, 0);
    patchRTs.Add(rt);

    if (contrastSimulationMaterial != null && activeContrastFactor < 1.0f)
    {
        contrastSimulationMaterial.SetFloat("_ContrastFactor", activeContrastFactor);
        Graphics.Blit(baseTex, rt, contrastSimulationMaterial);

        // Prüfe ob Simulation wirkt
        RenderTexture.active = rt;
        Texture2D check = new Texture2D(1, 1);
        check.ReadPixels(new Rect(64, 64, 1, 1), 0, 0);
        check.Apply();
        RenderTexture.active = null;
        Debug.Log($">>> Patch nach Simulation: {check.GetPixel(0,0)}, "
                + $"contrastFactor={activeContrastFactor}");
        Destroy(check);
    }
    else
    {
        Debug.LogWarning($">>> KEINE Simulation angewendet: "
                       + $"material={contrastSimulationMaterial != null}, "
                       + $"factor={activeContrastFactor}");
        Graphics.Blit(baseTex, rt);
    }
    return rt;
}

    // ── Antworten ────────────────────────────────────────────────────
    void OnYes()
    {
        lastSeenContrast = contrastLevels[currentStep];
        consecutiveFails = 0;
        currentStep++;
        ShowStep();
    }

    void OnNo()
    {
        consecutiveFails++;
        if (consecutiveFails >= 2) { Finish(); return; }
        currentStep++;
        ShowStep();
    }

    // ── Ergebnis berechnen ───────────────────────────────────────────
    void Finish()
{
    testActive = false;
    ActivePanel.SetActive(false);

    // Relative Position im Test
    // currentStep = Schritt bei dem aufgehört wurde
    // 0 = sofort aufgehört = maximale Beeinträchtigung
    // contrastLevels.Length-1 = letzter Schritt = minimale Beeinträchtigung

    // lastSeenIndex = letzter Schritt bei dem Ja gedrückt wurde
    int lastSeenIndex = System.Array.IndexOf(contrastLevels, lastSeenContrast);
    if (lastSeenIndex < 0) lastSeenIndex = currentStep - 1;

    // severity: 0 Schritte geschafft → severity=1.0
    //           alle Schritte geschafft → severity=0.0
    float severity = 1.0f - Mathf.Clamp01(
        (float)lastSeenIndex / (contrastLevels.Length - 1)
    );

    // GammaEstimate aus severity
    float gammaEstimate = Mathf.Lerp(1.0f, 2.5f, severity);

    // logThreshold für wissenschaftliche Dokumentation
    float logThreshold = Mathf.Log10(lastSeenContrast);

    var result = new AnalysisResult
    {
        effectType = TargetEffect,
        severity   = severity,
        confidence = 0.9f
    };
    result.parameters["threshold"]     = lastSeenContrast;
    result.parameters["logThreshold"]  = logThreshold;
    result.parameters["gammaEstimate"] = gammaEstimate;
    result.parameters["channelBias"]   = 0f;

    Debug.Log($">>> Kontrast-Analyse: threshold={lastSeenContrast:F3} "
            + $"| lastSeenIndex={lastSeenIndex} / {contrastLevels.Length-1} "
            + $"| severity={severity:F2} "
            + $"| gamma={gammaEstimate:F2}");

    onComplete?.Invoke(result);
}

    // ── Controller Input ─────────────────────────────────────────────
    void Update()
    {
        if (!testActive) return;
        if (OVRInput.GetDown(OVRInput.RawButton.X)) { Debug.Log(">>> X: Ja");   OnYes(); }
        if (OVRInput.GetDown(OVRInput.RawButton.Y)) { Debug.Log(">>> Y: Nein"); OnNo();  }
    }

    // ── Cleanup ──────────────────────────────────────────────────────
    public override void Cleanup()
    {
        testActive = false;
        if (metaTestPanel  != null) metaTestPanel.SetActive(false);
        if (appleTestPanel != null) appleTestPanel.SetActive(false);
        foreach (var rt in patchRTs) if (rt != null) rt.Release();
        patchRTs.Clear();
    }
}