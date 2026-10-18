using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewElement", menuName = "Runic/Element")]
public class Element : ScriptableObject
{
    [Header("Basic Identity")]
    [SerializeField] private string elementId;
    [SerializeField] private string elementName;
    [SerializeField] private Color elementColor;

    [Header("Elemental Interactions")]
    [SerializeField] private List<Element> elementVulnerabilities = new List<Element>();
    [SerializeField] private float vulnerabilityMultiplier = 2f;
    [SerializeField] private List<Element> elementResistances = new List<Element>();
    [SerializeField] private float resistanceMultiplier = 0.5f;

    /*
    [Header("Elemental Reactions")]
    
    [Header("Elemental Conditions")]
    */

    public string ElementId => elementId;
    public string ElementName => elementName;
    public Color ElementColor => elementColor;

    public float GetIncomingMultiplier(Element attacking)
    {
        if (attacking == null) return 1f;
        if (elementVulnerabilities.Contains(attacking)) return vulnerabilityMultiplier;
        if (elementResistances.Contains(attacking)) return resistanceMultiplier;
        return 1f;
    }
}