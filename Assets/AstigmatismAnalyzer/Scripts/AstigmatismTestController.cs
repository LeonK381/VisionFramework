using UnityEngine;

/// <summary>
/// Steuert die beiden Testphasen:
/// - Phase A: Linienstern rotieren, User markiert schlechteste Linie (Trigger)
/// - Phase B: Unschärfe-Aufbau, User bestätigt Stärke (Trigger)
/// Ergebnisse werden in AstigmatismResult geschrieben.
/// </summary>
public class AstigmatismTestController : MonoBehaviour
{
    [Header("Stimulus")]
    public Transform lineStar;         // referenziere das LineStar Prefab Transform (in Szene)

    [Header("Phase settings")]
    public float rotationSpeed = 45f;  // deg / sec in Phase A
    public float blurIncreaseSpeed = 0.25f; // per sec in Phase B

    private bool phaseA = true;
    private bool phaseB = false;

    private float selectedAxis = 0f;
    private float blurStrength = 0f;

    void Start()
    {
        phaseA = true;
        phaseB = false;
        blurStrength = 0f;
        if (lineStar == null) Debug.LogWarning("AstigmatismTestController: lineStar not assigned.");
    }

    void Update()
    {
        // Input: hier wird OVRInput benutzt, aber du kannst es durch Input.GetKeyDown für Editor ersetzen
        bool confirmPressed = false;

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space)) confirmPressed = true;
#else
        // Oculus/Touch: primary index trigger
        confirmPressed = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);
#endif

        if (phaseA)
        {
            // rotate the star
            if (lineStar != null) lineStar.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

            if (confirmPressed)
            {
                // axis = current rotation of star modulo 180
                float z = (lineStar.eulerAngles.z) % 180f;
                selectedAxis = z;
                Debug.Log($"[AstigTest] Phase A confirmed. Selected axis (deg): {selectedAxis:F1}");

                phaseA = false;
                phaseB = true;
                blurStrength = 0f;
            }
        }
        else if (phaseB)
        {
            // increment blur strength gradually
            blurStrength += blurIncreaseSpeed * Time.deltaTime;
            blurStrength = Mathf.Clamp01(blurStrength);

            // optional: feed blur value into a visualization (e.g. material)
            AstigmatismResult.previewBlur = blurStrength;

            if (confirmPressed)
            {
                AstigmatismResult.axis = selectedAxis;
                AstigmatismResult.strength = blurStrength;

                Debug.Log($"[AstigTest] Phase B confirmed. Strength: {blurStrength:F3}");

                // apply profile automatically (if manager present)
                var mgr = FindObjectOfType<ApplyAstigmatismCompensation>();
                if (mgr != null) mgr.Apply();

                phaseB = false;
            }
        }
    }
}

