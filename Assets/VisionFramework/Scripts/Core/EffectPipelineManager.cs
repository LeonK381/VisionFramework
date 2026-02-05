using System.Collections.Generic;
using UnityEngine;

public class EffectPipelineManager : MonoBehaviour
{
    [Header("Render Targets")]
    public RenderTexture inputCopyRT; 
    public RenderTexture outputRT; 

    [Header("Effect Stack (runtime only)")]
    public List<Material> effectStack = new List<Material>();

    [Header("Effect Library")]
    public EffectLibrary effectLibrary;

    private RenderTexture tempA;
    private RenderTexture tempB;
    private Texture inputTexture; 

    [Header("Debug")]
    public VisualImpairmentProfile debugProfile;

    void Start()
    {
        Debug.Log("EffectPipelineManager START");
        effectStack.Clear();

        if (debugProfile == null || effectLibrary == null || outputRT == null)
        {
            Debug.LogError("Setup unvollständig: Profil, Library oder OutputRT fehlt!");
            return;
        }

        // Temp RenderTextures erstellen
        tempA = new RenderTexture(outputRT.width, outputRT.height, 0, outputRT.format);
        tempA.Create();

        tempB = new RenderTexture(outputRT.width, outputRT.height, 0, outputRT.format);
        tempB.Create();

        Debug.Log("Wende Profil an: " + debugProfile.profileName);
        ApplyProfile(debugProfile);
    }

    public void SetInputTexture(Texture tex)
    {
        inputTexture = tex;
    }

    public void ApplyProfile(VisualImpairmentProfile profile)
    {
        Debug.Log("ApplyProfile START für " + profile.profileName);

        foreach (var mat in effectStack) { if (mat != null) Destroy(mat); }
        effectStack.Clear();

        if (profile == null || effectLibrary == null) return;

        foreach (var entry in profile.effects)
        {
            var p = entry.parameters;
            Material originalMat = effectLibrary.GetMaterial(p.effectType);
            
            if (originalMat == null) continue;

            Material mat = Instantiate(originalMat);

            // Parameter-Zuweisung je nach Typ
            switch (p.effectType)
            {
                case VisualEffectType.Blur:
                    var blur = (BlurParameters)p;
                    mat.SetFloat("_BlurStrength", blur.blurStrength * 0.001f);
                    mat.SetFloat("_UseFovea", blur.useFovea ? 1f : 0f);
                    mat.SetFloat("_FoveaRadius", blur.foveaRadius);
                    mat.SetFloat("_FoveaSoftness", blur.foveaSoftness);
                    mat.SetVector("_FoveaCenter", blur.foveaCenter);
                    break;

                case VisualEffectType.Contrast:
                    var contrast = (ContrastParameters)p;
                    Debug.Log($"CONTRAST APPLY: factor={contrast.contrastFactor}");
                    mat.SetFloat("_ContrastFactor", contrast.contrastFactor);
                    break;
                
                case VisualEffectType.FieldOfView:
                    var fov = (FieldOfViewParameters)p;
                    mat.SetFloat("_Radius", fov.radius);
                    mat.SetFloat("_Softness", fov.softness);
                    mat.SetVector("_Center", fov.center);
                    // 0 für Central, 1 für Peripheral
                    mat.SetFloat("_Type", (float)fov.defectType);
                    break;

                case VisualEffectType.Distortion:
                    var d = (DistortionParameters)p;
                    Debug.Log($"D Werte werden gesetzt: {d.strength}");
                    mat.SetFloat("_Strength", d.strength);
                    mat.SetFloat("_AxisControl", d.axisControl);
                    mat.SetFloat("_Rotation", d.rotation);
                    break;

                case VisualEffectType.ColorChange:
                    var c = (ColorChangeParameters)p;
                    Matrix4x4 matrix = Matrix4x4.identity;

                    if (c.defectType == ColorDefectType.Protanopia) {
                        // Protanopie Matrix (vereinfacht nach Viénot)
                        matrix.SetRow(0, new Vector4(0.567f, 0.433f, 0.000f, 0.000f));
                        matrix.SetRow(1, new Vector4(0.558f, 0.442f, 0.000f, 0.000f));
                        matrix.SetRow(2, new Vector4(0.000f, 0.242f, 0.758f, 0.000f));
                    } 
                    else if (c.defectType == ColorDefectType.CataractYellow) {
                        // Gelbstich (Blauanteile reduzieren, Rot/Grün leicht verstärken)
                        matrix.SetRow(0, new Vector4(1.000f, 0.000f, 0.000f, 0.000f));
                        matrix.SetRow(1, new Vector4(0.000f, 0.950f, 0.000f, 0.000f));
                        matrix.SetRow(2, new Vector4(0.000f, 0.000f, 0.600f, 0.000f));
                        }
                        mat.SetMatrix("_ColorMatrix", matrix);
                        mat.SetFloat("_Intensity", c.intensity);
                        break;

                case VisualEffectType.Artifacts:
                    var art = (ArtifactsParameters)p;
                    mat.SetFloat("_FloaterIntensity", art.floaterIntensity);
                    mat.SetFloat("_Density", art.density);
                    mat.SetFloat("_DriftSpeed", art.driftSpeed);
                    // Wir nutzen 'size', um die Distanzberechnung im Shader zu beeinflussen
                    mat.SetFloat("_FloaterSize", art.size); 
                    break;
            }
            effectStack.Add(mat);
        }
        Debug.Log("ApplyProfile ENDE | Effektanzahl: " + effectStack.Count);
    }

    void Update()
    {
        if (inputTexture == null || effectStack.Count == 0)
        {
            // Falls kein Effekt aktiv, Passthrough direkt durchreichen
            if (inputTexture != null && outputRT != null) Graphics.Blit(inputTexture, outputRT);
            return;
        }

        if (!inputCopyRT.IsCreated()) inputCopyRT.Create();

        // 1️⃣ Passthrough → InputCopyRT
        Graphics.Blit(inputTexture, inputCopyRT);

        RenderTexture currentSource = inputCopyRT;
        RenderTexture currentTarget = tempA;

        // 2️⃣ Effekt-Pipeline (Ping-Pong)
        foreach (Material effect in effectStack)
        {
            Graphics.Blit(currentSource, currentTarget, effect);
            var swap = currentSource;
            currentSource = currentTarget;
            currentTarget = (currentTarget == tempA) ? tempB : tempA;
        }

        // 3️⃣ Final → OutputRT
        Graphics.Blit(currentSource, outputRT);
    }

    void OnDestroy()
    {
        foreach (var mat in effectStack) { if (mat != null) Destroy(mat); }
        if (tempA != null) tempA.Release();
        if (tempB != null) tempB.Release();
    }
}