// TestUISetup.cs
using UnityEngine;
using UnityEngine.UI;

public class TestUISetup : MonoBehaviour
{
    [Header("Referenzen")]
    public Canvas testCanvas;
    public Camera centerEyeCamera;

    [Header("Position vor dem Auge")]
    public float distanceFromEye = 1.5f;   // Meter vor der Kamera
    public float canvasWidth     = 1.2f;   // Meter breit
    public float canvasHeight    = 0.8f;   // Meter hoch

    void Start()
    {
        SetupWorldSpaceCanvas();
    }

    public void SetupWorldSpaceCanvas()
    {
        if (testCanvas == null || centerEyeCamera == null) return;

        // World Space Canvas
        testCanvas.renderMode = RenderMode.WorldSpace;
        testCanvas.worldCamera = centerEyeCamera;

        // Direkt vor die Kamera positionieren
        Transform camTransform = centerEyeCamera.transform;
        testCanvas.transform.position = camTransform.position
                                      + camTransform.forward * distanceFromEye;
        testCanvas.transform.rotation = camTransform.rotation;

        // Größe setzen
        RectTransform rt = testCanvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(canvasWidth * 1000f, canvasHeight * 1000f);
        rt.localScale = Vector3.one * 0.001f; // 1 Unit = 1mm

        // Eigener Layer damit er ÜBER dem Quad liegt
        testCanvas.gameObject.layer = LayerMask.NameToLayer("UI");
        testCanvas.sortingOrder = 100;

        // GraphicRaycaster für Interaktion
        var raycaster = testCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            raycaster = testCanvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    // Canvas immer zur Kamera ausrichten (falls Kopf bewegt wird)
    void LateUpdate()
    {
        if (testCanvas == null || !testCanvas.gameObject.activeSelf) return;
        if (centerEyeCamera == null) return;

        Transform cam = centerEyeCamera.transform;
        testCanvas.transform.position = cam.position + cam.forward * distanceFromEye;
        testCanvas.transform.rotation = cam.rotation;
    }
}