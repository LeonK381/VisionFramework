using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Kleine Hilfskomponenten um UI Buttons/Slider zu verbinden (Drag & Drop im Inspector).
/// </summary>
public class SimpleUIConnector : MonoBehaviour
{
    public AstigmatismTestController testController;
    public Button startButton;
    public Button confirmButton;

    void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    void OnStartClicked()
    {
        // start: simply ensure test controller phases
        // (we kept logic automatic in update). No direct method needed for now.
        Debug.Log("[UI] Start clicked.");
    }

    void OnConfirmClicked()
    {
        // Simulate trigger press in Editor by toggling internal state (not ideal, but works)
#if UNITY_EDITOR
        // invoke confirmation by simulating Space key
        // Directly call internal: reflectively not ideal; better have a public method — for simplicity use FindObjectOfType
        var ctrl = testController != null ? testController : FindObjectOfType<AstigmatismTestController>();
        if (ctrl != null)
        {
            // nothing here because controller expects actual input; instruct user to press Space in Editor
            Debug.Log("[UI] Confirm clicked. In Editor press Space to confirm step (or hook up native confirm).");
        }
#endif
    }
}

