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
    [SerializeField] private Runic owner;
    [SerializeField] private LayerMask hitLayerMask;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Collider hitbox;
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
    float mass, bool gravity, LayerMask layerMask, float rng = 0, float lifeTime = 0, Runic own = null, bool player = false)
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

    private void OnTriggerEnter(Collider other)
    {
        Runic targetRunic = other.GetComponent<Runic>();
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
                targetRunic.TakeDamage((int)power, owner);
            }
            else if (playerProjectile)
                targetRunic.TakeDamage((int)power, playerAttacker:true);
        }
        else if (targetPlayer != null && !(playerProjectile || owner.TameState == RunicState.Tamed))
        {
            targetPlayer.TakeDamage((int)power);
        }

        Destroy(gameObject);
    }
}
