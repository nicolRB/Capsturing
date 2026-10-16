using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using System.Collections.Generic;

public class RunicMovement : MonoBehaviour
{
    private Runic runic;
    private RunicCombat runicCombat;
    private Quaternion lastMovingRotation;
    private Vector3 faceTargetPosition;
    private bool hasFaceTarget;

    [Header("Follow Behavior")]
    [SerializeField] private float followerOffset = 2f;
    [Tooltip("Follow position mode: 0 = behind, 1 = side, 2 = ahead, 3 = any nearby position.")]
    [SerializeField] private int followMode = 1;
    [SerializeField] private int followSide = 1;
    [SerializeField] private float followRange = 0.75f;
    [SerializeField] private float unboundFollowDistance = 2.5f;
    [Tooltip("Distance kept from the player in follow mode 3")]
    [SerializeField] private float maxProximity = 0.7f;
    [SerializeField] private float stopRange = 0.5f;
    [SerializeField] private float leadDistance = 4.5f;
    
    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.2f; // Updates five times per second
    [SerializeField] private float faceTurnSpeed = 540f;
    private float updateTimer = 0f;

    [Header("Teleport Settings")]
    [SerializeField] private float timeToTeleport = 5f;
    private float teleportTimer = 0f;
    [SerializeField] private float teleportDistance = 10f;
    private bool teleportTimerStarted = false;

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float chaseSideOffset = 2f;
    private Transform chaseTarget;
    private int chaseSide;
    private bool leapInProgress;
    private bool leapEndRequested;
    private bool leapHasAscended;
    private bool leapRequiresAscent;
    private bool wasKinematicBeforeLeap;
    private bool agentWasEnabledBeforeLeap;
    private float leapStartedAt;
    private readonly HashSet<Collider> leapGroundContacts = new HashSet<Collider>();
    private Rigidbody rb;
    private bool charging;
    private float prevSpeed, prevAcceleration, prevStoppingDistance;
    private bool prevAutoBraking;
    private Transform dashTarget;
    private float dashGap;
    private bool dashing;

    private float speed = 5f;
    private float walkSpeed = 2.5f;
    private float sprintSpeed = 9f;

    public bool Leaping => leapInProgress;
    public bool Charging => charging;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        runic = GetComponent<Runic>();
        runicCombat = GetComponent<RunicCombat>();
        agent = GetComponent<NavMeshAgent>();

        speed = runic.Speed;
        walkSpeed = runic.WalkSpeed;
        sprintSpeed = runic.SprintSpeed;

        if (runic.Player != null)
            lastMovingRotation = runic.Player.transform.rotation;
    }

    void Update()
    {
        if (charging && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (dashing)
            {
                if (dashTarget == null) EndCharge();   // alvo destruído
                else agent.SetDestination(ComputeDashGoal());
            }

            if (charging && !agent.pathPending && agent.remainingDistance <= 0.1f)
                EndCharge();
        }

        UpdateFacing();

        if (Keyboard.current.qKey.wasPressedThisFrame)
            followSide = -followSide;
        
        bool interactableActive = runic.PlayerInteraction != null && runic.PlayerInteraction.InteractableTarget != null;

        bool isNotCastingOrChanneling = runic.Player.CastingState != PlayerController.CastState.Channeling 
                                && runic.Player.CastingState != PlayerController.CastState.Casting;

        if (!interactableActive && Keyboard.current.fKey.wasPressedThisFrame 
            && !runic.Pointer.FollowPoint && isNotCastingOrChanneling)
        {
            followMode = (followMode + 1) % 4;
        }

        updateTimer += Time.deltaTime;

        if (runic.TameState == RunicState.Tamed) 
        {
            // Only does logic in regular intervals
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                if (runic.ActState == BehaviorState.Following || runic.ActState == BehaviorState.CommandedMove)
                    TameMovement();
            }
        }
    }

    void FixedUpdate()
    {
        if (leapInProgress && GetComponent<Rigidbody>().linearVelocity.y > 0.1f)
            leapHasAscended = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        TrackLeapGroundContact(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        TrackLeapGroundContact(collision);
    }

    void OnCollisionExit(Collision collision)
    {
        leapGroundContacts.Remove(collision.collider);
    }

    private void TrackLeapGroundContact(Collision collision)
    {
        if (!leapInProgress || (leapRequiresAscent && !leapHasAscended))
            return;

        if (rb == null || rb.linearVelocity.y > 0.1f)
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                leapGroundContacts.Add(collision.collider);
                TryCompleteLeap();
                return;
            }
        }
    }

    public void TameMovement()
    {
        if (charging) return;
        agent.updateRotation = true;
        hasFaceTarget = false;
        Vector3 targetPosition;
        Vector3 right = lastMovingRotation * Vector3.right;
        Vector3 forward = lastMovingRotation * Vector3.forward;

        if (runic.ActState == BehaviorState.CommandedMove)
        {
            SetMovementSpeed(speed);
            HandleCommandedMovement(runic.Pointer.GroundIndicator.transform.position);
            return;
        }

        if (runic.Player.IsTryingToWalk)
        {
            SetMovementSpeed(Mathf.Min(walkSpeed, runic.Player.WalkSpeed));
        }
        else if (runic.Player.IsTryingToSprint)
        {
            SetMovementSpeed(Mathf.Min(sprintSpeed, runic.Player.SprintSpeed));
        }
        else 
            SetMovementSpeed(Mathf.Min(speed, runic.Player.JogSpeed));

        if (!runic.Pointer.FollowPoint)
        {
            switch (followMode)
            {
                case 0: // Behind
                    targetPosition = runic.Player.transform.position - forward * followerOffset;
                    break;
                case 1: // To the side
                    targetPosition =runic.Player.transform.position + right * followSide * followerOffset;
                    break;
                case 2: // Ahead
                    targetPosition =runic.Player.transform.position + (forward * leadDistance) + (right * followSide * followerOffset);
                    break;
                case 3: // Nearby in any direction
                    Vector3 toRunic = transform.position - runic.Player.transform.position;
                    toRunic.y = 0; // remains on the horizontal plane
                    if (toRunic.sqrMagnitude < 0.01f)
                    {
                        toRunic = -runic.Player.transform.forward; // avoids dividing by zero
                    }
                    targetPosition = runic.Player.transform.position + toRunic.normalized * unboundFollowDistance;
                    break;
                default:
                    targetPosition =runic.Player.transform.position;
                    break;
            }            
        }
        else 
        targetPosition = runic.Pointer.IndicatedPosition;

        updateTimer = 0; // resets the frame counter

        float targetDistance = CalculateTargetDistance(targetPosition);
        float playerDistance = CalculateTargetDistance(runic.Player.transform.position);

        if (runic.ActState == BehaviorState.Following && targetDistance > followRange)
            TrySetDestination(targetPosition);

        if (followMode == 3 && runic.ActState == BehaviorState.Following && playerDistance < followRange) agent.stoppingDistance = maxProximity;
        else agent.stoppingDistance = stopRange;

        if (playerDistance > teleportDistance && runic.ActState == BehaviorState.Following)
        {
            if (!teleportTimerStarted)
            {
                teleportTimerStarted = true;
                teleportTimer = Time.time;
            }

            float elapsed = Time.time - teleportTimer;

            if (elapsed >= timeToTeleport && playerDistance > teleportDistance)
            {
                TeleportToTarget(runic.Player.transform.position - forward * 5f);
            }
        } 
        else
            teleportTimerStarted = false;
        
        // updates the reference rotation only when the player is moving
        if (runic.Player.Moving)
            lastMovingRotation = runic.Player.transform.rotation;
    }

    private float CalculateTargetDistance(Vector3 targetPosition)
    {
        float targetDistance = Vector3.Distance(transform.position, targetPosition);

        return targetDistance;
    }

    private void TeleportToTarget(Vector3 targetPosition)
    {
        transform.position = targetPosition;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    private bool TrySetDestination(Vector3 targetPosition, float sampleRadius = 1.5f)
    {
        if (charging || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        float searchRadius = Mathf.Max(sampleRadius, 0.01f);
        if (!NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, searchRadius, agent.areaMask))
        {
            agent.ResetPath();
            return false;
        }

        if (agent.SetDestination(hit.position))
            return true;

        agent.ResetPath();
        return false;
    }

    public void MoveToPosition(Vector3 targetPosition)
    {
        TrySetDestination(targetPosition);
    }

    public bool Leap(Vector3 targetPosition, float force)
    {
        // Searches for the point in NavMesh closest to the intended target.
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 3.0f, agent.areaMask))
        {
            targetPosition = hit.position; // Ensures that the landing is on a valid NavMesh server.
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (!BeginPhysicsMotion(rb, true))
            return false;

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;

        rb.linearVelocity = ComputeLeapVelocity(targetPosition, force * 0.8f);
        return true;
    }

    public bool HighLeap(Vector3 targetPosition, float force)
    {
        // Searches for the point in NavMesh closest to the intended target.
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 3.0f, agent.areaMask))
        {
            targetPosition = hit.position; // Ensures that the landing is on a valid NavMesh server.
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (!BeginPhysicsMotion(rb, true))
            return false;

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;

        Vector3 jumpVelocity = (direction * force) + (Vector3.up * (force * 2f));
        rb.linearVelocity = ComputeLeapVelocity(targetPosition, force * 2f);
        return true;
    }

    private Vector3 ComputeLeapVelocity(Vector3 targetPosition, float verticalSpeed)
    {
        Vector3 flat = targetPosition - transform.position;
        float dy = flat.y;          // height difference to the target
        flat.y = 0f;

        float g = Mathf.Abs(Physics.gravity.y);

        // Flight time until reaching the target altitude: vy*t - g*t²/2 = dy
        float disc = Mathf.Max(0f, verticalSpeed * verticalSpeed - 2f * g * dy);
        float flightTime = (verticalSpeed + Mathf.Sqrt(disc)) / g;

        Vector3 v = flat / flightTime;   // exact horizontal speed
        v.y = verticalSpeed;
        return v;
    }

    public void ApplyKnockback(Vector3 impulse)
    {
        if (rb == null || impulse.sqrMagnitude <= 0f)
            return;

        if (!leapInProgress)
        {
            if (!BeginPhysicsMotion(rb, false))
                return;

            leapEndRequested = true;
            leapHasAscended = true;
        }

        if (impulse.y > 0f && rb.linearVelocity.y < 0f)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
        }

        rb.AddForce(impulse, ForceMode.Impulse);
    }

    private bool BeginPhysicsMotion(Rigidbody rb, bool requiresAscent)
    {
        if (rb == null || leapInProgress)
            return false;

        EndCharge();

        wasKinematicBeforeLeap = rb.isKinematic;
        agentWasEnabledBeforeLeap = agent != null && agent.enabled;

        if (agentWasEnabledBeforeLeap)
        {
            agent.ResetPath();
            agent.enabled = false;
        }

        leapInProgress = true;
        leapEndRequested = false;
        leapHasAscended = false;
        leapRequiresAscent = requiresAscent;
        leapStartedAt = Time.time;
        leapGroundContacts.Clear();
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        return true;
    }

    // Re-enable the NavMeshAgent after landing.
    public void EndLeap()
    {
        if (!leapInProgress)
            return;

        leapEndRequested = true;
        TryCompleteLeap();
    }

    private void TryCompleteLeap()
    {
        if (!leapInProgress || !leapEndRequested || (leapRequiresAscent && !leapHasAscended)
            || leapGroundContacts.Count == 0 || Time.time - leapStartedAt < 0.08f)
            return;

        if (runicCombat != null)
        {
            runicCombat.TriggerLeapLandingImpact();
        }

        // Try to find NavMesh below or near where it landed.
        NavMeshHit landingHit;
        bool hasNavMeshLanding = NavMesh.SamplePosition(
            transform.position, 
            out landingHit, 
            1.5f, // Search radius
            agent != null ? agent.areaMask : NavMesh.AllAreas
        );

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = wasKinematicBeforeLeap;
        }

        if (hasNavMeshLanding)
        {
            if (agent != null && agentWasEnabledBeforeLeap)
            {
                agent.enabled = true;
                agent.Warp(landingHit.position);
            }
        }
        else
        {
            // Rune is stuck. Teleport to a safe location.
            RecoverStuckRunic();
        }

        leapInProgress = false;
        leapEndRequested = false;
        leapHasAscended = false;
        leapRequiresAscent = false;
        leapGroundContacts.Clear();
    }

    private void RecoverStuckRunic()
    {
        Debug.LogWarning($"{gameObject.name} ficou preso fora da NavMesh! Resgatando...", this);

        Vector3 safePosition = transform.position;

        // If there is a player, rescue it near the player.
        if (runic != null && runic.Player != null)
        {
            safePosition = runic.Player.transform.position;
        }

        // Validates the secure position in NavMesh.
        if (NavMesh.SamplePosition(safePosition, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            safePosition = hit.position;
        }

        transform.position = safePosition;

        if (agent != null && agentWasEnabledBeforeLeap)
        {
            agent.enabled = true;
            agent.Warp(safePosition);
        }
    }

    public void HandleCombatMovement(Transform targetTransform, CombatStyle style)
    {
        SetMovementSpeed(sprintSpeed);

        agent.updateRotation = false;
        if (targetTransform == null) return;

        Vector3 currentPos = transform.position;
        Vector3 targetPos = targetTransform.position;
        Vector3 directionToTarget = targetPos - currentPos;
            directionToTarget.y = 0;
        float dist = directionToTarget.magnitude;

        Vector3 destination = currentPos;

        switch (style)
        {
            case CombatStyle.FaceOn:
                // Maintain a short distance (e.g., between 1.5 and 2.5 units).
                float idealFaceOnDist = 3.5f;
                if (dist < idealFaceOnDist - 0.5f)
                {
                    // Backs off a little if too close.
                    destination = targetPos - (directionToTarget.normalized * idealFaceOnDist);
                }
                else if (dist > idealFaceOnDist + 0.75f)
                {
                    // Moves closer if it's outside the ideal distance.
                    destination = targetPos;
                }
                else
                {
                    SetMovementSpeed(walkSpeed);

                    Vector3 awayFromTarget = -directionToTarget;
                    if (awayFromTarget.sqrMagnitude < 0.01f)
                        awayFromTarget = transform.forward;

                    float currentAngle = Mathf.Atan2(awayFromTarget.x, awayFromTarget.z) * Mathf.Rad2Deg;
                    float angleOffset = Random.Range(-30f, 30f);
                    float newAngle = currentAngle + angleOffset;

                    float randomFaceOnDist = Random.Range(idealFaceOnDist - 0.5f, 
                        idealFaceOnDist + 0.5f);
                    
                    destination = targetPos + Quaternion.Euler(0f, newAngle, 0f) 
                        * Vector3.forward * randomFaceOnDist;
                }

                runicCombat.SetMovementTimer(0.7f);

                break;

            case CombatStyle.Skittish:
                // Maintain a greater distance (e.g., between 4 and 7 units) and perform small, random bursts.
                float minSkittishTargetDist = 4f;
                float maxSkittishTargetDist = 7f;
                float minSkitterDistance = 2.5f;
                float MaxSkitterDistance = 4f;

                float randomSkittishDist = Random.Range(minSkittishTargetDist, maxSkittishTargetDist);

                if (dist < minSkittishTargetDist)
                {
                    destination = targetPos - (directionToTarget.normalized * randomSkittishDist);
                }
                else if (dist > maxSkittishTargetDist)
                {
                    destination = targetPos;
                }
                else
                {
                    SetMovementSpeed(sprintSpeed);

                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        NavMeshPath path = new NavMeshPath();
                        for (int attempt = 0; attempt < 12; attempt++)
                        {
                            float randomAngle = Random.Range(0f, 360f);
                            float randSkitterDist = Random.Range(minSkitterDistance, MaxSkitterDistance);
                            Vector3 offset = Quaternion.Euler(0f, randomAngle, 0f)
                                * Vector3.forward * randSkitterDist;
                            Vector3 candidate = currentPos + offset;

                            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, agent.areaMask))
                                continue;

                            float sampledDistance = Vector3.Distance(hit.position, targetPos);
                            if (sampledDistance < minSkittishTargetDist || sampledDistance > maxSkittishTargetDist)
                                continue;

                            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                            {
                                destination = hit.position;
                                break;
                            }
                        }
                    }
                }
                
                runicCombat.SetMovementTimer(1.25f);

                break;
        }

        TrySetDestination(destination, 2f);
    }

    public void FaceTarget(Vector3 targetPosition)
    {
        if (charging) return;

        Vector3 lookDirection = targetPosition - transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude < 0.001f)
            return;

        faceTargetPosition = targetPosition;
        hasFaceTarget = true;
        if (agent != null)
            agent.updateRotation = false;
    }

    private void UpdateFacing()
    {
        if (!hasFaceTarget)
            return;

        Vector3 lookDirection = faceTargetPosition - transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            faceTurnSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            hasFaceTarget = false;
    }

    public void StartCharge(float chargeSpeed, float maxDistance)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || leapInProgress)
            return;

        if (charging) EndCharge();

        Debug.Log("Beginning charge");

        Vector3 dir = transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        dir.Normalize();

        // Limit the destination to the edge of the NavMesh, so the thrust doesn't fail at SamplePosition.
        Vector3 goal = transform.position + dir * maxDistance;
        if (NavMesh.Raycast(transform.position, goal, out NavMeshHit edge, agent.areaMask))
            goal = edge.position;

        prevSpeed = agent.speed;
        prevAcceleration = agent.acceleration;
        prevAutoBraking = agent.autoBraking;
        prevStoppingDistance = agent.stoppingDistance;

        agent.speed = chargeSpeed;
        agent.acceleration = chargeSpeed * 10f;   // reaches speed almost instantly.
        agent.autoBraking = false;
        agent.stoppingDistance = 0f;
        agent.updateRotation = false;             // locks the steering during the charge.
        hasFaceTarget = false;

        charging = true;
        agent.SetDestination(goal);
    }

    public void EndCharge()
    {
        if (!charging) return;

        charging = false;
        dashing = false;
        dashTarget = null;

        if (agent == null) return;

        Debug.Log("Ending charge");

        agent.speed = prevSpeed;
        agent.acceleration = prevAcceleration;
        agent.autoBraking = prevAutoBraking;
        agent.stoppingDistance = prevStoppingDistance;

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    private bool BeginBurst(Vector3 goal, float burstSpeed)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || leapInProgress)
            return false;

        if (charging) EndCharge();

        if (NavMesh.SamplePosition(goal, out NavMeshHit snapped, 2f, agent.areaMask))
            goal = snapped.position;

        prevSpeed = agent.speed;
        prevAcceleration = agent.acceleration;
        prevAutoBraking = agent.autoBraking;
        prevStoppingDistance = agent.stoppingDistance;

        agent.speed = burstSpeed;
        agent.acceleration = burstSpeed * 10f;
        agent.autoBraking = false;
        agent.stoppingDistance = 0f;
        agent.updateRotation = false;
        hasFaceTarget = false;

        charging = true;
        agent.SetDestination(goal);
        return true;
    }

    public void StartDashToTarget(Transform target, float dashSpeed, float gap)
    {
        if (target == null || agent == null) return;

        dashTarget = target;
        dashGap = gap;

        // Already close enough; no dash is needed.
        Vector3 surface = GetDashSurfacePoint();
        Vector3 flat = surface - transform.position;
        flat.y = 0f;
        if (flat.magnitude <= agent.radius + dashGap)
        {
            dashTarget = null;
            return;
        }

        dashing = BeginBurst(ComputeDashGoal(), dashSpeed);
        if (!dashing) dashTarget = null;
    }

    private Vector3 GetDashSurfacePoint()
    {
        Collider col = dashTarget.GetComponent<Collider>();
        return col != null ? col.ClosestPoint(transform.position) : dashTarget.position;
    }

    // Point on the target's side facing the Runic, offset from its surface.
    private Vector3 ComputeDashGoal()
    {
        Vector3 surface = GetDashSurfacePoint();
        Vector3 back = transform.position - surface;
        back.y = 0f;
        if (back.sqrMagnitude < 0.001f) back = -transform.forward;
        back.Normalize();

        Vector3 goal = surface + back * (agent.radius + dashGap);
        goal.y = transform.position.y;
        return goal;
    }

    public void HandleCommandedMovement(Vector3 PointedPos)
    {
        hasFaceTarget = false;
        if (agent != null)
            agent.updateRotation = true;

        switch (runic.CommandedMovementMode)
        {
            case CommandedMoveMode.Stand:
                TrySetDestination(PointedPos);
                break;
            
            case CommandedMoveMode.Scout:
                break;

            case CommandedMoveMode.Patrol:
                break;

            default:
                break;
        }        
    }

    public void Wander(float maxWanderDistance = 10)
    {
        hasFaceTarget = false;
        if (agent != null)
            agent.updateRotation = true;

        SetMovementSpeed(walkSpeed);

        float randomAngle = Random.Range(0, 360);
        float randomDistance = Random.Range(1f, maxWanderDistance);

        Vector3 offset = Quaternion.Euler(0f, randomAngle, 0f)
            * Vector3.forward * randomDistance;
        Vector3 wanderPosition = transform.position + offset;

        TrySetDestination(wanderPosition, maxWanderDistance);
    }

    public void Patrol(Vector3 territoryPos, float territoryRadius, float territoryThreatRadius = 0)
    {
        hasFaceTarget = false;
        if (agent != null)
            agent.updateRotation = true;

        SetMovementSpeed(walkSpeed);

        float randomAngle = Random.Range(0f, 360f);
        float randomDistance = Random.Range(0f, territoryRadius + territoryThreatRadius);

        Vector3 offset = Quaternion.Euler(0f, randomAngle, 0f)
            * Vector3.forward * randomDistance;
        Vector3 patrolPosition = territoryPos + offset;

        TrySetDestination(patrolPosition, territoryRadius);
    }

    public void Flee(Vector3 threatPosition, float fleeDistance = 15f)
    {
        hasFaceTarget = false;
        if (agent != null)
            agent.updateRotation = true;

        SetMovementSpeed(sprintSpeed);
        
        Vector3 awayDir = transform.position - threatPosition;
        awayDir.y = 0f;
        awayDir.Normalize();

        if (awayDir.sqrMagnitude < 0.001f)
        {
            awayDir = transform.forward;
            awayDir.y = 0f;

            if (awayDir.sqrMagnitude < 0.001f)
                awayDir = Vector3.forward;
            else
                awayDir.Normalize();
        }

        // Adds a random variation of up to 45 degrees to the left or right.
        float randomAngle = Random.Range(-45f, 45f);
        Vector3 deviatedDir = Quaternion.Euler(0f, randomAngle, 0f) * awayDir;

        Vector3 targetPosition = transform.position + (deviatedDir * fleeDistance);

        TrySetDestination(targetPosition, fleeDistance);
    }

    public void Chase(Transform targetTransform)
    {
        if (targetTransform == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        hasFaceTarget = false;
        agent.updateRotation = true;
        SetMovementSpeed(sprintSpeed);

        Vector3 targetPos = targetTransform.position;
        float targetDist = Vector3.Distance(targetPos, transform.position);
        if (targetDist >= 3f)
        {
            chaseTarget = null;
            chaseSide = 0;
            
            TrySetDestination(targetPos);
        }
        else 
        {
            if (chaseTarget != targetTransform || chaseSide == 0)
            {
                chaseTarget = targetTransform;
                chaseSide = Random.Range(0, 2) == 0 ? -1 : 1;
            }

            Vector3 targetRight = targetTransform.right;
            targetRight.y = 0f;
            if (targetRight.sqrMagnitude < 0.001f)
            {
                Vector3 directionToTarget = targetPos - transform.position;
                directionToTarget.y = 0f;
                targetRight = Vector3.Cross(Vector3.up, directionToTarget);
            }

            Vector3 flankPosition = targetPos + targetRight.normalized * chaseSide * chaseSideOffset;
            if (!TrySetDestination(flankPosition))
                TrySetDestination(targetPos);
        }
    }

    public void MoveToCuriosity(Vector3 curiosityPos)
    {
        Vector3 toRunic = transform.position - curiosityPos;
        toRunic.y = 0; // remains on the horizontal plane
        if (toRunic.sqrMagnitude < 0.01f)
        {
            toRunic = -runic.Player.transform.forward; // avoids dividing by zero
        }
        Vector3 targetPosition = curiosityPos + toRunic.normalized * unboundFollowDistance;
        MoveToPosition(targetPosition);
    }

    public void Dodge(Vector3 DodgeImpulse)
    {
        ApplyKnockback(DodgeImpulse);
        runic.Dodge();
    }

    public void SetMovementSpeed(float speed)
    {
        if (charging) return;
        if (agent != null)
            agent.speed = speed;
    }
}
