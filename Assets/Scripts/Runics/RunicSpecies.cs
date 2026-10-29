using UnityEngine;
using System.Collections.Generic;

public enum IntraspeciesRelation { Indifferent, Allied, Territorial, Hostile }

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

    [Header("Sizes")]
    [SerializeField] private float width = 1f;
    [SerializeField] private float height = 1f;

    public float Width => width;
    public float Height => height;

    [Header("Base Stats")]
    [SerializeField] private int baseHP;
    [SerializeField] private float baseAttack;
    [SerializeField] private float baseDefense;
    [SerializeField] private float baseWeight = 1;
    [SerializeField] private float baseKnockbackResistance;
    [SerializeField] private float baseSpeed;
    [SerializeField] private float baseWalkSpeed;
    [SerializeField] private float baseSprintSpeed;
    [SerializeField] private float baseMagic;
    [SerializeField] private float baseMagicDefense;
    [SerializeField] private float recovery = 2;
    [SerializeField] private float specialRecovery = 10;
    [SerializeField] private float aggression = 0.5f;
    [SerializeField] private float specialAggression = 0.2f;
    [SerializeField] private List<Element> elements;

    [Header("Abilities")]
    [Tooltip("Skills always available to individuals of this species.")]
    [SerializeField] private List<Skill> basicSkills;
    [Tooltip("Skills that individuals of this species can learn.")]
    [SerializeField] private List<Skill> skills;

    [Header("Behavior")]
    [SerializeField] private CombatStyle defaultCombatStyle;
    [SerializeField] private WildNature defaultNature;
    [SerializeField] private bool packSpecies;
    [SerializeField] private float packSpawnChance;
    [SerializeField] private int minPackSize;
    [SerializeField] private int maxPackSize;
    [SerializeField] private IntraspeciesRelation sameSpeciesRelation;

    public string SpeciesName => speciesName;
    public string SpeciesId => speciesId;
    public string Description => description;
    public Sprite SpeciesIcon => speciesIcon;
    public List<GameObject> SpeciesModels => speciesModels;
    public int BaseHP => baseHP;
    public float BaseAttack => baseAttack;
    public float BaseDefense => baseDefense;
    public float BaseWeight => baseWeight;
    public float BaseKnockbackResistance => baseKnockbackResistance;
    public float BaseSpeed => baseSpeed;
    public float BaseWalkSpeed => baseWalkSpeed;
    public float BaseSprintSpeed => baseSprintSpeed;
    public float BaseMagic => baseMagic;
    public float BaseMagicDefense => baseMagicDefense;
    public float Recovery => recovery;
    public float SpecialRecovery => specialRecovery;
    public float Aggression => aggression;
    public float Specialaggression => specialAggression;
    public List<Element> Elements => elements;
    public List<Skill> BasicSkills => basicSkills;
    public List<Skill> Skills => skills;
    public CombatStyle DefaultCombatStyle => defaultCombatStyle;
    public WildNature DefaultNature => defaultNature;
    public IntraspeciesRelation SameSpeciesRelation => sameSpeciesRelation;
}