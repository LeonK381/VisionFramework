using UnityEngine;
using UnityEngine.XR.ARFoundation;

// Bereitstellung des Kamerabildes und der Tiefendaten aus ARFoundation
public class AppleARKitProvider : MonoBehaviour
{
    [Header("AR Foundation Setup")]
    public ARCameraManager cameraManager;
    public AROcclusionManager occlusionManager;

    [Header("Pipeline Connection")]
    public EffectPipelineManager pipelineManager;

    [Header("Color Fix Settings")]
    public Material appleARFixMaterial;
    private RenderTexture arConversionRT;

    // Registriert den Callback für AR-Frames
    void OnEnable() { if (cameraManager != null) cameraManager.frameReceived += OnFrameReceived; }

    // Entfernt den Callback für AR-Frames
    void OnDisable() { if (cameraManager != null) cameraManager.frameReceived -= OnFrameReceived; }

    void OnFrameReceived(ARCameraFrameEventArgs args)
    {
        ProcessColorImage(args);
        ProcessDepthImage();
    }

    // Verarbeitet das Kamerabild und konvertiert YCbCr zu RGB
    void ProcessColorImage(ARCameraFrameEventArgs args)
    {
        if (appleARFixMaterial == null) return;

        for (int i = 0; i < args.textures.Count; i++)
        {
            if (args.propertyNameIds[i] == Shader.PropertyToID("_textureY"))
                appleARFixMaterial.SetTexture("_YTex", args.textures[i]);
            else if (args.propertyNameIds[i] == Shader.PropertyToID("_textureCbCr"))
                appleARFixMaterial.SetTexture("_UVTex", args.textures[i]);
        }

        if (arConversionRT == null)
        {
            arConversionRT = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default);
            arConversionRT.Create();
            pipelineManager.SetInputTexture(arConversionRT);
        }

        Graphics.Blit(null, arConversionRT, appleARFixMaterial);
    }

    // Extrahiert Tiefendaten vom LiDAR-Sensor
    void ProcessDepthImage()
    {
        if (occlusionManager == null) return;
        try
        {
            Texture2D depthTex = occlusionManager.environmentDepthTexture;
            Debug.Log("Depth image ist da");
            if (depthTex != null) pipelineManager.SetDepthTexture(depthTex);
        }
        catch { }
    }
}