using UnityEngine;

// Manager für Test-Overlays während interaktiver Analysen
// Verwaltet temporäre visuelle Overlays die während Sehtests über das Kamerabild gelegt werden
public class TestOverlayManager : MonoBehaviour
{
    [Header("Overlay Materials")]
    public Material blurOverlayMaterial;
    public Material centralOverlayMaterial;
    public Material contrastOverlayMaterial;
    public Material colorOverlayMaterial;
    public Material distortionOverlayMaterial;

    private Texture activeOverlay = null;
    private Material activeMaterial = null;

   // Gibt an, ob aktuell ein Overlay aktiv ist
    public bool IsActive => activeOverlay != null;

    // Setzt ein neues Test-Overlay
    public void SetOverlay(Texture tex, Material mat)
    {
        activeOverlay = tex;
        activeMaterial = mat;
    }

    // Entfernt das aktuelle Overlay
    public void ClearOverlay()
    {
        activeOverlay = null;
        activeMaterial = null;
    }

    // Wendet das aktive Overlay auf das Kamerabild an
    public void Apply(RenderTexture src, RenderTexture dst)
    {
        if (activeOverlay == null || activeMaterial == null)
        {
            Graphics.Blit(src, dst);
            return;
        }

        activeMaterial.SetTexture("_OverlayTex", activeOverlay);
        Graphics.Blit(src, dst, activeMaterial);
    }
}