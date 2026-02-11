using UnityEngine;

[ExecuteInEditMode] // Damit wir das Ergebnis schon im Editor sehen
public class PerfectQuadScaler : MonoBehaviour
{
    public Camera targetCamera;
    [Tooltip("Distanz zur Kamera. 0.1 (10cm) ist ideal für VR-Filter.")]
    public float distance = 0.1f; 

    void LateUpdate() // LateUpdate sorgt dafür, dass die Kamera-Position aktuell ist
    {
        if (targetCamera == null) return;

        // 1. Positioniere das Quad exakt mittig vor der Kamera
        transform.position = targetCamera.transform.position + targetCamera.transform.forward * distance;
        
        // 2. Richte das Quad plan zur Kamera aus
        transform.rotation = targetCamera.transform.rotation;

        // 3. Berechne die Größe basierend auf dem Field of View (FOV)
        // Die Formel nutzt den Tangens des halben Winkels
        float frustumHeight = 2.0f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float frustumWidth = frustumHeight * targetCamera.aspect;

        // 4. Skaliere das Quad exakt auf diese Maße
        // Ein Unity-Quad ist standardmäßig 1x1 Meter groß
        transform.localScale = new Vector3(frustumWidth, frustumHeight, 1f);
    }
}
