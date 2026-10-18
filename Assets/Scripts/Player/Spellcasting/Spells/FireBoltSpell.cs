using UnityEngine;

public class FireBoltSpell : SpellBase
{
    [Header ("Firebolt Stats")]
    [SerializeField] private float potency;
    [SerializeField] private float range;
    [SerializeField] private float projectileSpeed;
    [SerializeField] private Element element;

    private float finalPower;

    [Header ("Firebolt Prefab")]
    [SerializeField] private GameObject fireBoltPrefab;

    [Header ("Spawn Settings")]
    [SerializeField] private Transform firePoint; // Starting point
    [SerializeField] private float maxAimDistance = 100f; // Maximum aiming radius distance
    [SerializeField] private LayerMask aimLayerMask; // Layers that the targeting radius can reach (prevents hitting the player themselves)

    public override void Start()
    {
        base.Start();
    }

    public override void OnCastStart()
    {
        
    }

    public override void OnChannelComplete(ChannelingGameCoordinator.ChannelingResult result)
    {
        float calculatedResult = ComputeChannelScore(result);

        if (calculatedResult <= 0f)
        {
            spellcastingCoordinator.CancelCast();
            return;
        }

        finalPower = potency * calculatedResult * player.Magic;
    }

    public override void OnSpellCast()
    {
        if (fireBoltPrefab != null)
        {
            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;

            Camera mainCam = Camera.main;
            Vector3 targetPoint;

            if (mainCam != null)
            {
                // Creates a ray from the center of the screen (0.5, 0.5) forward.
                Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                
                if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayerMask))
                {
                    // If the ray hits something, the target is the point of impact.
                    targetPoint = hit.point;
                }
                else
                {
                    // If ray doesn't hit anything (e.g., looking at the sky), the target is the 
                    // furthest point on the horizon of the ray.
                    targetPoint = ray.GetPoint(maxAimDistance);
                }
            }
            else
            {
                // Fallback in case the main camera is not found: uses the direction the player is looking.
                targetPoint = spawnPosition + (transform.forward * maxAimDistance);
            }

            // Calculates the exact direction from the origin to the camera's aiming point.
            Vector3 shootDirection = (targetPoint - spawnPosition).normalized;
            Quaternion spawnRotation = Quaternion.LookRotation(shootDirection);

            // Instantiate the projectile with the corrected rotation.
            GameObject spawnedBolt = Instantiate(fireBoltPrefab, spawnPosition, spawnRotation);
            
            ProjectileScript fireBoltScript = spawnedBolt.GetComponent<ProjectileScript>();
            if (fireBoltScript != null)
            {
                fireBoltScript.Setup(finalPower, 0, projectileSpeed, ProjectileLimiter.Range, 
                1, false, aimLayerMask, range, player:true,
                projectileDamageType: SkillDamageType.Magical, element: element, attackStat: player.Magic);
            }
        }

        RaiseSpellResolved();
    }

    public override void Cancel()
    {
        finalPower = 0f;
    }
}