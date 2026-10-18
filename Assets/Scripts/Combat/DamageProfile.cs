using System.Collections.Generic;
using UnityEngine;

public readonly struct DamageProfile
{
    public SkillDamageType DamageType { get; }
    public Element Element { get; }
    public float AttackStat { get; }
    public bool HasStab { get; }

    public DamageProfile(SkillDamageType damageType, Element element, float attackStat, bool hasStab = false)
    {
        DamageType = damageType;
        Element = element;
        AttackStat = attackStat;
        HasStab = hasStab;
    }

    public static DamageProfile FromSkill(Skill skill, Runic attacker)
    {
        bool hasStab = attacker.Elements != null
            && attacker.Elements.Contains(skill.SkillElement);

        return new DamageProfile(
            skill.DamageType,
            skill.SkillElement,
            attacker.GetKeyStatValue(skill.KeyStat),
            hasStab);
    }
}

public static class DamageCalculator
{
    public static float GetDamageMultiplier(
        DamageProfile profile,
        IList<Element> defenderElements = null)
    {
        float multiplier = 1f;

        if (defenderElements != null)
        {
            foreach (Element defenderElement in defenderElements)
            {
                if (defenderElement != null)
                    multiplier *= defenderElement.GetIncomingMultiplier(profile.Element);
            }
        }

        if (profile.HasStab)
            multiplier *= 1.25f;

        return multiplier;
    }

    public static int Calculate(
        float totalPower,
        DamageProfile profile,
        float defense,
        float magicDefense,
        IList<Element> defenderElements = null)
    {
        float mitigationDefense = profile.DamageType switch
        {
            SkillDamageType.Magical => magicDefense,
            SkillDamageType.True => 0f,
            _ => defense
        };

        float mitigation = mitigationDefense / Mathf.Max(mitigationDefense + profile.AttackStat, 0.01f);
        float damage = totalPower * 2f * (1f - mitigation);
        damage *= GetDamageMultiplier(profile, defenderElements);

        damage *= Random.Range(0.85f, 1.15f);

        return Mathf.Max(Mathf.RoundToInt(damage), Mathf.CeilToInt(totalPower * 0.1f));
    }
}