using UnityEngine;

[CreateAssetMenu(fileName = "NovoElemento", menuName = "Runico/Elemento")]
public class Element : ScriptableObject
{
    [SerializeField] private string elementId;
    [SerializeField] private string elementName;

    public string ElementId => elementId;
    public string ElementName => elementName;
}