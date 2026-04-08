using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlurAnalyzer : InteractiveAnalyzer
{
    public override VisualEffectType TargetEffect => VisualEffectType.Blur;

    [Header("Platform UI")]
    public GameObject metaTestPanel;
    public GameObject appleTestPanel;

    [Header("UI Elemente")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI stimulusText;      
    public TextMeshProUGUI progressText;
    public Button yesButton;               
    public Button noButton;                  

    [Header("Simulation auf Stimulus")]
    public Material blurSimulationMaterial;  
    public RawImage stimulusDisplay;          

    [Header("Stimulus Kamera")]
    public Camera     stimulusCamera;     
    public RenderTexture stimulusSourceRT;  
    public RenderTexture stimulusBlurredRT; 
    private float[] sizeLevels;
    private string[] optotypes = { "E", "F", "P", "T", "O", "Z", "L", "D" };

    private int   currentStep;
    private int   consecutiveFails;
    private float lastSeenSize;
    private float activeBlurStrength = 1.0f;
    private bool  testActive = false;

    

    private List<RenderTexture> stimulusRTs = new List<RenderTexture>();
    private Action<AnalysisResult> onComplete;

    private GameObject ActivePanel => Application.platform == RuntimePlatform.Android
        ? metaTestPanel : appleTestPanel;

    void Awake()
    {
        sizeLevels = GenerateLogSizes(1.0f, 0.05f, 0.1f);

        Debug.Log($">>> BlurAnalyzer: {sizeLevels.Length} Größenstufen generiert");
        foreach (var s in sizeLevels)
            Debug.Log($"    Größe: {s:F3}");
    }

    static float[] GenerateLogSizes(float startSize, float endSize, float stepSize)
    {
        var steps = new List<float>();
        float logStart = Mathf.Log10(startSize); 
        float logEnd   = Mathf.Log10(endSize);   
        for (float log = logStart; log >= logEnd; log -= stepSize)
        {
            float size = Mathf.Pow(10f, log);
            steps.Add(Mathf.Round(size * 1000f) / 1000f);
        }
        return steps.ToArray();
    }

    public void SetSimulationParams(float blurStrength)
    {
        activeBlurStrength = blurStrength;
        Debug.Log($">>> BlurAnalyzer: blurStrength = {blurStrength}");
    }

/*
    // ── Einstiegspunkt ───────────────────────────────────────────────
    public override void StartAnalysis(Action<AnalysisResult> callback)
    {
        onComplete       = callback;
        currentStep      = 0;
        consecutiveFails = 0;
        lastSeenSize     = sizeLevels[0];
        testActive       = true;

        Debug.Log(">>> BlurAnalyzer.StartAnalysis aufgerufen");
        Debug.Log($">>> metaTestPanel: {metaTestPanel}");
        Debug.Log($">>> appleTestPanel: {appleTestPanel}");

        Debug.Log($">>> Plattform: {Application.platform}");
        Debug.Log($">>> Ist Android: {Application.platform == RuntimePlatform.Android}");

        if (metaTestPanel  != null)
    {
            metaTestPanel.SetActive(Application.platform == RuntimePlatform.Android);
            Debug.Log($">>> Panel aktiviert: {metaTestPanel.name}");
            Debug.Log($">>> activeSelf: {metaTestPanel.activeSelf}");
            Debug.Log($">>> activeInHierarchy: {metaTestPanel.activeInHierarchy}");
    }
        if (appleTestPanel != null)
            appleTestPanel.SetActive(Application.platform == RuntimePlatform.IPhonePlayer);

        if (instructionText != null)
            instructionText.text = "Kannst du den Buchstaben noch lesen?\n\n"
                                 + "<b>X</b> = Ja       <b>Y</b> = Nein";
        BindButtons();
        ShowStep();
    }*/

public override void StartAnalysis(Action<AnalysisResult> callback)
{
    onComplete       = callback;
    currentStep      = 0;
    consecutiveFails = 0;
    lastSeenSize     = sizeLevels[0];
    testActive       = true;

    metaTestPanel.SetActive(true);

    if (instructionText != null)
        instructionText.text = "Kannst du den Buchstaben noch erkennen?";

    BindButtons();
    ShowStep();
}

    void BindButtons()
    {
        if (ActivePanel == null) { Debug.LogWarning(">>> ActivePanel null"); return; }
        var buttons = ActivePanel.GetComponentsInChildren<Button>();
        foreach (var btn in buttons)
        {
            btn.onClick.RemoveAllListeners();
            if (btn.name.Contains("Yes")) btn.onClick.AddListener(OnYes);
            if (btn.name.Contains("No"))  btn.onClick.AddListener(OnNo);
        }
    }

  void ShowStep()
{
    if (currentStep >= sizeLevels.Length) { Finish(); return; }

    float size    = sizeLevels[currentStep];
    string letter = optotypes[UnityEngine.Random.Range(0, optotypes.Length)];

    if (stimulusText != null)
    {
        stimulusText.text     = letter;
        stimulusText.fontSize = size * 200f;
    }

    if (progressText != null)
        progressText.text = $"Schritt {currentStep + 1} / {sizeLevels.Length}";

    Debug.Log($">>> Zeige Buchstabe '{letter}' in Größe {size:F3}");

    StartCoroutine(ApplyBlurNextFrame());
}
    void OnYes()
    {
        lastSeenSize     = sizeLevels[currentStep];
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

void Finish()
{
    testActive = false;
    ActivePanel.SetActive(false);

    float severity = 1.0f - Mathf.Clamp01(
        (lastSeenSize - 0.05f) / (1.0f - 0.05f)
    );

    float compensationStrength = MapSizeToCompensation(lastSeenSize);

    var result = new AnalysisResult
    {
        effectType = TargetEffect,
        severity   = severity,
        confidence = 0.85f
    };
    result.parameters["lastSeenSize"]         = lastSeenSize;
    result.parameters["severity"]             = severity;
    result.parameters["compensationStrength"] = compensationStrength;

    Debug.Log($">>> Blur-Analyse: lastSeenSize={lastSeenSize:F3}, "
            + $"severity={severity:F2}, "
            + $"compensationStrength={compensationStrength:F2}");

    onComplete?.Invoke(result);
}

float MapSizeToCompensation(float lastSeenSize)
{
    float normalized = Mathf.Clamp01(
        (lastSeenSize - 0.05f) / (1.0f - 0.05f)
    );

    float t = Mathf.Pow(normalized, 0.5f);

    return Mathf.Lerp(0f, 2.5f, t);
}

    void Update()
    {
        if (!testActive) return;
        if (OVRInput.GetDown(OVRInput.RawButton.X)) { Debug.Log(">>> X: Ja"); OnYes(); }
        if (OVRInput.GetDown(OVRInput.RawButton.Y)) { Debug.Log(">>> Y: Nein"); OnNo(); }
    }

    public override void Cleanup()
    {
        testActive = false;
        if (metaTestPanel  != null) metaTestPanel.SetActive(false);
        if (appleTestPanel != null) appleTestPanel.SetActive(false);
        foreach (var rt in stimulusRTs) if (rt != null) rt.Release();
        stimulusRTs.Clear();
    }

System.Collections.IEnumerator ApplyBlurNextFrame()
{
    yield return new WaitForEndOfFrame();

    RenderTexture.active = stimulusSourceRT;
    GL.Clear(true, true, new Color(1f, 1f, 1f, 1f)); 
    RenderTexture.active = null;

    if (blurSimulationMaterial != null
        && stimulusBlurredRT    != null
        && activeBlurStrength   > 0f)
    {
        blurSimulationMaterial.SetFloat("_BlurStrength", activeBlurStrength);
        Graphics.Blit(stimulusSourceRT, stimulusBlurredRT, blurSimulationMaterial);
        stimulusDisplay.texture = stimulusBlurredRT;
    }
    else
    {
        stimulusDisplay.texture = stimulusSourceRT;
    }
}
}
