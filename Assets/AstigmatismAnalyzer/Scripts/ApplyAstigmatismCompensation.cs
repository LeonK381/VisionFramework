using UnityEngine;

/// <summary>
/// Setzt die ermittelten Parameter in das Kompensations-Material.
/// </summary>
public class ApplyAstigmatismCompensation : MonoBehaviour
{
    [Header("Material (Astigmatism compensation)")]
    public Material astigmatismMaterial; // Material, das den Shader "Custom/AstigCompensation" nutzt

    void Start()
    {
        // optional: apply if results exist
        // Apply();
    }

    public void Apply()
    {
        if (astigmatismMaterial == null)
        {
            Debug.LogWarning("ApplyAstigmatismCompensation: astigmatismMaterial not set.");
            return;
        }

        astigmatismMaterial.SetFloat("_AstigAxis", AstigmatismResult.axis);
        astigmatismMaterial.SetFloat("_AstigStrength", AstigmatismResult.strength);

        Debug.Log($"[ApplyAstig] Applied axis={AstigmatismResult.axis:F1} strength={AstigmatismResult.strength:F3}");
    }
}

