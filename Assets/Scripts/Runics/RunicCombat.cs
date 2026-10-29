using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RunicCombat : MonoBehaviour
{
    [Header("References")]
    private Runic runic;
    private RunicMovement runicMovement;
    private CombatStyle combatStyle;
    private RunicHitboxRegistry hitboxRegistry;

    [Header("Target")]
    private Runic currentTarget;
    [SerializeField] private bool playerTarget;

    [Header("Combat Timers")]
    private float recoveryTimer;
    private float specialRecoveryTimer;
    private float movementTimer;
    [SerializeField] private float updateInterval = 0.2f;
    private float updateTimer;
    [SerializeField] private float skillGiveUpTime = 4f;
    private float giveUpTimer;

    [Header("Skill State")]
    private float skillDurationTimer;
    private Skill preparedSkill;
    private Skill queuedUpSkill;
    private bool usingSkill;
    [SerializeField, Min(0.02f)] private float landingImpactDuration = 0.2f;
    private Skill pendingLandingImpactSkill;
    private SkillStep pendingLandingImpactStep;
    private bool hasPendingLandingImpact;
    private Coroutine landingImpactResetCoroutine;
    private Dictionary<Skill, float> skillCooldowns = new Dictionary<Skill, float>();

    [Header("Hunting")]
    private bool hunting;

    public Dictionary<Skill, float> SkillCooldowns => skillCooldowns;
    public bool AttackingPlayer => playerTarget;

    private readonly List<Coroutine> activeEmissionCoroutines = new List<Coroutine>();
    private Collider bodyCollider;

    public void SetPaused(bool paused)
    {
        hitboxRegistry?.SetPaused(paused);
    }

    void Start()
    {
        InitializeCooldowns();
    }

    private void Awake()
    {
        runic = GetComponent<Runic>();
        runicMovement = GetComponent<RunicMovement>();
        hitboxRegistry = GetComponent<RunicHitboxRegistry>();
        bodyCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (runic.IsPaused) return;


        UpdateTimers();
        UpdateSkillCooldowns(); // Updates cooldowns every frame

        if (preparedSkill && giveUpTimer >= skillGiveUpTime)
        {
            giveUpTimer = 0;
            preparedSkill = null;
        }

        // If there is a prepared skill, check if it has entered range to use it
        if (preparedSkill != null && GetCurrentTargetTransform() != null)
        {
            float targetDistance = GetDistanceFromTarget();
            if (preparedSkill.Range >= targetDistance)
            {
                Skill skillToExecute = preparedSkill;
                preparedSkill = null; // Clear preparation
                
                runicMovement.MoveToPosition(transform.position); // Stop movement before attack
                UseSkill(skillToExecute);
            }
        }

        if (updateTimer >= updateInterval)
        {
            updateTimer = 0;

            if (runic.TameState == RunicState.Tamed 
                && (runic.ActState == BehaviorState.CommandedAttack || runic.ActState == BehaviorState.Fighting))
            {
                TameCombat();
            }
            else if (runic.TameState == RunicState.Wild && runic.ActState == BehaviorState.Fighting)
            {
                WildCombat();
            }
            else if (runic.ActState == BehaviorState.Hunting)
            {
                Hunt();
            }
        }
    }

    void TameCombat()
    {
        if (skillDurationTimer <= 0)
            usingSkill = false;

        if (runic.ActState == BehaviorState.CommandedAttack)
            currentTarget = runic.Player.CommandedTarget;

        if (currentTarget == null)
            return;

        if (!IsBusy() && queuedUpSkill != null)
        {
            ReadySkill(queuedUpSkill);
            queuedUpSkill = null;
        }

        if (preparedSkill != null && currentTarget != null)
        {
            float targetDistance = GetDistanceFromTarget();

            if (preparedSkill.Range >= targetDistance)
            {
                Skill skillToExecute = preparedSkill;
                preparedSkill = null; 
                
                runicMovement.MoveToPosition(transform.position); 
                UseSkill(skillToExecute);
            }
            else
            {
                Collider targetCollider = currentTarget.GetComponent<Collider>();
                Vector3 rawDestination = targetCollider != null 
                    ? targetCollider.ClosestPoint(transform.position) : currentTarget.transform.position;

                // Keep the destination on the NavMesh to avoid pathing failures.
                if (UnityEngine.AI.NavMesh.SamplePosition(rawDestination, 
                    out UnityEngine.AI.NavMeshHit hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                    if (runicMovement != null)
                        runicMovement.MoveToPosition(hit.position);
            }
        } 
        else if (recoveryTimer <= 0 && currentTarget != null && !usingSkill)
        {
            if (Random.value <= runic.Aggression)
            {
                bool wantSpecial = specialRecoveryTimer <= 0 
                    && HasAnySpecialSkillReady() && Random.value <= runic.SpecialAggression;
                List<Skill> candidatePool = wantSpecial ? runic.Skills : runic.BasicSkills;

                if (candidatePool == null || candidatePool.Count == 0 || !HasAnySkillReadyInPool(candidatePool))
                {
                    candidatePool = (candidatePool == runic.Skills) ? runic.BasicSkills : runic.Skills;
                }

                if (candidatePool != null && candidatePool.Count > 0)
                {
                    float targetDistance = GetDistanceFromTarget();
                    Skill chosenSkill = ChooseWeightedSkill(candidatePool, targetDistance);

                    if (chosenSkill != null)
                    {
                        if (chosenSkill.Range >= targetDistance)
                        {
                            UseSkill(chosenSkill);
                        }
                        else
                        {
                            Debug.Log(runic + " repositioning to use " + chosenSkill);
                            runicMovement.SetMovementSpeed(runic.SprintSpeed);
                            ReadySkill(chosenSkill);
                        }
                    }
                }
            }
        }

        if (preparedSkill == null && movementTimer <= 0 && currentTarget != null && !usingSkill)
        {
            runicMovement.HandleCombatMovement(currentTarget.transform, combatStyle);
        }

        if (!runicMovement.Charging)
            runicMovement.FaceTarget(currentTarget.transform.position);
    }

    bool HasAnySkillReadyInPool(List<Skill> pool)
    {
        foreach (var skill in pool)
        {
            if (skill != null && IsSkillReady(skill)) return true;
        }
        return false;
    }

    public Skill ChooseWeightedSkill(List<Skill> candidateSkills, float targetDistance)
    {
        List<Skill> validSkills = new List<Skill>();
        List<float> weights = new List<float>();
        float totalWeight = 0f;

        foreach (var skill in candidateSkills)
        {
            if (skill == null) continue;

            if (IsSkillReady(skill))
            {
                validSkills.Add(skill);

                float distanceDifference = Mathf.Abs(skill.Range - targetDistance);
                float weight = 1f / (1f + distanceDifference); 

                weights.Add(weight);
                totalWeight += weight;
            }
        }

        if (validSkills.Count == 0) return null;

        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        for (int i = 0; i < validSkills.Count; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue <= cumulativeWeight)
            {
                return validSkills[i];
            }
        }

        return validSkills[0];
    }

    public void ReadySkill(Skill skill)
    {
        preparedSkill = skill;
        giveUpTimer = 0f;
    }

    private Coroutine activeSkillCoroutine;

    public void UseSkill(Skill skill)
    {
        Debug.Log(runic + " using Skill: " + skill.SkillName);

        StopAllEmissions();
        ClearPendingLandingImpact();

        recoveryTimer = runic.Recovery + skill.Duration;
        if (runic.Skills.Contains(skill))
            specialRecoveryTimer = runic.SpecialRecovery;

        skillDurationTimer = skill.Duration;
        usingSkill = true;
        PutSkillOnCooldown(skill);

        if (activeSkillCoroutine != null)
        {
            StopCoroutine(activeSkillCoroutine);
            runicMovement.EndLeap();
        }
        
        activeSkillCoroutine = StartCoroutine(ExecuteSkillRoutine(skill));
    }

    private System.Collections.IEnumerator ExecuteSkillRoutine(Skill skill)
    {
        if (skill.Steps == null || skill.Steps.Length == 0)
            yield break;

        // Create a copy or sort the steps by delay to ensure correct temporal sequencing
        SkillStep[] sortedSteps = (SkillStep[])skill.Steps.Clone();
        System.Array.Sort(sortedSteps, (a, b) => a.Delay.CompareTo(b.Delay));

        float elapsedTime = 0f;

        for (int i = 0; i < sortedSteps.Length; i++)
        {
            SkillStep currentStep = sortedSteps[i];

            // 1. Wait until the moment (delay) to start this step
            while (elapsedTime < currentStep.Delay)
            {
                if (!usingSkill) yield break; // Interrupted by Flinch
                if (runic.IsPaused)
                {
                    yield return null;
                    continue;
                }
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 2. Determine when this step ends (next step delay or the total skill duration)
            float nextStepDelay = (i < sortedSteps.Length - 1) 
                ? sortedSteps[i + 1].Delay : skill.Duration;

            // 3. Trigger the step start (single trigger, re-aim, initial impulse, etc.)
            ExecuteStepStart(skill, currentStep, nextStepDelay - elapsedTime);

            // 4. Keep the step active/executing during the time window between this delay and the next one
            while (elapsedTime < nextStepDelay)
            {
                if (!usingSkill)
                {
                    ExecuteStepEnd(currentStep);
                    yield break;
                }

                if (runic.IsPaused)
                {
                    yield return null;
                    continue;
                }

                // If continuous logic is needed during the step window (e.g., keep hitbox active or read input)
                ExecuteStepUpdate(currentStep);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            ExecuteStepEnd(currentStep);
        }

        // Ensure the total skill duration is respected until the end
        while (elapsedTime < skill.Duration)
        {
            if (!usingSkill) yield break;
            if (runic.IsPaused)
            {
                yield return null;
                continue;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        usingSkill = false;
    }

    private void ExecuteStepStart(Skill skill,SkillStep step, float stepDuration)
    {
        // Re-aim if necessary at the start of the step
        Transform targetTransform = GetCurrentTargetTransform();
        if (step.ReAimAtTarget && targetTransform != null)
        {
            Vector3 lookDir = (targetTransform.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }

        // Immediate movement execution (Dash, Leap, Teleport, etc.)
        switch (step.MovementType)
        {
            case SkillMovementType.DashToTarget:
                if (targetTransform != null)
                    runicMovement?.StartDashToTarget(targetTransform, step.Force, step.StopDistance);
                break;

            case SkillMovementType.LeapTowardsTarget:
                if (targetTransform != null && runicMovement != null)
                {
                    Collider targetCol = targetTransform.GetComponent<Collider>();
                    Vector3 leapTarget = targetCol != null ? targetCol.ClosestPoint(transform.position) 
                        : targetTransform.position;
                    
                    // Launch the leap using Rigidbody velocity.
                    if (runicMovement.Leap(leapTarget, step.Force))
                        PrepareLandingImpact(skill);
                }
                break;

            case SkillMovementType.HighLeap:
                if (targetTransform != null && runicMovement != null)
                {
                    Collider targetCol = targetTransform.GetComponent<Collider>();
                    Vector3 leapTarget = targetCol != null ? targetCol.ClosestPoint(transform.position) 
                        : targetTransform.position;
                    
                    // Launches the runic high up to land near the target.
                    if (runicMovement.HighLeap(leapTarget, step.Force))
                        PrepareLandingImpact(skill);
                }
                break;

            case SkillMovementType.TeleportToTarget:
                if (targetTransform != null)
                {
                    Collider targetCol = targetTransform.GetComponent<Collider>();
                    Vector3 telePos = targetCol != null ? targetCol.ClosestPoint(transform.position) : targetTransform.position;
                    transform.position = telePos;
                }
                break;

            case SkillMovementType.ChargeForward:
                runicMovement?.StartCharge(step.Force, step.Force * stepDuration);
                break;
        }

        // Hitbox and damage

        if (step.HitboxType != SkillHitboxType.None
            && step.HitboxType != SkillHitboxType.JumpImpactAOE)
        {
            float totalPower = runic.GetKeyStatValue(skill.KeyStat) * (skill.Power / 100f);
            hitboxRegistry.ActivateHitbox(
                step.HitboxType, 
                step.HitboxSize, 
                skill, 
                totalPower, 
                step.Continuous
            );
        }

        StartEmissions(skill, step, stepDuration, false);
    }

    private void ExecuteStepUpdate(SkillStep step)
    {
        // If any step needs frame-by-frame update logic while active
        // (e.g., keep the runic tracking the target during a continuous charge)
    }

    private void ExecuteStepEnd(SkillStep step)
    {
        if ((step.MovementType == SkillMovementType.LeapTowardsTarget
            || step.MovementType == SkillMovementType.HighLeap) && runicMovement != null)
            runicMovement.EndLeap();
        if ((step.MovementType == SkillMovementType.ChargeForward
            || step.MovementType == SkillMovementType.DashToTarget) && runicMovement != null)
            runicMovement.EndCharge();

        // Reset only the hitbox used by this step.
        if (step.HitboxType != SkillHitboxType.None
            && step.HitboxType != SkillHitboxType.JumpImpactAOE)
            hitboxRegistry.ResetHitbox(step.HitboxType);
    }

    public void TriggerLeapLandingImpact()
    {
        if (!hasPendingLandingImpact || pendingLandingImpactSkill == null || hitboxRegistry == null)
            return;

        hasPendingLandingImpact = false;

        if (landingImpactResetCoroutine != null)
            StopCoroutine(landingImpactResetCoroutine);

        // Clear any hitbox that was active during the leap.
        hitboxRegistry.ResetAllHitboxes();

        float totalPower = runic.GetKeyStatValue(pendingLandingImpactSkill.KeyStat)
            * (pendingLandingImpactSkill.Power / 100f);

        hitboxRegistry.ActivateHitbox(
            pendingLandingImpactStep.HitboxType,
            pendingLandingImpactStep.HitboxSize,
            pendingLandingImpactSkill,
            totalPower,
            pendingLandingImpactStep.Continuous
        );
        StartEmissions(pendingLandingImpactSkill, pendingLandingImpactStep, landingImpactDuration, true);
        landingImpactResetCoroutine = StartCoroutine(ResetLandingImpactAfterDelay());
    }

    private void PrepareLandingImpact(Skill skill)
    {
        ClearPendingLandingImpact();
        StopAllEmissions();
        if (skill.Steps == null)
            return;

        SkillStep? best = null;
        foreach (SkillStep step in skill.Steps)
        {
            if (step.HitboxType != SkillHitboxType.JumpImpactAOE)
                continue;

            bool hasLandingEmission = step.Emissions != null && step.Emissions.Any(e =>
                e.EmissionPrefab != null && e.Origin == SkillHitboxType.JumpImpactAOE);

            // Prioriza o step com VFX de pouso; senão, guarda o primeiro como fallback.
            if (hasLandingEmission) { best = step; break; }
            if (best == null) best = step;
        }

        if (best == null) return;

        pendingLandingImpactSkill = skill;
        pendingLandingImpactStep = best.Value;
        hasPendingLandingImpact = true;
    }

    private void StartEmissions(Skill skill, SkillStep step, float stepDuration, bool landing)
    {
        if (step.Emissions == null) return;

        foreach (SkillEmission emission in step.Emissions)
        {
            if (emission.EmissionPrefab == null) continue;

            // Emissions originating from JumpImpactAOE should only spawn on landing.
            if (emission.Origin == SkillHitboxType.JumpImpactAOE != landing) continue;

            activeEmissionCoroutines.Add(
                StartCoroutine(EmissionRoutine(skill, emission, stepDuration, landing)));
        }
    }

    private void StopAllEmissions()
    {
        foreach (Coroutine c in activeEmissionCoroutines)
            if (c != null) StopCoroutine(c);
        activeEmissionCoroutines.Clear();
    }

    private System.Collections.IEnumerator WaitUnpaused(float seconds)
    {
        float waited = 0f;
        while (waited < seconds)
        {
            if (!runic.IsPaused)
                waited += Time.deltaTime;
            yield return null;
        }
    }

    private System.Collections.IEnumerator EmissionRoutine(
        Skill skill, SkillEmission emission, float stepDuration, bool landing)
    {
        if (emission.ContinuousEmission)
        {
            // Emit at intervals throughout the step window.
            float interval = Mathf.Max(emission.MultiFireDelay, 0.05f);
            float elapsed = 0f;

            while (elapsed < stepDuration && (landing || usingSkill))
            {
                SpawnEmission(skill, emission, 0, 1, true);
                yield return WaitUnpaused(interval);
                elapsed += interval;
            }
            yield break;
        }

        int count = Mathf.Max(1, emission.Amount);
        for (int i = 0; i < count; i++)
        {
            if (!landing && !usingSkill) yield break;

            SpawnEmission(skill, emission, i, count, false);

            // A delay of 0 fires all projectiles at once (shotgun); > 0 fires them sequentially.
            if (emission.MultiFireDelay > 0f && i < count - 1)
                yield return WaitUnpaused(emission.MultiFireDelay);
        }
    }

    private void SpawnEmission(Skill skill, SkillEmission emission, int index, int count, bool continuous)
    {
        // Origin: the selected hitbox collider (mouth, claw, etc.) or the runic itself.
        Transform origin = transform;
        if (emission.Origin != SkillHitboxType.None && hitboxRegistry != null)
        {
            Collider originCol = hitboxRegistry.GetCollider(emission.Origin);
            if (originCol != null) origin = originCol.transform;
        }
        Vector3 position = origin.position;

        // Landing effects originate on the ground, not at the hitbox pivot.
        if (emission.Origin == SkillHitboxType.JumpImpactAOE)
            position = GetFeetPoint(position);

        // Direction: aim at the target when firing; without a target, use the runic's forward direction.
        Quaternion baseRotation = transform.rotation;
        Transform target = GetCurrentTargetTransform();
        if (target != null)
        {
            Collider tc = target.GetComponent<Collider>();
            Vector3 aimPoint = tc != null ? tc.bounds.center : target.position;
            Vector3 dir = aimPoint - position;
            if (dir.sqrMagnitude > 0.0001f)
                baseRotation = Quaternion.LookRotation(dir);
        }

        // Spread: spreadAngles is the TOTAL arc (x = pitch, y = yaw) distributed across projectiles.
        Vector3 angles = Vector3.zero;
        bool spreads = count > 1 || continuous;
        if (count > 1 && !continuous)
        {
            float t = index / (float)(count - 1) - 0.5f; // -0.5 .. +0.5
            angles = emission.SpreadAngles * t;
        }
        if (spreads && emission.SpreadRandomness > 0f)
        {
            float r = emission.SpreadRandomness; // In degrees.
            angles += new Vector3(Random.Range(-r, r), Random.Range(-r, r), 0f);
        }

        Quaternion rotation = emission.RotateToFaceTarget
            ? baseRotation * Quaternion.Euler(angles)
            : Quaternion.Euler(emission.CustomRotation) * emission.EmissionPrefab.transform.rotation;

        GameObject instance = Instantiate(emission.EmissionPrefab, position, rotation);

        float scaleMul = emission.EmissionScale > 0f ? emission.EmissionScale : 1f;
        instance.transform.localScale = emission.EmissionPrefab.transform.localScale * scaleMul;

        // Configure projectiles; VFX-only prefabs need no additional setup.
        ProjectileScript projectile = instance.GetComponent<ProjectileScript>();
        if (projectile != null)
        {
            projectile.Setup(
                runic,
                skill,
                emission.Speed,
                runic.GetKeyStatValue(skill.KeyStat),
                runic.TameState == RunicState.Tamed);
        }
    }

    private void ClearPendingLandingImpact()
    {
        hasPendingLandingImpact = false;
        pendingLandingImpactSkill = null;
    }

    private System.Collections.IEnumerator ResetLandingImpactAfterDelay()
    {
        float duration = Mathf.Max(landingImpactDuration, Time.fixedDeltaTime);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!runic.IsPaused)
                elapsed += Time.deltaTime;
            yield return null;
        }

        hitboxRegistry.ResetHitbox(SkillHitboxType.JumpImpactAOE);
        landingImpactResetCoroutine = null;
    }

    public void Flinch()
    {
        ClearPendingLandingImpact();
        StopAllEmissions();
        usingSkill = false;
        preparedSkill = null;
        skillDurationTimer = 0;

        if (activeSkillCoroutine != null)
        {
            StopCoroutine(activeSkillCoroutine);
            activeSkillCoroutine = null;
        }

        runicMovement.EndLeap();
    }

    private void UpdateSkillCooldowns()
    {
        List<Skill> keys = new List<Skill>(skillCooldowns.Keys);
        foreach (var skill in keys)
        {
            if (skillCooldowns[skill] > 0f)
            {
                skillCooldowns[skill] -= Time.deltaTime;
            }
        }
    }

    private Vector3 GetFeetPoint(Vector3 fallback)
    {
        if (bodyCollider == null) return fallback;
        Bounds b = bodyCollider.bounds;
        return new Vector3(b.center.x, b.min.y, b.center.z);
    }

    public void SetCombatStyle(CombatStyle style)
    {
        combatStyle = style;
    }

    public void InitializeCooldowns()
    {
        skillCooldowns.Clear();

        if (runic.BasicSkills != null)
        {
            foreach (var skill in runic.BasicSkills)
            {
                if (skill != null && !skillCooldowns.ContainsKey(skill))
                {
                    skillCooldowns.Add(skill, 0f);
                }
            }
        }

        if (runic.Skills != null)
        {
            foreach (var skill in runic.Skills)
            {
                if (skill != null && !skillCooldowns.ContainsKey(skill))
                {
                    skillCooldowns.Add(skill, 0f);
                }
            }
        }
    }
    
    public bool IsSkillReady(Skill skill)
    {
        if (skillCooldowns.TryGetValue(skill, out float cd))
        {
            return cd <= 0f;
        }
        return true; 
    }

    public bool IsSpecialSkillIndexReady(int index)
    {
        if (runic == null || runic.Skills == null || index < 0 || index >= runic.Skills.Count)
            return false;

        Skill skill = runic.Skills[index];
        return skill != null && IsSkillReady(skill);
    }

    public bool HasAnyBasicSkillReady()
    {
        if (runic.BasicSkills == null || runic.BasicSkills.Count == 0)
            return false;

        foreach (var skill in runic.BasicSkills)
        {
            if (skill != null && IsSkillReady(skill))
            {
                return true;
            }
        }
        return false;
    }

    public bool HasAnySpecialSkillReady()
    {
        if (runic.Skills == null || runic.Skills.Count == 0)
            return false;

        foreach (var skill in runic.Skills)
        {
            if (skill != null && IsSkillReady(skill))
            {
                return true;
            }
        }
        return false;
    }

    public bool HasAnySkillReady()
    {
        if ((runic.Skills == null && runic.BasicSkills == null) 
        || runic.Skills.Count + runic.BasicSkills.Count == 0)
            return false;

        List<Skill> allSkill = new List<Skill>();
        allSkill.AddRange(runic.Skills);
        allSkill.AddRange(runic.BasicSkills);

        foreach (var skill in allSkill)
        {
            if (skill != null && IsSkillReady(skill))
            {
                return true;
            }
        }
        return false;
    }

    public void PutSkillOnCooldown(Skill skill)
    {
        if (skillCooldowns.ContainsKey(skill))
        {
            skillCooldowns[skill] = skill.Cooldown;
        }
    }

    public float GetDistanceFromTarget()
    {
        Transform targetTransform = GetCurrentTargetTransform();
        if (targetTransform == null) return float.MaxValue;

        Collider targetCollider = targetTransform.GetComponent<Collider>();
        Collider myCollider = GetComponent<Collider>();

        if (targetCollider != null && myCollider != null)
        {
            Vector3 closestPointToTarget = targetCollider.ClosestPoint(transform.position);
            Vector3 myClosestPoint = myCollider.ClosestPoint(closestPointToTarget);

            return Vector3.Distance(myClosestPoint, closestPointToTarget);
        }

        return Vector3.Distance(transform.position, targetTransform.position);
    }

    private Transform GetCurrentTargetTransform()
    {
        if (playerTarget)
            return runic.Player != null ? runic.Player.transform : null;

        return currentTarget != null ? currentTarget.transform : null;
    }

    public void TryHunting(Runic targetPrey)
    {
        if (!hunting && targetPrey != null)
        {
            hunting = true;
            playerTarget = false;
            currentTarget = targetPrey;
        }
        runic.HealthBar.UpdateBarColor();
    }

    public void GiveUpHunt()
    {
        hunting = false;
        playerTarget = false;
        currentTarget = null;
        runic.HealthBar.UpdateBarColor();
    }

    public void WildCombat()
    {
        if (skillDurationTimer <= 0)
            usingSkill = false;

        Transform targetTransform = GetCurrentTargetTransform();
        if (targetTransform == null) return;

        if (preparedSkill != null)
        {
            float targetDistance = GetDistanceFromTarget();

            if (preparedSkill.Range >= targetDistance)
            {
                Skill skillToExecute = preparedSkill;
                preparedSkill = null; 
                
                runicMovement.MoveToPosition(transform.position); 
                UseSkill(skillToExecute);
            }
            else
            {
                Collider targetCollider = targetTransform.GetComponent<Collider>();
                Vector3 rawDestination = targetCollider != null 
                    ? targetCollider.ClosestPoint(transform.position) : targetTransform.position;

                // Keep the destination on the NavMesh to avoid pathing failures.
                if (UnityEngine.AI.NavMesh.SamplePosition(rawDestination, 
                    out UnityEngine.AI.NavMeshHit hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                    if (runicMovement != null)
                        runicMovement.MoveToPosition(hit.position);
            }
        } 
        else if (recoveryTimer <= 0 && !usingSkill && HasAnySkillReady())
        {
            if (Random.value <= runic.Aggression)
            {
                float targetDistance = GetDistanceFromTarget();
                List<Skill> allSkills = new List<Skill>();
                allSkills.AddRange(runic.BasicSkills);
                allSkills.AddRange(runic.Skills);
                Skill chosenSkill = ChooseWeightedSkill(allSkills, targetDistance);

                if (chosenSkill != null)
                {
                    if (chosenSkill.Range >= targetDistance)
                    {
                        UseSkill(chosenSkill);
                    }
                    else
                    {
                        Debug.Log(runic + " repositioning to use " + chosenSkill);
                        runicMovement.SetMovementSpeed(runic.SprintSpeed);
                        ReadySkill(chosenSkill);
                    }
                }
            }
        }

        if (preparedSkill == null && movementTimer <= 0 && !usingSkill)
        {
            runicMovement.HandleCombatMovement(targetTransform, combatStyle);
        }

        runicMovement.FaceTarget(targetTransform.position);
    }

    public void Hunt()
    {
        if (skillDurationTimer <= 0)
            usingSkill = false;

        if (currentTarget == null)
            return;

        if (preparedSkill != null && currentTarget != null)
        {
            float targetDistance = GetDistanceFromTarget();

            if (preparedSkill.Range >= targetDistance)
            {
                Skill skillToExecute = preparedSkill;
                preparedSkill = null; 
                
                runicMovement.MoveToPosition(transform.position); 
                UseSkill(skillToExecute);
            }
            else
            {
                Collider targetCollider = currentTarget.GetComponent<Collider>();
                Vector3 rawDestination = targetCollider != null 
                    ? targetCollider.ClosestPoint(transform.position) : currentTarget.transform.position;

                // Keep the destination on the NavMesh to avoid pathing failures.
                if (UnityEngine.AI.NavMesh.SamplePosition(rawDestination, 
                    out UnityEngine.AI.NavMeshHit hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                    if (runicMovement != null)
                        runicMovement.MoveToPosition(hit.position);
            }
        }
        else if (recoveryTimer <= 0 && !usingSkill)
        {
            if (Random.value <= runic.Aggression && HasAnySkillReadyInPool(runic.BasicSkills))
            {
                float targetDistance = GetDistanceFromTarget();
                Skill chosenSkill = ChooseWeightedSkill(runic.BasicSkills, targetDistance);

                if (chosenSkill != null)
                {
                    if (chosenSkill.Range >= targetDistance)
                    {
                        UseSkill(chosenSkill);
                    }
                    else
                    {
                        Debug.Log(runic + " repositioning to use " + chosenSkill);
                        runicMovement.SetMovementSpeed(runic.SprintSpeed);
                        ReadySkill(chosenSkill);
                    }
                }
            }
        }

        if (preparedSkill == null && movementTimer <= 0 && currentTarget != null && !usingSkill)
        {
            runicMovement.Chase(currentTarget.transform);
            Debug.Log(runic + " chasing " + currentTarget);
        }

        runicMovement.FaceTarget(currentTarget.transform.position);
    }

    public bool IsBusy()
    {
        return usingSkill || runicMovement.Leaping;
    }

    public void QueueUpSkill(Skill skill)
    {
        queuedUpSkill = skill;
    }

    void UpdateTimers()
    {
        updateTimer += Time.deltaTime;
        
        if (recoveryTimer > 0)
            recoveryTimer -= Time.deltaTime;
        
        if (skillDurationTimer > 0)
            skillDurationTimer -= Time.deltaTime;

        if (movementTimer > 0)
            movementTimer -= Time.deltaTime;
            
        if (preparedSkill)
        {
            giveUpTimer += Time.deltaTime;
        }
    }

    public void SetMovementTimer(float time){
        movementTimer = time;
    }

    public void SetTarget(Runic target){
        currentTarget = target;
        playerTarget = false;
        runic.HealthBar.UpdateBarColor();
    }

    public void SetPlayerTarget(){
        currentTarget = null;
        playerTarget = true;
        runic.HealthBar.UpdateBarColor();
    }

    public void ResetSkillCooldowns()
    {
        List<Skill> keys = new List<Skill>(skillCooldowns.Keys);
        foreach (var skill in keys)
        {
            skillCooldowns[skill] = 0;
        }
    }
}