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
        while (!OVRPermissionsRequester.IsPermissionGranted(
            OVRPermissionsRequester.Permission.PassthroughCameraAccess))
        {
            yield return null;
        }

        while (!m_cameraAccess.IsPlaying)
        {
            yield return null;
        }

        pipelineManager.SetInputTexture(m_cameraAccess.GetTexture());
        Debug.Log("Meta Passthrough Texture an Pipeline gesendet.");
    }
}