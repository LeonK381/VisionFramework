using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.EventSystems;

public class EffectPipelineManager : MonoBehaviour
{
    public CompensationPipelineManager compensationManager;

    public Material displayMaterial;

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

  
    [Range(0f, 2f)] private float masterIntensity = 1.0f; 
    private float changeSpeed = 0.5f;

    [Header("Apple / Mobile UI")]
    public RawImage screenOutputUI; 

    [Header("Eye-Tracking")]
    private List<Material> effectStack = new List<Material>();
    private RenderTexture tempA;
    private RenderTexture tempB;
    private Texture inputTexture; 

    public Camera mainCamera;

    private InputDevice eyeDevice;
    private bool eyeTrackingAvailable = false;
    public float gazeMoveSpeed = 0.4f;

    private Vector2 profileCenter = new Vector2(0.5f,0.5f);

    private Vector2 gazeUV = new Vector2(0.5f,0.5f);
    private Texture depthTexture;

    void Start()
    {  
        InitializeEyeTracking();

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

    void InitializeEyeTracking()
{
    List<InputDevice> devices = new List<InputDevice>();

    InputDevices.GetDevicesWithCharacteristics(
        InputDeviceCharacteristics.EyeTracking,
        devices
    );

    if(devices.Count > 0)
    {
        eyeDevice = devices[0];
        eyeTrackingAvailable = true;

        Debug.Log("Eye Tracking gefunden: " + eyeDevice.name);
    }
    else
    {
        eyeTrackingAvailable = false;
        Debug.Log("Kein Eye Tracking verfügbar.");
    }
}

bool IsTouchOnSlider()
{
    if (Input.touchCount == 0 || intensitySlider == null)
        return false;

    Touch touch = Input.GetTouch(0);

    RectTransform sliderRect = intensitySlider.GetComponent<RectTransform>();

    return RectTransformUtility.RectangleContainsScreenPoint(
        sliderRect,
        touch.position,
        null
    );
}

    public void SwitchToNextProfile()
    {
        if (availableProfiles.Count == 0) return;
        currentProfileIndex = (currentProfileIndex + 1) % availableProfiles.Count;
        VisualImpairmentProfile activeProfile = availableProfiles[currentProfileIndex];
        masterIntensity = 1.0f;
        if (intensitySlider != null) intensitySlider.value = 1.0f;
        ApplyProfile(activeProfile);
        gazeUV = profileCenter;
        UpdateUI(activeProfile.profileName); 
        compensationManager.ResetCompensation();
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

  bool TryGetOpenXRGaze(out Vector2 uv)
{
    uv = gazeUV;

    if(!eyeTrackingAvailable || !eyeDevice.isValid)
        return false;

    if(eyeDevice.TryGetFeatureValue(CommonUsages.eyesData, out Eyes eyes))
    {
        if(eyes.TryGetFixationPoint(out Vector3 fixationPoint))
        {
            Vector3 viewport = mainCamera.WorldToViewportPoint(fixationPoint);

            uv = new Vector2(viewport.x, viewport.y);

            return true;
        }
    }

    return false;
}

void UpdateSimulatedGaze()
{
    Vector2 joy = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);

    if(joy.magnitude > 0.1f)
    {
        gazeUV += joy * gazeMoveSpeed * Time.deltaTime;

        gazeUV.x = Mathf.Clamp01(gazeUV.x);
        gazeUV.y = Mathf.Clamp01(gazeUV.y);
    }
}

void UpdateGaze()
{
    Vector2 uv;

    if(TryGetOpenXRGaze(out uv))
    {
        gazeUV = uv;
        return;
    }

    if(Application.platform == RuntimePlatform.IPhonePlayer)
    {
        UpdateTouchGaze();
        return;
    }

    if(Application.platform == RuntimePlatform.Android)
    {
        UpdateSimulatedGaze();
    }
}

void UpdateShaderGaze()
{
    foreach(Material effect in effectStack)
    {
        if(effect == null) continue;

        if(effect.HasProperty("_Center"))
            effect.SetVector("_Center", gazeUV);
    }
}
void UpdateTouchGaze()
{
    if(Input.touchCount > 0)
    {
        if(IsTouchOnSlider())
            return;

        Touch touch = Input.GetTouch(0);

        if(touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
        {
            Vector2 pos = touch.position;

            gazeUV = new Vector2(
                pos.x / Screen.width,
                pos.y / Screen.height
            );

            gazeUV.x = Mathf.Clamp01(gazeUV.x);
            gazeUV.y = Mathf.Clamp01(gazeUV.y);
        }
    }
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
                float blurStrength = blur.blurStrength;

                if (!blur.useDepth || depthTexture == null)
                {
                    Debug.Log("Depth ist nicht vorhanden!");
                    blurStrength *= 0.01f * masterIntensity;
                }
                else
                {
                    Debug.Log("Depth ist vorhanden!");
                    blurStrength *= 0.01f;
                }

                mat.SetFloat("_BlurStrength", blurStrength);

                mat.SetFloat("_Anisotropy", blur.anisotropy * masterIntensity);
                mat.SetFloat("_AxisAngle", blur.axisAngle);
                mat.SetInt("_Samples", blur.sampleCount);

                bool hasDepth = depthTexture != null;
                mat.SetFloat("_UseDepth",
                    (blur.useDepth && hasDepth) ? 1f : 0f);
               
                float distanceIntensity = masterIntensity;

                // 0 → 1 normalisieren
                float t = Mathf.Clamp01(distanceIntensity / 2.0f);

                // nichtlinear (wichtig!)
                t = t * t;

                // Basis = dein „normales Sehen“
                float baseFocus = blur.focusDistance;

                // Grenzen definieren
                float minFocus = 0.05f;   // sehr nah (starke Myopie)
                float maxFocus = 2.0f;    // sehr weit (Hyperopie)

                // Fokus berechnen
                float focus;

                if (blur.focusMode > 0) // Myopie
                {
                    // Intensität ↑ → Fokus näher
                    focus = Mathf.Lerp(baseFocus, minFocus, t);
                }
                else // Hyperopie
                {
                    // Intensität ↑ → Fokus weiter weg
                    focus = Mathf.Lerp(baseFocus, maxFocus, t);
                }
                Debug.Log("Aktuelle Distanz:" + focus);
                mat.SetFloat("_FocusDistance", focus);
                mat.SetFloat("_DefocusSign", (float)blur.focusMode);
                mat.SetFloat("_UseFovea", blur.useFovea ? 1f : 0f);
                mat.SetFloat("_InvertFovea", blur.invertFovea ? 1f : 0f);
                mat.SetFloat("_FoveaRadius", blur.foveaRadius);
                mat.SetFloat("_FoveaSoftness", blur.foveaSoftness);
                mat.SetVector("_FoveaCenter", blur.foveaCenter);

                var blurAnalyzer    = compensationManager.GetAnalyzer<BlurAnalyzer>();
                

                if (blurAnalyzer    != null)
                    blurAnalyzer.SetSimulationParams(blurStrength);
                break;
            case VisualEffectType.Contrast:
                var con = (ContrastParameters)p;
                var contrastAnalyzer = compensationManager.GetAnalyzer<ContrastAnalyzer>();
                

                if (contrastAnalyzer    != null)
                    contrastAnalyzer.SetSimulationParams(con.contrastFactor);
                
                /*
                float contrastScaled = Mathf.Lerp(
                    1f,
                    con.contrastFactor,
                    Mathf.Pow(normalized, 1.2f)
                );*/

                mat.SetFloat("_ContrastFactor", con.contrastFactor * masterIntensity);
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

                mat.SetFloat("_Type", (float)fov.defectType);

                profileCenter = fov.center;
                //gazeUV = profileCenter;

                mat.SetVector("_Center", gazeUV);

                mat.SetFloat("_Radius", radiusScaled);
                mat.SetFloat("_Softness", softnessScaled);
                mat.SetFloat("_SensitivityLoss", sensitivityScaled);
                mat.SetFloat("_Irregularity", irregularityScaled);
                mat.SetFloat("_AngularIrregularity", fov.angularIrregularity);
                mat.SetFloat("_NoiseScale", fov.noiseScale);
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
                Debug.Log($">>> ColorChange: defectType={colorParams.defectType}, "
                + $"als int={(int)colorParams.defectType}");
                 var colorAnalyzer = compensationManager.GetAnalyzer<ColorAnalyzer>();

                if (colorAnalyzer != null)
                {
                    colorAnalyzer.SetSimulationParams(colorParams.defectType, colorParams.intensity);
                    Debug.Log($">>> ColorChange: {colorParams.defectType}");
                }
                break;
            case VisualEffectType.Artifacts:
                var art = (ArtifactsParameters)p;
                mat.SetFloat("_Intensity", art.intensity * masterIntensity);
                mat.SetFloat("_Density", art.density);
                mat.SetFloat("_FloaterSize", art.size * masterIntensity);
                mat.SetFloat("_DriftSpeed", art.driftSpeed);
                mat.SetFloat("_Darkness", art.darkness * masterIntensity);
                mat.SetFloat("_Transparency", art.transparency);
                mat.SetFloat("_ArtifactMode", (float)art.artifactMode);
                mat.SetFloat("_PeripheryMask", art.avoidCenter ? 1f : 0f);
                mat.SetFloat("_PeripheryRadius", art.centerRadius);
                mat.SetFloat("_Irregularity", art.irregularity);
                mat.SetFloat("_NoiseScale", art.noiseScale);
                mat.SetFloat("_Softness", art.softness);
                Debug.Log("Mode"+ (float)art.artifactMode);
                break;
        }
        effectStack.Add(mat);

        
    }
    var activeTypes = new List<VisualEffectType>();
        foreach (var entry in profile.effects)
            activeTypes.Add(entry.parameters.effectType);

    compensationManager.SetActiveEffects(activeTypes);
}

    void Update()
    {

        UpdateGaze();
        UpdateShaderGaze();

        if (inputTexture == null)
        {
            Debug.Log("Meta inputTexture ist NULL");
        }

        bool metaInput = OVRInput.GetDown(OVRInput.RawButton.A);
        bool appleDoubleTap = (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && Input.GetTouch(0).tapCount == 2);

        if (metaInput || appleDoubleTap) SwitchToNextProfile();

        bool toggleComp = OVRInput.GetDown(OVRInput.RawButton.B);

        if(toggleComp)
        {
            Debug.Log("B Button gedrückt");
            compensationManager.ToggleCompensation();
        }

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
        RenderTexture source = inputCopyRT;
        RenderTexture destination = tempA;

        foreach (Material effect in effectStack)
        {
            Graphics.Blit(source, destination, effect);
            source = destination;
            destination = (source == tempA) ? tempB : tempA;
        }
        Graphics.Blit(source, outputRT);
        RenderTexture finalOutput = compensationManager.Process(outputRT);

        displayMaterial.mainTexture = finalOutput;

        if (Application.platform == RuntimePlatform.IPhonePlayer && screenOutputUI != null)
            screenOutputUI.texture = outputRT;

        Debug.Log("Pipeline läuft – Effekte: " + effectStack.Count);
    }

    void OnDestroy()
    {
        foreach (var mat in effectStack) if (mat != null) Destroy(mat);
        if (tempA != null) tempA.Release();
        if (tempB != null) tempB.Release();
    }
}