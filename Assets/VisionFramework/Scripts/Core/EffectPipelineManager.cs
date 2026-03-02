using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EffectPipelineManager : MonoBehaviour
{
    [Header("Render Targets")]
    public RenderTexture inputCopyRT; 
    public RenderTexture outputRT; 

    [Header("Effect Library")]
    public EffectLibrary effectLibrary;

    [Header("UI Assignment (Manual)")]
    public TextMeshProUGUI profileText;
    public Slider intensitySlider;

    [Header("Profile Management")]
    public List<VisualImpairmentProfile> availableProfiles;
    private int currentProfileIndex = 0;

    [Header("Live Control")]
    [Range(0f, 2f)] public float masterIntensity = 1.0f; 
    public float changeSpeed = 0.5f;

    [Header("Apple / Mobile UI")]
    public RawImage screenOutputUI; 

    private List<Material> effectStack = new List<Material>();
    private RenderTexture tempA;
    private RenderTexture tempB;
    private Texture inputTexture; 

    public Camera mainCamera;

    public Transform eyeGaze;
    private Texture depthTexture;

    void Start()
    {  
        if (availableProfiles == null || availableProfiles.Count == 0 || outputRT == null)
        {
            return;
        }

        tempA = new RenderTexture(outputRT.width, outputRT.height, 0, outputRT.format);
        tempA.Create();
        tempB = new RenderTexture(outputRT.width, outputRT.height, 0, outputRT.format);
        tempB.Create();

        currentProfileIndex = 0;
        masterIntensity = 1.0f;
        
        if (intensitySlider != null) 
        {
            intensitySlider.value = masterIntensity;
            intensitySlider.gameObject.SetActive(true); 
        }

        ApplyProfile(availableProfiles[currentProfileIndex]);
        UpdateUI(availableProfiles[currentProfileIndex].profileName);
    }
    public void SetDepthTexture(Texture tex)
    {
        depthTexture = tex;
    }

    public void SwitchToNextProfile()
    {
        if (availableProfiles.Count == 0) return;
        currentProfileIndex = (currentProfileIndex + 1) % availableProfiles.Count;
        VisualImpairmentProfile activeProfile = availableProfiles[currentProfileIndex];
        masterIntensity = 1.0f;
        if (intensitySlider != null) intensitySlider.value = 1.0f;
        ApplyProfile(activeProfile);
        UpdateUI(activeProfile.profileName); 
    }

    private void UpdateUI(string profileName)
    {
        if (profileText == null) return;
        string instruction = Application.platform == RuntimePlatform.IPhonePlayer ? "<b>Double Tap</b> zum Wechseln." : "Drücke <b>'A'</b> zum Wechseln.";
        profileText.text = instruction + "\n<color=#FFA500>Aktuelles Sehproblem: " + profileName + "</color>";
    }

    public void UI_SetMasterIntensity(float value)
    {
        masterIntensity = value;
        if (availableProfiles.Count > 0) ApplyProfile(availableProfiles[currentProfileIndex]);
    }

    public void SetInputTexture(Texture tex) => inputTexture = tex;

   bool TryGetEyeGazeUV(out Vector2 uv)
{
    uv = new Vector2(0.5f, 0.5f);

    if (eyeGaze == null || mainCamera == null) 
        return false;

    Vector3 origin = eyeGaze.position;
    Vector3 forward = eyeGaze.forward;

    Vector3 worldPoint = origin + forward * 10f;

    Vector3 viewport = mainCamera.WorldToViewportPoint(worldPoint);

    uv = new Vector2(viewport.x, viewport.y);

    return true;
}

float GetDiopters(float intensity, BlurParameters blur)
{
    float maxDpt = blur.maxDiopters; 

    float dpt = Mathf.Lerp(0f, maxDpt, intensity);

    if (blur.defocusMode == DefocusMode.Myopie)
        dpt *= -1f;

    return dpt;
}
    public void ApplyProfile(VisualImpairmentProfile profile)
{
    if (profile == null || effectLibrary == null) return;

    float normalized = masterIntensity;

    if(intensitySlider != null) 
        intensitySlider.gameObject.SetActive(profile.effects.Count > 0);

    foreach (var mat in effectStack) { if (mat != null) Destroy(mat); }
    effectStack.Clear();

    foreach (var entry in profile.effects)
    {
        var p = entry.parameters;
        Material originalMat = effectLibrary.GetMaterial(p.effectType);
        if (originalMat == null) continue;

        Material mat = Instantiate(originalMat);

        if (depthTexture != null && mat.HasProperty("_CameraDepthTexture"))
        {
            mat.SetTexture("_CameraDepthTexture", depthTexture);
        }

        switch (p.effectType)
        {
            case VisualEffectType.Blur:

                var blur = (BlurParameters)p;

                float scatterScaled = blur.scatterStrength * (normalized * normalized);
                float blurScaled = blur.blurStrength * Mathf.Lerp(0.5f, 1.2f, normalized);

                mat.SetFloat("_EnableBlur", 1f);
                float baseStrength = blur.blurStrength * masterIntensity;

                if (!blur.useDepthBlur || depthTexture == null)
                {
                    Debug.Log("Depth ist nicht vorhanden!");
                    baseStrength *= 0.01f;
                }
                else
                {
                    Debug.Log("Depth ist vorhanden!");
                    baseStrength *= 0.004f;
                }

                mat.SetFloat("_BaseStrength", baseStrength);

                mat.SetFloat("_Anisotropy", blur.anisotropy);
                mat.SetFloat("_AxisAngle", blur.axisAngle);
                mat.SetInt("_Samples", blur.samples);

                mat.SetFloat("_ChromaticShift", blur.chromaticShift);
                mat.SetFloat("_AnisoContrastLoss", blur.anisotropicContrastLoss);

                bool hasDepth = depthTexture != null;
                mat.SetFloat("_UseDepth",
                    (blur.useDepthBlur && hasDepth) ? 1f : 0f);
                /*
                float diopters = GetDiopters(masterIntensity, blur);
                float focusShift = Mathf.Clamp01(0.5f + diopters * 0.08f);
                mat.SetFloat("_FocusDistance", focusShift);*/
                mat.SetFloat("_FocusDistance", blur.focusDistance);
                //float depthMul = blur.depthIntensityMultiplier * Mathf.Lerp(0.5f, 1.5f, Mathf.Abs(diopters)/blur.maxDiopters);
                mat.SetFloat("_DepthMultiplier", blur.depthIntensityMultiplier);
                mat.SetFloat("_DefocusSign", (float)blur.defocusMode);

                mat.SetFloat("_UseFovea", blur.useFovea ? 1f : 0f);
                mat.SetFloat("_InvertFovea", blur.invertFovea ? 1f : 0f);
                mat.SetFloat("_FoveaRadius", blur.foveaRadius);
                mat.SetFloat("_FoveaSoftness", blur.foveaSoftness);
                mat.SetVector("_FoveaCenter", blur.foveaCenter);

                mat.SetFloat("_ScatterStrength", scatterScaled);
                mat.SetFloat("_ScatterThreshold", blur.scatterThreshold);

                break;
            case VisualEffectType.Contrast:
                var con = (ContrastParameters)p;
                float contrastScaled = Mathf.Lerp(
                    1f,
                    con.contrastFactor,
                    Mathf.Pow(normalized, 1.2f)
                );

                mat.SetFloat("_ContrastFactor", contrastScaled);
                break;
            case VisualEffectType.FieldOfView:
                var fov = (FieldOfViewParameters)p;

                float intensity = Mathf.Max(0.001f, masterIntensity);

                float radiusScaled = fov.radius;
                float sensitivityScaled = fov.sensitivityLoss;
                float softnessScaled = fov.softness;
                float irregularityScaled = fov.irregularity;

                if (fov.defectType == FieldOfViewParameters.DefectType.Peripheral)
                {
                    radiusScaled = fov.radius / intensity;

                    sensitivityScaled = Mathf.Clamp01(
                        fov.sensitivityLoss * intensity
                    );

                    softnessScaled = fov.softness * Mathf.Lerp(1f, 0.8f, intensity - 1f);

                    irregularityScaled = fov.irregularity * Mathf.Lerp(1f, 1.3f, intensity - 1f);
                }
                else if (fov.defectType == FieldOfViewParameters.DefectType.Central)
                {
                    radiusScaled = fov.radius * intensity;

                    sensitivityScaled = Mathf.Clamp01(
                        fov.sensitivityLoss * Mathf.Pow(intensity, 1.3f)
                    );

                    softnessScaled = fov.softness * Mathf.Lerp(1f, 1.2f, intensity - 1f);

                    irregularityScaled = fov.irregularity * Mathf.Lerp(1f, 1.8f, intensity - 1f);
                }
                else if (fov.defectType == FieldOfViewParameters.DefectType.Arcuate)
                {

                    radiusScaled = fov.radius * Mathf.Lerp(1f, 1.4f, intensity - 1f);

                    sensitivityScaled = Mathf.Clamp01(
                        fov.sensitivityLoss * Mathf.Pow(intensity, 1.4f)
                    );

                    softnessScaled = fov.softness * Mathf.Lerp(1f, 0.85f, intensity - 1f);

                    irregularityScaled = fov.irregularity * Mathf.Lerp(1f, 1.5f, intensity - 1f);
                }

                mat.SetFloat("_Type", (float)fov.defectType);

                Vector2 gazeUV;
                if (TryGetEyeGazeUV(out gazeUV))
                    mat.SetVector("_Center", gazeUV);
                else
                    mat.SetVector("_Center", fov.center);

                mat.SetFloat("_Radius", radiusScaled);
                mat.SetFloat("_Softness", softnessScaled);
                mat.SetFloat("_SensitivityLoss", sensitivityScaled);
                mat.SetFloat("_Irregularity", irregularityScaled);
                mat.SetFloat("_AngularIrregularity", fov.angularIrregularity);
                mat.SetFloat("_NoiseScale", fov.noiseScale);

                mat.SetFloat("_ArcCurvature", fov.arcCurvature);
                mat.SetFloat("_ArcThickness", fov.arcThickness);
                mat.SetFloat("_ArcRotation", fov.arcRotation);
                break;
            case VisualEffectType.Distortion:
                var dist = (DistortionParameters)p;
                mat.SetFloat("_Strength", dist.strength * masterIntensity);
                mat.SetFloat("_Rotation", dist.rotation);
                
                mat.SetFloat("_Mode", (float)dist.mode); 
                
                mat.SetFloat("_WaveFreq", dist.waveFrequency);
                mat.SetFloat("_CenterRad", dist.centerRadius);
                break;
            case VisualEffectType.ColorChange:
                var colorParams = (ColorChangeParameters)p;
                float scale = masterIntensity;
                float colorScaled;
                if (scale <= 1f)
                    colorScaled = colorParams.intensity * scale;
                else
                    colorScaled = colorParams.intensity * Mathf.Pow(scale, 1.2f);
                mat.SetFloat("_Intensity", Mathf.Clamp01(colorScaled));
                Matrix4x4 colorMatrix = Matrix4x4.identity;

                if (colorParams.defectType == ColorDefectType.Protanopia)
                {
                    colorMatrix.SetRow(0, new Vector4(0.152286f, 1.052583f, -0.204868f, 0));
                    colorMatrix.SetRow(1, new Vector4(0.114503f, 0.786281f, 0.099216f, 0));
                    colorMatrix.SetRow(2, new Vector4(-0.003882f, -0.048116f, 1.051998f, 0));
                    colorMatrix.SetRow(3, new Vector4(0,0,0,1));
                }

                else if (colorParams.defectType == ColorDefectType.CataractYellow)
                {
                    colorMatrix.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0));
                    colorMatrix.SetRow(1, new Vector4(0.0f, 0.95f, 0.0f, 0));
                    colorMatrix.SetRow(2, new Vector4(0.0f, 0.0f, 0.4f, 0));
                    colorMatrix.SetRow(3, new Vector4(0,0,0,1));
                }
                mat.SetMatrix("_ColorMatrix", colorMatrix);
                break;
            case VisualEffectType.Artifacts:
                var art = (ArtifactsParameters)p;
                mat.SetFloat("_FloaterIntensity", art.floaterIntensity * masterIntensity);
                mat.SetFloat("_Density", art.density);
                mat.SetFloat("_FloaterSize", art.size);
                mat.SetFloat("_DriftSpeed", art.driftSpeed);
                mat.SetFloat("_Darkness", art.darkness);
                mat.SetFloat("_Transparency", art.transparency);
                mat.SetFloat("_ArtifactMode", (float)art.artifactMode);
                Debug.Log("Mode"+ (float)art.artifactMode);
                break;
        }
        effectStack.Add(mat);
    }
}

    void Update()
    {
        bool metaInput = OVRInput.GetDown(OVRInput.RawButton.A);
        bool appleDoubleTap = (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && Input.GetTouch(0).tapCount == 2);

        if (metaInput || appleDoubleTap) SwitchToNextProfile();

        Vector2 joy = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
        if (Mathf.Abs(joy.x) > 0.1f)
        {
            masterIntensity = Mathf.Clamp(masterIntensity + (joy.x * changeSpeed * Time.deltaTime), 0f, 2f);
            if (intensitySlider != null) intensitySlider.value = masterIntensity;
            ApplyProfile(availableProfiles[currentProfileIndex]);
        }

        if (inputTexture == null || outputRT == null) return;
        if (!inputCopyRT.IsCreated()) inputCopyRT.Create();

        Graphics.Blit(inputTexture, inputCopyRT);
        RenderTexture src = inputCopyRT;
        RenderTexture dst = tempA;

        foreach (Material effect in effectStack)
        {
            Graphics.Blit(src, dst, effect);
            src = dst;
            dst = (src == tempA) ? tempB : tempA;
        }
        Graphics.Blit(src, outputRT);

        if (Application.platform == RuntimePlatform.IPhonePlayer && screenOutputUI != null)
            screenOutputUI.texture = outputRT;
    }

    void OnDestroy()
    {
        foreach (var mat in effectStack) if (mat != null) Destroy(mat);
        if (tempA != null) tempA.Release();
        if (tempB != null) tempB.Release();
    }
}