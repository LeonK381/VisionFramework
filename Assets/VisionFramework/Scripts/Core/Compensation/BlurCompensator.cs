using UnityEngine;

public class BlurCompensator : EffectCompensator
{
    public override VisualEffectType TargetEffect => VisualEffectType.Blur;

    public Material compensationMaterial;

    private float activeBlurStrength = 1.0f;

    public void SetSimulationParams(float blurStrength)
    {
        activeBlurStrength = blurStrength;
        Debug.Log($">>> BlurAnalyzer: blurStrength = {blurStrength}");
    }

public override void ApplyCompensation(AnalysisResult result)
{
    if (compensationMaterial == null) return;

    float compensationStrength = result.parameters.ContainsKey("compensationStrength")
        ? result.parameters["compensationStrength"] : 0f;

    float severity      = result.severity;
    float perceptual    = Mathf.Pow(Mathf.Clamp01(severity), 0.6f);

    // USMStrength ersetzt EdgeStrength – direkt auf _USMStrength mappen
    float usmStrength   = Mathf.Pow(compensationStrength, 0.5f) * 3.0f * combinedScale;
    float contrastBoost = 1.0f + compensationStrength * 1.2f * combinedScale;
    float saturation    = 1.0f;

    // Korrekte Property-Namen die im Shader existieren
    compensationMaterial.SetFloat("_USMStrength",   usmStrength);
    compensationMaterial.SetFloat("_ContrastBoost", contrastBoost);
    compensationMaterial.SetFloat("_Saturation",    saturation);
    compensationMaterial.SetFloat("_Severity",      perceptual * combinedScale);

    Debug.Log($">>> BLUR SHADER PARAMS:");
    Debug.Log($"11    _USMStrength:         {usmStrength:F3}");
    Debug.Log($"11    _ContrastBoost:       {contrastBoost:F3}");
    Debug.Log($"11    _Saturation:          {saturation:F3}");
    Debug.Log($"11    _Severity:            {perceptual:F3}");
    Debug.Log($"11    compensationStrength: {compensationStrength:F3}");
    Debug.Log($"11   severity raw:         {severity:F3}");
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