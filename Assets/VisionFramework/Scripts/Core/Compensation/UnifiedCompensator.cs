using UnityEngine;
using System.Collections.Generic;

public class UnifiedCompensator : MonoBehaviour
{
    public Material material;

// UnifiedCompensator.cs – ApplyAll()
public void ApplyAll(Dictionary<VisualEffectType, AnalysisResult> results)
{
    float compensationStrength = 0f;
    float severity             = 0f;
    float gammaEstimate        = 1.0f;
    float channelBias          = 0f;

    if (results.TryGetValue(VisualEffectType.Blur, out var blurRes))
        compensationStrength = blurRes.parameters.ContainsKey("compensationStrength")
            ? blurRes.parameters["compensationStrength"] : 0f;

    if (results.TryGetValue(VisualEffectType.Contrast, out var contrastRes))
    {
        severity      = contrastRes.severity;
        gammaEstimate = contrastRes.parameters.ContainsKey("gammaEstimate")
            ? contrastRes.parameters["gammaEstimate"] : 1.0f;
        channelBias   = contrastRes.parameters.ContainsKey("channelBias")
            ? contrastRes.parameters["channelBias"] : 0f;
    }

    // Blur-Test → Shader
    float edgeStrength  = compensationStrength * 3.0f;
    float localContrast = compensationStrength * 0.5f;

    // Kontrast-Test → Shader
    float gammaCorrection = 1.0f / Mathf.Max(gammaEstimate, 0.01f);
    float globalContrast  = 1.0f + severity * 2.0f;
    float saturation      = 1.0f + severity * 0.4f;
    float blueBoost       = Mathf.Lerp(1.0f, 1.6f, channelBias);

    material.SetFloat("_BlurStrength",   compensationStrength);
    material.SetFloat("_EdgeStrength",   edgeStrength);
    material.SetFloat("_LocalContrast",  localContrast);
    material.SetFloat("_GammaContrast",  gammaCorrection);
    material.SetFloat("_GlobalContrast", globalContrast);
    material.SetFloat("_Saturation",     saturation);
    material.SetFloat("_BlueBoost",      blueBoost);

    Debug.Log($">>> SHADER PARAMS: "
            + $"BlurStr={compensationStrength:F3}, "
            + $"Edge={edgeStrength:F3}, "
            + $"Local={localContrast:F3}, "
            + $"Gamma={gammaCorrection:F3}, "
            + $"Global={globalContrast:F3}, "
            + $"Sat={saturation:F3}, "
            + $"Blue={blueBoost:F3}");
}
   public void Process(RenderTexture src, RenderTexture dst)
    {
        if (material == null)
        {
            Debug.LogError(">>> UnifiedCompensator: Material fehlt!");
            Graphics.Blit(src, dst);
            return;
        }

        Graphics.Blit(src, dst, material);
    }
}
