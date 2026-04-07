using UnityEngine;

public class ContrastCompensator : EffectCompensator
{
    public override VisualEffectType TargetEffect => VisualEffectType.Contrast;
    public Material compensationMaterial;

    // NEU: contrastFactor vom EffectPipelineManager gesetzt
    private float activeContrastFactor = 1.0f;

    public void SetSimulationParams(float contrastFactor)
    {
        activeContrastFactor = contrastFactor;
        Debug.Log($">>> ContrastCompensator: contrastFactor = {contrastFactor}");
    }

  public override void ApplyCompensation(AnalysisResult result)
{
    if (compensationMaterial == null) return;

    float severity    = result.severity;
    float channelBias = result.parameters.ContainsKey("channelBias")
                      ? result.parameters["channelBias"] : 0.0f;

    // combinedScale reduziert Aggressivität
    float saturation = Mathf.Lerp(1.0f, 1.3f, severity * combinedScale);
    float blueBoost  = Mathf.Lerp(1.0f, 1.4f, channelBias);

    compensationMaterial.SetFloat("_Severity",   severity * combinedScale);
    compensationMaterial.SetFloat("_Saturation", saturation);
    compensationMaterial.SetFloat("_BlueBoost",  blueBoost);

    Debug.Log($">>> Kontrast-Kompensation: Scale={combinedScale:F2}, "
            + $"Sat={saturation:F3}");

    Debug.Log($">>> CONTRAST SHADER PARAMS:");
    Debug.Log($"11    _Severity:   {severity * combinedScale:F3}");
    Debug.Log($"11    _Saturation: {saturation:F3}");
    Debug.Log($"11    _BlueBoost:  {blueBoost:F3}");
}

   // In beiden Compensators – Blit():
public override void Blit(RenderTexture src, RenderTexture dst)
{
    Debug.Log($">>> {TargetEffect} Blit: src={src?.width}x{src?.height}, "
            + $"dst={dst?.width}x{dst?.height}");

    if (compensationMaterial != null)
        Graphics.Blit(src, dst, compensationMaterial);
    else
    {
        Debug.LogWarning($">>> {TargetEffect}: compensationMaterial ist NULL!");
        Graphics.Blit(src, dst);
    }
}
}