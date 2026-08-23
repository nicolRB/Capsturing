using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NovaEspecieDeRunico", menuName = "Runico/Especie de Runico")]
public class RunicSpecies : ScriptableObject
{
    public string speciesName;
    public string speciesId;
    public string description;
    public Sprite speciesIcon;
    public List<float> baseStats; // Lista de estatísticas base da espécie
    public List<Skill> skills; // Lista de habilidades da espécie
    public List<string> elements;
}