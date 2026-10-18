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

            // TODO: aplicar defesa do jogador quando ele tiver stats
            playerTarget.TakeDamage(Mathf.RoundToInt(storedPower));
            playerTarget.ApplyKnockback(ownerRunic.transform, knockback);
            return;
        }

        Runic target = other.GetComponentInParent<Runic>();
        if (target == null || target == ownerRunic
            || target.InvencibilityTime > 0
            || !CanHit(target)) return;

        int finalDamage = target.CalculateDamage(skill, ownerRunic, storedPower);
        target.TakeDamage(finalDamage, ownerRunic);

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