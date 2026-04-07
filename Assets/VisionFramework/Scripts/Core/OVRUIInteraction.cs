// OVRUIInteraction.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OVRUIInteraction : MonoBehaviour
{
    public OVRInputModule ovrInputModule;   // auf EventSystem GameObject
    public LineRenderer   laserPointer;     // visueller Strahl vom Controller
    public Transform      rightController;

    [Header("Layer für UI-Raycast")]
    public LayerMask uiLayerMask;

    void Update()
    {
        UpdateLaserPointer();
        HandleControllerInput();
    }

    void UpdateLaserPointer()
    {
        if (laserPointer == null || rightController == null) return;

        laserPointer.SetPosition(0, rightController.position);

        Ray ray = new Ray(rightController.position, rightController.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 5f, uiLayerMask))
            laserPointer.SetPosition(1, hit.point);
        else
            laserPointer.SetPosition(1, rightController.position
                                      + rightController.forward * 3f);
    }

    void HandleControllerInput()
    {
        // Index Trigger als "Klick"
        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
        {
            Ray ray = new Ray(rightController.position, rightController.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 5f, uiLayerMask))
            {
                var button = hit.collider.GetComponent<Button>();
                button?.onClick.Invoke();
            }
        }
    }
}
