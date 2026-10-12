using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RunicCombat : MonoBehaviour
{
    private Runic runic;
    private RunicMovement runicMovement;
    private CombatStyle combatStyle;
    private RunicHitboxRegistry hitboxRegistry;

    private Runic currentTarget;
    private bool playerTarget;

    private float recoveryTimer;
    private float specialRecoveryTimer;
    private float movementTimer;

    [SerializeField] private float updateInterval = 0.2f;
    private float updateTimer;

    private float skillDurationTimer;
    private Skill preparedSkill;
    private bool usingSkill;
    private Dictionary<Skill, float> skillCooldowns = new Dictionary<Skill, float>();

    private bool hunting;

    void Start()
    {
        InitializeCooldowns();
    }

    private void Awake()
    {
        runic = GetComponent<Runic>();
        runicMovement = GetComponent<RunicMovement>();
        hitboxRegistry = GetComponent<RunicHitboxRegistry>();
    }

    void Update()
    {
        UpdateTimers();
        UpdateSkillCooldowns(); // Updates cooldowns every frame

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
            Debug.Log(runic + " repositioning");
        }

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
    }

    private Coroutine activeSkillCoroutine;

    public void UseSkill(Skill skill)
    {
        Debug.Log(runic + " using Skill: " + skill.SkillName);

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
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 2. Trigger the step start (single trigger, re-aim, initial impulse, etc.)
            ExecuteStepStart(skill, currentStep);

            // 3. Determine when this step ends (next step delay or the total skill duration)
            float nextStepDelay = (i < sortedSteps.Length - 1) ? sortedSteps[i + 1].Delay : skill.Duration;

            // 4. Keep the step active/executing during the time window between this delay and the next one
            while (elapsedTime < nextStepDelay)
            {
                if (!usingSkill)
                {
                    ExecuteStepEnd(currentStep);
                    yield break;
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
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        usingSkill = false;
    }

    private void ExecuteStepStart(Skill skill,SkillStep step)
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
            case SkillMovementType.DashForward:
                if (runicMovement != null)
                {
                    Vector3 dashTarget = transform.position + transform.forward * step.Force;
                    runicMovement.MoveToPosition(dashTarget);
                }
                break;

            case SkillMovementType.LeapTowardsTarget:
                if (targetTransform != null && runicMovement != null)
                {
                    Collider targetCol = targetTransform.GetComponent<Collider>();
                    Vector3 leapTarget = targetCol != null ? targetCol.ClosestPoint(transform.position) : targetTransform.position;
                    
                    // Launch the leap using Rigidbody velocity.
                    runicMovement.Leap(leapTarget, step.Force);
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
        }

        // Hitbox and damage

        if (step.HitboxType != SkillHitboxType.None)
        {
            float totalPower = GetKeyStatValue(skill.KeyStat) * (skill.Power / 100f);
            hitboxRegistry.ActivateHitbox(step.HitboxType, step.HitboxSize, totalPower, skill.KnockBackValue, step.Continuous);
        }
    }

    private void ExecuteStepUpdate(SkillStep step)
    {
        // If any step needs frame-by-frame update logic while active
        // (e.g., keep the runic tracking the target during a continuous charge)
    }

    private void ExecuteStepEnd(SkillStep step)
    {
        if (step.MovementType == SkillMovementType.LeapTowardsTarget && runicMovement != null)
            runicMovement.EndLeap();

        hitboxRegistry.ResetAllHitboxes();
    }

    public void Flinch()
    {
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
    }

    public void GiveUpHunt()
    {
        hunting = false;
        playerTarget = false;
        currentTarget = null;
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
            Debug.Log(runic + " repositioning");
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

    void UpdateTimers()
    {
        updateTimer += Time.deltaTime;
        
        if (recoveryTimer > 0)
            recoveryTimer -= Time.deltaTime;
        
        if (skillDurationTimer > 0)
            skillDurationTimer -= Time.deltaTime;

        if (movementTimer > 0)
            movementTimer -= Time.deltaTime;
    }

    private float GetKeyStatValue(SkillKeyStat keyStat)
    {
        switch (keyStat)
        {
            case SkillKeyStat.Attack: return runic.Attack;
            case SkillKeyStat.Magic: return runic.Magic;
            case SkillKeyStat.Defense: return runic.Defense;
            case SkillKeyStat.MagicDefense: return runic.MagicDefense;
            case SkillKeyStat.Hp: return runic.CurrentHP;
            case SkillKeyStat.Speed: return runic.Speed;
            case SkillKeyStat.None: return 100f;
            default: return 0f;
        }
    }

    public void SetMovementTimer(float time){
        movementTimer = time;
    }

    public void SetTarget(Runic target){
        currentTarget = target;
        playerTarget = false;
    }

    public void SetPlayerTarget(){
        currentTarget = null;
        playerTarget = true;
    }
}