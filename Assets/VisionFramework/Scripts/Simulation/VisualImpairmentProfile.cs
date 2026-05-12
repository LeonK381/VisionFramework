using System.Collections.Generic;
using UnityEngine;


//ScriptableObject für ein Sehbeeinträchtigungsprofil
// Ein Profil kombiniert mehrere visuelle Effekte, um eine bestimmte Sehbeeinträchtigung oder Kombination von Beeinträchtigungen zu simulieren.
[CreateAssetMenu(menuName = "Vision/Visual Impairment Profile")]
public class VisualImpairmentProfile : ScriptableObject
{
    public string profileName;

    public List<VisualEffectEntry> effects = new();

    // Fügt einen neuen Effekt zum Profil hinzu
    public void AddEffect(VisualEffectType type)
    {
        var entry = new VisualEffectEntry();
        entry.CreateParameters(type);
        effects.Add(entry);
    }

    [ContextMenu("Add Blur Effect")]
    private void AddBlur()
    {
        AddEffect(VisualEffectType.Blur);
    }

    [ContextMenu("Add Contrast Loss Effect")]
    private void AddContrast()
    {
        AddEffect(VisualEffectType.Contrast);
    }
    
    [ContextMenu("Add Field Of View Effect")]
    private void AddFieldOfView()
    {
        AddEffect(VisualEffectType.FieldOfView);
    }

    [ContextMenu("Add Distortion Effect")]
    private void AddDistortion()
    {
        AddEffect(VisualEffectType.Distortion);
    }

    [ContextMenu("Add Color Change Effect")]
    private void AddColorChange() {
        AddEffect(VisualEffectType.ColorChange);
    }

    [ContextMenu("Add Artifacts Effect")]
    private void AddArtifacts() {
        AddEffect(VisualEffectType.Artifacts);
    }
}




