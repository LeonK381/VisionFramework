using UnityEngine;

/// <summary>
/// Positioniert und skaliert ein Quad so, dass es im angegebenen Abstand die Kamera-Frustum exakt füllt.
/// Optional: Overscan, vertical offset, Texture-Aspect-Korrektur.
/// Verwendung: Quad als Child der Kamera (CenterEyeAnchor) oder unabhängig (dann TargetCamera angeben).
/// </summary>
[ExecuteAlways]
public class FitQuadToCameraAdvanced : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera targetCamera;
    public float distance = 0.9f;

    [Header("Quad Tuning")]
    [Range(1.0f, 1.1f)]
    public float overscan = 1.0f;
    [Range(-0.2f, 0.2f)]
    public float verticalOffset = 0.0f;

    [Header("Texture Aspect (optional)")]
    public Material optionalMaterialForAspect;
    public bool parentToCamera = true;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        Fit();
    }

#if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying) Fit();
    }
#endif

    public void Fit()
    {
        if (targetCamera == null) return;

        float minDist = Mathf.Max(targetCamera.nearClipPlane + 0.001f, 0.01f);
        if (distance <= minDist) distance = minDist + 0.01f;

        if (parentToCamera && transform.parent != targetCamera.transform)
        {
            transform.SetParent(targetCamera.transform, false);
        }

        transform.localPosition = new Vector3(0f, verticalOffset * distance, distance);
        transform.localRotation = Quaternion.identity;

        Vector3[] corners = new Vector3[4];
        targetCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), distance, Camera.MonoOrStereoscopicEye.Mono, corners);

        Vector3 ll = corners[0];
        Vector3 ul = corners[1];
        Vector3 ur = corners[2];
        Vector3 lr = corners[3];

        float width = Vector3.Distance(ll, lr);
        float height = Vector3.Distance(ll, ul);

        width *= overscan;
        height *= overscan;

        if (optionalMaterialForAspect != null)
        {
            Texture tex = optionalMaterialForAspect.GetTexture("_MainTex");
            if (tex != null)
            {
                float texAspect = (float)tex.width / tex.height;
                float camAspect = width / height;
                if (Mathf.Abs(texAspect - camAspect) > 0.01f)
                {
                    float desiredWidth = height * texAspect;
                    width = desiredWidth;
                }
            }
        }

        transform.localScale = new Vector3(width, height, 1f);
    }
}



