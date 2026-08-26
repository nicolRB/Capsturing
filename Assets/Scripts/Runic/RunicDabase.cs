using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "RunicDatabase", menuName = "Runico/Database de Espécies")]
public class RunicDatabase : ScriptableObject
{
    [Header("Espécies")]
    public List<RunicSpecies> allSpecies = new List<RunicSpecies>();

    // Cache construído sob demanda — não serializado, só existe em runtime
    private Dictionary<string, RunicSpecies> lookupSpecies;

    [Header("Elementos")]
    public List<Element> allElements = new List<Element>();
    private Dictionary<string, Element> lookupElement;

    [Header("Habilidades")]
    public List<Skill> allSkills = new List<Skill>();
    private Dictionary<string, Skill> lookupSkill;

    private void BuildLookup()
    {
        lookupSpecies = new Dictionary<string, RunicSpecies>();

        foreach (var species in allSpecies)
        {
            if (species == null) continue;

            if (string.IsNullOrEmpty(species.speciesId))
            {
                Debug.LogWarning($"RunicDatabase: '{species.name}' tem speciesId vazio, ignorado.", species);
                continue;
            }

            if (lookupSpecies.ContainsKey(species.speciesId))
            {
                Debug.LogError($"RunicDatabase: speciesId duplicado '{species.speciesId}' em '{species.name}' e '{lookupSpecies[species.speciesId].name}'.");
                continue; // mantém a primeira, ignora a duplicata
            }

            lookupSpecies[species.speciesId] = species;
        }
    }

    public RunicSpecies GetSpeciesById(string speciesId)
    {
        if (lookupSpecies == null) BuildLookup();

        if (lookupSpecies.TryGetValue(speciesId, out RunicSpecies result))
            return result;

        Debug.LogWarning($"RunicDatabase: nenhuma espécie encontrada para speciesId '{speciesId}'.");
        return null;
    }

    public Skill GetSkillById(string skillId)
    {
        if (lookupSkill == null)
        {
            lookupSkill = new Dictionary<string, Skill>();

            foreach (Skill skill in allSkills)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.skillId))
                    lookupSkill[skill.skillId] = skill;
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