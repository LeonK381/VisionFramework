using System.Collections;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;

// Bereitstellung des Passthrough-Kamerabilds
public class MetaPassthroughProvider : MonoBehaviour
{
    [SerializeField] private PassthroughCameraAccess m_cameraAccess;
    [SerializeField] private EffectPipelineManager pipelineManager;
    private IEnumerator Start()
    {
        // Auf Kamera-Berechtigung warten
        while (!OVRPermissionsRequester.IsPermissionGranted(
            OVRPermissionsRequester.Permission.PassthroughCameraAccess))
        {
            yield return null;
        }

        // Auf Kamera-Start warten
        while (!m_cameraAccess.IsPlaying)
        {
            yield return null;
        }

        // Textur extrahieren
        var tex = m_cameraAccess.GetTexture();

        if (tex == null)
        {
            Debug.LogError("Failed to get texture from camera access!");
        }
        pipelineManager.SetInputTexture(tex);
    }
}