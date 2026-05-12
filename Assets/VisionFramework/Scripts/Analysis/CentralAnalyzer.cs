using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

// Sub-Analyzer für zentrale Gesichtsfeldausfälle (Central Scotoma)
[System.Serializable]
public class CentralAnalyzer
{
    private TextMeshProUGUI progressText;
    private Action<AnalysisResult> onComplete;
    private EffectPipelineManager pipeline;

    private bool isRunning = false;
    private int currentPoint = 0;
    private int consecutiveFails = 0;

    private List<Vector2> testPoints = new List<Vector2>();
    private List<bool> pointsSeen = new List<bool>();

    // Testradien von außen nach innen – Humphrey Visual Field Test (Anderson 1992)
    private static readonly float[] radiiNorm = { 0.40f, 0.30f, 0.20f, 0.15f, 0.10f, 0.05f };
    private const int directions = 8;

    // Startet den Central-Scotoma-Test
    public void Start(
        EffectPipelineManager pipeline,
        TextMeshProUGUI progressText,
        float simulatedRadius,
        Action<AnalysisResult> callback)
    {
        this.pipeline = pipeline;
        this.progressText = progressText;
        this.onComplete = callback;

        currentPoint = 0;
        consecutiveFails = 0;
        isRunning = true;

        testPoints.Clear();
        pointsSeen.Clear();

        GenerateTestPoints();
        ShowPoint();
    }

    // Generiert alle Testpunkte in konzentrischen Ringen
    void GenerateTestPoints()
    {
        for (int r = 0; r < radiiNorm.Length; r++)
            for (int d = 0; d < directions; d++)
            {
                float angle = d * (360f / directions) * Mathf.Deg2Rad;
                testPoints.Add(new Vector2(
                    0.5f + Mathf.Cos(angle) * radiiNorm[r],
                    0.5f + Mathf.Sin(angle) * radiiNorm[r]));
                pointsSeen.Add(false);
            }
    }

    // Zeigt den aktuellen Testpunkt
    void ShowPoint()
    {
        if (currentPoint >= testPoints.Count) { Finish(); return; }

        Vector2 pos = testPoints[currentPoint];
        Texture2D tex = GeneratePointTexture(pos);

        if (pipeline?.testOverlayManager != null)
            pipeline.SetTestOverlay(tex, pipeline.testOverlayManager.centralOverlayMaterial);

        if (progressText != null)
            progressText.text = $"Punkt {currentPoint + 1} / {testPoints.Count}";
    }

    // Generiert Textur mit blauem Testpunkt an normalisierter Position
    Texture2D GeneratePointTexture(Vector2 normalizedPos)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        int px = Mathf.RoundToInt(normalizedPos.x * size);
        int py = Mathf.RoundToInt(normalizedPos.y * size);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (Vector2.Distance(new Vector2(x, y), new Vector2(px, py)) < 12)
                    pixels[y * size + x] = new Color(0, 0, 1, 1);
            }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
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

    // Callback für "Ja" - Punkt sichtbar
    public void OnYes()
    {
        if (!isRunning) return;
        pointsSeen[currentPoint] = true;
        consecutiveFails = 0;
        currentPoint++;
        ShowPoint();
    }

    // Callback für "Nein" - Punkt nicht sichtbar
    public void OnNo()
    {
        if (!isRunning) return;
        pointsSeen[currentPoint] = false;
        consecutiveFails++;

        if (consecutiveFails >= 2) { Finish(); return; }

        currentPoint++;
        ShowPoint();
    }

    // Beendet den Test und berechnet das Ergebnis
    void Finish()
    {
        isRunning = false;
        pipeline?.ClearTestOverlay();

        float lastSeenRadius = 0f;
        for (int r = 0; r < radiiNorm.Length; r++)
        {
            int seenInRing = 0;
            for (int d = 0; d < directions; d++)
            {
                int idx = r * directions + d;
                if (idx < pointsSeen.Count && pointsSeen[idx])
                    seenInRing++;
            }
            if (seenInRing >= directions / 2)
                lastSeenRadius = radiiNorm[r];
        }

        float maxRadius = radiiNorm[0]; 
        float severity = 1.0f - Mathf.Clamp01(lastSeenRadius / maxRadius);
        float scotomRadiusUV = lastSeenRadius;

        var result = new AnalysisResult
        {
            effectType = VisualEffectType.FieldOfView,
            severity = severity,
        };
        result.parameters["lastSeenRadius"] = lastSeenRadius;
        result.parameters["scotomRadiusUV"] = scotomRadiusUV;
        result.parameters["severity"] = severity;
        result.parameters["defectType"] = 0f;

        onComplete?.Invoke(result);
    }


    // Räumt den Analyzer auf
    public void Cleanup()
    {
        isRunning = false;
        pipeline?.ClearTestOverlay();
        testPoints.Clear();
        pointsSeen.Clear();
    }
}