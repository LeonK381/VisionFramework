using System.Collections;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

public class ShaderSampleManager : MonoBehaviour
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
        // Passthrough-Texture NICHT auf Quad,
        // sondern in die Pipeline einspeisen
        pipelineManager.SetInputTexture(m_cameraAccess.GetTexture());
    }
}

