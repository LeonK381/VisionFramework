// EffectCompensator.cs
using UnityEngine;

public abstract class EffectCompensator : MonoBehaviour
{
    public abstract VisualEffectType TargetEffect { get; }

    // Bekommt Ergebnis, setzt Shader-Parameter
    public abstract void ApplyCompensation(AnalysisResult result);

    // Wendet Shader auf RenderTexture an
    public abstract void Blit(RenderTexture src, RenderTexture dst);

    protected float combinedScale = 1.0f;
    public void SetCombinedScale(float scale) => combinedScale = scale;
}