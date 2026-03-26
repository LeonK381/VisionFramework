using System.Collections;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MetaPassthroughProvider : MonoBehaviour
{
    [SerializeField] private PassthroughCameraAccess m_cameraAccess;
    [SerializeField] private EffectPipelineManager pipelineManager;


    private IEnumerator Start()
{
    Debug.Log("MetaPassthroughProvider gestartet");

    while (!OVRPermissionsRequester.IsPermissionGranted(
        OVRPermissionsRequester.Permission.PassthroughCameraAccess))
    {
        Debug.Log("Warte auf Permission...");
        yield return null;
    }

    Debug.Log("Permission OK");

    while (!m_cameraAccess.IsPlaying)
    {
        Debug.Log("Warte auf IsPlaying...");
        yield return null;
    }

    Debug.Log("CameraAccess läuft");

    var tex = m_cameraAccess.GetTexture();

    if (tex == null)
        Debug.Log("GetTexture() ist NULL!");
    else
        Debug.Log("Texture erfolgreich erhalten!");

    pipelineManager.SetInputTexture(tex);
}
}