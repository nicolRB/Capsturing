using UnityEngine;
using System.Collections.Generic;

public class HitboxItem : MonoBehaviour
{
    private Runic ownerRunic;
    private float storedPower;
    private float storedKnockback;
    private bool continuous;
    private readonly Dictionary<Component, float> nextDamageTime = new();
    private readonly HashSet<Component> knockedBackTargets = new();
    [SerializeField] private float damageInterval = 0.25f;

    private HashSet<Component> hitTargets = new HashSet<Component>();

    public void Setup(Runic owner, float power, float knockback, bool continuous)
    {
        ownerRunic = owner;
        storedPower = power;
        storedKnockback = knockback;
        this.continuous = continuous;
        hitTargets.Clear();
        knockedBackTargets.Clear();
    }

    private void OnTriggerEnter(Collider other) => TryApplyHit(other);
    private void OnTriggerStay(Collider other) => TryApplyHit(other);
    
    private void TryApplyHit(Collider other)
    {
        Debug.Log("Hitbox hit: " + other.gameObject.name, other.gameObject);
        if (ownerRunic == null) return;

        PlayerController playerTarget = other.GetComponentInParent<PlayerController>();
        if (playerTarget != null)
        {
            if (ownerRunic.TameState == RunicState.Tamed || !CanHit(playerTarget)) return;

            playerTarget.TakeDamage(Mathf.RoundToInt(storedPower));
            playerTarget.ApplyKnockback(ownerRunic.transform, storedKnockback);
            return;
        }

        Runic target = other.GetComponentInParent<Runic>();
        if (target == null || target == ownerRunic || !CanHit(target)) return;

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(storedPower));
        target.TakeDamage(finalDamage, ownerRunic);
        if (knockedBackTargets.Add(target))
            target.ApplyKnockback(ownerRunic, storedKnockback);
    }

    private bool CanHit(Component target)
    {
        if (!continuous) return hitTargets.Add(target);

        if (nextDamageTime.TryGetValue(target, out float nextTime) &&
            Time.time < nextTime)
            return false;

        nextDamageTime[target] = Time.time + damageInterval;
        return true;
    }
}
