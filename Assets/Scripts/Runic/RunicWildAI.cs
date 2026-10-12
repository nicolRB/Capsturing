using UnityEngine;

public class RunicWildAI : MonoBehaviour
{
    private enum FleeThreatType { None, Runic, Player, EnvironmentPoint, EnvironmentObject }

    [Header("Territory")]
    [SerializeField] private GameObject territoryAnchor;
    [SerializeField] private Vector3 territoryPosition;
    [SerializeField] private float territoryRadius;
    [SerializeField] private float territoryThreatRadius;
    [SerializeField] private bool hasTerritoryPosition;

    [Header("Proximity Tolerance")]
    [SerializeField] private float cautionRange = 10f;
    [SerializeField] private float hostileRange = 5f;

    [Header("Timing")]
    [SerializeField] private float actInterval = 5f;
    private float actTimer;
    [SerializeField] private float maxWanderDistance = 10f;

    [Header("Combat/Fleeing")]
    [SerializeField] private bool fightingPlayer;
    [SerializeField] private bool fleePlayer;
    [SerializeField] private Vector3 dangerousLocation;
    [SerializeField] private bool dangerousLocationSet;
    [SerializeField] private float fleeSafeDistance = 20f;
    [SerializeField] private float fleeUpdateInterval = 0.5f;
    private float fleeUpdateTimer;
    private Transform dangerousSource;
    private bool dangerousSourceIsTransform;
    private FleeThreatType activeFleeThreatType;
    private Runic activeRunicThreat;
    private Transform activeEnvironmentThreat;
    private Vector3 activeFleeThreatPosition;

    [Header("Hunting")]
    [SerializeField] private Runic targetPrey;
    [SerializeField] private float huntRange = 40f;

    [Header("Idle")]
    [SerializeField] private float idleAnimationInterval = 6f;
    [SerializeField] private float idleTimer;

    [Header("Hunger")]
    [SerializeField] private float maxSatiation;
    [SerializeField] private float currentSatiation;
    [SerializeField] private float hungerTickInterval = 60;
    [SerializeField] private float hungerTickTimer;
    [SerializeField] private float hungerTickValue = 2;

    [Header("Other Activities")]
    [SerializeField] private float challengeTimer;
    [SerializeField] private float investigateTimer;
    [SerializeField] private float investigateCooldownTimer;
    [SerializeField] Transform curiosityTransform;
    [SerializeField] Vector3 curiosityPos;

    [Header("References")]
    [SerializeField] private Runic runic;
    [SerializeField] private RunicMovement runicMovement;
    [SerializeField] private RunicCombat runicCombat;
    [SerializeField] private RunicSenses runicSenses;

    public float CautionRange => cautionRange;
    public float HostileRange => hostileRange;
    public float MaxSatiation => maxSatiation;
    public float CurrentSatiation => currentSatiation;
    public float TerritoryRadius => territoryRadius;
    public float TerritoryThreatRadius => territoryThreatRadius;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (runic == null)
            runic = GetComponent<Runic>();
            
        if (runicMovement == null)
            runicMovement = GetComponent<RunicMovement>();
            
        if (runicCombat == null)
            runicCombat = GetComponent<RunicCombat>();
        
        if (runicSenses == null)
            runicSenses = GetComponent<RunicSenses>();

        if (runic.Nature == WildNature.Territorial)
        {
            if (territoryAnchor == null && !hasTerritoryPosition)
            {
                territoryPosition = transform.position;
                hasTerritoryPosition = true;
            }
                
            if (territoryRadius == 0)
                territoryRadius = 10f;

            if (territoryThreatRadius == 0)
                territoryThreatRadius = 5f;
        }

        if (runic != null){
            maxSatiation = runic.Weight * 10;
            currentSatiation = runic.Weight * 5;
        }
    }

    void Update()
    {
        UpdateTimers();

        if (runic.ActState == BehaviorState.Fleeing)
        {
            fleeUpdateTimer += Time.deltaTime;
            if (fleeUpdateTimer >= fleeUpdateInterval)
            {
                fleeUpdateTimer = 0f;
                UpdateFleeing();
            }
        }

        if (actTimer >= actInterval)
        {
            actTimer = 0;
            WildBehavior();
        }

        /*
        if (hungerTickTimer >= hungerTickInterval)
        {
            hungerTickTimer = 0;
            currentSatiation -= hungerTickValue;
        }
        */
    }

    void WildBehavior()
    {
        runic.PruneDestroyedRelations();

        switch (runic.ActState)
        {
            case BehaviorState.Idle:
                float roll = Random.value;
                if (roll > 0.9)
                    runic.SetBehaviorState(BehaviorState.Wandering);
                else if (roll > 0.4 && idleTimer <= 0)
                {
                    // idle animation
                }
                else
                {
                    // sit
                }
                    
                break;
            
            case BehaviorState.Wandering:
                runicMovement.Wander(maxWanderDistance);
                break;

            case BehaviorState.Patrolling:
                if (territoryAnchor != null)
                    runicMovement.Patrol(territoryAnchor.transform.position, 
                        territoryRadius, territoryThreatRadius);
                else if (hasTerritoryPosition)
                    runicMovement.Patrol(territoryPosition, 
                        territoryRadius, territoryThreatRadius);
                else runicMovement.Wander(maxWanderDistance);
                break;

            case BehaviorState.Threatening:
                // threat animation
                break;

            case BehaviorState.Fleeing:
                break;

            case BehaviorState.Hunting:
                if (targetPrey != null)
                {
                    float preyDistance = Vector3.Distance(transform.position, targetPrey.transform.position);
                    if (preyDistance <= huntRange)
                    {
                        runicCombat.TryHunting(targetPrey);
                    }
                    else 
                    {   
                        runic.SetBehaviorState(BehaviorState.Idle);
                        runicCombat.GiveUpHunt();
                    }
                }
                else
                {
                    runic.SetBehaviorState(BehaviorState.Idle);
                    runicCombat.GiveUpHunt();
                }
                
                break;
            
            case BehaviorState.Fighting:
                if (runic.EngagedEnemies.Count > 0)
                {
                    if (fightingPlayer && Random.Range(0, runic.EngagedEnemies.Count + 1) == runic.EngagedEnemies.Count)
                    {
                        runicCombat.SetPlayerTarget();
                    }
                    else
                    {
                        Runic target = runic.EngagedEnemies[Random.Range(0, runic.EngagedEnemies.Count)];
                        runicCombat.SetTarget(target);
                    }
                }
                else if (fightingPlayer)
                    runicCombat.SetPlayerTarget();
                else
                {
                    runic.SetBehaviorState(BehaviorState.Idle);
                    runicCombat.SetTarget(null);
                }
                break;
            case BehaviorState.Investigating:
                if (investigateTimer <= 0)
                {
                    runic.SetBehaviorState(BehaviorState.Wandering);
                }
                else if (curiosityTransform == null)
                {
                    runicMovement.MoveToCuriosity(curiosityTransform.position);
                }
                else 
                {
                    runicMovement.MoveToCuriosity(curiosityPos);
                }
                break;

            default:
                runicMovement.Wander(maxWanderDistance);
                break;
        }
    }

    public void SetEnvironmentalThreat(Transform source)
    {
        dangerousSource = source;
        dangerousSourceIsTransform = source != null;
        dangerousLocation = source != null
            ? source.position
            : transform.position + transform.forward;
        dangerousLocationSet = true;
        activeFleeThreatType = FleeThreatType.None;
    }

    private void UpdateFleeing()
    {
        runic.PruneDestroyedRelations();

        if (!TryGetActiveFleeThreatPosition(out Vector3 threatPosition))
        {
            if (!TrySelectNearestFleeThreat() || !TryGetActiveFleeThreatPosition(out threatPosition))
            {
                EndFleeing();
                return;
            }
        }

        Vector3 separation = transform.position - threatPosition;
        separation.y = 0f;
        if (separation.sqrMagnitude >= fleeSafeDistance * fleeSafeDistance)
        {
            activeFleeThreatType = FleeThreatType.None;
            activeRunicThreat = null;
            activeEnvironmentThreat = null;

            if (!TrySelectNearestFleeThreat() || !TryGetActiveFleeThreatPosition(out threatPosition))
            {
                EndFleeing();
                return;
            }
        }

        runicMovement.Flee(threatPosition);
    }

    private bool TrySelectNearestFleeThreat()
    {
        activeFleeThreatType = FleeThreatType.None;
        activeRunicThreat = null;
        activeEnvironmentThreat = null;
        float safeDistance = Mathf.Max(fleeSafeDistance, 0.01f);
        float nearestDistance = safeDistance * safeDistance;

        foreach (Runic threat in runic.Threats)
        {
            if (threat == null || threat == runic)
                continue;

            float distance = (threat.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                activeFleeThreatType = FleeThreatType.Runic;
                activeRunicThreat = threat;
                activeFleeThreatPosition = threat.transform.position;
            }
        }

        if (fleePlayer && runic.Player != null)
        {
            Vector3 playerPosition = runic.Player.transform.position;
            float distance = (playerPosition - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                activeFleeThreatType = FleeThreatType.Player;
                activeFleeThreatPosition = playerPosition;
            }
        }

        if (dangerousLocationSet && dangerousSourceIsTransform && dangerousSource != null)
        {
            float distance = (dangerousSource.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                activeFleeThreatType = FleeThreatType.EnvironmentObject;
                activeEnvironmentThreat = dangerousSource;
                activeFleeThreatPosition = dangerousSource.position;
            }
        }
        else if (dangerousLocationSet && !dangerousSourceIsTransform)
        {
            float distance = (dangerousLocation - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                activeFleeThreatType = FleeThreatType.EnvironmentPoint;
                activeFleeThreatPosition = dangerousLocation;
            }
        }

        return activeFleeThreatType != FleeThreatType.None;
    }

    private bool TryGetActiveFleeThreatPosition(out Vector3 threatPosition)
    {
        switch (activeFleeThreatType)
        {
            case FleeThreatType.Runic when activeRunicThreat != null:
                threatPosition = activeRunicThreat.transform.position;
                return true;
            case FleeThreatType.Player when runic.Player != null:
                threatPosition = runic.Player.transform.position;
                return true;
            case FleeThreatType.EnvironmentObject when activeEnvironmentThreat != null:
                threatPosition = activeEnvironmentThreat.position;
                return true;
            case FleeThreatType.EnvironmentPoint:
                threatPosition = activeFleeThreatPosition;
                return true;
            default:
                threatPosition = default;
                return false;
        }
    }

    private void EndFleeing()
    {
        activeFleeThreatType = FleeThreatType.None;
        activeRunicThreat = null;
        activeEnvironmentThreat = null;
        dangerousSource = null;
        dangerousSourceIsTransform = false;
        dangerousLocationSet = false;
        runic.SetBehaviorState(BehaviorState.Idle);
    }

    // function for taunting before engaging in battle
    public void Challenge(){
        challengeTimer = 2;
    }

    void Eat(float nutritionValue)
    {
        currentSatiation += nutritionValue;
    }

    void EatRunic(Runic runicBody)
    {
        currentSatiation += runicBody.Weight * 5;
    }

    public float DistanceFromTerritory(Transform creature){
        float outOfTerritory = territoryRadius + territoryThreatRadius + 1;
        if (creature == null) return outOfTerritory;

        Vector3 centerPosition = Vector3.zero;
    
        if (territoryAnchor != null)
        {
            centerPosition = territoryAnchor.transform.position;
        }
        else if (hasTerritoryPosition)
        {
            centerPosition = territoryPosition;
        }
        else
        {
            // If there is no defined territory, returns out of territory.
            return outOfTerritory; 
        }

        // Calculates the horizontal distance (ignoring the Y-axis to avoid distortions if there are elevations in the terrain).
        Vector3 creaturePosXZ = new Vector3(creature.position.x, 0f, creature.position.z);
        Vector3 centerPosXZ = new Vector3(centerPosition.x, 0f, centerPosition.z);

        return Vector3.Distance(creaturePosXZ, centerPosXZ);
    }

    public void InvestigateTransform(Transform curiosity){
        if(investigateCooldownTimer <= 0)
        {
            investigateTimer = 10f;
            investigateCooldownTimer = 120f;
            runic.SetBehaviorState(BehaviorState.Investigating);
            curiosityTransform = curiosity;
        }
    }

    public void InvestigatePosition(Vector3 position){
        if(investigateCooldownTimer <= 0)
        {
            investigateTimer = 10f;
            investigateCooldownTimer = 120f;
            runic.SetBehaviorState(BehaviorState.Investigating);
            curiosityPos = position;
        }
    }

    public void SetFightPlayer(bool state)
    {
        fightingPlayer = state;
    }

    public void SetFleePlayer(bool state)
    {
        fleePlayer = state;
    }

    public void SetTargetPrey(Runic prey){
        targetPrey = prey;
    }

    void UpdateTimers()
    {
        actTimer += Time.deltaTime;
        hungerTickInterval += Time.deltaTime;
        if (idleTimer > 0)
            idleTimer -= Time.deltaTime;
        if (challengeTimer > 0)
            challengeTimer -= Time.deltaTime;
        if (investigateTimer > 0)
            investigateTimer -= Time.deltaTime;
        if (investigateCooldownTimer > 0)
            investigateCooldownTimer -= Time.deltaTime;
    }
}