using UnityEngine;
using System.Collections.Generic;

public enum SpeciesRelation { Predator, Prey, Rival, Territorial, Indifferent, Allied }

[System.Serializable]
public class SpeciesRelationEntry
{
    public RunicSpecies speciesA;
    public RunicSpecies speciesB;
    public SpeciesRelation relation;
}

[CreateAssetMenu(fileName = "RunicDatabase", menuName = "Runic/Runic Database")]
public class RunicDatabase : ScriptableObject
{
    [Header("Species")]
    [SerializeField] private List<RunicSpecies> allSpecies = new List<RunicSpecies>();
    
    // Runtime-only lookup cache, built on demand.
    private Dictionary<string, RunicSpecies> lookupSpecies;

    public List<RunicSpecies> AllSpecies => allSpecies;
    public Dictionary<string, RunicSpecies> LookupSpecies => lookupSpecies;

    
    [Header("SpeciesRelations")]
    [SerializeField] private List<SpeciesRelationEntry> speciesRelations;
    private Dictionary<(string, string), SpeciesRelation> relationLookup;

    public Dictionary<(string, string), SpeciesRelation> RelationLookup => relationLookup;

    [Header("Elements")]
    [SerializeField] private List<Element> allElements = new List<Element>();
    private Dictionary<string, Element> lookupElement;

    public List<Element> AllElements => allElements;
    public Dictionary<string, Element> LookupElement => lookupElement;

    [Header("Skills")]
    [SerializeField] private List<Skill> allSkills = new List<Skill>();
    private Dictionary<string, Skill> lookupSkill;

    private void BuildLookup()
    {
        lookupSpecies = new Dictionary<string, RunicSpecies>();

        foreach (var species in allSpecies)
        {
            if (species == null) continue;

            if (string.IsNullOrEmpty(species.SpeciesId))
            {
                Debug.LogWarning($"RunicDatabase: '{species.name}' has an empty SpeciesId and was ignored.", species);
                continue;
            }

            if (lookupSpecies.ContainsKey(species.SpeciesId))
            {
                Debug.LogError($"RunicDatabase: duplicate SpeciesId '{species.SpeciesId}' found in '{species.name}' and '{lookupSpecies[species.SpeciesId].name}'.");
                continue; // Keep the first entry and ignore the duplicate.
            }

            lookupSpecies[species.SpeciesId] = species;
        }
    }

    public RunicSpecies GetSpeciesById(string SpeciesId)
    {
        if (lookupSpecies == null) BuildLookup();

        if (lookupSpecies.TryGetValue(SpeciesId, out RunicSpecies result))
            return result;

        Debug.LogWarning($"RunicDatabase: no species found for SpeciesId '{SpeciesId}'.");
        return null;
    }

    private void BuildRelationLookup()
    {
        relationLookup = new Dictionary<(string, string), SpeciesRelation>();

        foreach (SpeciesRelationEntry entry in speciesRelations)
        {
            if (entry == null || entry.speciesA == null || entry.speciesB == null)
                continue;

            string speciesAId = entry.speciesA.SpeciesId;
            string speciesBId = entry.speciesB.SpeciesId;

            if (string.IsNullOrEmpty(speciesAId) || string.IsNullOrEmpty(speciesBId))
                continue;

            var key = (speciesAId, speciesBId);

            if (!relationLookup.ContainsKey(key))
                relationLookup.Add(key, entry.relation);
            else
                Debug.LogWarning($"RunicDatabase: duplicate relation for '{speciesAId}' and '{speciesBId}'.");
        }
    }

    public bool TryGetRelation(RunicSpecies speciesA, RunicSpecies speciesB, out SpeciesRelation relation)
    {
        if (relationLookup == null)
            BuildRelationLookup();

        relation = SpeciesRelation.Indifferent;

        if (speciesA == null || speciesB == null)
            return false;

        string speciesAId = speciesA.SpeciesId;
        string speciesBId = speciesB.SpeciesId;
        if (string.IsNullOrEmpty(speciesAId) || string.IsNullOrEmpty(speciesBId))
            return false;

        if (relationLookup.TryGetValue((speciesAId, speciesBId), out relation))
            return true;

        if (!relationLookup.TryGetValue((speciesBId, speciesAId), out SpeciesRelation reverseRelation))
            return false;

        relation = reverseRelation switch
        {
            SpeciesRelation.Predator => SpeciesRelation.Prey,
            SpeciesRelation.Prey => SpeciesRelation.Predator,
            _ => reverseRelation
        };

        return true;
    }

    public SpeciesRelation GetRelation(RunicSpecies speciesA, RunicSpecies speciesB)
    {
        SpeciesRelation relation = SpeciesRelation.Indifferent;

        if (TryGetRelation(speciesA, speciesB, out SpeciesRelation foundRelation))
            relation = foundRelation;

        return relation;
    }

    public Skill GetSkillById(string skillId)
    {
        if (lookupSkill == null)
        {
            lookupSkill = new Dictionary<string, Skill>();

            foreach (Skill skill in allSkills)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.SkillId))
                    lookupSkill[skill.SkillId] = skill;
            }
        }

        lookupSkill.TryGetValue(skillId, out Skill result);
        return result;
    }

    public List<Skill> GetSkillsByIds(List<string> skillIds)
    {
        List<Skill> result = new List<Skill>();

        if (skillIds == null) return result;

        foreach (string skillId in skillIds)
        {
            Skill skill = GetSkillById(skillId);

            if (skill != null)
                result.Add(skill);
        }

        return result;
    }
}