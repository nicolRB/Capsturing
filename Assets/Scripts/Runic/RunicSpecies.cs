using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewRunicSpecies", menuName = "Runic/Runic Species")]
public class RunicSpecies : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string speciesName;
    [SerializeField] private string speciesId;
    [SerializeField] private string description;
    [Header("Visuals")]
    [SerializeField] private Sprite speciesIcon;
    [Tooltip("Models available when instantiating this species.")]
    [SerializeField] private List<GameObject> speciesModels;
    [Header("Base Stats")]
    [SerializeField] private float baseHP;
    [SerializeField] private float baseAttack;
    [SerializeField] private float baseDefense;
    [SerializeField] private float baseSpeed;
    [SerializeField] private float baseMagic;
    [SerializeField] private float baseMagicDefense;
    [SerializeField] private List<Element> elements;
    [Header("Abilities")]
    [Tooltip("Skills always available to individuals of this species.")]
    [SerializeField] private List<Skill> basicSkills;
    [Tooltip("Skills that individuals of this species can learn.")]
    [SerializeField] private List<Skill> skills;

    public string SpeciesName => speciesName;
    public string SpeciesId => speciesId;
    public string Description => description;
    public Sprite SpeciesIcon => speciesIcon;
    public List<GameObject> SpeciesModels => speciesModels;
    public float BaseHP => baseHP;
    public float BaseAttack => baseAttack;
    public float BaseDefense => baseDefense;
    public float BaseSpeed => baseSpeed;
    public float BaseMagic => baseMagic;
    public float BaseMagicDefense => baseMagicDefense;
    public List<Element> Elements => elements;
    public List<Skill> BasicSkills => basicSkills;
    public List<Skill> Skills => skills;
}