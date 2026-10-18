using UnityEngine;
using System.Collections.Generic;

public class HitboxItem : MonoBehaviour
{
    private Runic ownerRunic;
    private Skill skill;
    private float storedPower;
    private bool continuous;
    private readonly Dictionary<Component, float> nextDamageTime = new();
    private readonly HashSet<Component> knockedBackTargets = new();
    private readonly HashSet<Component> hitTargets = new();
    [SerializeField] private float damageInterval = 0.25f;

    public void Setup(Runic owner, Skill skill, float power, bool continuous)
    {
        ownerRunic = owner;
        this.skill = skill;
        storedPower = power;
        this.continuous = continuous;
        hitTargets.Clear();
        knockedBackTargets.Clear();
        nextDamageTime.Clear();
    }

    private void OnTriggerEnter(Collider other) => TryApplyHit(other);
    private void OnTriggerStay(Collider other) => TryApplyHit(other);

    private void TryApplyHit(Collider other)
    {
        if (ownerRunic == null || skill == null) return;

        float knockback = skill.KnockBackValue;

        PlayerController playerTarget = other.GetComponentInParent<PlayerController>();
        if (playerTarget != null)
        {
            if (ownerRunic.TameState == RunicState.Tamed
                || playerTarget.InvencibilityTime > 0
                || !CanHit(playerTarget)) return;

            int playerDamage = playerTarget.CalculateDamage(skill, ownerRunic, storedPower);
            playerTarget.TakeDamage(playerDamage);
            playerTarget.ApplyKnockback(ownerRunic.transform, knockback);
            return;
        }

        Runic target = other.GetComponentInParent<Runic>();
        if (target == null || target == ownerRunic
            || target.InvencibilityTime > 0
            || !CanHit(target)) return;

        DamageProfile profile = DamageProfile.FromSkill(skill, ownerRunic);
        int finalDamage = target.CalculateDamage(storedPower, profile);
        Color damageColor = profile.Element != null ? profile.Element.ElementColor : Color.white;
        float damageScale = DamageCalculator.GetDamageMultiplier(profile, target.Elements);
        target.TakeDamage(finalDamage, ownerRunic, damageColor: damageColor, damageScale: damageScale);

        if (knockedBackTargets.Add(target))
            target.ApplyKnockback(ownerRunic, knockback);
    }

    private bool CanHit(Component target)
    {
        if (!continuous) return hitTargets.Add(target);

        if (nextDamageTime.TryGetValue(target, out float nextTime) && Time.time < nextTime)
            return false;

        nextDamageTime[target] = Time.time + damageInterval;
        return true;
    }
}