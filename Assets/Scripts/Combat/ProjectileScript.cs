using UnityEngine;

public enum ProjectileLimiter {
    UntilHit,
    Range,
    Time
}

public class ProjectileScript : MonoBehaviour
{
    [SerializeField] private float power;
    [SerializeField] private float knockback;
    [SerializeField] private float range;
    [SerializeField] private bool playerProjectile;
    private Vector3 spawnPosition;
    [SerializeField] private ProjectileLimiter limiter;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private Runic owner;
    [SerializeField] private LayerMask hitLayerMask;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Collider hitbox;
    [SerializeField] private Element element;
    [SerializeField] private SkillDamageType damageType;
    private float sourceAttackStat;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnPosition = transform.position;
    }

    void Update()
    {
        if (limiter == ProjectileLimiter.Range)
        {
            if (Vector3.Distance(spawnPosition, transform.position) >= range)
            {
                // Optional Dissipation effect goes here maybe
                Destroy(gameObject);
            }
        }
    }

    public void Setup(float pwr, float kb, float speed, ProjectileLimiter limit, 
    float mass, bool gravity, LayerMask layerMask, float rng = 0, float lifeTime = 0, Runic own = null, 
    bool player = false, SkillDamageType? projectileDamageType = null, Element element = null, float attackStat = 0f)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        power = pwr;
        knockback = kb;
        range = rng;
        limiter = limit;
        owner = own;
        playerProjectile = player;
        if (projectileDamageType.HasValue)
            damageType = projectileDamageType.Value;
        if (element != null)
            this.element = element;
        sourceAttackStat = attackStat;
        if (layerMask != 0)
        {
            hitLayerMask = layerMask;
        }

        if (rb != null)
        {
            rb.useGravity = gravity;
            rb.mass = mass;
            rb.linearVelocity = transform.forward * speed;
            
            if (layerMask != 0)
                rb.includeLayers = layerMask;
        }

        if (limit == ProjectileLimiter.Time)
            Destroy(gameObject, lifeTime);

        rb.linearVelocity = transform.forward * speed;
    }

    public void Setup(Runic own, Skill skill, float projectileSpeed, float attackStat, bool player)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        owner = own;
        playerProjectile = player;
        power = skill.Power;
        knockback = skill.KnockBackValue;
        damageType = skill.DamageType;
        element = skill.SkillElement;
        sourceAttackStat = attackStat;
        spawnPosition = transform.position;

        if (rb != null)
            rb.linearVelocity = transform.forward * projectileSpeed;

        if (limiter == ProjectileLimiter.Time)
            Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Runic targetRunic = other.GetComponent<Runic>();
        
        if (owner != null && targetRunic == owner) return;

        if (targetRunic == null)
            targetRunic = other.GetComponentInParent<Runic>();

        PlayerController targetPlayer = other.GetComponent<PlayerController>();
        if (targetPlayer == null)
            targetPlayer = other.GetComponentInParent<PlayerController>();

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.LookRotation(transform.forward));
        }

        if (targetRunic != null && targetRunic != owner 
        && !(targetRunic.TameState == RunicState.Tamed && playerProjectile))
        {
            if (owner != null)
            {
                float attackStat = sourceAttackStat > 0f
                    ? sourceAttackStat
                    : owner.GetKeyStatValue(damageType == SkillDamageType.Magical
                        ? SkillKeyStat.Magic
                        : SkillKeyStat.Attack);
                bool hasStab = owner.Elements != null && owner.Elements.Contains(element);
                DamageProfile profile = new DamageProfile(damageType, element, attackStat, hasStab);
                int damage = targetRunic.CalculateDamage(power, profile);
                Color damageColor = profile.Element != null ? profile.Element.ElementColor : Color.white;
                float damageScale = DamageCalculator.GetDamageMultiplier(profile, targetRunic.Elements);
                targetRunic.TakeDamage(damage, owner, damageColor: damageColor, damageScale: damageScale);
            }
            else if (playerProjectile)
            {
                DamageProfile profile = new DamageProfile(damageType, element, sourceAttackStat);
                int damage = targetRunic.CalculateDamage(power, profile);
                Color damageColor = profile.Element != null ? profile.Element.ElementColor : Color.white;
                float damageScale = DamageCalculator.GetDamageMultiplier(profile, targetRunic.Elements);
                targetRunic.TakeDamage(damage, playerAttacker: true,
                    damageColor: damageColor, damageScale: damageScale);
            }
        }
        else if (targetPlayer != null && !(playerProjectile
            || (owner != null && owner.TameState == RunicState.Tamed)))
        {
            float attackStat = sourceAttackStat;
            if (attackStat <= 0f && owner != null)
                attackStat = owner.GetKeyStatValue(damageType == SkillDamageType.Magical
                    ? SkillKeyStat.Magic
                    : SkillKeyStat.Attack);
            bool hasStab = owner != null && owner.Elements != null && owner.Elements.Contains(element);
            DamageProfile profile = new DamageProfile(damageType, element, attackStat, hasStab);
            targetPlayer.TakeDamage(targetPlayer.CalculateDamage(power, profile));
        }

        Destroy(gameObject);
    }
}
