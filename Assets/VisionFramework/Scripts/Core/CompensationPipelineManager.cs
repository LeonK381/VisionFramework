using UnityEngine;

public class CompensationPipelineManager : MonoBehaviour
{
    public Material sharpenMaterial;

    public RenderTexture compensatedRT;

    private bool compensationActive = false;

    public void ToggleCompensation()
    {
        compensationActive = !compensationActive;
    }

    public RenderTexture Process(RenderTexture input)
    {
        if (!compensationActive)
            return input;

        Debug.Log("Compensation startet");
        float blur = AnalyzeBlur(input);
        float strength = MapBlurToCompensation(blur);
        Debug.Log("Compensation Stärke" + strength);

        sharpenMaterial.SetFloat("_Strength", strength);

        Graphics.Blit(input, compensatedRT, sharpenMaterial);

        return compensatedRT;
    }

    float AnalyzeBlur(RenderTexture rt)
{
    Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);

    RenderTexture.active = rt;
    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
    tex.Apply();

    Color[] pixels = tex.GetPixels();

    float variance = 0f;
    float mean = 0f;

    foreach (var p in pixels)
        mean += p.grayscale;

    mean /= pixels.Length;

    foreach (var p in pixels)
        variance += Mathf.Pow(p.grayscale - mean, 2);

    variance /= pixels.Length;

    return variance;
}

float MapBlurToCompensation(float blur)
{
    float normalized = Mathf.Clamp01(1.0f - blur * 10f);
    return normalized;
}
}
