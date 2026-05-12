using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.EventSystems;


// Hauptverwaltung der visuellen Effekt-Pipeline für die Simulation von Sehbeeinträchtigungen.
// Koordiniert Rendering, Eye-Tracking, Kompensation und Benutzereingaben
public class EffectPipelineManager : MonoBehaviour
{
    [Header("Kompensation")]
    public CompensationPipelineManager compensationManager;
    public UnifiedCompensator unified;

   [Header("Rendering")]
    public Material displayMaterial;
    public RenderTexture inputCopyRT;
    public RenderTexture outputRT;

    [Header("Effekte")]
    public EffectLibrary effectLibrary;

    [Header("UI")]
    public TextMeshProUGUI profileText;
    public Slider intensitySlider;
    public RawImage screenOutputUI;

    [Header("Profile")]
    public List<VisualImpairmentProfile> availableProfiles;
    
    

    [Header("Eye-Tracking")]
    public Camera mainCamera;
    [Range(0.1f, 1.0f)]
    public float gazeMoveSpeed = 0.4f;

    [Header("Test Overlay")]
    public TestOverlayManager testOverlayManager;

    // Gibt an, ob aktuell ein Sehtest aktiv ist
    public bool IsTestActive { get; set; } = false;

    private int currentProfileIndex;
    private float masterIntensity = 1.0f;
    private const float INTENSITY_CHANGE_SPEED = 0.5f;
    private const float MIN_INTENSITY = 0.0f;
    private const float MAX_INTENSITY = 2.0f;

    private List<Material> effectStack  = new List<Material>();
    private RenderTexture tempA;
    private RenderTexture tempB;
    private Texture inputTexture;
    private Texture depthTexture;

    private InputDevice eyeDevice;
    private bool eyeTrackingAvailable = false;
    private Vector2 gazeUV = new Vector2(0.5f, 0.5f);
    private Vector2 profileCenter = new Vector2(0.5f, 0.5f);

    // Aktiviert ein Testoverlay
    public void SetTestOverlay(Texture tex, Material mat)
        => testOverlayManager?.SetOverlay(tex, mat);

    // Entfernt das Testoverlay
    public void ClearTestOverlay()
        => testOverlayManager?.ClearOverlay();

    void Start()
    {
        InitializeEyeTracking();

        if (availableProfiles == null ||
            availableProfiles.Count == 0 ||
            outputRT == null) return;

        tempA = new RenderTexture(outputRT.width, outputRT.height, 0, outputRT.format);
        tempB = new RenderTexture(outputRT.width, outputRT.height, 0, outputRT.format);
        tempA.Create();
        tempB.Create();

        masterIntensity = 1.0f;

        if (intensitySlider != null)
        {
            intensitySlider.value = masterIntensity;
            intensitySlider.minValue = MIN_INTENSITY;
            intensitySlider.maxValue = MAX_INTENSITY;
            intensitySlider.gameObject.SetActive(true);
        }

        ApplyProfile(availableProfiles[currentProfileIndex]);
        UpdateUI(availableProfiles[currentProfileIndex].profileName);
    }

     void Update()
    {
        UpdateGaze();
        UpdateShaderGaze();
        HandleInput();
        RenderPipeline();
    }

    void OnDestroy()
    {
        foreach (var mat in effectStack)
            if (mat != null) Destroy(mat);

        tempA?.Release();
        tempB?.Release();
    }

    // Setzt die Eingabe-Textur für die Pipeline
    public void SetInputTexture(Texture tex)  => inputTexture = tex;

    // Setzt die Tiefen-Textur für tiefenabhängige Effekte
    public void SetDepthTexture(Texture tex)  => depthTexture = tex;

    // Setzt die globale Effektintensität
    public void SetMasterIntensity(float value)
    {
        masterIntensity = value;
        if (availableProfiles.Count > 0)
            ApplyProfile(availableProfiles[currentProfileIndex]);
    }

    // Wechselt zum nächsten Sehprofil in der Liste
    public void SwitchToNextProfile()
    {
        if (availableProfiles.Count == 0) 
        {
            Debug.LogWarning("No visual impairment profiles available");
            return;
        }
        currentProfileIndex = (currentProfileIndex + 1) % availableProfiles.Count;
        VisualImpairmentProfile profile = availableProfiles[currentProfileIndex];

        masterIntensity = 1.0f;
        if (intensitySlider != null) intensitySlider.value = 1.0f;

        ApplyProfile(profile);
        gazeUV = profileCenter;
        UpdateUI(profile.profileName);
        compensationManager.ResetCompensation();
    }

    // Verarbeitet plattformspezifische Benutzereingaben
    void HandleInput()
    {
        // Profil wechseln
        bool metaNext      = OVRInput.GetDown(OVRInput.RawButton.A);
        bool appleDoubleTap = !IsTestActive
                           && Input.touchCount == 1
                           && Input.GetTouch(0).phase == TouchPhase.Began
                           && Input.GetTouch(0).tapCount == 2;

        if (metaNext || appleDoubleTap)
        {
            SwitchToNextProfile();
            return;
        }

        // Kompensation togglen
        bool metaComp      = OVRInput.GetDown(OVRInput.RawButton.B);
        bool twoFingerTap  = !IsTestActive
                          && Input.touchCount == 2
                          && Input.GetTouch(0).phase == TouchPhase.Began
                          && Input.GetTouch(1).phase == TouchPhase.Began;

        if (metaComp || twoFingerTap)
            compensationManager.ToggleCompensation();

        // Intensität anpassen (nur Meta Quest)
        Vector2 joy = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
        if (Mathf.Abs(joy.x) > 0.1f)
        {
            masterIntensity = Mathf.Clamp(masterIntensity + joy.x * INTENSITY_CHANGE_SPEED * Time.deltaTime, 0f, 2f);
            if (intensitySlider != null)
                intensitySlider.value = masterIntensity;
            ApplyProfile(availableProfiles[currentProfileIndex]);
        }
    }

    // Hauptrender-Pipeline: Wendet Effekte an und wendet Kompensation an
    // 1. Kamerabild kopieren
    // 2. Test-Overlay anwenden (falls aktiv)
    // 3. Effekt-Stack durchlaufen
    // 4. Kompensation anwenden
    // 5. Plattformspezifische Ausgabe
    void RenderPipeline()
    {
        if (inputTexture == null || outputRT == null) return;
        if (!inputCopyRT.IsCreated()) inputCopyRT.Create();

        compensationManager.SetOriginalTexture(inputTexture);

        Graphics.Blit(inputTexture, inputCopyRT);

        if (testOverlayManager != null && testOverlayManager.IsActive)
        {
            RenderTexture temp = RenderTexture.GetTemporary(
                inputCopyRT.width, inputCopyRT.height, 0, inputCopyRT.format);
            testOverlayManager.Apply(inputCopyRT, temp);
            Graphics.Blit(temp, inputCopyRT);
            RenderTexture.ReleaseTemporary(temp);
        }

        RenderTexture source      = inputCopyRT;
        RenderTexture destination = tempA;

        foreach (Material effect in effectStack)
        {
            Graphics.Blit(source, destination, effect);
            source      = destination;
            destination = (source == tempA) ? tempB : tempA;
        }

        Graphics.Blit(source, outputRT);

        RenderTexture finalOutput = compensationManager.Process(outputRT);


        #if UNITY_IOS
        if (screenOutputUI != null)
            screenOutputUI.texture = finalOutput;
        #else
        displayMaterial.mainTexture = finalOutput;
        #endif
    }

    // Aktualisiert die Blickposition basierend auf verfügbarer Hardware
    void UpdateGaze()
    {
        if (TryGetOpenXRGaze(out Vector2 uv))
        {
            gazeUV = uv;
            return;
        }

        if (Application.platform == RuntimePlatform.IPhonePlayer)
            UpdateTouchGaze();
        else if (Application.platform == RuntimePlatform.Android)
            UpdateSimulatedGaze();
    }

    // Aktualisiert Blickposition in allen aktiven Shader-Materials
    void UpdateShaderGaze()
    {
        foreach (Material effect in effectStack)
        {
            if (effect != null && effect.HasProperty("_Center"))
                effect.SetVector("_Center", gazeUV);
        }
    }

    // Versucht Blickposition über OpenXR Eye-Tracking zu ermitteln
    bool TryGetOpenXRGaze(out Vector2 uv)
    {
        uv = gazeUV;
        if (!eyeTrackingAvailable || !eyeDevice.isValid) return false;

        if (eyeDevice.TryGetFeatureValue(CommonUsages.eyesData, out Eyes eyes))
        {
            if (eyes.TryGetFixationPoint(out Vector3 fixationPoint))
            {
                Vector3 viewport = mainCamera.WorldToViewportPoint(fixationPoint);
                uv = new Vector2(viewport.x, viewport.y);
                return true;
            }
        }
        return false;
    }

    // Aktualisiert Blickposition über Touch-Eingabe (Handheld-Geräte)
    void UpdateTouchGaze()
    {
        if (IsTestActive) return;
        if (Input.touchCount == 0) return;
        if (IsTouchOnUI()) return;

        if (compensationManager != null && compensationManager.IsAnalysisRunning())
            return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
        {
            gazeUV = new Vector2(
                touch.position.x / Screen.width,
                touch.position.y / Screen.height);
            gazeUV.x = Mathf.Clamp01(gazeUV.x);
            gazeUV.y = Mathf.Clamp01(gazeUV.y);
        }
    }

    // Aktualisiert simulierte Blickposition über linken Thumbstick (Meta Quest)
    void UpdateSimulatedGaze()
    {
        if (compensationManager != null && compensationManager.IsAnalysisRunning())
            return;

        #if UNITY_ANDROID
        Vector2 joy = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
        if (joy.magnitude > 0.1f)
        {
            gazeUV += joy * gazeMoveSpeed * Time.deltaTime;
            gazeUV.x = Mathf.Clamp01(gazeUV.x);
            gazeUV.y = Mathf.Clamp01(gazeUV.y);
        }
        #endif
    }

    // Initialisiert das Eye-Tracking über OpenXR
    void InitializeEyeTracking()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.EyeTracking, devices);

        if (devices.Count > 0)
        {
            eyeDevice            = devices[0];
            eyeTrackingAvailable = true;
        }
    }

    // Prüft, ob Touch-Eingabe auf dem Intensitäts-Slider erfolgt
    bool IsTouchOnSlider()
    {
        if (Input.touchCount == 0 || intensitySlider == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            intensitySlider.GetComponent<RectTransform>(),
            Input.GetTouch(0).position,
            null);
    }

    // Prüft, ob Touch-Eingabe auf irgendeinem UI-Element erfolgt
    bool IsTouchOnUI()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(
                    Input.GetTouch(i).fingerId))
                return true;
        }
        return false;
    }

    // Aktualisiert UI-Text mit plattformspezifischen Anweisungen und Profilname
    void UpdateUI(string profileName)
    {
        if (profileText == null) return;

        string instruction = Application.platform == RuntimePlatform.IPhonePlayer
            ? "<b>Zwei Finger</b> für Kompensation.\n<b>Double Tap</b> zum Wechseln."
            : "Drücke <b>'A'</b> zum Wechseln.\nDrücke <b>'B'</b> für Kompensation.";

        profileText.text = instruction + $"\n<color=#FFA500>Aktuelles Sehproblem: {profileName}</color>";
    }

    // Wendet ein Sehbeeinträchtigungsprofil an
    public void ApplyProfile(VisualImpairmentProfile profile)
    {
        if (profile == null || effectLibrary == null) return;

        intensitySlider?.gameObject.SetActive(profile.effects.Count > 0);

        foreach (var mat in effectStack)
            if (mat != null) Destroy(mat);
        effectStack.Clear();

        foreach (var entry in profile.effects)
        {
            var p = entry.parameters;
            Material originalMat = effectLibrary.GetMaterial(p.effectType);
            if (originalMat == null) continue;

            Material mat = Instantiate(originalMat);

            if (depthTexture != null && mat.HasProperty("_CameraDepthTexture"))
                mat.SetTexture("_CameraDepthTexture", depthTexture);

            ApplyEffectParameters(p, mat);
            effectStack.Add(mat);
        }

        var activeTypes = new List<VisualEffectType>();
        foreach (var entry in profile.effects)
            activeTypes.Add(entry.parameters.effectType);
        compensationManager.SetActiveEffects(activeTypes);
    }

    // Routet Effektparameter zum entsprechenden Anwendungs-Handler
    void ApplyEffectParameters(VisualEffectParameters p, Material mat)
    {
        switch (p.effectType)
        {
            case VisualEffectType.Blur:
                ApplyBlur((BlurParameters)p, mat);
                break;
            case VisualEffectType.Contrast:
                ApplyContrast((ContrastParameters)p, mat);
                break;
            case VisualEffectType.FieldOfView:
                ApplyFieldOfView((FieldOfViewParameters)p, mat);
                break;
            case VisualEffectType.Distortion:
                ApplyDistortion((DistortionParameters)p, mat);
                break;
            case VisualEffectType.ColorChange:
                ApplyColorChange((ColorChangeParameters)p, mat);
                break;
            case VisualEffectType.Artifacts:
                ApplyArtifacts((ArtifactsParameters)p, mat);
                break;
        }
    }

    // Wendet Unschärfe-Parameter auf Material an
    void ApplyBlur(BlurParameters blur, Material mat)
    {
        float blurStrength = blur.blurStrength;
        bool  hasDepth = depthTexture != null;

        blurStrength *= blur.useDepth && hasDepth
            ? 0.01f
            : 0.01f * masterIntensity;

        mat.SetFloat("_BlurStrength", blurStrength);
        mat.SetFloat("_Anisotropy", blur.anisotropy * masterIntensity);
        mat.SetFloat("_AxisAngle", blur.axisAngle);
        mat.SetInt  ("_Samples", blur.sampleCount);
        mat.SetFloat("_UseDepth", blur.useDepth && hasDepth ? 1f : 0f);

        float t = Mathf.Clamp01(masterIntensity / 2.0f);
        t  *= t;
        float focus = blur.focusMode > 0
            ? Mathf.Lerp(blur.focusDistance, 0.05f, t)
            : Mathf.Lerp(blur.focusDistance, 2.00f, t);

        mat.SetFloat("_FocusDistance", focus);
        mat.SetFloat("_DefocusSign", (float)blur.focusMode);
        mat.SetFloat("_UseFovea", blur.useFovea ? 1f : 0f);
        mat.SetFloat("_InvertFovea", blur.invertFovea ? 1f : 0f);
        mat.SetFloat("_FoveaRadius", blur.foveaRadius);
        mat.SetFloat("_FoveaSoftness", blur.foveaSoftness);
        mat.SetVector("_FoveaCenter", blur.foveaCenter);

        compensationManager.GetAnalyzer<BlurAnalyzer>()
            ?.SetSimulationParams(blurStrength);
    }

    // Wendet Kontrastverlust-Parameter auf Material an
    void ApplyContrast(ContrastParameters con, Material mat)
    {
        mat.SetFloat("_ContrastFactor", con.contrastFactor * masterIntensity);

        compensationManager.GetAnalyzer<ContrastAnalyzer>()
            ?.SetSimulationParams(con.contrastFactor);
    }

    // Wendet Gesichtsfeldausfall-Parameter auf Material an
    void ApplyFieldOfView(FieldOfViewParameters fov, Material mat)
    {
        float intensity = Mathf.Max(0.001f, masterIntensity);
        float radiusScaled = fov.radius;
        float sensitivityScaled = fov.sensitivityLoss;
        float softnessScaled = fov.softness;
        float irregularityScaled = fov.irregularity;

        if (fov.defectType == FieldOfViewParameters.DefectType.Peripheral)
        {
            radiusScaled = fov.radius / intensity;
            sensitivityScaled = Mathf.Clamp01(fov.sensitivityLoss * intensity);
            softnessScaled = fov.softness * Mathf.Lerp(1f, 0.8f, intensity - 1f);
            irregularityScaled = fov.irregularity * Mathf.Lerp(1f, 1.3f, intensity - 1f);
        }
        else if (fov.defectType == FieldOfViewParameters.DefectType.Central)
        {
            radiusScaled = fov.radius * intensity;
            sensitivityScaled = Mathf.Clamp01(fov.sensitivityLoss * Mathf.Pow(intensity, 1.3f));
            softnessScaled = fov.softness * Mathf.Lerp(1f, 1.2f, intensity - 1f);
            irregularityScaled = fov.irregularity * Mathf.Lerp(1f, 1.8f, intensity - 1f);
        }

        profileCenter = fov.center;

        mat.SetFloat ("_Type", (float)fov.defectType);
        mat.SetVector("_Center", gazeUV);
        mat.SetFloat ("_Radius", radiusScaled);
        mat.SetFloat ("_Softness", softnessScaled);
        mat.SetFloat ("_SensitivityLoss", sensitivityScaled);
        mat.SetFloat ("_Irregularity", irregularityScaled);
        mat.SetFloat ("_AngularIrregularity",fov.angularIrregularity);
        mat.SetFloat ("_NoiseScale", fov.noiseScale);

        if (fov.defectType == FieldOfViewParameters.DefectType.Scattered)
        {
            mat.SetFloat("_Density", fov.density * masterIntensity);
            mat.SetFloat("_SpotSize", fov.spotSize * masterIntensity);
            mat.SetFloat("_Darkness",  fov.darkness * masterIntensity);
            mat.SetFloat("_SpotSoftness", fov.spotSoftness);
            mat.SetFloat("_Asymmetry", fov.asymmetry);
            mat.SetFloat("_PreferPeriphery", fov.preferPeriphery ? 1f : 0f);
            mat.SetFloat("_PeripheryRadius", fov.peripheryRadius);
        }

        compensationManager.GetAnalyzer<FieldOfViewAnalyzer>()
            ?.SetSimulationParams((int)fov.defectType, fov.radius);
    }

    // Wendet Verzerrungs-Parameter auf Material an
    void ApplyDistortion(DistortionParameters dist, Material mat)
    {
        mat.SetFloat("_Strength", dist.strength * masterIntensity);
        mat.SetFloat("_Rotation", dist.rotation);
        mat.SetFloat("_Mode", (float)dist.mode);
        mat.SetFloat("_WaveFreq", dist.waveFrequency);
        mat.SetFloat("_CenterRad", dist.centerRadius);
    }

    // Wendet Farbveränderung-Parameter auf Material an
    void ApplyColorChange(ColorChangeParameters colorParams, Material mat)
    {
        float colorScaled = masterIntensity <= 1f
            ? colorParams.intensity * masterIntensity
            : colorParams.intensity * Mathf.Pow(masterIntensity, 1.2f);

        mat.SetFloat("_Intensity", Mathf.Clamp01(colorScaled));

        Matrix4x4 colorMatrix = Matrix4x4.identity;

        if (colorParams.defectType == ColorDefectType.Protanopia)
        {
            colorMatrix.SetRow(0, new Vector4(0.152286f, 1.052583f, -0.204868f, 0));
            colorMatrix.SetRow(1, new Vector4(0.114503f, 0.786281f,  0.099216f, 0));
            colorMatrix.SetRow(2, new Vector4(-0.003882f, -0.048116f, 1.051998f, 0));
            colorMatrix.SetRow(3, new Vector4(0, 0, 0, 1));
        }
        else if (colorParams.defectType == ColorDefectType.CataractYellow)
        {
            colorMatrix.SetRow(0, new Vector4(1.10f, 0.10f, 0.00f, 0));
            colorMatrix.SetRow(1, new Vector4(0.05f, 0.95f, 0.00f, 0));
            colorMatrix.SetRow(2, new Vector4(0.00f, 0.05f, 0.40f, 0));
            colorMatrix.SetRow(3, new Vector4(0, 0, 0, 1));
        }

        mat.SetMatrix("_ColorMatrix", colorMatrix);

        compensationManager.GetAnalyzer<ColorAnalyzer>()
            ?.SetSimulationParams(colorParams.defectType, colorParams.intensity);
    }

    // Wendet Artefakte-Parameter auf Material an
    void ApplyArtifacts(ArtifactsParameters art, Material mat)
    {
        mat.SetFloat("_Intensity", art.intensity  * masterIntensity);
        mat.SetFloat("_Density", art.density * masterIntensity);
        mat.SetFloat("_FloaterSize", art.size * masterIntensity);
        mat.SetFloat("_DriftSpeed", art.driftSpeed);
        mat.SetFloat("_Darkness", art.darkness * masterIntensity);
        mat.SetFloat("_Transparency", art.transparency);
    }
}