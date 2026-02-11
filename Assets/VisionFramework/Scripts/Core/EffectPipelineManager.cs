using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    [Header("Profile Management")]
    public List<VisualImpairmentProfile> availableProfiles;
    private int currentProfileIndex = 0;

    [Header("UI Display")]
    public TextMeshProUGUI profileText;

    public void SwitchToNextProfile()
    {
        if (availableProfiles.Count == 0) return;

        currentProfileIndex = (currentProfileIndex + 1) % availableProfiles.Count;
        VisualImpairmentProfile activeProfile = availableProfiles[currentProfileIndex];
        
        ApplyProfile(activeProfile);
        UpdateUI("Drücke 'A' am rechten Controller zum Wechseln.\n" + 
                 "Aktuelles Sehproblem: " + activeProfile.profileName); // Hier profileName nutzen, falls vorhanden
    }

    private void UpdateUI(string message)
    {
        if (profileText != null) profileText.text = message;
    }

    void Start()
    {
        Debug.Log("EffectPipelineManager START");
        effectStack.Clear();

        if (availableProfiles == null || availableProfiles.Count == 0 || effectLibrary == null || outputRT == null)
        {
            Debug.LogError("Setup unvollständig: Profile-Liste leer, Library oder OutputRT fehlt!");
            return;
        }

        tempA = new RenderTexture(outputRT.width, outputRT.height, 0, outputRT.format);
        tempA.Create();

        tempB = new RenderTexture(outputRT.width, outputRT.height, 0, outputRT.format);
        tempB.Create();

        currentProfileIndex = 0;
        ApplyProfile(availableProfiles[currentProfileIndex]);
        UpdateUI("Drücke 'A' am rechten Controller zum Wechseln.\n" + 
                     "Aktuelles Sehproblem: " + availableProfiles[currentProfileIndex].profileName);
    } // <-- Hier war die Klammer wichtig!

    public void SetInputTexture(Texture tex)
    {
        inputTexture = tex;
    }

    public void ApplyProfile(VisualImpairmentProfile profile)
    {
        if (profile == null || effectLibrary == null) return;
        
        Debug.Log("ApplyProfile für: " + profile.profileName);

        foreach (var mat in effectStack) { if (mat != null) Destroy(mat); }
        effectStack.Clear();

        foreach (var entry in profile.effects)
        {
            var p = entry.parameters;
            Material originalMat = effectLibrary.GetMaterial(p.effectType);
            if (originalMat == null) continue;

            Material mat = Instantiate(originalMat);

            switch (p.effectType)
            {
                case VisualEffectType.Blur:
                    var blur = (BlurParameters)p;
                    mat.SetFloat("_BlurStrength", blur.blurStrength * 0.001f); // Skalierung für Shader
                    mat.SetVector("_Direction", blur.blurDirection);
                    mat.SetFloat("_UseFovea", blur.useFovea ? 1f : 0f);
                    mat.SetFloat("_InvertFovea", blur.invertFovea ? 1f : 0f);
                    mat.SetFloat("_FoveaRadius", blur.foveaRadius);
                    mat.SetFloat("_FoveaSoftness", blur.foveaSoftness);
                    mat.SetVector("_FoveaCenter", blur.foveaCenter);
                    break;

                case VisualEffectType.Contrast:
                    var contrast = (ContrastParameters)p;
                    mat.SetFloat("_ContrastFactor", contrast.contrastFactor);
                    break;
                
                case VisualEffectType.FieldOfView:
                    var fov = (FieldOfViewParameters)p;
                    mat.SetFloat("_Radius", fov.radius);
                    mat.SetFloat("_Softness", fov.softness);
                    mat.SetVector("_Center", fov.center);
                    mat.SetFloat("_Type", (float)fov.defectType);
                    break;

                case VisualEffectType.Distortion:
                    var d = (DistortionParameters)p;
                    mat.SetFloat("_Strength", d.strength);
                    //mat.SetFloat("_AxisControl", d.axisControl);
                    mat.SetFloat("_Rotation", d.rotation);
                    mat.SetFloat("_Mode", (float)d.mode);
                    mat.SetFloat("_WaveFreq", d.waveFrequency);
                    mat.SetFloat("_CenterRad", d.centerRadius);
                    break;

                case VisualEffectType.ColorChange:
                    var c = (ColorChangeParameters)p;
                    Matrix4x4 matrix = Matrix4x4.identity;
                    if (c.defectType == ColorDefectType.Protanopia) {
                        matrix.SetRow(0, new Vector4(0.567f, 0.433f, 0.000f, 0.000f));
                        matrix.SetRow(1, new Vector4(0.558f, 0.442f, 0.000f, 0.000f));
                        matrix.SetRow(2, new Vector4(0.000f, 0.242f, 0.758f, 0.000f));
                    } 
                    else if (c.defectType == ColorDefectType.CataractYellow) {
                        matrix.SetRow(0, new Vector4(1.000f, 0.000f, 0.000f, 0.000f));
                        matrix.SetRow(1, new Vector4(0.000f, 0.950f, 0.000f, 0.000f));
                        matrix.SetRow(2, new Vector4(0.000f, 0.000f, 0.600f, 0.000f));
                    } // <-- Klammer ergänzt
                    mat.SetMatrix("_ColorMatrix", matrix);
                    mat.SetFloat("_Intensity", c.intensity);
                    break;

                case VisualEffectType.Artifacts:
                    var art = (ArtifactsParameters)p;
                    mat.SetFloat("_FloaterIntensity", art.floaterIntensity);
                    mat.SetFloat("_Density", art.density);
                    mat.SetFloat("_DriftSpeed", art.driftSpeed);
                    mat.SetFloat("_FloaterSize", art.size); 
                    break;
            }
            effectStack.Add(mat);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || OVRInput.GetDown(OVRInput.RawButton.A))
        {
            SwitchToNextProfile();
        }

        if (inputTexture == null || outputRT == null) return;

        if (effectStack.Count == 0)
        {
            Graphics.Blit(inputTexture, outputRT);
            return;
        }

        if (!inputCopyRT.IsCreated()) inputCopyRT.Create();
        Graphics.Blit(inputTexture, inputCopyRT);

        RenderTexture currentSource = inputCopyRT;
        RenderTexture currentTarget = tempA;

        foreach (Material effect in effectStack)
        {
            Graphics.Blit(currentSource, currentTarget, effect);
            currentSource = currentTarget;
            currentTarget = (currentSource == tempA) ? tempB : tempA;
        }

        Graphics.Blit(currentSource, outputRT);
    }

    void OnDestroy()
    {
        foreach (var mat in effectStack) { if (mat != null) Destroy(mat); }
        if (tempA != null) tempA.Release();
        if (tempB != null) tempB.Release();
    }
}