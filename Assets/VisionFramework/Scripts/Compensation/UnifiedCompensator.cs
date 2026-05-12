using System.Collections.Generic;
using UnityEngine;

// Vereinheitlicht alle Kompensationsstrategien
// Wendet verschiedene Kompensationstechniken an um Sehbeeinträchtigungen auszugleichen:
public class UnifiedCompensator : MonoBehaviour
{
    [Header("Kompensations-Shader")]
    public Material spatialFilterMaterial;
    public Material globalAdjustmentMaterial;
    public Material colorCorrectionMaterial;
    public Material fovMaterial;

    private Texture originalTexture;

    // Setzt die originale Eingabetextur (Minifikation)
    public void SetOriginalTexture(Texture tex)
    {
        originalTexture = tex;
    }

    // Wendet alle verfügbaren Kompensationsstrategien auf Basis der Analyseergebnisse an
    public void ApplyAll(Dictionary<VisualEffectType, AnalysisResult> results)
    {
        float compensationStrength = 0f;
        float severity = 0f;
        float gammaEstimate = 1.0f;
        float channelBias = 0f;
        float redGreenComp = 0f;
        float yellowBias = 0f;
        float defectType = 0f;
        float fovSeverity = 0f;
        float fovDefectType = 0f;
        float artSeverity = 0f;

        if (results.TryGetValue(VisualEffectType.Blur, out var blurRes))
            compensationStrength = GetParam(blurRes, "compensationStrength");

        if (results.TryGetValue(VisualEffectType.Contrast, out var contrastRes))
        {
            severity = contrastRes.severity;
            gammaEstimate = GetParam(contrastRes, "gammaEstimate", 1.0f);
            channelBias = GetParam(contrastRes, "channelBias");
        }

        if (results.TryGetValue(VisualEffectType.ColorChange, out var colorRes))
        {
            defectType = GetParam(colorRes, "defectType");
            redGreenComp = GetParam(colorRes, "redGreenAxis");
            yellowBias = GetParam(colorRes, "yellowBias");
            channelBias = GetParam(colorRes, "channelBias");
        }

        if (results.TryGetValue(VisualEffectType.Artifacts, out var artRes))
            artSeverity = artRes.severity;

        if (!results.ContainsKey(VisualEffectType.FieldOfView))
        {
            fovMaterial?.SetFloat("_FOVDefectType", -1f);
        }
        else if (results.TryGetValue(VisualEffectType.FieldOfView, out var fovRes))
        {
            fovDefectType = GetParam(fovRes, "defectType");
            fovSeverity = fovRes.severity;

            float distSeverity = results.TryGetValue(
                VisualEffectType.Distortion, out var distResInner)
                ? distResInner.severity : 0f;

            ApplyFOVMaterial(fovRes, fovDefectType, fovSeverity, distSeverity);
        }

        float scatteredBoost = fovDefectType > 1.5f ? fovSeverity : 0f;
        float edgeStrength = compensationStrength * 3.0f;

        if (results.TryGetValue(VisualEffectType.Distortion, out var distRes))
            edgeStrength += distRes.severity * 3.0f;

        if (fovDefectType > 1.5f && fovSeverity > 0f)
            edgeStrength += fovSeverity * 3.0f;

        edgeStrength += artSeverity * 6.0f;
        float localContrast = compensationStrength * 0.8f;

        if (results.TryGetValue(VisualEffectType.Distortion, out var distResLocal)
            && distResLocal.severity > 0.2f)
            localContrast += distResLocal.severity * 0.5f;

        if (artSeverity > 0.1f)
            localContrast += artSeverity * 0.3f;

        float globalContrast = 1.0f + severity * 2.0f;
        float gammaCorrection = 1.0f / Mathf.Max(gammaEstimate, 0.01f);
        float saturation = 1.0f + severity * 0.4f + scatteredBoost * 0.2f + compensationStrength * 0.1f;
        float blueBoost = Mathf.Lerp(1.0f, 1.6f, channelBias);

        if (spatialFilterMaterial != null)
        {
            spatialFilterMaterial.SetFloat("_EdgeStrength", edgeStrength);
            spatialFilterMaterial.SetFloat("_LocalContrast", localContrast);
            spatialFilterMaterial.SetFloat("_CompensationStrength", compensationStrength);
        }

        if (globalAdjustmentMaterial != null)
        {
            globalAdjustmentMaterial.SetFloat("_GammaContrast", gammaCorrection);
            globalAdjustmentMaterial.SetFloat("_GlobalContrast", globalContrast);
            globalAdjustmentMaterial.SetFloat("_Saturation", saturation);
            globalAdjustmentMaterial.SetFloat("_BlueBoost", blueBoost);
        }

        if (colorCorrectionMaterial != null)
        {
            colorCorrectionMaterial.SetFloat("_DefectType", defectType);
            colorCorrectionMaterial.SetFloat("_RedGreenComp", redGreenComp);
            colorCorrectionMaterial.SetFloat("_YellowBias", yellowBias);
        }
    }

    // Übergibt abhängig vom Gesichtsfeldausfall-Typ die relevanten Parameter
    void ApplyFOVMaterial(AnalysisResult fovRes,
                          float fovDefectType,
                          float fovSeverity,
                          float distSeverity)
    {
        if (fovMaterial == null) return;

        if (fovDefectType < 0.5f)
        {
            float scotomRadius = GetParam(fovRes, "scotomRadius", 0.15f);
            float combinedEdge = 2.0f + fovSeverity * 2.0f + distSeverity * 2.0f;

            fovMaterial.SetFloat("_FOVDefectType", fovDefectType);
            fovMaterial.SetFloat("_ScotomRadius", scotomRadius);
            fovMaterial.SetFloat("_EdgeWidth", 0.05f);
            fovMaterial.SetFloat("_EdgeStrength", combinedEdge);
            fovMaterial.SetFloat("_ContrastBoost", fovSeverity * 0.8f);
        }
        else if (fovDefectType < 1.5f)
        {
            float tunnelRadius = GetParam(fovRes, "tunnelRadiusUV", 0.3f);

            fovMaterial.SetFloat("_FOVDefectType", fovDefectType);
            fovMaterial.SetFloat("_TunnelRadius", tunnelRadius);
            fovMaterial.SetFloat("_TunnelSoftness", 0.02f);
        }
        else
        {
            fovMaterial.SetFloat("_FOVDefectType", -1f);
        }
    }

    // Rendering-Pipeline: Wendet alle Kompensationsshader nacheinander an
    public void Process(RenderTexture src, RenderTexture dst)
    {
        RenderTexture temp1 = RenderTexture.GetTemporary(
            src.width, src.height, 0, src.format);
        RenderTexture temp2 = RenderTexture.GetTemporary(
            src.width, src.height, 0, src.format);
        RenderTexture temp3 = RenderTexture.GetTemporary(
            src.width, src.height, 0, src.format);

        bool spatialActive = spatialFilterMaterial != null &&
            (spatialFilterMaterial.GetFloat("_EdgeStrength") > 0.01f ||
             spatialFilterMaterial.GetFloat("_LocalContrast") > 0.01f);

        Blit(src, temp1, spatialActive ? spatialFilterMaterial : null);

        bool globalActive = globalAdjustmentMaterial != null &&
            (globalAdjustmentMaterial.GetFloat("_GlobalContrast") > 1.01f ||
             globalAdjustmentMaterial.GetFloat("_GammaContrast") < 0.99f ||
             globalAdjustmentMaterial.GetFloat("_Saturation") > 1.01f ||
             globalAdjustmentMaterial.GetFloat("_BlueBoost") > 1.01f);

        Blit(temp1, temp2, globalActive ? globalAdjustmentMaterial : null);

        bool colorActive = colorCorrectionMaterial != null &&
            (colorCorrectionMaterial.GetFloat("_RedGreenComp") > 0.01f ||
             colorCorrectionMaterial.GetFloat("_YellowBias") > 0.01f);

        Blit(temp2, temp3, colorActive ? colorCorrectionMaterial : null);

        float fovType = fovMaterial != null
            ? fovMaterial.GetFloat("_FOVDefectType") : -1f;

        if (fovMaterial != null && fovType >= 0f && fovType < 0.5f)
        {
            Graphics.Blit(temp3, dst, fovMaterial);
        }
        else if (fovMaterial != null && fovType >= 0.5f
                 && fovType < 1.5f && originalTexture != null)
        {
            fovMaterial.SetTexture("_OriginalTex", originalTexture);
            Graphics.Blit(temp3, dst, fovMaterial);
        }
        else
        {
            Graphics.Blit(temp3, dst);
        }

        RenderTexture.ReleaseTemporary(temp1);
        RenderTexture.ReleaseTemporary(temp2);
        RenderTexture.ReleaseTemporary(temp3);
    }

    float GetParam(AnalysisResult res, string key, float fallback = 0f)
        => res.parameters.ContainsKey(key) ? res.parameters[key] : fallback;

    void Blit(RenderTexture src, RenderTexture dst, Material mat)
    {
        if (mat != null)
            Graphics.Blit(src, dst, mat);
        else
            Graphics.Blit(src, dst);
    }
}