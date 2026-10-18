using UnityEngine;

public class RunicSenses : MonoBehaviour
{
    private Runic runic;
    private RunicWildAI wildAI;
    private RunicTamedAI tamedAI;
    private RunicCombat runicCombat;

    [Header("Senses Settings")]
    [SerializeField] private float sightRange = 12f;
    [SerializeField] private float fieldOfViewAngle = 90f;
    [SerializeField] private float hearingRange = 6f;
    
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private float checkInterval = 0.3f;
    private float checkTimer;

    [Header("Current Perceptions")]
    [SerializeField] private Transform currentDetectedTarget;
    public Transform CurrentDetectedTarget => currentDetectedTarget;

    void Awake()
    {
        runic = GetComponent<Runic>();
        wildAI = GetComponent<RunicWildAI>();
        tamedAI = GetComponent<RunicTamedAI>();
        runicCombat = GetComponent<RunicCombat>();
    }

    void Update()
    {
        if (runic.IsPaused) return;

        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            ScanEnvironment();
        }
    }

    void ScanEnvironment()
    {
        if (runic.ActState == BehaviorState.Fighting || runic.ActState == BehaviorState.Fleeing)
            return;

        float maxRange = Mathf.Max(sightRange, hearingRange);
        Collider[] hits = Physics.OverlapSphere(transform.position, maxRange, detectionLayer);

        Transform bestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            // Ignores itself
            if (hit.gameObject == gameObject) continue;

            Vector3 targetPos = hit.transform.position;
            float distanceToTarget = Vector3.Distance(transform.position, targetPos);

            bool detected = false;

            // 1. Hearing Check (Omnidirectional Proximity)
            if (distanceToTarget <= hearingRange)
            {
                detected = true;
            }
            // 2. Vision Check (Within range and within frontal angle)
            else if (distanceToTarget <= sightRange)
            {
                Vector3 directionToTarget = (targetPos - transform.position).normalized;
                directionToTarget.y = 0f;
                
                float angle = Vector3.Angle(transform.forward, directionToTarget);
                if (angle <= fieldOfViewAngle / 2f)
                {
                    // Optional: Add a Raycast here to ensure there's no wall in front.
                    detected = true;
                }
            }

            if (detected)
            {
                // If detected, choose the closest one as a priority target.
                if (distanceToTarget < closestDistance)
                {
                    closestDistance = distanceToTarget;
                    bestTarget = hit.transform;
                }
            }
        }

        HandlePerceivedTarget(bestTarget);
    }

    void HandlePerceivedTarget(Transform target)
    {
        currentDetectedTarget = target;

        if (target != null)
        {
            // If player spotted
            PlayerController player = target.GetComponent<PlayerController>();
            if (player == null)
                player = target.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                // Depending on the nature of the runic, either flees or attacks the player.
                if (runic.Nature == WildNature.Fearful)
                {
                    runic.SetBehaviorState(BehaviorState.Fleeing);
                    wildAI.SetFleePlayer(true);
                }
                else if (runic.Nature == WildNature.Hostile)
                {
                    runic.SetBehaviorState(BehaviorState.Fighting);
                    wildAI.SetFightPlayer(true);
                } else if (runic.Nature == WildNature.Territorial)
                {
                    float territoryDistance = wildAI.DistanceFromTerritory(runic.Player.transform);
                    float distanceToRunic = Vector3.Distance(runic.Player.transform.position, 
                        runic.transform.position);

                    if ((territoryDistance < wildAI.TerritoryThreatRadius
                        && territoryDistance > wildAI.TerritoryRadius)
                        || (distanceToRunic < wildAI.CautionRange && distanceToRunic > wildAI.HostileRange))
                    {
                        runic.SetBehaviorState(BehaviorState.Threatening);
                    }
                    else if(territoryDistance < wildAI.TerritoryRadius
                        || distanceToRunic < wildAI.HostileRange)
                    {
                        runic.SetBehaviorState(BehaviorState.Fighting);
                        wildAI.SetFightPlayer(true);
                    }
                    else if(runic.IsFriendly())
                    {
                        wildAI.InvestigateTransform(runic.Player.transform);
                    }
                }
            }
            
            // If spotted another Runic (e.g., Prey or Enemy)
            Runic otherRunic = target.GetComponent<Runic>();
            if (otherRunic == null)
                otherRunic = target.GetComponentInParent<Runic>();
            if (otherRunic != null && otherRunic.TameState == RunicState.Wild)
            {
                SpeciesRelation speciesRelation = runic.RunicDatabase.GetRelation(
                    runic.Species, otherRunic.Species);

                bool sameSpecies = runic.Species == otherRunic.Species;

                if (speciesRelation == SpeciesRelation.Predator)
                {
                    runic.SetBehaviorState(BehaviorState.Fleeing);
                    runic.AddThreat(otherRunic);
                    runicCombat.GiveUpHunt();
                }
                else if (speciesRelation == SpeciesRelation.Prey 
                    && wildAI.CurrentSatiation <= wildAI.MaxSatiation / 2)
                {
                    runic.SetBehaviorState(BehaviorState.Hunting);
                    wildAI.SetTargetPrey(otherRunic);
                }
                else if (speciesRelation == SpeciesRelation.Rival
                    || (sameSpecies && runic.Species.SameSpeciesRelation == IntraspeciesRelation.Hostile
                    && !runic.Allies.Contains(otherRunic))
                    )
                {
                    runic.SetBehaviorState(BehaviorState.Fighting);
                    runic.AddEnemy(otherRunic);
                }
                else if (speciesRelation == SpeciesRelation.Allied
                || (sameSpecies && runic.Species.SameSpeciesRelation == IntraspeciesRelation.Allied))
                {
                    runic.AddAlly(otherRunic);
                    if (otherRunic.ActState == BehaviorState.Fighting)
                    {
                        runic.SetBehaviorState(BehaviorState.Fighting);
                        foreach (Runic enemy in otherRunic.EngagedEnemies)
                        {
                            runic.AddEnemy(enemy);
                        }
                    }
                    else if (otherRunic.ActState == BehaviorState.Fleeing)
                    {
                        if (!runic.IsFearful() || runic.IsProtective())
                        {
                            runic.SetBehaviorState(BehaviorState.Fighting);
                            foreach (Runic threat in otherRunic.Threats)
                            {
                                runic.AddEnemy(threat);
                            }
                        }
                        else
                        {
                            runic.SetBehaviorState(BehaviorState.Fleeing);
                            foreach (Runic threat in otherRunic.Threats)
                            {
                                runic.AddThreat(threat);
                            }
                        }
                    }
                }
            }
        }
    }
}