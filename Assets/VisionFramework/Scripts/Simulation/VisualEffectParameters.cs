using UnityEngine;

// Abstrakte Basisklasse für alle visuellen Effektparameter.
// Definiert die gemeinsame Schnittstelle für alle Effekttypen und ermöglicht polymorphe Speicherung in VisualEffectEntry durch [SerializeReference].
[System.Serializable]
public abstract class VisualEffectParameters
{
    [HideInInspector]
    public VisualEffectType effectType;
}
