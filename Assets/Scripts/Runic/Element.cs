using UnityEngine;

[CreateAssetMenu(fileName = "NewElement", menuName = "Runic/Element")]
public class Element : ScriptableObject
{
    [SerializeField] private string elementId;
    [SerializeField] private string elementName;

    public string ElementId => elementId;
    public string ElementName => elementName;
}