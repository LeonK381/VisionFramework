using UnityEngine;
using UnityEngine.XR.ARFoundation;

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

    void OnEnable() { if (cameraManager != null) cameraManager.frameReceived += OnFrameReceived; }
    void OnDisable() { if (cameraManager != null) cameraManager.frameReceived -= OnFrameReceived; }

    void OnFrameReceived(ARCameraFrameEventArgs args)
    {
        ProcessColorImage(args);
        ProcessDepthImage();
    }

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

    void ProcessDepthImage()
    {
        if (occlusionManager == null) return;
        try 
        {
            Texture2D depthTex = occlusionManager.environmentDepthTexture;
            if (depthTex != null) pipelineManager.SetDepthTexture(depthTex);
        }
        catch {}
    }
}