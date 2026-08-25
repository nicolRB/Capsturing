using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NovaEspecieDeRunico", menuName = "Runico/Especie de Runico")]
public class RunicSpecies : ScriptableObject
{
    public string speciesName;
    public string speciesId;
    public string description;
    public Sprite speciesIcon;
    public float baseHP;
    public float baseAttack;
    public float baseDefense;
    public float baseSpeed;
    public float baseMagic;
    public float baseMagicDefense;
    public List<Element> elements;
    public List<Skill> basicSkills; // Lista de habilidades sempre disponíveis para índivíduos da espécie
    public List<Skill> skills; // Lista de habilidades que a espécie pode aprender
}